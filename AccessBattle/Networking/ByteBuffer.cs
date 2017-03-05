using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccessBattle.Networking
{
    public class ByteBuffer
    {
        int _start, _next, _length;
        byte[] _buffer;

        public ByteBuffer(int capacity)
        {
            if (capacity < 1) throw new ArgumentException("Capacity must be at least 1");
            _buffer = new byte[capacity];
            _start = 0;
            _next = 0;
            _length = 0;
        }

        public int Capacity { get { return _buffer.Length; } }
        public int Length
        {
            get { return _length; }
        }

        public void Clear()
        {
            _start = 0;
            _next = 0;
            _length = 0;
        }

        public bool Add(byte[] data)
        {
            return Add(data, 0, data.Length);
        }

        public bool Add(byte[] data, int index, int length)
        {
            // Sanity check
            if (index + length > data.Length)
                return false;
            // Check if enough space is left
            if (_buffer.Length - _length < length)
                return false;

            var spaceAtTheEnd = _buffer.Length - _next;
            _length += length;

            // Overflow required?
            if (_start < _next && spaceAtTheEnd < length)
            {
                Array.Copy(data, index, _buffer, _next, spaceAtTheEnd);
                var remaining = length - spaceAtTheEnd;
                Array.Copy(data, index + spaceAtTheEnd, _buffer, 0, remaining);
                _next = remaining;                
                return true;
            }
            Array.Copy(data, index, _buffer, _next, length);
            _next = _next + length;
            _next %= _buffer.Length;
            return true;
        }

        public bool Add(byte data)
        {
            return Add(new byte[] { data }, 0, 1);
        }

        public bool Take(int length, out byte[] data)
        {
            if (length > _length || length <= 0)
            {
                data = null;
                return false;
            }
            _length -= length;
            data = new byte[length];
            var bytesToEnd = _buffer.Length - _start;
            // Overflow required?
            if (bytesToEnd < length)
            {
                Array.Copy(_buffer, _start, data, 0, bytesToEnd);
                _start = length - bytesToEnd;
                Array.Copy(_buffer, 0, data, bytesToEnd, _start);
                if (_length == 0) Clear();
                return true;
            }
            Array.Copy(_buffer, _start, data, 0, length);
            _start += length;
            _start %= _buffer.Length;
            //if (_length == 0) Clear();
            return true;
        }
    }
}
