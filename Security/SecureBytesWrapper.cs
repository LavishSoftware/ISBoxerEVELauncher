using System;
using System.Runtime.ConstrainedExecution;

namespace ISBoxerEVELauncher.Security
{
    /// <summary>
    /// This is an IDispoable wrapper for secure byte[] arrays, wiping pre-existing data when altered, Disposed or GC'd. It is designed similar to SecureStringWrapper for convenience.
    /// </summary>
    public sealed class SecureBytesWrapper : CriticalFinalizerObject, IDisposable
    {
        byte[] _Bytes;
        public byte[] Bytes
        {
            get
            {
                return _Bytes;
            }
            set
            {
                if (_disposed)
                    throw new ObjectDisposedException("SecureBytesWrapper");
                Destroy();
                _Bytes = value;
            }
        }

        /// <summary>
        /// Initialize without a byte array. "Bytes" can be set later.
        /// </summary>
        public SecureBytesWrapper()
        {
        }

        const string hexChars = "0123456789abcdef";
        static byte[] _hexTable;
        /// <summary>
        /// Initialize with byte array via the SecureStringWrapper, but possibly convert from hex string...
        /// </summary>
        /// <param name="ssw"></param>
        /// <param name="convertFromHex"></param>
        public SecureBytesWrapper(SecureStringWrapper ssw, bool convertFromHex)
        {
            if (!convertFromHex)
            {
                CopyBytes(ssw.ToByteArray());
                return;
            }
            if (_hexTable == null)
            {
                _hexTable = new byte[256];
                _hexTable['0'] = 0;
                _hexTable['1'] = 1;
                _hexTable['2'] = 2;
                _hexTable['3'] = 3;
                _hexTable['4'] = 4;
                _hexTable['5'] = 5;
                _hexTable['6'] = 6;
                _hexTable['7'] = 7;
                _hexTable['8'] = 8;
                _hexTable['9'] = 9;
                _hexTable['a'] = 10;
                _hexTable['b'] = 11;
                _hexTable['c'] = 12;
                _hexTable['d'] = 13;
                _hexTable['e'] = 14;
                _hexTable['f'] = 15;
            }

            using (SecureBytesWrapper temp = new SecureBytesWrapper(ssw, false))
            {
                _Bytes = new byte[temp.Bytes.Length / 2];
                for (int i = 0; i < temp.Bytes.Length; i += 2)
                {
                    byte b1 = temp.Bytes[i];
                    byte b2 = temp.Bytes[i + 1];
                    _Bytes[i / 2] = (byte)((_hexTable[b1] * 16) + _hexTable[b2]);
                }

            }
        }

        /// <summary>
        /// Duplicates the given byte array, instead of co-opting it.
        /// </summary>
        /// <param name="copyFrom"></param>
        public void CopyBytes(byte[] copyFrom)
        {
            Bytes = new byte[copyFrom.Length];
            System.Buffer.BlockCopy(copyFrom, 0, Bytes, 0, copyFrom.Length);
        }

        /// <summary>
        /// Determine if there are any bytes in the array...
        /// </summary>
        public bool HasData
        {
            get
            {
                return _Bytes != null && _Bytes.Length > 0;
            }
        }

        /// <summary>
        /// Purely for similarity to SecureStringWrapper
        /// </summary>
        /// <returns></returns>
        public byte[] ToByteArray()
        {
            return _Bytes;
        }

        private bool _disposed = false;

        public void Dispose()
        {
            if (!_disposed)
            {
                Destroy();
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }

        private void Destroy()
        {
            if (_Bytes == null)
            {
                return;
            }

            for (int i = 0; i < _Bytes.Length; i++)
            {
                _Bytes[i] = 0;
            }
            _Bytes = null;
        }

        ~SecureBytesWrapper()
        {
            Dispose();
        }
    }
}
