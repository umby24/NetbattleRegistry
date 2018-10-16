using System;
using System.Linq;
using System.Text;
using System.Threading;

namespace Netbattle_Registry.Common
{
    public class ByteBuffer
    {
        public int Length {
            get {
                lock (_opLocker) {
                    return _buffer.Length;
                }
            }
        }

        private byte[] _buffer;
        private readonly object _opLocker = new object();

        public ByteBuffer() {
            _buffer = new byte[0];
        }

        /// <summary>
        /// This event is called any time data is added to the buffer.
        /// </summary>
        public event EmptyEventArgs DataAdded;

        #region Read
        public byte PeekByte() {
            lock (_opLocker) {
                return _buffer[0];
            }
        }

        public byte ReadByte() {
            lock (_opLocker) {
                WaitForData(1);
                byte value = _buffer[0];

                RemoveBytes(1);
                return value;
            }
        }

        public short ReadShort() {
            lock (_opLocker) {
                byte[] data = ReadBytes(2);
                RemoveBytes(2);
                Array.Reverse(data);
                return BitConverter.ToInt16(data, 0);
            }
        }

        public int ReadInt() {
            lock (_opLocker) {
                byte[] data = ReadBytes(4);
                RemoveBytes(4);
                Array.Reverse(data);
                return BitConverter.ToInt32(data, 0);
            }
        }

        public string ReadString(int length) {
            lock (_opLocker) {
                byte[] data = ReadBytes(length);
                RemoveBytes(length);
                return Encoding.ASCII.GetString(data).Trim();
            }
        }

        public byte[] ReadByteArray(int length) {
            lock (_opLocker) {
                byte[] data = ReadBytes(length);
                RemoveBytes(length);
                return data;
            }
        }

        #endregion
        #region Write

        public void WriteByte(byte value) {
            AddBytes(new[] { value });
        }


        public void WriteInt(int value) {
            byte[] data = BitConverter.GetBytes(value);
            Array.Reverse(data);
            AddBytes(data);
        }

        public void WriteString(string value) {
            byte[] data = Encoding.ASCII.GetBytes(value);
            AddBytes(data);
        }

        public void WriteAndPrefixString(string value) {
            byte[] data = Encoding.ASCII.GetBytes(value);
            byte[] encrypted = XorModule.XorEncrypt(data);
            var length = (byte)(encrypted.Length - 1); // -- Because netbattle logic..
            byte[] finalData = { length }; // -- Prepend the finished array with the length.
            finalData = finalData.Concat(encrypted).ToArray();
            AddBytes(finalData);
        }
        #endregion

        /// <summary>
        /// Waits for the buffer to have the asked for length.
        /// </summary>
        /// <param name="length"></param>
        private void WaitForData(int length) {
            while (_buffer.Length < length) {
                Thread.Sleep(0);
            }
        }

        #region Control

        public void Purge() {
            DataAdded?.Invoke();
        }

        /// <summary>
        /// Adds data to the backing buffer.
        /// </summary>
        /// <param name="data"></param>
        public void AddBytes(byte[] data) {
            lock (_opLocker) {
                if (_buffer.Length == 0) {
                    _buffer = data;
                    return;
                }

                int tempLength = _buffer.Length + data.Length;
                var tempBuff = new byte[tempLength];

                Buffer.BlockCopy(_buffer, 0, tempBuff, 0, _buffer.Length);
                Buffer.BlockCopy(data, 0, tempBuff, _buffer.Length, data.Length);

                _buffer = tempBuff;
            }
        }

        /// <summary>
        /// Retrieves all data from, and clears the buffer.
        /// </summary>
        /// <returns>All data contained in the buffer</returns>
        public byte[] GetAllBytes() {
            lock (_opLocker) {
                var myData = new byte[_buffer.Length];
                Buffer.BlockCopy(_buffer, 0, myData, 0, _buffer.Length);
                _buffer = new byte[0];
                return myData;
            }
        }

        /// <summary>
        /// Reads the given amount of bytes out of the buffer.
        /// </summary>
        /// <param name="amount">The number of bytes to read and return</param>
        /// <returns>Array of the bytes that were read.</returns>
        private byte[] ReadBytes(int amount) {
            var output = new byte[amount];
            WaitForData(amount);
            Buffer.BlockCopy(_buffer, 0, output, 0, amount);
            return output;
        }

        /// <summary>
        /// Removes bytes from the beginning of the buffer.
        /// </summary>
        /// <param name="length">The number of bytes to remove.</param>
        private void RemoveBytes(int length) {
            int tempLength = _buffer.Length - length;
            var tempBuff = new byte[tempLength];

            Buffer.BlockCopy(_buffer, length, tempBuff, 0, tempLength);

            _buffer = tempBuff;
        }

        #endregion
    }
}
