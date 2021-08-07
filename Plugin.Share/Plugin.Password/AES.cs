using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Plugin.Password
{
    public class AES : Q.Helper.AESHelper, Extension.IAES
    {
        public AES()
        {

        }
        public bool SkipSysEncrypt { get; set; } = false;

        public Func<string> SetPassword { get; set; }
        public string Pwd { get; set; }
        public byte[] Decrypt(byte[] data)
        {
            string xml = Encoding.UTF8.GetString(data);
            if (xml.Contains("xml") && System.Windows.Forms.MessageBox.Show("正在使用未加密的文档，是否需要加密？","加密提示", System.Windows.Forms.MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.No)
            {
                return data;
            }

            //解密前需要一个密码
            if (string.IsNullOrEmpty(Pwd))
            {
                Pwd = SetPassword.Invoke();
                //密码与默认密码混合处理
            }
            if (string.IsNullOrWhiteSpace(Pwd))
            {
                System.Windows.Forms.MessageBox.Show("未输入密码，文件不会加解密");
                return data;
            }
            else
                try
                {
                    byte[] newData = Decrypt(data, getKey(out var iv2), iv2);
                    xml = Encoding.UTF8.GetString(newData);
                    if (xml.Contains("xml"))
                    {
                        return newData;
                    }
                    else
                    {
                        Pwd = string.Empty;
                        throw new FileLoadException("密码错误");
                    }
                }
                catch (Exception ex)
                {
                    Pwd = string.Empty;
                    throw new FileLoadException("解密失败, 可能是密码错误\r\n" + ex.Message);
                }
        }

        byte[] getKey(out byte[] iv2)
        {
            Byte[] bKey = new Byte[32];
            Array.Copy(Convert.FromBase64String(key), bKey, Convert.FromBase64String(key).Length);
            var a = new BitArray(bKey);
            byte[] pKey = new byte[32];
            Array.Copy(Encoding.UTF8.GetBytes(Pwd), pKey, Encoding.UTF8.GetByteCount(Pwd));
            var p = new BitArray(pKey);
            var n = a.Xor(p);
            Array.Clear(bKey, 0, bKey.Length);
            n.CopyTo(bKey, 0);
            iv2 = new Byte[16];
            Array.Copy(Convert.FromBase64String(iv), iv2, Math.Min(iv.Length, Convert.FromBase64String(iv).Length));
            return bKey;
        }
        public byte[] Encrypt(byte[] data)
        {
            //加密使用默认密码
            if (string.IsNullOrWhiteSpace(Pwd))
                return data;
            else
                return Encrypt(data, getKey(out var iv2), iv2);
        }
    }



}
