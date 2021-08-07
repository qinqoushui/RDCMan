using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.IO;

namespace Q.Helper
{

    /// <summary>
    /// AES算法描述简介
    /// DES数据加密标准算法由于密钥长度较小(56位),已经不适应当今分布式开放网络对数据加密安全性的要求，因此1997年NIST公开征集新的数据加密标准,即AES。经过三轮的筛选,比利时Joan Daeman和Vincent Rijmen提交的Rijndael算法被提议为AES的最终算法。此算法将成为美国新的数据加密标准而被广泛应用在各个领域中。尽管人们对AES还有不同的看法,但总体来说,AES作为新一代的数据加密标准汇聚了强安全性、高性能、高效率、易用和灵活等优点。AES设计有三个密钥长度:128,192,256位，相对而言，AES的128密钥比DES的56密钥强1021倍。
    /// </summary>
    public class AESHelper
    {






        #region 加密



        /// <summary>
        /// AES加密
        /// </summary>
        /// <param name="data">被加密的明文</param>
        /// <param name="key">密钥</param>
        /// <param name="vector">向量</param>
        /// <returns>密文</returns>
        public byte[] Encrypt(Byte[] data, String key, String vector)
        {
            Byte[] bKey = new Byte[32];
            Array.Copy(Convert.FromBase64String(key), bKey, Convert.FromBase64String(key).Length);
            Byte[] bVector = new Byte[16];
            Array.Copy(Convert.FromBase64String(vector), bVector, Convert.FromBase64String(vector).Length > bVector.Length ? bVector.Length : Convert.FromBase64String(vector).Length);
            return Encrypt(data, bKey, bVector);
        }

        public static byte[] Encrypt(Byte[] data, Byte[] key, Byte[] vector)
        {

            Byte[] Cryptograph = null; // 加密后的密文

            Rijndael Aes = Rijndael.Create();
            Aes.Mode = CipherMode.ECB;
            Aes.Padding = PaddingMode.PKCS7;
            try
            {
                // 开辟一块内存流
                using (MemoryStream Memory = new MemoryStream())
                {
                    // 把内存流对象包装成加密流对象
                    using (CryptoStream Encryptor = new CryptoStream(Memory,
                     Aes.CreateEncryptor(key, vector),
                     CryptoStreamMode.Write))
                    {
                        // 明文数据写入加密流
                        Encryptor.Write(data, 0, data.Length);
                        Encryptor.FlushFinalBlock();

                        Cryptograph = Memory.ToArray();
                    }
                }
            }
            catch
            {
                Cryptograph = null;
            }

            return Cryptograph;
        }

        #endregion

        #region 解密


        /// <summary>
        /// AES解密
        /// </summary>
        /// <param name="data">被解密的密文</param>
        /// <param name="key">密钥</param>
        /// <param name="vector">向量</param>
        /// <returns>明文</returns>
        public Byte[] Decrypt(Byte[] data, String key, String vector)
        {
            //Byte[] bKey = new Byte[32];
            //Array.Copy(Convert.FromBase64String (Key.PadRight(bKey.Length)), bKey, bKey.Length);
            //Byte[] bVector = new Byte[16];
            //Array.Copy(Convert.FromBase64String(Vector.PadRight(bVector.Length)), bVector, bVector.Length);

            Byte[] bKey = new Byte[32];
            Array.Copy(Convert.FromBase64String(key), bKey, Convert.FromBase64String(key).Length);
            Byte[] bVector = new Byte[16];
            Array.Copy(Convert.FromBase64String(vector), bVector, Convert.FromBase64String(vector).Length > bVector.Length ? bVector.Length : Convert.FromBase64String(vector).Length);
            return Decrypt(data, bKey, bVector);
        }

        public static Byte[] Decrypt(Byte[] data, byte[] key, byte[] vector)
        {

            Byte[] original = null; // 解密后的明文

            Rijndael Aes = Rijndael.Create();
            Aes.Mode = CipherMode.ECB;
            Aes.Padding = PaddingMode.PKCS7;
            try
            {
                // 开辟一块内存流，存储密文
                using (MemoryStream Memory = new MemoryStream(data))
                {
                    // 把内存流对象包装成加密流对象
                    using (CryptoStream Decryptor = new CryptoStream(Memory,
                    Aes.CreateDecryptor(key, vector),
                    CryptoStreamMode.Read))
                    {
                        // 明文存储区
                        using (MemoryStream originalMemory = new MemoryStream())
                        {
                            Byte[] Buffer = new Byte[1024];
                            Int32 readBytes = 0;
                            while ((readBytes = Decryptor.Read(Buffer, 0, Buffer.Length)) > 0)
                            {
                                originalMemory.Write(Buffer, 0, readBytes);
                            }

                            original = originalMemory.ToArray();
                        }
                    }
                }
            }
            catch
            {
                original = null;
            }

            return original;
        }

        #endregion

        #region 创建密钥

        /// <summary>
        /// 创建密钥
        /// </summary>
        /// <param name="IV">向量</param>
        /// <returns>密钥</returns>
        public static string CreateKey(out string IV)
        {
            byte[] iv;
            byte[] key = CreateKey(out iv);
            if ((key != null) && (key.Length > 0))
            {
                IV = Convert.ToBase64String(iv);
                return Convert.ToBase64String(key);
            }
            else
            {
                IV = "";
                return "";
            }
        }
        public static byte[] CreateKey(out byte[] IV)
        {
            Byte[] bKey = new Byte[32];
            IV = new byte[16];
            Random rnd = new Random(DateTime.Now.Millisecond);
            for (int i = 0; i < 32; i++)
            {
                bKey[i] = (byte)rnd.Next(0, 256);
                //System.Threading.Thread.Sleep(4);
                //Console.WriteLine(bKey[i]);
            }
            for (int i = 0; i < 16; i++)
            {
                IV[i] = (byte)rnd.Next(0, 256);
                // System.Threading.Thread.Sleep(4);
            }
            return bKey;
        }


        #endregion

        #region 应用

        protected string key = "Rie0L0pJwoxkp8e+bT1FO8WTZVKE7Kpy4m2WILU0uAQ=";
        protected string iv = "qP3/UmgK0swbdIXmL/F6FA==";




        #endregion

    }
}
