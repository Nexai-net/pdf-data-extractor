// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace PDF.Data.Extractor.Console.Models
{
    public record class DocumentExtractorKPI(double PageExtractionTime, int nbPage, double OutputWriteTime);
}
