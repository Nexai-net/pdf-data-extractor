// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace PDF.Data.Extractor.Abstractions
{
    /// <summary>
    /// Define the type of data block
    /// </summary>
    public enum BlockTypeEnum
    {
        None,
        Text,
        Image,
        Column,
        Document,
        Page
    }
}
