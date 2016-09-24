using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace ISBoxerEVELauncher
{
    /// <summary>
    /// This is an IDisposable wrapper for SecureString, which provides secure byte array access
    /// http://codereview.stackexchange.com/questions/107860/converting-a-securestring-to-a-byte-array
    /// </summary>
    
    public sealed class SecureStringWrapper : CriticalFinalizerObject, IDisposable
    {
        private readonly Encoding encoding;
        private readonly SecureString secureString;
        private byte[] _bytes = null;
        public SecureStringWrapper(SecureString secureString)
            : this(secureString, Encoding.UTF8)
        { }

        public SecureStringWrapper(SecureString secureString, Encoding encoding)
    {
        if (secureString == null)
        {
            throw new ArgumentNullException("secureString");
        }

        this.encoding = encoding ?? Encoding.UTF8;
        this.secureString = secureString;
    }

        public bool HasData
        {
            get
            {
                return _bytes != null && _bytes.Length > 0;
            }
        }

        public unsafe byte[] ToByteArray()
        {
            if (_bytes != null)
                return _bytes;

            int maxLength = encoding.GetMaxByteCount(secureString.Length);

            IntPtr bytes = IntPtr.Zero;
            IntPtr str = IntPtr.Zero;

            try
            {
                bytes = Marshal.AllocHGlobal(maxLength);
                str = Marshal.SecureStringToBSTR(secureString);

                char* chars = (char*)str.ToPointer();
                byte* bptr = (byte*)bytes.ToPointer();
                int len = encoding.GetBytes(chars, secureString.Length, bptr, maxLength);

                _bytes = new byte[len];
                for (int i = 0; i < len; ++i)
                {
                    _bytes[i] = *bptr;
                    bptr++;
                }

                return _bytes;
            }
            finally
            {
                if (bytes != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(bytes);
                }
                if (str != IntPtr.Zero)
                {
                    Marshal.ZeroFreeBSTR(str);
                }
            }
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
            if (_bytes == null) { return; }

            for (int i = 0; i < _bytes.Length; i++)
            {
                _bytes[i] = 0;
            }
            _bytes = null;
        }

        ~SecureStringWrapper()
        {
            Dispose();
        }
    }

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
            if (_Bytes == null) { return; }

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
