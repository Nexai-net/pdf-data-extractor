// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace PDF.Data.Extractor.Viewer.Models
{
    using Microsoft.Extensions.Logging;

    public record class Log(LogLevel LogLevel, string message);
}
