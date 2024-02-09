// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace PDF.Data.Extractor.Console
{
    using global::Data.Block.Abstractions;

    using Microsoft.Extensions.Logging;

    using PDF.Data.Extractor.Console.Models;

    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;

    public static class DocumentExtractorHelper
    {
        public static async Task<DocumentExtractorKPI> ExtractDocumentInformationAsync(string file,
                                                                                       ExtractCommandLineArgument cmd,
                                                                                       PDFExtractorOptions option,
                                                                                       Uri finalDir,
                                                                                       int index,
                                                                                       ILoggerFactory consoleLoggerFactory)
        {

            using (var tokenSourceTimeout = new CancellationTokenSource(TimeSpan.FromMinutes(5)))
            using (var docBlockExtractor = new PDFExtractor(consoleLoggerFactory))
            {
                var pageTimer = new Stopwatch();
                pageTimer.Start();

                var docBlock = await docBlockExtractor.AnalyseAsync(file,
                                                                    tokenSourceTimeout.Token,
                                                                    options: option);

                pageTimer.Stop();

                var outputTimer = new Stopwatch();

                outputTimer.Start();

                if (!cmd.IncludeImages)
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

                        tokenSourceTimeout.Token.ThrowIfCancellationRequested();

                        var raw = Convert.FromBase64String(Encoding.UTF8.GetString(bytes));
                        return File.WriteAllBytesAsync(Path.Combine(imageFolder, img.Uid + "." + img.ImageExtension), raw, tokenSourceTimeout.Token);
                    }).ToArray();

                    await Task.WhenAll(saveImageTasks);
                }

                var settings = new JsonSerializerOptions()
                {
                    WriteIndented = true,
                    PropertyNameCaseInsensitive = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals
                };

                settings.Converters.Add(new JsonStringEnumConverter());

                var json = JsonSerializer.Serialize<DataDocumentBlock>(docBlock, settings);

                var outputFilename = Path.GetFileNameWithoutExtension(file);

                if (!string.IsNullOrEmpty(cmd.OutputName))
                {
                    outputFilename = cmd.OutputName;

                    if (index > 0)
                        outputFilename += index;
                }

                tokenSourceTimeout.Token.ThrowIfCancellationRequested();

                await File.WriteAllTextAsync(Path.Combine(finalDir.LocalPath, outputFilename + ".json"), json, tokenSourceTimeout.Token);

                outputTimer.Stop();

                return new DocumentExtractorKPI(pageTimer.Elapsed.TotalSeconds, docBlock.Children?.Count ?? 0, outputTimer.Elapsed.TotalSeconds);
            }
        }
    }
}
