// See https://aka.ms/new-console-template for more information

using CommandLine;
using CommandLine.Text;

using PDF.Data.Extractor;

using PDF.Data.Extractor.Abstractions;
using PDF.Data.Extractor.Console.Models;

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
    var builder = SentenceBuilder.Create();
    var errors = HelpText.RenderParsingErrorsTextAsLines(commandLine, builder.FormatError, builder.FormatMutuallyExclusiveSetErrors, 0);
    Console.Error.WriteLine(errors);

    var usage = HelpText.RenderUsageText(commandLine);
    Console.WriteLine(usage);

    Environment.ExitCode = -1;
    return;
}

using (var docBlockExtractor = new PDFExtractor())
{
    var docBlock = await docBlockExtractor.AnalyseAsync(commandLine.Value.Source!, 
                                                        options: new PDFExtractorOptions()
                                                        {
                                                            PageRange = Range.StartAt(1)
                                                        });

    var current = new Uri(Directory.GetCurrentDirectory(), UriKind.Absolute);

    var output = new Uri(current, commandLine.Value.Output ?? ".");

    var outputDirName = Path.GetFileNameWithoutExtension(commandLine.Value.Source);
    if (!string.IsNullOrEmpty(commandLine.Value.OutputName))
        outputDirName = commandLine.Value.OutputName;

    var finalDir = new Uri(Path.Combine(output.LocalPath, outputDirName));
    if (!Directory.Exists(finalDir.LocalPath))
    {
        Directory.CreateDirectory(finalDir.LocalPath);
    }

    if (!commandLine.Value.IncludeImages)
    {
        var imageFolder = Path.Combine(finalDir.LocalPath, "Images");
        if (!Directory.Exists(imageFolder))
            Directory.CreateDirectory(imageFolder);

        var saveImageTasks = (docBlock.Children?.OfType<DataPageBlock>() ?? Array.Empty<DataPageBlock>())
                                      .SelectMany(page => page.Children?.OfType<DataImageBlock>() ?? Array.Empty<DataImageBlock>())
                                      .Select(block =>
                                      {
                                          var bytes = block.ImageEncodedBytesBase64;

                                          if (bytes is null || bytes.Length == 0)
                                              return Task.CompletedTask;

                                          block.ClearImageBytes();

                                          var raw = Convert.FromBase64String(Encoding.UTF8.GetString(bytes));

                                          return File.WriteAllBytesAsync(Path.Combine(imageFolder, block.Uid + "." + block.ImageType.ToLowerInvariant()), raw);
                                      }).ToArray();

        await Task.WhenAll(saveImageTasks);
    }

    var settings = new JsonSerializerOptions()
    {
        WriteIndented = true,
    };

    settings.Converters.Add(new JsonStringEnumConverter());

    var json = JsonSerializer.Serialize<DataDocumentBlock>(docBlock, settings);

    await File.WriteAllTextAsync(Path.Combine(finalDir.LocalPath, "document.json"), json);
}
