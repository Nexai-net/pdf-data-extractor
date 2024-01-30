// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace Data.Block.Abstractions.MetaData
{
    using System;
    using System.Runtime.Serialization;

    [DataContract]
    public record class ImageMetaData(Guid Uid,
                                      string ImageExtension,
                                      string ImageType,
                                      byte[]? RawBase64Data,
                                      float Width,
                                      float Height,
                                      string Hash);
}
