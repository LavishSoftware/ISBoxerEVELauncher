using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

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
                if (_bytes != null)
                    return _bytes.Length > 0;
                return secureString.Length > 0;
            }
        }

        const string hexChars = "0123456789abcdef";
        /// <summary>
        /// Encode an arbitrary byte array as a hexadecimal string, into a SecureString
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static SecureStringWrapper ConvertToHex(byte[] bytes)
        {
            using (System.Security.SecureString ss = new System.Security.SecureString())
            {
                using (SecureStringWrapper ssw = new SecureStringWrapper(ss))
                {
                    // convert to hex
                    for (int i = 0; i < bytes.Length; i++)
                    {
                        char c1 = hexChars[bytes[i] / 16];
                        char c2 = hexChars[bytes[i] % 16];
                        ss.AppendChar(c1);
                        ss.AppendChar(c2);

                    }
                    ss.MakeReadOnly();

                    return new SecureStringWrapper(ss.Copy());
                }
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

   

}
