﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reclaimer.Blam.Common
{
    using static CacheMetadataFlags;
    using static CacheResourceCodec;
    using static HaloGame;

    [AttributeUsage(AttributeTargets.Field)]
    internal sealed class CacheMetadataAttribute : Attribute
    {
        public HaloGame Game { get; }
        public CacheGeneration Generation { get; }
        public CachePlatform Platform { get; }
        public CacheResourceCodec ResourceCodec { get; }
        public CacheMetadataFlags Flags { get; }

        public CacheMetadataAttribute(HaloGame game, CachePlatform platform)
            : this(game, platform, game < Halo3 ? Uncompressed : Deflate, None)
        { }

        public CacheMetadataAttribute(HaloGame game, CachePlatform platform, CacheMetadataFlags flags)
            : this(game, platform, game < Halo3 ? Uncompressed : Deflate, flags)
        { }

        public CacheMetadataAttribute(HaloGame game, CachePlatform platform, CacheResourceCodec codec)
             : this(game, platform, codec, None)
        { }

        public CacheMetadataAttribute(HaloGame game, CachePlatform platform, CacheResourceCodec codec, CacheMetadataFlags flags)
        {
            Game = game;
            Platform = platform;
            ResourceCodec = codec;
            Flags = flags;
            Generation = game switch
            {
                Halo1 => CacheGeneration.Gen1,
                Halo2 => CacheGeneration.Gen2,
                Halo3 or Halo3ODST or HaloReach => CacheGeneration.Gen3,
                Halo4 or Halo2X => CacheGeneration.Gen4,
                _ => throw new NotImplementedException()
            };
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    internal sealed class BuildStringAttribute : Attribute
    {
        public string BuildString { get; }
        public string StringIds { get; }
        public CacheResourceCodec? ResourceCodecOverride { get; }
        public CacheMetadataFlags? FlagsOverride { get; }

        public BuildStringAttribute(string buildString)
            : this(buildString, null, null, null)
        { }

        public BuildStringAttribute(string buildString, string stringIds)
            : this(buildString, stringIds, null, null)
        { }

        public BuildStringAttribute(string buildString, CacheMetadataFlags flags)
            : this(buildString, null, null, flags)
        { }

        public BuildStringAttribute(string buildString, string stringIds, CacheResourceCodec codec)
            : this(buildString, stringIds, codec, null)
        { }

        public BuildStringAttribute(string buildString, string stringIds, CacheMetadataFlags flags)
            : this(buildString, stringIds, null, flags)
        { }

        public BuildStringAttribute(string buildString, string stringIds, CacheResourceCodec codecOverride, CacheMetadataFlags flagsOverride)
            : this(buildString, stringIds, (CacheResourceCodec?)codecOverride, (CacheMetadataFlags?)flagsOverride)
        { }

        private BuildStringAttribute(string buildString, string stringIds, CacheResourceCodec? codecOverride, CacheMetadataFlags? flagsOverride)
        {
            BuildString = buildString;
            StringIds = stringIds;
            ResourceCodecOverride = codecOverride;
            FlagsOverride = flagsOverride;
        }
    }
}
