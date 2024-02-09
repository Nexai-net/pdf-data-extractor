// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace Data.Block.Abstractions.MetaData
{
    /// <summary>
    /// Meta Data used to get some font information
    /// </summary>
    public record class TextFontMetaData(Guid Uid,
                                         string Name,
                                         float FontSize,
                                         float MinWidth,
                                         float MaxWidth,
                                         float LineSizePoint);
}
