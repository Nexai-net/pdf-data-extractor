// See https://aka.ms/new-console-template for more information

using CommandLine;
using CommandLine.Text;

using Data.Block.Abstractions;

using Microsoft.Extensions.Logging;

using PDF.Data.Extractor;
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

var timer = new Stopwatch();

var consoleLoggerFactory = LoggerFactory.Create(b => b.AddConsole());

using (var docBlockExtractor = new PDFExtractor(consoleLoggerFactory))
{
    if (commandLine.Value.Timed)
        Console.WriteLine("Document analyse start : " + DateTime.Now);

    timer.Start();

    var option = new PDFExtractorOptions()
    {
        PageRange = Range.StartAt(1),
        InjectImageMetaData = commandLine.Value.IncludeImages,
        Asynchronous = !commandLine.Value.PreventParallelProcess,
    };

    var docBlock = await docBlockExtractor.AnalyseAsync(commandLine.Value.Source!,
                                                        options: option);

    var current = new Uri(Directory.GetCurrentDirectory() + "/", UriKind.Absolute);

    var output = new Uri(current, commandLine.Value.Output ?? ".");

    var outputDirName = Path.GetFileNameWithoutExtension(commandLine.Value.Source);
    if (!string.IsNullOrEmpty(commandLine.Value.OutputFolderName))
        outputDirName = commandLine.Value.OutputFolderName;

    var finalDir = new Uri(Path.Combine(output.LocalPath, outputDirName!));
    if (!Directory.Exists(finalDir.LocalPath))
    {
        Directory.CreateDirectory(finalDir.LocalPath);
    }

    var pageTimer = new Stopwatch();

    pageTimer.Start();
    if (!commandLine.Value.IncludeImages)
    {
        var imageFolder = Path.Combine(finalDir.LocalPath, "Images");
        if (!Directory.Exists(imageFolder))
            Directory.CreateDirectory(imageFolder);

        var images = docBlockExtractor.ImageManager.GetAll();

        var saveImageTasks = images.Select(img =>
                                   {
                                       var bytes = img.RawBase64Data;

                                       if (bytes is null || bytes.Length == 0)
                                           return Task.CompletedTask;

                                       var raw = Convert.FromBase64String(Encoding.UTF8.GetString(bytes));
                                       return File.WriteAllBytesAsync(Path.Combine(imageFolder, img.Uid + "." + img.ImageExtension), raw);
                                   }).ToArray();

        await Task.WhenAll(saveImageTasks);
    }

    pageTimer.Stop();

    if (commandLine.Value.Timed)
        Console.WriteLine("Page(s) Analyse number " + (docBlock.Children?.Count ?? 0) + " in " + pageTimer.Elapsed);

    var settings = new JsonSerializerOptions()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    settings.Converters.Add(new JsonStringEnumConverter());

    var json = JsonSerializer.Serialize<DataDocumentBlock>(docBlock, settings);

    var outputFilename = Path.GetFileNameWithoutExtension(commandLine.Value.Source) + ".json";

    if (!string.IsNullOrEmpty(commandLine.Value.OutputName))
        outputFilename = commandLine.Value.OutputName;

    await File.WriteAllTextAsync(Path.Combine(finalDir.LocalPath, outputFilename), json);

    timer.Stop();

    if (commandLine.Value.Timed)
    {
        Console.WriteLine("Write analyse output : " + timer.Elapsed);
        Console.WriteLine("Document analyze end : " + DateTime.Now);
    }
}
