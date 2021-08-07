using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.IO;

namespace Q.Helper
{

    /// <summary>
    /// AES�㷨�������
    /// DES���ݼ��ܱ�׼�㷨������Կ���Ƚ�С(56λ),�Ѿ�����Ӧ����ֲ�ʽ������������ݼ��ܰ�ȫ�Ե�Ҫ�����1997��NIST���������µ����ݼ��ܱ�׼,��AES���������ֵ�ɸѡ,����ʱJoan Daeman��Vincent Rijmen�ύ��Rijndael�㷨������ΪAES�������㷨�����㷨����Ϊ�����µ����ݼ��ܱ�׼�����㷺Ӧ���ڸ��������С��������Ƕ�AES���в�ͬ�Ŀ���,��������˵,AES��Ϊ��һ�������ݼ��ܱ�׼�����ǿ��ȫ�ԡ������ܡ���Ч�ʡ����ú������ŵ㡣AES�����������Կ����:128,192,256λ����Զ��ԣ�AES��128��Կ��DES��56��Կǿ1021����
    /// </summary>
    public class AESHelper
    {






        #region ����



        /// <summary>
        /// AES����
        /// </summary>
        /// <param name="data">�����ܵ�����</param>
        /// <param name="key">��Կ</param>
        /// <param name="vector">����</param>
        /// <returns>����</returns>
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

            Byte[] Cryptograph = null; // ���ܺ������

            Rijndael Aes = Rijndael.Create();
            Aes.Mode = CipherMode.ECB;
            Aes.Padding = PaddingMode.PKCS7;
            try
            {
                // ����һ���ڴ���
                using (MemoryStream Memory = new MemoryStream())
                {
                    // ���ڴ��������װ�ɼ���������
                    using (CryptoStream Encryptor = new CryptoStream(Memory,
                     Aes.CreateEncryptor(key, vector),
                     CryptoStreamMode.Write))
                    {
                        // ��������д�������
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

        #region ����


        /// <summary>
        /// AES����
        /// </summary>
        /// <param name="data">�����ܵ�����</param>
        /// <param name="key">��Կ</param>
        /// <param name="vector">����</param>
        /// <returns>����</returns>
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

            Byte[] original = null; // ���ܺ������

            Rijndael Aes = Rijndael.Create();
            Aes.Mode = CipherMode.ECB;
            Aes.Padding = PaddingMode.PKCS7;
            try
            {
                // ����һ���ڴ������洢����
                using (MemoryStream Memory = new MemoryStream(data))
                {
                    // ���ڴ��������װ�ɼ���������
                    using (CryptoStream Decryptor = new CryptoStream(Memory,
                    Aes.CreateDecryptor(key, vector),
                    CryptoStreamMode.Read))
                    {
                        // ���Ĵ洢��
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

        #region ������Կ

        /// <summary>
        /// ������Կ
        /// </summary>
        /// <param name="IV">����</param>
        /// <returns>��Կ</returns>
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

        #region Ӧ��

        protected string key = "Rie0L0pJwoxkp8e+bT1FO8WTZVKE7Kpy4m2WILU0uAQ=";
        protected string iv = "qP3/UmgK0swbdIXmL/F6FA==";




        #endregion

    }
}
