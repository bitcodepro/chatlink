using System.Security.Cryptography;
using System.Text;

namespace ChatLink.Services;

public static class AesGcmEncryptionService
{
    private static readonly int NonceSize = 12; // 96 bits for the nonce is standard
    private static readonly int TagSize = 16; // 128 bits for the tag is standard

    public static (byte[] encryptedData, byte[] nonce, byte[] tag) EncryptString(string plainText, byte[] key)
    {
        using (AesGcm aesGcm = new AesGcm(key, TagSize))
        {
            byte[] nonce = new byte[NonceSize];
            RandomNumberGenerator.Fill(nonce);

            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] encryptedData = new byte[plainTextBytes.Length];
            byte[] tag = new byte[TagSize];

            aesGcm.Encrypt(nonce, plainTextBytes, encryptedData, tag);

            return (encryptedData, nonce, tag);
        }
    }

    public static string DecryptString(byte[] cipherText, byte[] key, byte[] nonce, byte[] tag)
    {
        using (AesGcm aesGcm = new AesGcm(key, TagSize))
        {
            byte[] decryptedData = new byte[cipherText.Length];

            aesGcm.Decrypt(nonce, cipherText, tag, decryptedData);

            return Encoding.UTF8.GetString(decryptedData);
        }
    }

    public static string EncryptMessage(byte[] sharedKey, string message)
    {
        byte[] aesKey = new byte[32];
        Array.Copy(sharedKey, aesKey, Math.Min(sharedKey.Length, aesKey.Length));

        var (encryptedData, nonce, tag) = EncryptString(message, aesKey);

        byte[] combinedData = new byte[nonce.Length + tag.Length + encryptedData.Length];
        Array.Copy(nonce, 0, combinedData, 0, nonce.Length);
        Array.Copy(tag, 0, combinedData, nonce.Length, tag.Length);
        Array.Copy(encryptedData, 0, combinedData, nonce.Length + tag.Length, encryptedData.Length);

        // Convert combined data to Base64 for transmission
        string combinedDataBase64 = Convert.ToBase64String(combinedData);

        return combinedDataBase64;
    }

    public static string DecryptMessage(byte[] sharedKey, string combinedDataBase64)
    {
        byte[] aesKey = new byte[32];
        Array.Copy(sharedKey, aesKey, Math.Min(sharedKey.Length, aesKey.Length));

        byte[] receivedData = Convert.FromBase64String(combinedDataBase64);

        // Extract the nonce, tag, and encrypted data from the received data
        byte[] receivedNonce = new byte[NonceSize];
        byte[] receivedTag = new byte[TagSize];
        byte[] receivedEncryptedData = new byte[receivedData.Length - NonceSize - TagSize];

        Array.Copy(receivedData, 0, receivedNonce, 0, NonceSize);
        Array.Copy(receivedData, NonceSize, receivedTag, 0, TagSize);
        Array.Copy(receivedData, NonceSize + TagSize, receivedEncryptedData, 0, receivedEncryptedData.Length);

        string decryptedMessage = DecryptString(receivedEncryptedData, aesKey, receivedNonce, receivedTag);

        return decryptedMessage;
    }
}
