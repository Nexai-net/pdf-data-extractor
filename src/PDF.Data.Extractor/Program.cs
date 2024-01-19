// See https://aka.ms/new-console-template for more information

using CommandLine;
using CommandLine.Text;

using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using iText.Kernel.Pdf.Canvas.Parser.Util;
using iText.Pdfa;

using PDF.Data.Extractor.Abstractions;
using PDF.Data.Extractor.Models;
using PDF.Data.Extractor.Strategies;

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
{
    var pages = doc.GetNumberOfPages();

    var pageBlocks = new DataPageBlock[pages];

    //Parallel.For
    for (int number = 1; number <= pages; number++)
    {
        var page = doc.GetPage(1);

        var strategy = new DataBlockExtractStrategy();
        var processor = new PdfCanvasProcessor(strategy);
        processor.ProcessPageContent(page);

        var pageBlock = strategy.Compile(number, page);
        pageBlocks[number - 1] = pageBlock;
    }

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
                                     docInfo.GetTitle());

}
