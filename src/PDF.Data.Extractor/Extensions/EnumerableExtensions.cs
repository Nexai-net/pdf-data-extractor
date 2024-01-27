// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace PDF.Data.Extractor.Extensions
{
    using System;
    using System.Collections.Generic;

    internal static class EnumerableExtensions
    {
        /// <summary>
        /// Gets the tree element.
        /// </summary>
        public static IEnumerable<TElement?> GetTreeElement<TElement, TSource>(this TSource source,
                                                                               Func<TSource, IEnumerable<TElement>?> getChild,
                                                                               Func<TElement, IEnumerable<TElement>?> getChildRec)
        {
            if (source is null)
                return Array.Empty<TElement>();

            var children = getChild(source);

            if (children is null)
                return Array.Empty<TElement>();

            return GetTreeElement<TElement>(children, getChildRec);
        }

        /// <summary>
        /// Gets the tree element.
        /// </summary>
        public static IEnumerable<TElement?> GetTreeElement<TElement>(this IEnumerable<TElement>? source, Func<TElement, IEnumerable<TElement>?> getChild)
        {
            if (source is not null)
            {
                foreach (var element in source)
                {
                    yield return element;

                    if (getChild is not null)
                    {
                        var children = getChild.Invoke(element);
                        if (children is null)
                            continue;

                        foreach (var child in GetTreeElement(children, getChild))
                            yield return child;
                    }
                }
            }
        }
    }
}
