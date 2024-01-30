// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace PDF.Data.Extractor.Strategies
{
    using global::Data.Block.Abstractions;
    using global::Data.Block.Abstractions.MetaData;
    using global::Data.Block.Abstractions.Tags;
    using PDF.Data.Extractor.Services;

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Numerics;

    /// <summary>
    /// Merge blokc following linear algorithme
    /// </summary>
    /// <seealso cref="PDF.Data.Extractor.Services.IDataTextBlockMergeStrategy" />
    public abstract class DataTextBlockSiblingMergeBaseStrategy : DataBlockMergeBaseStrategy<DataTextBlock>
    {
        #region Fields

        private readonly int _maxMergeCount;

        #endregion

        #region Ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="DataTextBlockSiblingMergeBaseStrategy"/> class.
        /// </summary>
        protected DataTextBlockSiblingMergeBaseStrategy(IFontManager fontManager, int maxMergeCount = 5000)
        {
            this.FontManager = fontManager;
            this._maxMergeCount = maxMergeCount;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the font manager.
        /// </summary>
        public IFontManager FontManager { get; }

        #endregion

        #region Methods

        /// <summary>
        /// Test Linear rapporchment to merge
        /// </summary>
        protected sealed override IReadOnlyCollection<DataTextBlock> Merge(IEnumerable<DataTextBlock> dataBlocks, CancellationToken token)
        {
            IReadOnlyCollection<DataTextBlock> texts = dataBlocks.ToArray();
            int preventInfinitLoopCounter = this._maxMergeCount;
            bool needMoreMerge = false;

            do
            {
                needMoreMerge = false;

                // Try merge Left to Ritgh
                var remains = TryMerge(texts,
                                       this.FontManager,
                                       ValidateCanMerge,

                                       (source, sourceFont, target) => new DataTextBlock(Guid.NewGuid(),
                                                                                         source.FontLevel,
                                                                                         source.PointValue,
                                                                                         source.LineSize,
                                                                                         source.Scale,
                                                                                         source.Magnitude,
                                                                                         MergeText(source.Text, target.Text) ?? string.Empty,
                                                                                         source.FontInfoUid,
                                                                                         source.SpaceWidth,
                                                                                         MergeArea(source.Area, target.Area),
                                                                                         
                                                                                         (source.TextBoxIds ?? Array.Empty<float>())
                                                                                            .Concat(target.TextBoxIds ?? Array.Empty<float>())
                                                                                            .Distinct(),

                                                                                         MergeTags(source.Tags, target.Tags),
                                                                                         MergeChildren(source.Children, target.Children)),
                                       token);

                Debug.Assert(remains.Count <= texts.Count);

                if (texts.Count > remains.Count)
                    needMoreMerge = true;

                token.ThrowIfCancellationRequested();

                texts = remains;

                preventInfinitLoopCounter--;
            } while (needMoreMerge && preventInfinitLoopCounter > 0);

            // When Left to right finished try top to bottom

            return texts;
        }

        /// <summary>
        /// Validates if blocks can be merged.
        /// </summary>
        protected virtual bool ValidateCanMerge(DataTextBlock source,
                                                TextFontMetaData sourceFont,
                                                DataTextBlock target,
                                                TextFontMetaData targetFont)
        {
            return TryMergeSiblingBlock(GetSourceComparePoint(source),
                                       GetTargetComparePoint(target),
                                       GetCompareSourceLign(source),
                                       GetCompareSourceLign(target),
                                       GetCompareSourceLign(source),
                                       GetAllowedSpaceBetwenBlocks(source, sourceFont, target, targetFont));
        }

        /// <summary>
        /// Gets the allowed space betwen blocks.
        /// </summary>
        protected abstract float GetAllowedSpaceBetwenBlocks(DataTextBlock source, TextFontMetaData sourceFont, DataTextBlock target, TextFontMetaData targetFont);

        /// <summary>
        /// Gets the compare source lign.
        /// </summary>
        protected abstract Vector2 GetCompareSourceLign(DataTextBlock source);

        /// <summary>
        /// Gets the target compare point.
        /// </summary>
        protected abstract BlockPoint GetTargetComparePoint(DataTextBlock target);

        /// <summary>
        /// Gets the source compare point.
        /// </summary>
        protected abstract BlockPoint GetSourceComparePoint(DataTextBlock source);

        /// <summary>
        /// Merges the children.
        /// </summary>
        protected virtual IReadOnlyCollection<DataBlock> MergeChildren(IReadOnlyCollection<DataBlock>? source, IReadOnlyCollection<DataBlock>? target)
        {
            return (source ?? Array.Empty<DataBlock>())
                            .Concat(target ?? Array.Empty<DataBlock>())
                            .Distinct()
                            .ToArray();
        }

        /// <summary>
        /// Merges the children.
        /// </summary>
        protected virtual IReadOnlyCollection<DataTag> MergeTags(IReadOnlyCollection<DataTag>? source, IReadOnlyCollection<DataTag>? target)
        {
            return (source ?? Array.Empty<DataTag>())
                            .Concat(target ?? Array.Empty<DataTag>())
                            .Where(t => !string.IsNullOrEmpty(t.Raw))
                            .Distinct()
                            .ToArray();
        }

        /// <summary>
        /// Merges the area.
        /// </summary>
        protected abstract BlockArea MergeArea(BlockArea source, BlockArea target);

        /// <summary>
        /// Merges the text.
        /// </summary>
        protected virtual string? MergeText(in string? source, in string? target)
        {
            return source + target;
        }

        #region Tools

        private IReadOnlyCollection<DataTextBlock> TryMerge(IReadOnlyCollection<DataTextBlock> texts,
                                                            IFontManager fontManager,
                                                            Func<DataTextBlock, TextFontMetaData, DataTextBlock, TextFontMetaData, bool> mergeTest,
                                                            Func<DataTextBlock, TextFontMetaData, DataTextBlock, DataTextBlock> merge,
                                                            CancellationToken token)
        {
            if (texts.Count < 2)
                return texts;

            var localCollection = PrepateBlocks(texts);
            for (int i = 0; i < localCollection.Count; i++)
            {
                var current = localCollection[i];

                token.ThrowIfCancellationRequested();

                if (current is null)
                    continue;

                var currentFont = fontManager.Get(current.FontInfoUid);

                for (int nextIndx = i; nextIndx < localCollection.Count; nextIndx++)
                {
                    token.ThrowIfCancellationRequested();

                    if (nextIndx == i)
                        continue;

                    var next = localCollection[nextIndx];

                    if (next is null || current.Equals(next))
                        continue;

                    var nextFont = fontManager.Get(next.FontInfoUid);

                    // Check font and scale to see if the text use the same
                    // Use to differenciate Title, text, and remark ...
                    if (currentFont.FontSize != nextFont.FontSize)
                        continue;

                    if (!string.Equals(currentFont.Name, nextFont.Name, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (current.Scale != next.Scale)
                        continue;

                    if (current.Magnitude != next.Magnitude)
                        continue;

                    // With LineSizePoint we managed to ensure scaling
                    if (current.LineSize != next.LineSize || currentFont.LineSizePoint != nextFont.LineSizePoint)
                        continue;

                    if (!mergeTest(current, currentFont, next, nextFont))
                        continue;

                    var newBlock = merge(current, currentFont, next);

                    localCollection[i] = newBlock;
                    localCollection.RemoveAt(nextIndx);
                    i--;
                    break;

                }
            }

            token.ThrowIfCancellationRequested();
            return localCollection;
        }

        /// <summary>
        /// Prepates the blocks.
        /// </summary>
        protected virtual List<DataTextBlock> PrepateBlocks(IEnumerable<DataTextBlock> texts)
        {
            return texts.ToList();
        }

        /// <summary>
        /// Math to check if two block are sibling and could be merged
        /// </summary>
        private bool TryMergeSiblingBlock(BlockPoint blockASide,
                                          BlockPoint blockBSide,
                                          Vector2 blockAlignSideLine,
                                          Vector2 blockBlignSideLine,
                                          Vector2 alignLine,
                                          float distTolerance)
        {
            var blockDiff = BlockCoordHelper.Diff(blockASide, blockBSide);
            var dist = blockDiff.Length();

            var angle = BlockCoordHelper.RadianAngle(alignLine, blockDiff);
            var targetSourceAngle = BlockCoordHelper.RadianAngle(blockAlignSideLine, blockBlignSideLine);

            // Block are not aligned
            return (Math.Abs(angle) <= BlockCoordHelper.ALIGN_MAGNITUDE_TOLERANCE || Math.Abs(targetSourceAngle) <= BlockCoordHelper.ALIGN_MAGNITUDE_TOLERANCE) &&
                   dist <= distTolerance;
        }

        #endregion

        #endregion
    }
}
