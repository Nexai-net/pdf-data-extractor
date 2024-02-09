// See https://aka.ms/new-console-template for more information

using CommandLine;
using CommandLine.Text;

using Data.Block.Abstractions;

using Microsoft.Extensions.Logging;

using PDF.Data.Extractor;
using PDF.Data.Extractor.Console;
using PDF.Data.Extractor.Console.Models;

using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

var parser = new Parser(s =>
{
    s.AutoHelp = true;
    s.AutoVersion = true;
    s.GetoptMode = true;
});

var commandLine = parser.ParseArguments<ExtractCommandLineArgument>(args);

if (commandLine.Tag == ParserResultType.NotParsed)
{
    var usage = HelpText.AutoBuild(commandLine);

    if (!string.IsNullOrEmpty(usage))
        Console.WriteLine(usage);

    Environment.ExitCode = -1;
    return;
}

var files = new List<string>();

var cmd = commandLine.Value!;

if (!string.IsNullOrEmpty(cmd.Source))
    files.Add(cmd.Source);

if (!string.IsNullOrEmpty(cmd.SourceDir))
{
    var dirFiles = Directory.GetFiles(cmd.SourceDir,
                                      "*.pdf",
                                      cmd.SourceDirRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

    files.AddRange(dirFiles);
}

var consoleLoggerFactory = LoggerFactory.Create(b => b.AddConsole());

if (commandLine.Value.Timed)
    Console.WriteLine("Document analyse start : " + DateTime.Now);

var timer = new Stopwatch();
timer.Start();

var option = new PDFExtractorOptions()
{
    PageRange = Range.StartAt(0),
    InjectImageMetaData = commandLine.Value.IncludeImages,
    Asynchronous = !commandLine.Value.PreventParallelProcess,
};

var current = new Uri(Directory.GetCurrentDirectory() + "/", UriKind.Absolute);
var cmdOutput = new Uri((commandLine.Value.Output ?? ".") + "/", UriKind.RelativeOrAbsolute);

var output = cmdOutput.IsAbsoluteUri ? cmdOutput : new Uri(current, cmdOutput.OriginalString ?? ".");

var outputDirName = Path.GetFileNameWithoutExtension(commandLine.Value.Source);
if (!string.IsNullOrEmpty(commandLine.Value.OutputFolderName))
    outputDirName = commandLine.Value.OutputFolderName;

var finalDir = new Uri(Path.Combine(output.LocalPath, outputDirName!));
if (!Directory.Exists(finalDir.LocalPath))
{
    Directory.CreateDirectory(finalDir.LocalPath);
}

long fileCounter = 0;
var limitator = new SemaphoreSlim(Math.Max(4, Environment.ProcessorCount * 2));

var processTasks = files.Select((file, indx) =>
{
    return Task.Run(async () =>
    {
        DocumentExtractorKPI? kpi = null;
        try
        {
            using (limitator.WaitAsync())
            {
                Console.WriteLine($"Start processing {file}");
                kpi = await DocumentExtractorHelper.ExtractDocumentInformationAsync(file, cmd, option, finalDir, indx, consoleLoggerFactory);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("File failed " + file + " exception : " + ex);
        }

        var counter = Interlocked.Increment(ref fileCounter);
        Console.WriteLine($"File processed {file} {counter}/{files.Count}");

        return kpi;

    });
}).ToArray();

await Task.WhenAll(processTasks);

var kpis = processTasks.Where(t => t.IsCompletedSuccessfully && t.Result is not null)
                       .Select(t => t.Result)
                       .Aggregate(new DocumentExtractorKPI(0, 0, 0),
                                  (acc, kpi) => new DocumentExtractorKPI(acc.PageExtractionTime + kpi!.PageExtractionTime,
                                                                         acc.nbPage + kpi.nbPage,
                                                                         acc.OutputWriteTime + kpi.OutputWriteTime));

if (commandLine.Value.Timed)
{
    Console.WriteLine("Extract {0} page(s) in total {1:N4} sec. Avg {2:N4} sec / pages",
                      kpis!.nbPage,
                      kpis!.PageExtractionTime,
                      (kpis.PageExtractionTime == 0 ? 0 : (kpis.nbPage / kpis.PageExtractionTime)));

    Console.WriteLine("Write analyse output : {0:N4} sec", kpis!.OutputWriteTime);
    Console.WriteLine("Total : {0:N4}  sec", timer.Elapsed.TotalSeconds);
    Console.WriteLine("Document analyze end : " + DateTime.Now);
}
