﻿using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reclaimer.IO
{
    /// <summary>
    /// Reads primitive and complex data types from a stream in a specific byte order and encoding.
    /// </summary>
    public partial class EndianReader : BinaryReader, IEndianStream
    {
        private readonly long virtualOrigin;
        private readonly Encoding encoding;

        /// <summary>
        /// Gets or sets the endianness used when reading from the stream.
        /// </summary>
        public ByteOrder ByteOrder { get; set; }

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <seealso cref="EndianReader"/> class
        /// based on the specified stream with the system byte order and using UTF-8 encoding.
        /// </summary>
        /// <param name="input">The input stream.</param>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ArgumentNullException"/>
        public EndianReader(Stream input)
            : this(input, BitConverter.IsLittleEndian ? ByteOrder.LittleEndian : ByteOrder.BigEndian, new UTF8Encoding(), false)
        { }

        /// <summary>
        /// Initializes a new instance of the <seealso cref="EndianReader"/> class
        /// based on the specified stream with the specified byte order and using UTF-8 encoding.
        /// </summary>
        /// <param name="input">The input stream.</param>
        /// <param name="byteOrder">The byte order of the stream.</param>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ArgumentNullException"/>
        public EndianReader(Stream input, ByteOrder byteOrder)
            : this(input, byteOrder, new UTF8Encoding(), false)
        { }

        /// <summary>
        /// Initializes a new instance of the <seealso cref="EndianReader"/> class
        /// based on the specified stream with the specified byte order using UTF-8 encoding, and optionally leaves the stream open.
        /// </summary>
        /// <param name="input">The input stream.</param>
        /// <param name="byteOrder">The byte order of the stream.</param>
        /// <param name="leaveOpen">true to leave the stream open after the EndianReader object is disposed; otherwise, false.</param>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ArgumentNullException"/>
        public EndianReader(Stream input, ByteOrder byteOrder, bool leaveOpen)
            : this(input, byteOrder, new UTF8Encoding(), leaveOpen)
        { }

        /// <summary>
        /// Initializes a new instance of the <seealso cref="EndianReader"/> class
        /// based on the specified stream with the specified byte order and character encoding.
        /// </summary>
        /// <param name="input">The input stream.</param>
        /// <param name="byteOrder">The byte order of the stream.</param>
        /// <param name="encoding">The character encoding to use.</param>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ArgumentNullException"/>
        public EndianReader(Stream input, ByteOrder byteOrder, Encoding encoding)
            : this(input, byteOrder, encoding, false)
        { }

        /// <summary>
        /// Initializes a new instance of the <seealso cref="EndianReader"/> class
        /// based on the specified stream with the specified byte order and character encoding, and optionally leaves the stream open.
        /// </summary>
        /// <param name="input">The input stream.</param>
        /// <param name="byteOrder">The byte order of the stream.</param>
        /// <param name="encoding">The character encoding to use.</param>
        /// <param name="leaveOpen">true to leave the stream open after the EndianReader object is disposed; otherwise, false.</param>
        /// <exception cref="ArgumentException"/>
        /// <exception cref="ArgumentNullException"/>
        public EndianReader(Stream input, ByteOrder byteOrder, Encoding encoding, bool leaveOpen)
            : base(input, encoding, leaveOpen)
        {
            virtualOrigin = 0;
            this.encoding = encoding;
            ByteOrder = byteOrder;
        }

        /// <summary>
        /// Creates a copy of <paramref name="parent"/> that will treat the specified origin as the the beginning of the stream.
        /// The resulting <seealso cref="EndianReader"/> will not close the underlying stream when it is closed.
        /// </summary>
        /// <param name="parent">The <seealso cref="EndianReader"/> instance to copy.</param>
        /// <param name="virtualOrigin">The position in the stream that will be treated as the beginning.</param>
        protected EndianReader(EndianReader parent, long virtualOrigin)
            : base(BaseStreamOrThrow(parent), EncodingOrThrow(parent), true)
        {
            this.virtualOrigin = virtualOrigin;
            encoding = parent.encoding;
            ByteOrder = parent.ByteOrder;
        }

        private static Stream BaseStreamOrThrow(EndianReader parent) => parent?.BaseStream ?? throw new ArgumentNullException(nameof(parent));

        private static Encoding EncodingOrThrow(EndianReader parent) => parent?.encoding ?? throw new ArgumentNullException(nameof(parent));

        #endregion

        #region Overrides

        /// <summary>
        /// Reads a 2-byte floating point value from the current stream using the current byte order
        /// and advances the current position of the stream by two bytes.
        /// </summary>
        /// <inheritdoc cref="ReadHalf(ByteOrder)"/>
        public override Half ReadHalf() => ReadHalf(ByteOrder);

        /// <summary>
        /// Reads a 4-byte floating point value from the current stream using the current byte order
        /// and advances the current position of the stream by four bytes.
        /// </summary>
        /// <inheritdoc cref="ReadSingle(ByteOrder)"/>
        public override float ReadSingle() => ReadSingle(ByteOrder);

        /// <summary>
        /// Reads an 8-byte floating point value from the current stream using the current byte order
        /// and advances the current position of the stream by eight bytes.
        /// </summary>
        /// <inheritdoc cref="ReadDouble(ByteOrder)"/>
        public override double ReadDouble() => ReadDouble(ByteOrder);

        /// <summary>
        /// Reads a decimal value from the current stream using the current byte order
        /// and advances the current position of the stream by sixteen bytes.
        /// </summary>
        /// <inheritdoc cref="ReadDecimal(ByteOrder)"/>
        public override decimal ReadDecimal() => ReadDecimal(ByteOrder);

        /// <summary>
        /// Reads a 2-byte signed integer from the current stream using the current byte order
        /// and advances the current position of the stream by two bytes.
        /// </summary>
        /// <inheritdoc cref="ReadInt16(ByteOrder)"/>
        public override short ReadInt16() => ReadInt16(ByteOrder);

        /// <summary>
        /// Reads a 4-byte signed integer from the current stream using the current byte order
        /// and advances the current position of the stream by four bytes.
        /// </summary>
        /// <inheritdoc cref="ReadInt32(ByteOrder)"/>
        public override int ReadInt32() => ReadInt32(ByteOrder);

        /// <summary>
        /// Reads an 8-byte signed integer from the current stream using the current byte order
        /// and advances the current position of the stream by eight bytes.
        /// </summary>
        /// <inheritdoc cref="ReadInt64(ByteOrder)"/>
        public override long ReadInt64() => ReadInt64(ByteOrder);

        /// <summary>
        /// Reads a 2-byte unsigned integer from the current stream using the current byte order
        /// and advances the current position of the stream by two bytes.
        /// </summary>
        /// <inheritdoc cref="ReadUInt16(ByteOrder)"/>
        public override ushort ReadUInt16() => ReadUInt16(ByteOrder);

        /// <summary>
        /// Reads a 4-byte unsigned integer from the current stream using the current byte order
        /// and advances the current position of the stream by four bytes.
        /// </summary>
        /// <inheritdoc cref="ReadUInt32(ByteOrder)"/>
        public override uint ReadUInt32() => ReadUInt32(ByteOrder);

        /// <summary>
        /// Reads an 8-byte unsigned integer from the current stream using the current byte order
        /// and advances the current position of the stream by eight bytes.
        /// </summary>
        /// <inheritdoc cref="ReadUInt64(ByteOrder)"/>
        public override ulong ReadUInt64() => ReadUInt64(ByteOrder);

        /// <summary>
        /// Reads a length-prefixed string from the current stream using the current byte order
        /// and encoding of the <seealso cref="EndianReader"/>.
        /// </summary>
        /// <inheritdoc cref="ReadString(ByteOrder)"/>
        public override string ReadString() => ReadString(ByteOrder);

        /// <summary>
        /// Reads a globally unique identifier from the current stream using the current byte order
        /// and advances the current position of the stream by sixteen bytes.
        /// </summary>
        /// <inheritdoc cref="ReadGuid(ByteOrder)"/>
        public virtual Guid ReadGuid() => ReadGuid(ByteOrder);

        #endregion

        #region ByteOrder Read

        /// <summary>
        /// Reads a 2-byte floating-point value from the current stream using the specified byte order
        /// and advances the current position of the stream by two bytes.
        /// </summary>
        /// <inheritdoc cref="BinaryReader.ReadHalf"/>
        /// <inheritdoc cref="ReadInt32(ByteOrder)"/>
        public virtual Half ReadHalf(ByteOrder byteOrder)
        {
            if (byteOrder == ByteOrder.LittleEndian)
                return base.ReadHalf();

            var bytes = base.ReadBytes(2);
            Array.Reverse(bytes);
            return BitConverter.ToHalf(bytes, 0);
        }

        /// <summary>
        /// Reads a 4-byte floating-point value from the current stream using the specified byte order
        /// and advances the current position of the stream by four bytes.
        /// </summary>
        /// <inheritdoc cref="BinaryReader.ReadSingle"/>
        /// <inheritdoc cref="ReadInt32(ByteOrder)"/>
        public virtual float ReadSingle(ByteOrder byteOrder)
        {
            if (byteOrder == ByteOrder.LittleEndian)
                return base.ReadSingle();

            var bytes = base.ReadBytes(4);
            Array.Reverse(bytes);
            return BitConverter.ToSingle(bytes, 0);
        }

        /// <summary>
        /// Reads an 8-byte floating-point value from the current stream using the specified byte order
        /// and advances the current position of the stream by eight bytes.
        /// </summary>
        /// <inheritdoc cref="BinaryReader.ReadDouble"/>
        /// <inheritdoc cref="ReadInt32(ByteOrder)"/>
        public virtual double ReadDouble(ByteOrder byteOrder)
        {
            if (byteOrder == ByteOrder.LittleEndian)
                return base.ReadDouble();

            var bytes = base.ReadBytes(8);
            Array.Reverse(bytes);
            return BitConverter.ToDouble(bytes, 0);
        }

        /// <summary>
        /// Reads a decimal value from the current stream using the specified byte order
        /// and advances the current position of the stream by sixteen bytes.
        /// </summary>
        /// <inheritdoc cref="BinaryReader.ReadDecimal"/>
        /// <inheritdoc cref="ReadInt32(ByteOrder)"/>
        public virtual decimal ReadDecimal(ByteOrder byteOrder)
        {
            if (byteOrder == ByteOrder.LittleEndian)
                return base.ReadDecimal();

            var bits = new int[4];
            var bytes = base.ReadBytes(16);
            Array.Reverse(bytes);
            for (var i = 0; i < 4; i++)
                bits[i] = BitConverter.ToInt32(bytes, i * 4);
            return new decimal(bits);
        }

        /// <summary>
        /// Reads a 2-byte signed integer from the current stream using the specified byte order
        /// and advances the current position of the stream by two bytes.
        /// </summary>
        /// <inheritdoc cref="BinaryReader.ReadInt16"/>
        /// <inheritdoc cref="ReadInt32(ByteOrder)"/>
        public virtual short ReadInt16(ByteOrder byteOrder) => byteOrder == ByteOrder.LittleEndian ? base.ReadInt16() : BinaryPrimitives.ReverseEndianness(base.ReadInt16());

        /// <summary>
        /// Reads a 4-byte signed integer from the current stream using the specified byte order
        /// and advances the current position of the stream by four bytes.
        /// </summary>
        /// <param name="byteOrder">The byte order to use.</param>
        /// <inheritdoc cref="BinaryReader.ReadInt32"/>
        public virtual int ReadInt32(ByteOrder byteOrder) => byteOrder == ByteOrder.LittleEndian ? base.ReadInt32() : BinaryPrimitives.ReverseEndianness(base.ReadInt32());

        /// <summary>
        /// Reads an 8-byte signed integer from the current stream using the specified byte order
        /// and advances the current position of the stream by eight bytes.
        /// </summary>
        /// <inheritdoc cref="BinaryReader.ReadInt64"/>
        /// <inheritdoc cref="ReadInt32(ByteOrder)"/>
        public virtual long ReadInt64(ByteOrder byteOrder) => byteOrder == ByteOrder.LittleEndian ? base.ReadInt64() : BinaryPrimitives.ReverseEndianness(base.ReadInt64());

        /// <summary>
        /// Reads a 2-byte unsigned integer from the current stream using the specified byte order
        /// and advances the current position of the stream by two bytes.
        /// </summary>
        /// <inheritdoc cref="BinaryReader.ReadUInt16"/>
        /// <inheritdoc cref="ReadInt32(ByteOrder)"/>
        public virtual ushort ReadUInt16(ByteOrder byteOrder) => byteOrder == ByteOrder.LittleEndian ? base.ReadUInt16() : BinaryPrimitives.ReverseEndianness(base.ReadUInt16());

        /// <summary>
        /// Reads a 4-byte unsigned integer from the current stream using the specified byte order
        /// and advances the current position of the stream by four bytes.
        /// </summary>
        /// <inheritdoc cref="BinaryReader.ReadUInt32"/>
        /// <inheritdoc cref="ReadInt32(ByteOrder)"/>
        public virtual uint ReadUInt32(ByteOrder byteOrder) => byteOrder == ByteOrder.LittleEndian ? base.ReadUInt32() : BinaryPrimitives.ReverseEndianness(base.ReadUInt32());

        /// <summary>
        /// Reads an 8-byte unsigned integer from the current stream using the specified byte order
        /// and advances the current position of the stream by eight bytes.
        /// </summary>
        /// <inheritdoc cref="BinaryReader.ReadUInt64"/>
        /// <inheritdoc cref="ReadInt32(ByteOrder)"/>
        public virtual ulong ReadUInt64(ByteOrder byteOrder) => byteOrder == ByteOrder.LittleEndian ? base.ReadUInt64() : BinaryPrimitives.ReverseEndianness(base.ReadUInt64());

        /// <summary>
        /// Reads a globally unique identifier from the current stream using the specified byte order
        /// and advances the current position of the stream by sixteen bytes.
        /// </summary>
        /// <inheritdoc cref="ReadInt32(ByteOrder)"/>
        public virtual Guid ReadGuid(ByteOrder byteOrder)
        {
            var a = ReadInt32(byteOrder);
            var b = ReadInt16(byteOrder);
            var c = ReadInt16(byteOrder);
            var d = ReadBytes(8);

            return new Guid(a, b, c, d);
        }

        #endregion

        #region String Read

        /// <summary>
        /// Reads a length-prefixed string from the current stream using the specified byte order
        /// and the current encoding of the <seealso cref="EndianWriter"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"/>
        /// <exception cref="IOException"/>
        /// <exception cref="ObjectDisposedException"/>
        public virtual string ReadString(ByteOrder byteOrder)
        {
            var length = ReadInt32(byteOrder);

            if (length == 0)
                return string.Empty;

            return encoding.GetString(ReadBytes(length));
        }

        /// <summary>
        /// Reads a fixed-length string from the current stream, and optionally removes trailing white-space characters.
        /// </summary>
        /// <param name="length">The length of the string, in bytes.</param>
        /// <param name="trim">true to remove trailing white-space characters; otherwise, false.</param>
        /// <exception cref="ArgumentOutOfRangeException"/>
        /// <exception cref="IOException"/>
        /// <exception cref="ObjectDisposedException"/>
        public virtual string ReadString(int length, bool trim)
        {
            if (length < 0)
                throw Exceptions.ParamMustBeNonNegative(nameof(length), length);

            if (length == 0)
                return string.Empty;

            var result = encoding.GetString(ReadBytes(length));
            return trim ? result.TrimEnd() : result;
        }

        /// <summary>
        /// Reads a variable-length string from the current stream.
        /// The position of the stream is advanced to the position after the next occurence of a null character.
        /// </summary>
        /// <exception cref="EndOfStreamException"/>
        /// <exception cref="IOException"/>
        /// <exception cref="ObjectDisposedException"/>
        public virtual string ReadNullTerminatedString()
        {
            var bytes = new List<byte>();

            byte val;
            while (BaseStream.Position < BaseStream.Length && (val = ReadByte()) != 0)
                bytes.Add(val);

            return encoding.GetString(bytes.ToArray());
        }

        /// <summary>
        /// Reads a variable-length string from the current stream.
        /// The length of the string is determined by the first occurence of a null character.
        /// <para/> The position of the stream is advanced by the specified number of bytes, regardless of the resulting string length.
        /// </summary>
        /// <param name="maxLength">The maximum length of the string, in bytes.</param>
        /// <exception cref="ArgumentOutOfRangeException"/>
        /// <exception cref="IOException"/>
        /// <exception cref="ObjectDisposedException"/>
        public virtual string ReadNullTerminatedString(int maxLength)
        {
            if (maxLength < 0)
                throw Exceptions.ParamMustBeNonNegative(nameof(maxLength), maxLength);

            if (maxLength == 0)
                return string.Empty;

            var value = encoding.GetString(base.ReadBytes(maxLength));

            if (!value.Contains('\0'))
                return value;

            return value.Substring(0, value.IndexOf('\0'));
        }

        #endregion

        #region Peek

        /// <summary>
        /// Reads a 2-byte floating point value from the current stream using the current byte order
        /// and does not advance the current position of the stream.
        /// </summary>
        /// <inheritdoc cref="PeekHalf(ByteOrder)"/>
        public virtual Half PeekHalf() => PeekHalf(ByteOrder);

        /// <summary>
        /// Reads a 4-byte floating point value from the current stream using the current byte order
        /// and does not advance the current position of the stream.
        /// </summary>
        /// <inheritdoc cref="PeekSingle(ByteOrder)"/>
        public virtual float PeekSingle() => PeekSingle(ByteOrder);

        /// <summary>
        /// Reads an 8-byte floating point value from the current stream using the current byte order
        /// and does not advance the current position of the stream.
        /// </summary>
        /// <inheritdoc cref="PeekDouble(ByteOrder)"/>
        public virtual double PeekDouble() => PeekDouble(ByteOrder);

        /// <summary>
        /// Reads a decimal value from the current stream using the current byte order
        /// and does not advance the current position of the stream.
        /// </summary>
        /// <inheritdoc cref="PeekDecimal(ByteOrder)"/>
        public virtual decimal PeekDecimal() => PeekDecimal(ByteOrder);

        /// <summary>
        /// Reads a 2-byte signed integer from the current stream using the current byte order
        /// and does not advance the current position of the stream.
        /// </summary>
        /// <inheritdoc cref="PeekInt16(ByteOrder)"/>
        public virtual short PeekInt16() => PeekInt16(ByteOrder);

        /// <summary>
        /// Reads a 4-byte signed integer from the current stream using the current byte order
        /// and does not advance the current position of the stream.
        /// </summary>
        /// <inheritdoc cref="PeekInt32(ByteOrder)"/>
        public virtual int PeekInt32() => PeekInt32(ByteOrder);

        /// <summary>
        /// Reads an 8-byte signed integer from the current stream using the current byte order
        /// and does not advance the current position of the stream.
        /// </summary>
        /// <inheritdoc cref="PeekInt64(ByteOrder)"/>
        public virtual long PeekInt64() => PeekInt64(ByteOrder);

        /// <summary>
        /// Reads a 2-byte unsigned integer from the current stream using the current byte order
        /// and does not advance the current position of the stream.
        /// </summary>
        /// <inheritdoc cref="PeekUInt16(ByteOrder)"/>
        public virtual ushort PeekUInt16() => PeekUInt16(ByteOrder);

        /// <summary>
        /// Reads a 4-byte unsigned integer from the current stream using the current byte order
        /// and does not advance the current position of the stream.
        /// </summary>
        /// <inheritdoc cref="PeekUInt32(ByteOrder)"/>
        public virtual uint PeekUInt32() => PeekUInt32(ByteOrder);

        /// <summary>
        /// Reads an 8-byte unsigned integer from the current stream using the current byte order
        /// and does not advance the current position of the stream.
        /// </summary>
        /// <inheritdoc cref="PeekUInt64(ByteOrder)"/>
        public virtual ulong PeekUInt64() => PeekUInt64(ByteOrder);

        /// <summary>
        /// Reads a globally unique identifier from the current stream using the specified byte order
        /// and does not advance the current position of the stream.
        /// </summary>
        /// <inheritdoc cref="PeekGuid(ByteOrder)"/>
        public virtual Guid PeekGuid() => PeekGuid(ByteOrder);

        #endregion

        #region ByteOrder Peek

        /// <summary>
        /// Reads a 2-byte floating point value from the current stream using the specified byte order
        /// and does not advance the current position of the stream.
        /// </summary>
        /// <inheritdoc cref="ReadHalf(ByteOrder)"/>
        public virtual Half PeekHalf(ByteOrder byteOrder)
        {
            var origin = BaseStream.Position;
            var value = ReadHalf(byteOrder);
            BaseStream.Position = origin;
            return value;
        }

        /// <summary>
        /// Reads a 4-byte floating point value from the current stream using the specified byte order
        /// and does not advance the current position of the stream.
        /// </summary>
        /// <inheritdoc cref="ReadSingle(ByteOrder)"/>
        public virtual float PeekSingle(ByteOrder byteOrder)
        {
            var origin = BaseStream.Position;
            var value = ReadSingle(byteOrder);
            BaseStream.Position = origin;
            return value;
        }

        /// <summary>
        /// Reads an 8-byte floating point value from the current stream using the specified byte order
        /// and does not advance the current position of the stream.
        /// </summary>
        /// <inheritdoc cref="ReadDouble(ByteOrder)"/>
        public virtual double PeekDouble(ByteOrder byteOrder)
        {
            var origin = BaseStream.Position;
            var value = ReadDouble(byteOrder);
            BaseStream.Position = origin;
            return value;
        }

        /// <summary>
        /// Reads a decimal value from the current stream using the specified byte order
        /// and does not advance the current position of the stream.
        /// </summary>
        /// <inheritdoc cref="ReadDecimal(ByteOrder)"/>
        public virtual decimal PeekDecimal(ByteOrder byteOrder)
        {
            var origin = BaseStream.Position;
            var value = ReadDecimal(byteOrder);
            BaseStream.Position = origin;
            return value;
        }

        /// <summary>
        /// Reads a 2-byte signed integer from the current stream using the specified byte order
        /// and does not advance the current position of the stream.
        /// </summary>
        /// <inheritdoc cref="ReadInt16(ByteOrder)"/>
        public virtual short PeekInt16(ByteOrder byteOrder)
        {
            var origin = BaseStream.Position;
            var value = ReadInt16(byteOrder);
            BaseStream.Position = origin;
            return value;
        }

        /// <summary>
        /// Reads a 4-byte signed integer from the current stream using the specified byte order
        /// and does not advance the current position of the stream.
        /// </summary>
        /// <inheritdoc cref="ReadInt32(ByteOrder)"/>
        public virtual int PeekInt32(ByteOrder byteOrder)
        {
            var origin = BaseStream.Position;
            var value = ReadInt32(byteOrder);
            BaseStream.Position = origin;
            return value;
        }

        /// <summary>
        /// Reads an 8-byte signed integer from the current stream using the specified byte order
        /// and does not advance the current position of the stream.
        /// </summary>
        /// <inheritdoc cref="ReadInt64(ByteOrder)"/>
        public virtual long PeekInt64(ByteOrder byteOrder)
        {
            var origin = BaseStream.Position;
            var value = ReadInt64(byteOrder);
            BaseStream.Position = origin;
            return value;
        }

        /// <summary>
        /// Reads a 2-byte unsigned integer from the current stream using the specified byte order
        /// and does not advance the current position of the stream.
        /// </summary>
        /// <inheritdoc cref="ReadUInt16(ByteOrder)"/>
        public virtual ushort PeekUInt16(ByteOrder byteOrder)
        {
            var origin = BaseStream.Position;
            var value = ReadUInt16(byteOrder);
            BaseStream.Position = origin;
            return value;
        }

        /// <summary>
        /// Reads a 4-byte unsigned integer from the current stream using the specified byte order
        /// and does not advance the current position of the stream.
        /// </summary>
        /// <inheritdoc cref="ReadUInt32(ByteOrder)"/>
        public virtual uint PeekUInt32(ByteOrder byteOrder)
        {
            var origin = BaseStream.Position;
            var value = ReadUInt32(byteOrder);
            BaseStream.Position = origin;
            return value;
        }

        /// <summary>
        /// Reads an 8-byte unsigned integer from the current stream using the specified byte order
        /// and does not advance the current position of the stream.
        /// </summary>
        /// <inheritdoc cref="ReadUInt64(ByteOrder)"/>
        public virtual ulong PeekUInt64(ByteOrder byteOrder)
        {
            var origin = BaseStream.Position;
            var value = ReadUInt64(byteOrder);
            BaseStream.Position = origin;
            return value;
        }

        /// <summary>
        /// Reads a globally unique identifier from the current stream using the specified byte order
        /// and does not advance the current position of the stream.
        /// </summary>
        /// <inheritdoc cref="ReadGuid(ByteOrder)"/>
        public virtual Guid PeekGuid(ByteOrder byteOrder)
        {
            var origin = BaseStream.Position;
            var value = ReadGuid(byteOrder);
            BaseStream.Position = origin;
            return value;
        }

        #endregion

        #region Other

        /// <summary>
        /// Gets the position of the base stream.
        /// If the current instance was created using <see cref="CreateVirtualReader"/>
        /// the position returned will be relative to the virtual origin.
        /// </summary>
        public long Position => BaseStream.Position - virtualOrigin;

        /// <summary>
        /// Sets the position of the underlying stream relative to a given origin.
        /// </summary>
        /// <param name="offset">A byte offest relative to the origin parameter.</param>
        /// <param name="origin">A value of type SeekOrigin indicating the reference point used to obtain the new position.</param>
        /// <exception cref="IOException"/>
        /// <exception cref="NotSupportedException"/>
        /// <exception cref="ObjectDisposedException"/>
        public void Seek(long offset, SeekOrigin origin)
        {
            long address = 0;
            switch (origin)
            {
                case SeekOrigin.Begin:
                    address = virtualOrigin + offset;
                    break;
                case SeekOrigin.Current:
                    address = BaseStream.Position + offset;
                    break;
                case SeekOrigin.End:
                    address = BaseStream.Length + offset;
                    break;
            }

            SeekAbsolute(address);
        }

        private void SeekAbsolute(long address)
        {
            if (BaseStream.Position != address)
                BaseStream.Position = address;
        }

        /// <summary>
        /// Creates an <seealso cref="EndianReader"/> based on the same stream
        /// with the same byte order and encoding that will treat the current position
        /// as the beginning of the stream and will not dispose of the underlying stream when it is closed.
        /// </summary>
        public virtual EndianReader CreateVirtualReader() => CreateVirtualReader(BaseStream.Position);

        /// <summary>
        /// Creates an <seealso cref="EndianReader"/> based on the same stream
        /// with the same byte order and encoding that will treat the specified offset
        /// as the beginning of the stream and will not dispose of the underlying stream when it is closed.
        /// </summary>
        /// <param name="origin">The position in the stream that will be treated as the beginning.</param>
        /// <exception cref="ArgumentOutOfRangeException"/>
        public virtual EndianReader CreateVirtualReader(long origin)
        {
            if (origin < 0 || origin > BaseStream.Length)
                throw Exceptions.OutOfStreamBounds(nameof(origin), origin);

            return new EndianReader(this, origin);
        }

        /// <summary>
        /// Calls and returns the value of <seealso cref="ReadObject{T}"/> until the specified number of objects have been read or the end of the stream has been reached.
        /// </summary>
        /// <inheritdoc cref="ReadEnumerable{T}(int, double)"/>
        public IEnumerable<T> ReadEnumerable<T>(int count)
        {
            if (count < 0)
                throw Exceptions.ParamMustBeNonNegative(nameof(count), count);

            int i = 0;
            while (i++ < count && BaseStream.Position < BaseStream.Length)
                yield return ReadObject<T>();
        }

        /// <summary>
        /// Calls and returns the value of <seealso cref="ReadObject{T}(double)"/> until either the specified number of objects have been read or the end of the stream has been reached.
        /// </summary>
        /// <typeparam name="T">The type of object to read.</typeparam>
        /// <param name="count">The maximum number of objects to read.</param>
        /// <param name="version">The version of the type to read.</param>
        /// <inheritdoc cref="ReadObject{T}(double)"/>
        public IEnumerable<T> ReadEnumerable<T>(int count, double version)
        {
            if (count < 0)
                throw Exceptions.ParamMustBeNonNegative(nameof(count), count);

            int i = 0;
            while (i++ < count && BaseStream.Position < BaseStream.Length)
                yield return ReadObject<T>(version);
        }

        #endregion
    }
}
