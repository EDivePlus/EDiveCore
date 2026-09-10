// Author: Michal Petr
// Created: 10.09.2026

using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace EDIVE.NativeUtils
{
    public static class HashUtils
    {
        public static byte[] Sha256(byte[] data)
        {
            using var sha = SHA256.Create();
            return sha.ComputeHash(data);
        }

        public static byte[] Sha256(Stream stream)
        {
            using var sha = SHA256.Create();
            return sha.ComputeHash(stream);
        }

        public static byte[] Sha256(string text) => Sha256(Encoding.UTF8.GetBytes(text));

        public static byte[] Sha256File(string path)
        {
            using var stream = File.OpenRead(path);
            return Sha256(stream);
        }

        public static string Sha256Hex(byte[] data) => ToHex(Sha256(data));
        public static string Sha256Hex(Stream stream) => ToHex(Sha256(stream));
        public static string Sha256Hex(string text) => ToHex(Sha256(text));
        public static string Sha256FileHex(string path) => ToHex(Sha256File(path));

        public static string ToHex(byte[] bytes)
        {
            var builder = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes)
                builder.Append(b.ToString("x2"));
            return builder.ToString();
        }
    }
}
