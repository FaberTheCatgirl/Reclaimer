﻿using Adjutant.Blam.Common;
using Adjutant.Spatial;
using Adjutant.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Dds;
using System.IO.Endian;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adjutant.Blam.Halo3
{
    public class bitmap : IBitmap
    {
        private readonly ICacheFile cache;
        private readonly IIndexItem item;

        public bitmap(ICacheFile cache, IIndexItem item)
        {
            this.cache = cache;
            this.item = item;
        }

        [Offset(84)]
        public BlockCollection<SequenceBlock> Sequences { get; set; }

        [Offset(96)]
        public BlockCollection<BitmapDataBlock> Bitmaps { get; set; }

        [Offset(140)]
        public BlockCollection<BitmapResourceBlock> Resources { get; set; }

        [Offset(152)]
        public BlockCollection<BitmapResourceBlock> InterleavedResources { get; set; }

        #region IBitmap

        private static readonly Dictionary<TextureFormat, DxgiFormat> dxgiLookup = new Dictionary<TextureFormat, DxgiFormat>
        {
            { TextureFormat.DXT1, DxgiFormat.BC1_UNorm },
            { TextureFormat.DXT3, DxgiFormat.BC2_UNorm },
            { TextureFormat.DXT5, DxgiFormat.BC3_UNorm },
            { TextureFormat.A8R8G8B8, DxgiFormat.B8G8R8A8_UNorm },
            { TextureFormat.X8R8G8B8, DxgiFormat.B8G8R8X8_UNorm },
            { TextureFormat.R5G6B5, DxgiFormat.B5G6R5_UNorm },
            { TextureFormat.A1R5G5B5, DxgiFormat.B5G5R5A1_UNorm },
            { TextureFormat.A4R4G4B4, DxgiFormat.B4G4R4A4_UNorm }
        };

        private static readonly Dictionary<TextureFormat, XboxFormat> xboxLookup = new Dictionary<TextureFormat, XboxFormat>
        {
            { TextureFormat.A8, XboxFormat.A8 },
            { TextureFormat.A8Y8, XboxFormat.Y8A8 },
            { TextureFormat.AY8, XboxFormat.AY8 },
            { TextureFormat.CTX1, XboxFormat.CTX1 },
            { TextureFormat.DXT3a_mono, XboxFormat.DXT3a_mono },
            { TextureFormat.DXT3a_alpha, XboxFormat.DXT3a_alpha },
            { TextureFormat.DXT5a, XboxFormat.DXT5a_scalar },
            { TextureFormat.DXT5a_mono, XboxFormat.DXT5a_mono },
            { TextureFormat.DXT5a_alpha, XboxFormat.DXT5a_alpha },
            { TextureFormat.DXN, XboxFormat.DXN },
            { TextureFormat.DXN_mono_alpha, XboxFormat.DXN_mono_alpha },
            { TextureFormat.P8, XboxFormat.Y8 },
            { TextureFormat.P8_bump, XboxFormat.Y8 },
            { TextureFormat.Y8, XboxFormat.Y8 }
        };

        private static readonly CubemapLayout Halo3CubeLayout = new CubemapLayout
        {
            Face1 = CubemapFace.Right,
            Face2 = CubemapFace.Left,
            Face3 = CubemapFace.Back,
            Face4 = CubemapFace.Front,
            Face5 = CubemapFace.Top,
            Face6 = CubemapFace.Bottom,
            Orientation1 = RotateFlipType.Rotate270FlipNone,
            Orientation2 = RotateFlipType.Rotate90FlipNone,
            Orientation3 = RotateFlipType.Rotate180FlipNone,
            Orientation6 = RotateFlipType.Rotate180FlipNone
        };

        string IBitmap.Name => item.FullPath;

        int IBitmap.SubmapCount => Bitmaps.Count;

        CubemapLayout IBitmap.CubeLayout => Halo3CubeLayout;

        public DdsImage ToDds(int index)
        {
            if (index < 0 || index >= Bitmaps.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            var submap = Bitmaps[index];

            var resource = InterleavedResources.Any()
                ? InterleavedResources[submap.InterleavedIndex].ResourcePointer
                : Resources[index].ResourcePointer;

            var data = resource.ReadData();

            var bpp = submap.BitmapFormat.Bpp();
            for (int i = 0; i < data.Length - 1; i += bpp)
                Array.Reverse(data, i, bpp);

            if (submap.Flags.HasFlag(BitmapFlags.Swizzled))
            {
                var blockSize = submap.BitmapFormat.LinearBlockSize();
                var texelPitch = submap.BitmapFormat.LinearTexelPitch();

                var virtualHeight = submap.BitmapType == TextureType.CubeMap 
                    ? submap.Height * 6 
                    : submap.Height;

                data = TextureUtils.XTextureScramble(data, submap.Width, virtualHeight, blockSize, texelPitch, false);
            }

            DdsImage dds;
            if (dxgiLookup.ContainsKey(submap.BitmapFormat))
                dds = new DdsImage(submap.Height, submap.Width, dxgiLookup[submap.BitmapFormat], DxgiTextureType.Texture2D, data);
            else if (xboxLookup.ContainsKey(submap.BitmapFormat))
                dds = new DdsImage(submap.Height, submap.Width, xboxLookup[submap.BitmapFormat], DxgiTextureType.Texture2D, data);
            else throw Exceptions.BitmapFormatNotSupported(submap.BitmapFormat.ToString());

            if (submap.BitmapType == TextureType.CubeMap)
            {
                dds.TextureFlags = TextureFlags.DdsSurfaceFlagsCubemap;
                dds.CubemapFlags = CubemapFlags.DdsCubemapAllFaces;
                dds.DX10ResourceFlags = D3D10ResourceMiscFlags.TextureCube;
            }

            return dds;
        }

        #endregion
    }

    [FixedSize(64)]
    public class SequenceBlock
    {
        [Offset(0)]
        [NullTerminated(Length = 32)]
        public string Name { get; set; }

        [Offset(32)]
        public short FirstSubmapIndex { get; set; }

        [Offset(34)]
        public short BitmapCount { get; set; }

        [Offset(52)]
        public BlockCollection<SpriteBlock> Sprites { get; set; }
    }

    [FixedSize(32)]
    public class SpriteBlock
    {
        [Offset(0)]
        public short SubmapIndex { get; set; }

        [Offset(8)]
        public float Left { get; set; }

        [Offset(12)]
        public float Right { get; set; }

        [Offset(16)]
        public float Top { get; set; }

        [Offset(20)]
        public float Bottom { get; set; }

        [Offset(24)]
        public RealVector2D RegPoint { get; set; }
    }

    [FixedSize(48)]
    public class BitmapDataBlock
    {
        [Offset(0)]
        [FixedLength(4)]
        public string Class { get; set; }

        [Offset(4)]
        public short Width { get; set; }

        [Offset(6)]
        public short Height { get; set; }

        [Offset(8)]
        public byte Depth { get; set; }

        [Offset(9)]
        public BitmapFlags Flags { get; set; }

        [Offset(10)]
        public TextureType BitmapType { get; set; }

        [Offset(12)]
        public TextureFormat BitmapFormat { get; set; }

        [Offset(14)]
        public short MoreFlags { get; set; }

        [Offset(16)]
        public short RegX { get; set; }

        [Offset(18)]
        public short RegY { get; set; }

        [Offset(20)]
        public short MipmapCount { get; set; }

        [Offset(22)]
        public byte InterleavedIndex { get; set; }

        [Offset(23)]
        public byte Index2 { get; set; }

        [Offset(24)]
        public byte PixelsOffset { get; set; }

        [Offset(28)]
        public byte PixelsSize { get; set; }
    }

    [FixedSize(8)]
    public class BitmapResourceBlock
    {
        [Offset(0)]
        public ResourceIdentifier ResourcePointer { get; set; }

        [Offset(4)]
        public int Unknown { get; set; }
    }

    [Flags]
    public enum BitmapFlags : byte
    {
        Swizzled = 8
    }

    public enum TextureType : short
    {
        Texture2D = 0,
        Texture3D = 1,
        CubeMap = 2,
        Sprite = 3,
        UIBitmap = 4
    }

    public enum TextureFormat : short
    {
        A8 = 0,
        Y8 = 1,
        AY8 = 2,
        A8Y8 = 3,
        Unused4 = 4,
        Unused5 = 5,
        R5G6B5 = 6,
        Unused7 = 7,
        A1R5G5B5 = 8,
        A4R4G4B4 = 9,
        X8R8G8B8 = 10,
        A8R8G8B8 = 11,
        Unused12 = 12,
        Unused13 = 13,
        DXT1 = 14,
        DXT3 = 15,
        DXT5 = 16,
        P8_bump = 17,
        P8 = 18,
        ARGBFP32 = 19,
        RGBFP32 = 20,
        RGBFP16 = 21,
        U8V8 = 22,
        Unknown23 = 23,
        Unknown24 = 24,
        Unknown25 = 25,
        Unknown26 = 26,
        Unknown27 = 27,
        Unknown28 = 28,
        Unknown29 = 29,
        Unknown30 = 30,
        DXT5a = 31,
        Unknown32 = 32,
        DXN = 33,
        CTX1 = 34,
        DXT3a_alpha = 35,
        DXT3a_mono = 36,
        DXT5a_alpha = 37,
        DXT5a_mono = 38,
        DXN_mono_alpha = 39,
        Unknown40 = 40,
        Unknown41 = 41,
        Unknown42 = 42,
        Unknown43 = 43,
        Unknown44 = 44
    }
}
