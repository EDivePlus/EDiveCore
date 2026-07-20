// Author: František Holubec
// Created: 20.07.2026

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace EDIVE.BuildTool.Signing
{
    internal static class KeystoreCrypto
    {
        private static readonly byte[] SALT = {0x45, 0x44, 0x49, 0x56, 0x45, 0x42, 0x54, 0x4B, 0x53, 0x74, 0x6F, 0x72, 0x65, 0x53, 0x61, 0x6C};
        private const int ITERATIONS = 100_000;
        private const int KEY_BYTES = 32;

        private static byte[] _key;
        private static byte[] Key => _key ??= DeriveKey();

        public static string Encrypt(string plainText)
        {
            using var aes = Aes.Create();
            aes.Key = Key;
            aes.GenerateIV();

            using var stream = new MemoryStream();
            stream.Write(aes.IV, 0, aes.IV.Length);
            using (var crypto = new CryptoStream(stream, aes.CreateEncryptor(), CryptoStreamMode.Write))
            {
                var bytes = Encoding.UTF8.GetBytes(plainText);
                crypto.Write(bytes, 0, bytes.Length);
            }
            return Convert.ToBase64String(stream.ToArray());
        }

        public static string Decrypt(string cipherText)
        {
            var payload = Convert.FromBase64String(cipherText);
            using var aes = Aes.Create();
            aes.Key = Key;

            var iv = new byte[aes.BlockSize / 8];
            if (payload.Length < iv.Length)
                throw new CryptographicException("Ciphertext is too short.");
            Buffer.BlockCopy(payload, 0, iv, 0, iv.Length);
            aes.IV = iv;

            using var stream = new MemoryStream(payload, iv.Length, payload.Length - iv.Length);
            using var crypto = new CryptoStream(stream, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using var reader = new StreamReader(crypto, Encoding.UTF8);
            return reader.ReadToEnd();
        }

        private static byte[] DeriveKey()
        {
            using var derive = new Rfc2898DeriveBytes(SystemInfo.deviceUniqueIdentifier, SALT, ITERATIONS, HashAlgorithmName.SHA256);
            return derive.GetBytes(KEY_BYTES);
        }
    }
}
