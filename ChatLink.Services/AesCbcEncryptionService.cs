using System.Security.Cryptography;

namespace ChatLink.Services;

public static class AesCbcEncryptionService
{
    public static (byte[] encryptedData, byte[] iv) EncryptString(string plainText, byte[] key)
    {
        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = key;
            aesAlg.GenerateIV();
            aesAlg.Mode = CipherMode.CBC;

            byte[] iv = aesAlg.IV;

            ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

            using (MemoryStream msEncrypt = new MemoryStream())
            {
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                {
                    swEncrypt.Write(plainText);
                }
                byte[] encrypted = msEncrypt.ToArray();
                return (encrypted, iv);
            }
        }
    }

    public static string DecryptString(byte[] cipherText, byte[] key, byte[] iv)
    {
        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Key = key;
            aesAlg.IV = iv;
            aesAlg.Mode = CipherMode.CBC;

            ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

            using (MemoryStream msDecrypt = new MemoryStream(cipherText))
            using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
            using (StreamReader srDecrypt = new StreamReader(csDecrypt))
            {
                return srDecrypt.ReadToEnd();
            }
        }
    }

    public static string EncryptMessage(byte[] sharedKey, string message)
    {
        byte[] aesKey = new byte[32];
        Array.Copy(sharedKey, aesKey, Math.Min(sharedKey.Length, aesKey.Length));

        var (encryptedData, iv) = EncryptString(message, aesKey);

        // Combine IV and encrypted data
        byte[] combinedData = new byte[iv.Length + encryptedData.Length];
        Array.Copy(iv, 0, combinedData, 0, iv.Length);
        Array.Copy(encryptedData, 0, combinedData, iv.Length, encryptedData.Length);

        // Convert combined data to Base64 for transmission
        string combinedDataBase64 = Convert.ToBase64String(combinedData);

        return combinedDataBase64;
    }

    public static string DecryptMessage(byte[] sharedKey, string combinedDataBase64)
    {
        byte[] aesKey = new byte[32];
        Array.Copy(sharedKey, aesKey, Math.Min(sharedKey.Length, aesKey.Length));

        byte[] combinedData = Convert.FromBase64String(combinedDataBase64);

        // Extract the IV and encrypted data
        byte[] iv = new byte[16]; // AES block size is 16 bytes
        byte[] encryptedData = new byte[combinedData.Length - iv.Length];

        Array.Copy(combinedData, 0, iv, 0, iv.Length);
        Array.Copy(combinedData, iv.Length, encryptedData, 0, encryptedData.Length);

        string decryptedMessage = AesCbcEncryptionService.DecryptString(encryptedData, aesKey, iv);

        return decryptedMessage;
    }
}
