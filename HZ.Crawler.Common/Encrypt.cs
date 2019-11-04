using System;
using System.Security.Cryptography;
using System.Text;

namespace HZ.Crawler.Common
{
    public abstract class Encrypt
    {
        public static string ToMd5(string src, Encoding encoding)
        {
            byte[] srcBytes = encoding.GetBytes(src);
            return ToMd5(srcBytes);
        }
        public static string ToMd5(byte[] bytes)
        {
            using (MD5 md5 = new MD5CryptoServiceProvider())
            {
                byte[] desBytes = md5.ComputeHash(bytes);
                return BitConverter.ToString(desBytes).Replace("-", "").ToLower();
            }
        }
        public static string ToSHA1(string src, Encoding encoding)
        {
            byte[] srcBytes = encoding.GetBytes(src);
            return ToSHA1(srcBytes);
        }
        public static string ToSHA1(byte[] bytes)
        {
            using (SHA1 sha1 = new SHA1CryptoServiceProvider())
            {
                byte[] retval = sha1.ComputeHash(bytes);
                var sb = new StringBuilder();
                for (int i = 0; i < retval.Length; i++)
                {
                    sb.Append(retval[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }
    }
}
