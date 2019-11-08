using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace HZ.Crawler.Common.Extensions
{
    public static partial class Extension
    {
        public static string GetUrlKeyValue(this string url, string key)
        {
            if (!string.IsNullOrEmpty(url) && !string.IsNullOrEmpty(key) && url.Contains("?"))
            {
                string[] kvs = url.Split('?')[1].Split('&');
                foreach (var item in kvs)
                {
                    string[] aStr = item.Split('=');
                    if (aStr[0] == key)
                        return aStr[1];
                }
            }
            return string.Empty;
        }

        public static bool IsLink(this string str)
        {
            Regex regex = new Regex(@"[a-zA-z]+://[^\s]*");
            return regex.Match(str).Success;
        }

        public static bool IsPhoneNum(this string str)
        {
            Regex regex = new Regex("0?(13|14|15|17|18|19)[0-9]{9}");
            return regex.Match(str).Success;
        }

        public static bool IsEmail(this string str)
        {
            Regex regex = new Regex(@"\w[-\w.+]*@([A-Za-z0-9][-A-Za-z0-9]+\.)+[A-Za-z]{2,14}");
            return regex.Match(str).Success;
        }

        public static string ToUrlEncode(this string str, bool isToUpper = true)
        {
            string func(string s)
            {
                string urlStr = Uri.EscapeDataString(s);//空格转义为%20
                if (isToUpper)
                {
                    urlStr = EncodeToUpper(urlStr);
                }
                return urlStr;
            }
            return ToEncode(str, func);
        }

        public static string ToHtmlEncode(this string str, bool isToUpper = true)
        {
            string func(string s)
            {
                string urlStr = HttpUtility.UrlEncode(s);//空格转义为+
                if (isToUpper)
                {
                    urlStr = EncodeToUpper(urlStr);
                }
                return urlStr;
            }
            return ToEncode(str, func);
        }
        static string ToEncode(string value, Func<string, string> func)
        {
            const int limit = 32766;
            var sb = new StringBuilder(value.Length);
            int loops = value.Length / limit;
            //https://docs.microsoft.com/en-us/dotnet/api/system.uri.escapedatastring?view=netframework-4.5
            for (int i = 0; i <= loops; i++)
            {
                string temp = i < loops ? value.Substring(limit * i, limit) : value.Substring(limit * i);
                sb.Append(func.Invoke(temp));
            }
            return sb.ToString();
        }
        static string EncodeToUpper(string encodeStr)
        {
            var list = Regex.Matches(encodeStr, "%[a-f0-9]{2}", RegexOptions.Compiled).Cast<Match>().Select(m => m.Value).Distinct();
            foreach (string item in list)
            {
                encodeStr = encodeStr.Replace(item, item.ToUpper());
            }
            return encodeStr;
        }

        public static string ToBase64(this string str, Encoding encoding = null)
        {
            if (string.IsNullOrEmpty(str))
            {
                return str;
            }
            if (encoding == null)
            {
                encoding = Encoding.UTF8;
            }
            return Convert.ToBase64String(encoding.GetBytes(str));
        }

    }
}
