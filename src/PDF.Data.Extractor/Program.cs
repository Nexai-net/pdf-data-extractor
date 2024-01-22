// See https://aka.ms/new-console-template for more information

using CommandLine;
using CommandLine.Text;

using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using iText.Kernel.Pdf.Canvas.Parser.Util;
using iText.Pdfa;

//using Newtonsoft.Json;
//using Newtonsoft.Json.Converters;
//using Newtonsoft.Json.Linq;

using PDF.Data.Extractor.Abstractions;
using PDF.Data.Extractor.Models;
using PDF.Data.Extractor.Services;
using PDF.Data.Extractor.Strategies;

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

DataDocumentBlock docBlock;

using (var reader = new PdfReader(commandLine.Value.Source))
using (var doc = new PdfDocument(reader))
using (var fontManager = new FontMetaDataInfoExtractStrategy())
{
    var mergeStrategy = new IDataBlockMergeStrategy[]
    {
        new DataTextBlockHorizontalSiblingMergeStrategy(fontManager),
        new DataTextBlockVerticalSiblingMergeStrategy(fontManager, alignRight: false),
        new DataTextBlockVerticalSiblingMergeStrategy(fontManager, alignRight: true)
        // Align by center
    };

    var pages = doc.GetNumberOfPages();
    var pageBlocks = new DataPageBlock[pages];

    //var pageDatas = Enumerable.Range(1, pages)
    //                          .Select(i => (indx: i, doc.GetPage(i)))
    //                          .Select(p =>
    //Parallel.For(0, pages, (number) =>
    //await Parallel.ForEachAsync(pageDatas, (p, token) =>
    //{
    //    return Task.Run(() =>
    //    {
    //var number = p.indx;
    //var page = p.Item2;

    var number = 5;
    var page = doc.GetPage(number);
    Console.WriteLine("Start Analyse Page " + number);
    //var page = doc.GetPage(number);

    var strategy = new DataBlockExtractStrategy(fontManager);
    var processor = new PdfCanvasProcessor(strategy);
    processor.ProcessPageContent(page);

    var pageBlock = strategy.Compile(number, page, mergeStrategy);
    pageBlocks[number - 1] = pageBlock;
    Console.WriteLine("Page " + number + " Analyzed");
    //    });
    //});

    //await Task.WhenAll(pageDatas);

    var docInfo = doc.GetDocumentInfo();

    var defaultPageSize = doc.GetDefaultPageSize();

    docBlock = new DataDocumentBlock(Guid.NewGuid(),
                                     Path.GetFileNameWithoutExtension(commandLine.Value.Source),
                                     new BlockArea(defaultPageSize.GetLeft(), defaultPageSize.GetTop(), defaultPageSize.GetWidth(), defaultPageSize.GetHeight()),
                                     pageBlocks,
                                     doc.GetPdfVersion().ToString(),
                                     docInfo.GetAuthor(),
                                     docInfo.GetKeywords(),
                                     docInfo.GetProducer(),
                                     docInfo.GetSubject(),
                                     docInfo.GetTitle(),
                                     fontManager.GetAll());

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
