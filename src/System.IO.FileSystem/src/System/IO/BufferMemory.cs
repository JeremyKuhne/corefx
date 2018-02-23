// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;

namespace System.IO
{
    internal struct BufferMemory<T> : IDisposable
    {
        T[] _buffer;
        ArraySegment<T> _segment;

        public BufferMemory(ReadOnlySpan<T> contents)
        {
            _buffer = default;
            _segment = default;
            EnsureCapacity(contents.Length);
            contents.CopyTo(this);
            SetSegment(0, contents.Length);
        }

        public void EnsureCapacity(int capacity)
        {
            if (_buffer == null)
            {
                _buffer = ArrayPool<T>.Shared.Rent(capacity);
            }
            else if(_buffer.Length < capacity)
            {
                ArrayPool<T>.Shared.Return(_buffer);
                _buffer = ArrayPool<T>.Shared.Rent(capacity);
            }
        }

        public void SetSegment(int offset, int count)
            => _segment = new ArraySegment<T>(_buffer, offset, count);

        public ArraySegment<T> Segment => _segment;

        public static implicit operator Span<T>(BufferMemory<T> buffer) => new Span<T>(buffer._buffer);

        public Span<T> Slice(int start, int length) => new Span<T>(_buffer, start, length);
        public Span<T> Slice(int start) => new Span<T>(_buffer, start, _buffer.Length - start);

        public T this[int index]
        {
            get => _buffer[index];
            set => _buffer[index] = value;
        }

        public void Dispose()
        {
            if (_buffer != null)
                ArrayPool<T>.Shared.Return(_buffer);
            _buffer = null;
        }
    }
}
