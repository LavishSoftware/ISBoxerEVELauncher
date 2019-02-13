using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ISBoxerEVELauncher.Extensions;
using ISBoxerEVELauncher.Games.EVE;

namespace ISBoxerEVELauncher.Web
{
    static public class CookieStorage
    {
        public static string GetCookieStoragePath()
        {
            string path = System.IO.Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "ISBoxer EVE Launcher","Cookies");

            System.IO.Directory.CreateDirectory(path);

            return path;
        }

        public static string GetCookiesFilename(EVEAccount eveAccount)
        {
            string filename = eveAccount.Username.ToLowerInvariant().SHA256();
            return System.IO.Path.Combine(GetCookieStoragePath(), filename);
        }

        public static string GetCookies(EVEAccount eveAccount)
        {
            try
            {
                string filePath = GetCookiesFilename(eveAccount);
                return System.IO.File.ReadAllText(filePath, Encoding.ASCII);
            }
            catch
            {
                return string.Empty;
            }
        }

        public static void WriteAllTextSafe(string filename, string text, Encoding encoding)
        {
            string tempPath = System.IO.Path.GetTempPath();
            string tempFile = System.IO.Path.Combine(tempPath, System.IO.Path.GetRandomFileName());
            string backupFile = System.IO.Path.Combine(tempPath, System.IO.Path.GetRandomFileName());

            System.IO.File.WriteAllText(tempFile, text, encoding);

            bool fileExists = System.IO.File.Exists(filename);
            if (fileExists)
            {
                try
                {
                    System.IO.File.Move(filename, backupFile);
                }
                catch
                {

                }
            }

            try
            {
                System.IO.File.Copy(tempFile, filename, true);

                System.IO.File.Delete(tempFile);
            }
            catch (Exception ex)
            {
                if (fileExists)
                {
                    try
                    {
                        System.IO.File.Move(backupFile, filename);
                    }
                    catch
                    {

                    }
                }
                throw;
            }
        }

        public static void SetCookies(EVEAccount eveAccount, string cookies)
        {
            string filePath = GetCookiesFilename(eveAccount);
            WriteAllTextSafe(filePath, cookies, Encoding.ASCII);
        }


        public static void DeleteCookies(EVEAccount eveAccount)
        {
            string filePath = GetCookiesFilename(eveAccount);
            System.IO.File.Delete(filePath);
        }
    }
}
