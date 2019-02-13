using System.Text;

namespace ISBoxerEVELauncher.Security
{
    public static class SHA256
    {
        public static string GenerateString(string inputString)
        {
            return GetStringFromHash(GenerateHash(inputString));
        }
        public static byte[] GenerateHash(string inputString)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(inputString);
            return GenerateHash(bytes);
        }

        public static byte[] GenerateHash(byte[] inputBytes)
        {
            using (System.Security.Cryptography.SHA256 sha256 = System.Security.Cryptography.SHA256Managed.Create())
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
