using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;
using System.Security.Policy;

namespace Extension
{
    public interface IAES
    {
        byte[] Encrypt(byte[] data);
        byte[] Decrypt(byte[] data);

        string Pwd { get; set; }
    }

    public class AESXmlUtil
    {
        static AESXmlUtil _ = new AESXmlUtil();
        public static AESXmlUtil Instance { get; } = _;


        public IAES AES { get; set; }
        internal void WriteFile(string temporaryFileName, byte[] data)
        {
            File.WriteAllBytes(temporaryFileName, AES.Encrypt(data));
        }
        internal byte[] ReadFile(string temporaryFileName)
        {
            return AES.Decrypt(File.ReadAllBytes(temporaryFileName));
        }

        //打开文件，可能 需要提示密码或者从狗中读取文件等
    }
    //public class AESXmlTextWriter : XmlTextWriter
    //{
    //    public static IAES AES { get; set; }
    //    public AESXmlTextWriter(string filename, Encoding encoding) : base(new MemoryStream(AES.Decrypt(File.ReadAllBytes(filename))), encoding)
    //    {
    //    }

    //}

    //public class AESXmlTextReader : XmlTextReader
    //{
    //    public static IAES AES { get; set; }
    //    public AESXmlTextReader(string url) : base(new MemoryStream(AES.Decrypt(File.ReadAllBytes(url))))
    //    {
    //    }
    //}
}
