using System;
using System.Security.Cryptography;
using System.Text;

namespace SamFirm.Utils
{
    internal static class Crypto
    {
        public static string DecryptStringFromBytes(byte[] cipherText, byte[] Key)
        {
            // Check arguments.
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException("cipherText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");

            // Declare the string used to hold
            // the decrypted text.
            string plaintext = null;

            // Create an Aes object
            // with the specified key.
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Mode = CipherMode.CBC;
                aesAlg.Key = Key;
                aesAlg.IV = Key[0..16];

                // Create a decryptor
                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                plaintext = Encoding.ASCII.GetString(decryptor.TransformFinalBlock(cipherText, 0, cipherText.Length));

                decryptor.Dispose();
            }

            return plaintext;
        }

        public static byte[] EncryptStringToBytes(string plainText, byte[] Key)
        {
            // Check arguments.
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException("plainText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            byte[] encrypted;

            // Create an Aes object
            // with the specified key.
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Mode = CipherMode.CBC;
                aesAlg.Key = Key;
                aesAlg.IV = Key[0..16];

                // Create an encryptor
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                encrypted = encryptor.TransformFinalBlock(Encoding.ASCII.GetBytes(plainText), 0, Encoding.ASCII.GetBytes(plainText).Length);

                encryptor.Dispose();
            }

            // Return the encrypted bytes.
            return encrypted;
        }
    }

    internal static class Auth
    {
        private const string NONCE_KEY = "hqzdurufm2c8mf6bsjezu1qgveouv7c7";
        private const string AUTH_KEY = "w13r4cvf4hctaujv";

        public static string DecryptNonce(string nonce)
        {
            return Crypto.DecryptStringFromBytes(Convert.FromBase64String(nonce), Encoding.ASCII.GetBytes(NONCE_KEY.ToString()));
        }

        public static string GetAuthorization(string decryptedNonce)
        {
            StringBuilder key = new StringBuilder();
            for (int i = 0; i < 16; i++)
            {
                int nonceChar = decryptedNonce[i];
                key.Append(NONCE_KEY[nonceChar % 16]);
            }
            key.Append(AUTH_KEY);

            return Convert.ToBase64String(Crypto.EncryptStringToBytes(decryptedNonce, Encoding.ASCII.GetBytes(key.ToString())));
        }

        public static string GetLogicCheck(string input, string decryptedNonce)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }
            StringBuilder builder = new StringBuilder();
            int num = 0;
            foreach (char ch in decryptedNonce)
            {
                int num2 = ch & '\x000f';
                if (input.Length <= (num2 + num))
                {
                    return string.Empty;
                }
                builder.Append(input[num2 + num]);
            }
            return builder.ToString();
        }
    }
}
