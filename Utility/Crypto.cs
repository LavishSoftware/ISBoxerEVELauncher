using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ISBoxerEVELauncher.Utility
{
    public static class Crypto
    {
        public static string GenerateSHA256String(string inputString)
        {
            return GetStringFromHash(GenerateSHA256Hash(inputString));
        }
        public static byte[] GenerateSHA256Hash(string inputString)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(inputString);
            return GenerateSHA256Hash(bytes);
        }

        public static byte[] GenerateSHA256Hash(byte[] inputBytes)
        {
            using (SHA256 sha256 = SHA256Managed.Create())
            {
                return sha256.ComputeHash(inputBytes);
            }
        }

        private static string GetStringFromHash(byte[] hash)
        {
            StringBuilder result = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                result.Append(hash[i].ToString("X2"));
            }
            return result.ToString();
        }
    }
}
