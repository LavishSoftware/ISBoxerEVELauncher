using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace ISBoxerEVELauncher.Security
{
    /// <summary>
    /// System for securely transmitting Master Key between ISBoxer EVE Launcher instances
    /// </summary>
    public class KeyTransmitter
    {
        public class Session : IDisposable
        {
            public Session(IntPtr window, System.Diagnostics.Process process)
            {
                RemoteWindow = window;
                Process = process;

                process.Exited += process_Exited;

                DH = new ECDiffieHellmanCng();

                DH.KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash;
                DH.HashAlgorithm = CngAlgorithm.Sha256;
                LocalPublicKey = new SecureBytesWrapper() { Bytes = DH.PublicKey.ToByteArray() };

                // this is generated using the remote public key, which we dont have yet
                LocalPrivateKey = new SecureBytesWrapper();
            }

            void process_Exited(object sender, EventArgs e)
            {
                // session terminated
                KeyTransmitter.Sessions.Remove(this);
                this.Dispose();
            }

            ECDiffieHellmanCng DH { get; set; }
           
            SecureBytesWrapper LocalPublicKey { get; set; }
            SecureBytesWrapper LocalPrivateKey { get; set; }

            public IntPtr RemoteWindow { get; set; }
            public System.Diagnostics.Process Process { get; set; }

            public void SetRemotePublicKey(byte[] bytes)
            {
                using (CngKey remotePublicKey = CngKey.Import(bytes, CngKeyBlobFormat.EccPublicBlob))
                {
                    LocalPrivateKey.CopyBytes(DH.DeriveKeyMaterial(remotePublicKey));
                }
            }

            public void Encrypt(byte[] secretMessage, out byte[] encryptedMessage, out byte[] iv)
            {
                using (Aes aes = new AesCryptoServiceProvider())
                {
                    aes.Key = LocalPrivateKey.Bytes;
                    iv = aes.IV;

                    // Encrypt the message
                    using (MemoryStream ciphertext = new MemoryStream())
                    {
                        using (CryptoStream cs = new CryptoStream(ciphertext, aes.CreateEncryptor(), CryptoStreamMode.Write))
                        {
                            cs.Write(secretMessage, 0, secretMessage.Length);
                            cs.Close();
                            encryptedMessage = ciphertext.ToArray();
                        }
                    }
                }
            }

            public void Decrypt(byte[] encryptedMessage, byte[] iv, SecureBytesWrapper decryptedMessage)
            {
                using (Aes aes = new AesCryptoServiceProvider())
                {
                    aes.Key = LocalPrivateKey.Bytes;
                    aes.IV = iv;

                    // Encrypt the message
                    using (MemoryStream ciphertext = new MemoryStream())
                    {
                        using (CryptoStream cs = new CryptoStream(ciphertext, aes.CreateDecryptor(), CryptoStreamMode.Write))
                        {
                            cs.Write(encryptedMessage, 0, encryptedMessage.Length);
                            cs.Close();
                            decryptedMessage.CopyBytes(ciphertext.ToArray());
                        }
                    }
                }
            }

            public void ReceivePublicKeyTransmission(Windows.COPYDATASTRUCT cds)
            {
                byte[] remotePublicKey = new byte[cds.cbData];
                Marshal.Copy(cds.lpData, remotePublicKey, 0, cds.cbData);

                SetRemotePublicKey(remotePublicKey);
            }

            public void ReceiveEncryptedMasterKeyTransmission(Windows.COPYDATASTRUCT cds)
            {
                byte[] combinedMessage = new byte[cds.cbData];
                Marshal.Copy(cds.lpData, combinedMessage, 0, cds.cbData);

                int ivLength = BitConverter.ToInt32(combinedMessage, 0);
                // validate length...
                if (ivLength > cds.cbData)
                {
                    // throw exception .. ?
                    throw new ApplicationException("Master Key transmission failed: Initialization Vector received incorrectly");
                }
                byte[] iv = new byte[ivLength];
                Buffer.BlockCopy(combinedMessage, sizeof(int), iv, 0, ivLength);

                byte[] encryptedMessage = new byte[cds.cbData - (ivLength + sizeof(int)) ];
                Buffer.BlockCopy(combinedMessage, sizeof(int)+ivLength, encryptedMessage, 0, encryptedMessage.Length);

                using (SecureBytesWrapper sbw = new SecureBytesWrapper())
                {
                    Decrypt(encryptedMessage, iv, sbw);
                    if (!App.Settings.TryPasswordMasterKey(sbw.Bytes))
                    {
                        throw new ApplicationException("Master Key transmission failed: Master Key received incorrectly");
                    }
                }
            }

            Windows.COPYDATASTRUCT GetPublicKeyTransmission(bool isRequest)
            {
                Windows.COPYDATASTRUCT cds = new Windows.COPYDATASTRUCT();
                cds.cbData = LocalPublicKey.Bytes.Length;
                cds.lpData = Marshal.AllocHGlobal(cds.cbData);
                Marshal.Copy(LocalPublicKey.Bytes, 0, cds.lpData, LocalPublicKey.Bytes.Length);
                if (isRequest)
                    cds.dwData = new IntPtr(10);
                else
                    cds.dwData = new IntPtr(12);
                // caller needs to Marshal.FreeHGlobal(cds.lpData);
                return cds;
            }

            Windows.COPYDATASTRUCT GetEncryptedMasterKeyTransmission()
            {
                byte[] encryptedMessage;
                byte[] iv;

                using (SecureBytesWrapper sbwKey = new SecureBytesWrapper(App.Settings.PasswordMasterKey,true))
                {
                    Encrypt(sbwKey.Bytes, out encryptedMessage, out iv);
                }
                Windows.COPYDATASTRUCT cds = new Windows.COPYDATASTRUCT();
                cds.cbData = sizeof(int) + iv.Length + encryptedMessage.Length;

                byte[] combinedMessage = new byte[cds.cbData];
                byte[] lengthBytes = BitConverter.GetBytes(iv.Length);

                Buffer.BlockCopy(lengthBytes, 0, combinedMessage, 0, lengthBytes.Length);
                Buffer.BlockCopy(iv, 0, combinedMessage, lengthBytes.Length, iv.Length);
                Buffer.BlockCopy(encryptedMessage, 0, combinedMessage, lengthBytes.Length + iv.Length, encryptedMessage.Length);

                cds.lpData = Marshal.AllocHGlobal(cds.cbData);
                Marshal.Copy(combinedMessage, 0, cds.lpData, combinedMessage.Length);
                cds.dwData = new IntPtr(11);
                // caller needs to Marshal.FreeHGlobal(cds.lpData);
                return cds;
            }

            public void TransmitPublicKey(Windows.MainWindow localWindow, bool isRequest)
            {
                Windows.COPYDATASTRUCT cds = GetPublicKeyTransmission(isRequest);

                HwndSource source = PresentationSource.FromVisual(localWindow) as HwndSource;

                Windows.MainWindow.SendMessage(this.RemoteWindow, Windows.MainWindow.WM_COPYDATA, source.Handle, ref cds);

                Marshal.FreeHGlobal(cds.lpData);
            }

            public void TransmitEncryptedMasterKey(Windows.MainWindow localWindow)
            {
                Windows.COPYDATASTRUCT cds = GetEncryptedMasterKeyTransmission();
                HwndSource source = PresentationSource.FromVisual(localWindow) as HwndSource;
                Windows.MainWindow.SendMessage(this.RemoteWindow, Windows.MainWindow.WM_COPYDATA, source.Handle, ref cds);
                Marshal.FreeHGlobal(cds.lpData);
            }

            public void Dispose()
            {
                if (LocalPrivateKey != null)
                {
                    LocalPrivateKey.Dispose();
                    LocalPrivateKey = null;
                }
                if (DH!=null)
                {
                    DH.Dispose();
                    DH = null;
                }
                if (LocalPublicKey != null)
                {
                    LocalPublicKey.Dispose();
                    LocalPublicKey = null;
                }                
            }
        }

        static ObservableCollection<Session> Sessions = new ObservableCollection<Session>();

        /// <summary>
        /// Request Master Key from Master Instance
        /// </summary>
        /// <param name="window"></param>
        /// <param name="remoteWindow"></param>
        /// <param name="remoteProcess"></param>
        public static void RequestMasterKey(Windows.MainWindow window, IntPtr remoteWindow, System.Diagnostics.Process remoteProcess)
        {
            Session session = Sessions.FirstOrDefault(q => q.RemoteWindow == remoteWindow && q.Process.Id == remoteProcess.Id);

            if (session == null)
            {
                session = new Session(remoteWindow, remoteProcess);
                Sessions.Add(session);
            }

            session.TransmitPublicKey(window,true);
        }

        public static bool ReceiveTransmission(Windows.MainWindow window, IntPtr remoteWindow, System.Diagnostics.Process remoteProcess, Windows.COPYDATASTRUCT cds)
        {
            Session session = Sessions.FirstOrDefault(q => q.RemoteWindow == remoteWindow && q.Process.Id == remoteProcess.Id);

            if (session == null)
            {
                session = new Session(remoteWindow, remoteProcess);
                Sessions.Add(session);
            }

            switch((long)cds.dwData)
            {
                case 10:
                    if (!App.Settings.HasPasswordMasterKey)
                    {
                        return false;
                    }
                    session.ReceivePublicKeyTransmission(cds);
                    session.TransmitPublicKey(window,false);
                    session.TransmitEncryptedMasterKey(window);
                    return true;
                case 11:
                    session.ReceiveEncryptedMasterKeyTransmission(cds);
                    return true;
                case 12:
                    session.ReceivePublicKeyTransmission(cds);
                    return true;
            }

            return false;
        }
    }
}
