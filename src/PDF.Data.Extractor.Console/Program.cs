// See https://aka.ms/new-console-template for more information

using CommandLine;
using CommandLine.Text;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using PDF.Data.Extractor;
using PDF.Data.Extractor.Console;
using PDF.Data.Extractor.Console.Models;

using System.Diagnostics;

var parser = new Parser(s =>
{
    s.AutoHelp = true;
    s.AutoVersion = true;
    s.GetoptMode = true;
    s.CaseSensitive = false;
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

ILoggerFactory consoleLoggerFactory = NullLoggerFactory.Instance;

if (cmd.Silent == false)
    consoleLoggerFactory = LoggerFactory.Create(b => b.AddConsole());

if (commandLine.Value.Timed)
    Console.WriteLine("Document analyse start : " + DateTime.Now);

var totalTimer = new Stopwatch();
totalTimer.Start();

var option = new PDFExtractorOptions()
{
    PageRange = Range.StartAt(0),
    InjectImageMetaData = commandLine.Value.IncludeImages,
    Asynchronous = !commandLine.Value.PreventParallelProcess,
    SkipExtractImages = commandLine.Value.SkipExtractImages,
};

var current = new Uri(Directory.GetCurrentDirectory() + "/", UriKind.Absolute);
var cmdOutput = new Uri((commandLine.Value.Output ?? ".") + "/", UriKind.RelativeOrAbsolute);

var output = cmdOutput.IsAbsoluteUri ? cmdOutput : new Uri(current, cmdOutput.OriginalString ?? ".");

var outputDirName = Path.GetFileNameWithoutExtension(commandLine.Value.Source) ?? ".extractResult";

if (!string.IsNullOrEmpty(commandLine.Value.OutputFolderName))
    outputDirName = commandLine.Value.OutputFolderName;

var finalDir = new Uri(Path.Combine(output.LocalPath, outputDirName!));
if (!Directory.Exists(finalDir.LocalPath))
{
    Directory.CreateDirectory(finalDir.LocalPath);
}

long fileCounter = 0;

int maxConcurrentDocument = (int)cmd.MaxConcurrentDocument;

if (maxConcurrentDocument == 0)
    maxConcurrentDocument = Environment.ProcessorCount / 4;

maxConcurrentDocument = Math.Max(1, maxConcurrentDocument);

Console.WriteLine($"Start extracting {files.Count} file(s). MaxConcurrentDocument : {maxConcurrentDocument}");

var limitator = new SemaphoreSlim(maxConcurrentDocument);

var globalLogger = consoleLoggerFactory.CreateLogger("Global");

var processTasks = files.Select((file, indx) =>
{
    return Task.Run(async () =>
    {
        DocumentExtractorKPI? kpi = null;
        try
        {
            await limitator.WaitAsync();

            try
            {
                if (cmd.Silent == false)
                    Console.WriteLine($"Start processing {file}");

                kpi = await DocumentExtractorHelper.ExtractDocumentInformationAsync(file, cmd, option, finalDir, indx, consoleLoggerFactory);
            }
            finally
            {
                limitator.Release();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("File failed " + file + " exception : " + ex);
            globalLogger.LogError(ex, "File failed {file} exception : {exception}", file, ex);
        }

        var counter = Interlocked.Increment(ref fileCounter);
        Console.WriteLine($"[{counter}/{files.Count}] - done - processed {file}");

        return kpi;

    });
}).ToArray();

await Task.WhenAll(processTasks);

totalTimer.Stop();

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
                      (kpis.nbPage == 0 ? 0 : (kpis.PageExtractionTime / kpis.nbPage)));

    Console.WriteLine("Write analyse output in total : {0:N4} sec", kpis!.OutputWriteTime);
    Console.WriteLine("Total : {0:N4}  sec, avg {1:N4} / documents", totalTimer.Elapsed.TotalSeconds, (files.Count == 0 ? 0 : (kpis.PageExtractionTime / files.Count)));
    Console.WriteLine("Document analyze end : " + DateTime.Now);
}
