﻿using Reclaimer.Blam.Common;
using Reclaimer.Blam.Utilities;
using Reclaimer.IO;
using System;
using System.Globalization;
using System.IO;

namespace Reclaimer.Blam.Halo4
{
    public enum PageType
    {
        Auto,
        Primary,
        Secondary,
        Tertiary
    }

    public struct ResourceIdentifier
    {
        private readonly ICacheFile cache;
        private readonly int identifier; //actually two shorts

        public ResourceIdentifier(int identifier, ICacheFile cache)
        {
            this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
            this.identifier = identifier;
        }

        public ResourceIdentifier(DependencyReader reader, ICacheFile cache)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));
            this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
            identifier = reader.ReadInt32();
        }

        public int Value => identifier;

        public int ResourceIndex => identifier & ushort.MaxValue;

        public byte[] ReadData(PageType mode) => ReadData(mode, int.MaxValue);

        public byte[] ReadData(PageType mode, int maxLength)
        {
            var resourceGestalt = cache.TagIndex.GetGlobalTag("zone").ReadMetadata<cache_file_resource_gestalt>();
            var resourceLayoutTable = cache.TagIndex.GetGlobalTag("play").ReadMetadata<cache_file_resource_layout_table>();

            var entry = resourceGestalt.ResourceEntries[ResourceIndex];

            if (entry.SegmentIndex < 0)
                throw new InvalidOperationException("Data not found");

            var segment = resourceLayoutTable.Segments[entry.SegmentIndex];
            var useTertiary = mode == PageType.Tertiary || (mode == PageType.Auto && segment.TertiaryPageIndex >= 0);
            var useSecondary = mode == PageType.Secondary || (mode == PageType.Auto && segment.SecondaryPageIndex >= 0);

            var pageIndex = useTertiary ? segment.TertiaryPageIndex : useSecondary ? segment.SecondaryPageIndex : segment.PrimaryPageIndex;
            var segmentOffset = useTertiary ? segment.TertiaryPageOffset : useSecondary ? segment.SecondaryPageOffset : segment.PrimaryPageOffset;

            if (pageIndex < 0 || segmentOffset < 0)
                throw new InvalidOperationException("Data not found");

            var page = resourceLayoutTable.Pages[pageIndex];
            if (mode == PageType.Auto && (page.DataOffset < 0 || page.CompressedSize == 0))
            {
                pageIndex = segment.PrimaryPageIndex;
                segmentOffset = segment.PrimaryPageOffset;
                page = resourceLayoutTable.Pages[pageIndex];
            }

            var targetFile = cache.FileName;
            if (page.CacheIndex >= 0)
            {
                var directory = Directory.GetParent(cache.FileName).FullName;
                var mapName = Utils.GetFileName(resourceLayoutTable.SharedCaches[page.CacheIndex].FileName);
                targetFile = Path.Combine(directory, mapName);
            }

            using (var fs = new FileStream(targetFile, FileMode.Open, FileAccess.Read))
            using (var reader = new EndianReader(fs, cache.ByteOrder))
            {
                uint dataTableAddress;
                //if (page.CacheIndex >= 0)
                //    dataTableAddress = 122880; //header size
                //else
                //{
                switch (cache.CacheType)
                {
                    case CacheType.MccHalo4U4:
                    case CacheType.MccHalo4_2212:
                    case CacheType.MccHalo2XU8:
                    case CacheType.MccHalo2X_2212:
                        reader.Seek(1224, SeekOrigin.Begin);
                        dataTableAddress = reader.ReadUInt32();
                        break;
                    default:
                        reader.Seek(cache.CacheType >= CacheType.MccHalo4 ? 1216 : 1152, SeekOrigin.Begin);
                        dataTableAddress = reader.ReadUInt32();
                        break;
                }
                //}

                reader.Seek(dataTableAddress + page.DataOffset, SeekOrigin.Begin);
                return ContentFactory.GetResourceData(reader, cache.Metadata.ResourceCodec, maxLength, segmentOffset, page.CompressedSize, page.DecompressedSize);
            }
        }

        public override string ToString() => Value.ToString(CultureInfo.CurrentCulture);

        #region Equality Operators

        public static bool operator ==(ResourceIdentifier value1, ResourceIdentifier value2) => value1.identifier == value2.identifier;
        public static bool operator !=(ResourceIdentifier value1, ResourceIdentifier value2) => !(value1 == value2);

        public static bool Equals(ResourceIdentifier value1, ResourceIdentifier value2) => value1.identifier.Equals(value2.identifier);
        public override bool Equals(object obj) => obj is ResourceIdentifier value && ResourceIdentifier.Equals(this, value);
        public bool Equals(ResourceIdentifier value) => ResourceIdentifier.Equals(this, value);

        public override int GetHashCode() => identifier.GetHashCode();

        #endregion
    }
}