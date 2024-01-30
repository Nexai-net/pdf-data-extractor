// Copyright (c) Nexai.
// The Democrite licenses this file to you under the MIT license.
// Produce by nexai & community (cf. docs/Teams.md)

namespace Data.Block.Abstractions.Tags
{
    using System.Runtime.Serialization;

    /// <summary>
    /// PDF tag, meta-data link to block
    /// </summary>
    [DataContract]
    public abstract class DataTag
    {
        #region Ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="DataTag"/> class.
        /// </summary>
        protected DataTag(DataTagTypeEnum type, string raw)
        {
            this.Type = type;
            this.Raw = raw;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the type.
        /// </summary>
        [DataMember]
        public DataTagTypeEnum Type { get; }

        /// <summary>
        /// Gets the raw.
        /// </summary>
        [DataMember]
        public string Raw { get; }

        #endregion

        #region Methods

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            if (obj is DataTag tag)
            {
                return this.Type == tag.Type &&
                       string.Equals(this.Raw, tag.Raw, StringComparison.OrdinalIgnoreCase) &&
                       OnEquals(tag);
            }
            return false;
        }

        /// <inheritdoc cref="object.Equals(object?)" />
        /// <remarks>
        ///     Null and type check are already done
        /// </remarks>
        protected virtual bool OnEquals(DataTag tag)
        {
            return true;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine(this.Type, OnGetHashCode());
        }

        /// <inheritdoc cref="object.GetHashCode" />
        protected virtual int OnGetHashCode()
        {
            return 0;
        }


        #endregion
    }
}
