﻿using System.Collections;

namespace Reclaimer.IO
{
    public abstract class DataBuffer<T> : IDataBuffer, IReadOnlyList<T>
        where T : struct
    {
        protected readonly byte[] buffer;
        protected readonly int start;
        protected readonly int stride;
        protected readonly int offset;

        protected abstract int SizeOf { get; }

        public int Count { get; }

        protected DataBuffer(byte[] buffer, int count, int start, int stride, int offset)
        {
            this.buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));

            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            if (start < 0 || (start > 0 && start >= buffer.Length))
                throw new ArgumentOutOfRangeException(nameof(start));

            if (stride < 0 || (buffer.Length > 0 && stride > buffer.Length))
                throw new ArgumentOutOfRangeException(nameof(stride));

            if (offset < 0 || offset + SizeOf > stride)
                throw new ArgumentOutOfRangeException(nameof(offset));

            if (buffer.Length < start + stride * count)
                throw new ArgumentException("Insufficient buffer length", nameof(buffer));

            Count = count;
            this.start = start;
            this.stride = stride;
            this.offset = offset;
        }

        protected Span<byte> CreateSpan(int index) => buffer.AsSpan(start + index * stride + offset, SizeOf);

        public abstract T this[int index] { get; set; }

        public T this[Index index]
        {
            get => this[index.GetOffset(Count)];
            set => this[index.GetOffset(Count)] = value;
        }

        public IEnumerable<T> this[Range range] => Subset(range);

        public IEnumerable<T> Subset(Range range) => Extensions.GetRange(this, range);
        public IEnumerable<T> Subset(int index, int length) => Extensions.GetSubset(this, index, length);

        #region IDataBuffer
        Type IDataBuffer.DataType => typeof(T);
        int IDataBuffer.Count => Count;
        int IDataBuffer.SizeOf => SizeOf;
        ReadOnlySpan<byte> IDataBuffer.Buffer => buffer;
        int IDataBuffer.Start => start;
        int IDataBuffer.Stride => stride;
        int IDataBuffer.Offset => offset;
        ReadOnlySpan<byte> IDataBuffer.GetBytes(int index) => CreateSpan(index);
        #endregion

        #region IEnumerable
        protected IEnumerable<T> Enumerate()
        {
            for (var i = 0; i < Count; i++)
                yield return this[i];
        }

        public IEnumerator<T> GetEnumerator() => Enumerate().GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Enumerate()).GetEnumerator();
        #endregion
    }
}
