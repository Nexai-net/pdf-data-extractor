// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace Data.Block.Abstractions.UTests
{
    using AutoFixture;
    using AutoFixture.Kernel;

    using Data.Block.Abstractions.Tags;

    using NFluent;

    using System.Collections.Generic;
    using System.Text.Json;

    public class DataSerializationsUTest
    {
        public sealed class SampleDataBlock : DataBlock
        {
            public SampleDataBlock(Guid uid,
                                   BlockTypeEnum type,
                                   BlockArea area,
                                   IReadOnlyCollection<DataTag>? tags,
                                   IReadOnlyCollection<DataBlock>? children) 
                : base(uid, type, area, tags, children)
            {
            }
        }

        [Theory]
        [InlineData(typeof(BlockArea))]
        [InlineData(typeof(BlockPoint))]
        [InlineData(typeof(DataLangTag))]
        [InlineData(typeof(DataPropTag))]
        [InlineData(typeof(DataRawTag))]
        [InlineData(typeof(DataTextBlock))]
        [InlineData(typeof(DataImageBlock))]
        [InlineData(typeof(DataPageBlock))]
        [InlineData(typeof(DataDocumentBlock))]

        public void Ensure_DataBlocks_Can_Be_Serialize_And_Deserialized(Type blockType)
        {
            var fixture = new Fixture();

            fixture.Behaviors.OfType<ThrowingRecursionBehavior>()
                             .ToList()
                             .ForEach(b => fixture.Behaviors.Remove(b));

            fixture.Behaviors.Add(new OmitOnRecursionBehavior());

            fixture.Customizations.Add(new TypeRelay(typeof(DataTag), typeof(DataLangTag)));
            fixture.Customizations.Add(new TypeRelay(typeof(DataTag), typeof(DataRawTag)));
            fixture.Customizations.Add(new TypeRelay(typeof(DataTag), typeof(DataPropTag)));

            //fixture.Customizations.Add(new TypeRelay(typeof(DataBlock), typeof(SampleDataBlock)));
            fixture.Register<DataBlock>(() =>
            {
                return new DataTextBlock(Guid.NewGuid(),
                                         fixture.Create<float>(),
                                         fixture.Create<float>(),
                                         fixture.Create<float>(),
                                         fixture.Create<float>(),
                                         fixture.Create<float>(),
                                         fixture.Create<string>(),
                                         fixture.Create<Guid>(),
                                         fixture.Create<float>(),
                                         fixture.Create<BlockArea>(),
                                         null,
                                         null,
                                         null);
            });

            var entity = fixture.Create(blockType, new SpecimenContext(fixture));

            var setting = new JsonSerializerOptions()
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            };

            setting.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());

            var newtonSettings = new Newtonsoft.Json.JsonSerializerSettings()
            {
                Formatting = Newtonsoft.Json.Formatting.Indented,
                TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Objects,
            };

            newtonSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());

            var newtonEntityJson = Newtonsoft.Json.JsonConvert.SerializeObject(entity, newtonSettings);

            var entityJson = JsonSerializer.Serialize(entity, setting);

            Check.That(entityJson).IsNotNull().And.IsNotEmpty();

            var newtonDeserializeEntity = Newtonsoft.Json.JsonConvert.DeserializeObject(newtonEntityJson, blockType, newtonSettings);

            var entityDeserialized = JsonSerializer.Deserialize(entityJson, blockType, setting);

            Check.That(entityDeserialized).IsNotNull();

            var entityDeserializedJson = JsonSerializer.Serialize(entityDeserialized, setting);

            Check.That(entityDeserializedJson).IsEqualTo(entityJson);
        }
    }
}