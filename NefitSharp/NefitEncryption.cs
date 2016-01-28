using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace NefitSharp
{
    class NefitEncryption
    {
        private readonly byte[] _chat = new byte[] { 0x58, 0xf1, 0x8d, 0x70, 0xf6, 0x67, 0xc9, 0xc7, 0x9e, 0xf7, 0xde, 0x43, 0x5b, 0xf0, 0xf9, 0xb1, 0x55, 0x3b, 0xbb, 0x6e, 0x61, 0x81, 0x62, 0x12, 0xab, 0x80, 0xe5, 0xb0, 0xd3, 0x51, 0xfb, 0xb1 };
        private readonly byte[] _email = new byte[] { 0x52, 0xea, 0xfb, 0x7a, 0x84, 0xe9, 0x5c, 0x1d, 0xbd, 0xb0, 0xff, 0xef, 0x1a, 0xa5, 0xc8, 0xd1, 0xaa, 0xb8, 0x15, 0x8b, 0x52, 0x32, 0x93, 0x4f, 0x15, 0x4a, 0x7c, 0xff, 0xee, 0x29, 0xb9, 0x23 };
        private readonly byte[] _alarm = new byte[] { 0xb7, 0x69, 0x18, 0x67, 0x79, 0x9c, 0x11, 0xd5, 0xb8, 0x37, 0xf8, 0xa5, 0xe8, 0x6e, 0x81, 0xc8, 0xe6, 0xd2, 0xbb, 0xcc, 0x62, 0x4f, 0x15, 0x7a, 0xc4, 0xf0, 0x3d, 0x5d, 0x37, 0x01, 0xe1, 0x1e };

        private byte[] _chatKey;
        private byte[] _emailKey;
        private byte[] _alarmKey;

        private readonly RijndaelManaged _rijndael;
        private readonly MD5 _md5;

        public NefitEncryption(string serial, string access, string password)
        {
            _rijndael = new RijndaelManaged();
            _md5 = MD5.Create();
            _rijndael.Mode = CipherMode.ECB;
            _rijndael.Padding = PaddingMode.Zeros;
            _chatKey = GenerateKey(_chat, access, password);
            _emailKey = GenerateKey(_email, serial, "gservice_smtp");
            _alarmKey = GenerateKey(_alarm, serial, "gservice_alarm");
        }

        private byte[] Combine(byte[] inputBytes1, byte[] inputBytes2)
        {
            byte[] inputBytes = new byte[inputBytes1.Length + inputBytes2.Length];
            Buffer.BlockCopy(inputBytes1, 0, inputBytes, 0, inputBytes1.Length);
            Buffer.BlockCopy(inputBytes2, 0, inputBytes, inputBytes1.Length, inputBytes2.Length);
            return inputBytes;
        }

        private byte[] GenerateKey(byte[] magicKey, string idKeyUuid, string password)
        {
            return Combine(_md5.ComputeHash(Combine(Encoding.Default.GetBytes(idKeyUuid), magicKey)), _md5.ComputeHash(Combine(magicKey,Encoding.Default.GetBytes(password))));
        }

        public string Decrypt(string cipherData)
        {
            try
            {
                List<byte> base64Str = new List<byte>(Convert.FromBase64String(cipherData));
                int num = base64Str.Count % 8;
                for (int i = 0; i < num; i++)
                {
                    base64Str.Add(0x00);
                }
                _rijndael.Key = _chatKey;
                StreamReader reader = new StreamReader(new CryptoStream(new MemoryStream(base64Str.ToArray()), _rijndael.CreateDecryptor(), CryptoStreamMode.Read));
                return reader.ReadToEnd().Trim('\0');
            }
            catch (CryptographicException e)
            {
                Debug.WriteLine("A Cryptographic error occurred: {0}", e.Message);
                return null;
            }
        }

        public string Encrypt(string data)
        {
            try
            {
                List<byte> hexString = new List<byte>(Encoding.Default.GetBytes(data));
                while (hexString.Count % 16 != 0)
                {
                    hexString.Add(0x00);
                }
                _rijndael.Key = _chatKey;

                CryptoStream stream = new CryptoStream(new MemoryStream(hexString.ToArray()), _rijndael.CreateEncryptor(), CryptoStreamMode.Read);
                MemoryStream textBytes = new MemoryStream();
                stream.CopyTo(textBytes);
                return Convert.ToBase64String(textBytes.ToArray());
            }
            catch (CryptographicException e)
            {
                Debug.WriteLine("A Cryptographic error occurred: {0}", e.Message);
                return null;
            }
        }
    }
}
