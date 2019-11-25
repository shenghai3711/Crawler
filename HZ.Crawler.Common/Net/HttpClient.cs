using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Collections.Specialized;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Web;

namespace HZ.Crawler.Common.Net
{
    class HttpClient : IHttpClient, IDisposable
    {
        //public CookieContainer CookieContainer { get; set; }
        // public WebHeaderCollection Headers { get; private set; }
        // public string UserAgent { get; set; }
        public int Timeout { get; set; }
        // public string Accept { get; set; }
        public Encoding Aencding { get; set; }
        private readonly Logger Logger;
        public IHttpRequest HttpRequest { get; set; }
        public IHttpResponse HttpResponse { get; set; }
        public HttpClient()
        {
            this.Logger = new Logger(typeof(HttpClient));
            this.Timeout = 100 * 1000;
            //this.CookieContainer = new CookieContainer();
            // Headers = new WebHeaderCollection();
            Aencding = Encoding.UTF8;
            this.HttpRequest = new HttpRequest();
            this.HttpResponse = new HttpResponse();
        }

        public HttpClient(int timeout)
        {
            this.Logger = new Logger(typeof(HttpClient));
            this.Timeout = timeout;
        }

        public string Request(string url, HttpMethod method, string data, Encoding encoding, string contentType = null, CredentialCache mycache = null, NameValueCollection moreHeader = null)
        {
            string urlFormated = url;

            if (method == HttpMethod.GET && !string.IsNullOrEmpty(data))
            {
                urlFormated = string.Format("{0}{1}{2}", url, url.IndexOf('?') > 0 ? "&" : "?", data);
            }
            HttpWebRequest req = CreateRequest(urlFormated);
            this.Logger.Info(urlFormated);
            if (!string.IsNullOrEmpty(HttpRequest.Accept))
            {
                req.Accept = HttpRequest.Accept;
            }
            foreach (string key in HttpRequest.Headers.Keys)
            {
                req.Headers.Add(key, HttpRequest.Headers.Get(key));
            }
            if (null != moreHeader)
            {
                req.Headers.Add(moreHeader);
            }
            //req.Headers.Add(this.Header);
            req.ReadWriteTimeout = Timeout;
            req.Method = method.ToString();
            if (!string.IsNullOrEmpty(this.HttpRequest.UserAgent))
            {
                req.UserAgent = this.HttpRequest.UserAgent;
            }
            if (!string.IsNullOrEmpty(this.HttpRequest.Referer))
            {
                req.Referer = this.HttpRequest.Referer;
            }

            if (mycache != null)
            {
                req.Credentials = mycache;
            }
            if (!string.IsNullOrEmpty(contentType))
            {
                req.ContentType = contentType;
            }

            req.SendChunked = false;
            req.KeepAlive = true;
            req.ServicePoint.Expect100Continue = false;
            req.ServicePoint.ConnectionLimit = Int16.MaxValue;
            req.AutomaticDecompression = DecompressionMethods.None;

            req.CookieContainer = this.HttpRequest.CookieContainer;

            byte[] binaryData = null;

            if (method == HttpMethod.POST)
            {
                if (!string.IsNullOrEmpty(data))
                {
                    this.Logger.Info(data);
                }
                byte[] bytes = string.IsNullOrEmpty(data) ? new byte[] { } : encoding.GetBytes(data);

                req.ContentLength = bytes.Length + (binaryData == null ? 0 : binaryData.Length);
                Stream rs = req.GetRequestStream();
                rs.Write(bytes, 0, bytes.Length);

                if (binaryData != null)
                {
                    rs.Write(binaryData, 0, binaryData.Length);
                }

                rs.Flush();
                rs.Close();
            }
            try
            {
                using (WebResponse res = req.GetResponse())
                {
                    try
                    {
                        this.HttpRequest.CookieContainer = req.CookieContainer;

                        return GetData(res);
                    }
                    finally
                    {
                        res.Close();
                    }
                }
            }
            catch (WebException e)
            {
                if (null == e.Response)
                {
                    throw e;
                }
                return GetData(e.Response);
                //using (StreamReader sr = new StreamReader(e.Response.GetResponseStream(), encoding))
                //{
                //    return sr.ReadToEnd();
                //}
            }
        }
        private static HttpWebRequest CreateRequest(string url)
        {
            HttpWebRequest req = null;

            if (url.Contains("https"))
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolTypeExtensions.Tls12 | SecurityProtocolTypeExtensions.Tls11 | SecurityProtocolType.Tls;
                ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);
                req = (HttpWebRequest)WebRequest.Create(url);
                req.ProtocolVersion = HttpVersion.Version11;
            }
            else
            {
                req = (HttpWebRequest)WebRequest.Create(url);
            }

            return req;
        }

        private string GetData(WebResponse res)
        {
            HttpWebResponse response = (HttpWebResponse)res;
            if (response != null)
            {
                this.HttpResponse.StatusCode = response.StatusCode;
                this.HttpResponse.StatusDescription = response.StatusDescription;
                this.HttpResponse.Header = response.Headers;
                this.HttpResponse.ResponseUri = response.ResponseUri.ToString();
            }
            using (var sr = new StreamReader(res.GetResponseStream(), Aencding))
            {
                this.HttpResponse.Html = sr.ReadToEnd();
            }
            this.Logger.Info(this.HttpResponse.Html);
            return this.HttpResponse.Html;
        }

        #region IDisposable 成员

        public void Dispose()
        {
        }

        #endregion
        public static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            return true;
        }

        private NameValueCollection Header = new NameValueCollection();

        public void SetHeader(string key, string value)
        {
            this.Header[key] = value;
        }

        public void ClearHeader()
        {
            this.Header.Clear();
        }
        private static class SecurityProtocolTypeExtensions
        {
            public const SecurityProtocolType Tls12 = (SecurityProtocolType)SslProtocolsExtensions.Tls12;
            public const SecurityProtocolType Tls11 = (SecurityProtocolType)SslProtocolsExtensions.Tls11;
            public const SecurityProtocolType SystemDefault = (SecurityProtocolType)0;
        }
        private static class SslProtocolsExtensions
        {
            public const SslProtocols Tls12 = (SslProtocols)0x00000C00;
            public const SslProtocols Tls11 = (SslProtocols)0x00000300;
        }
        private string NameValueCollectionToString(NameValueCollection collection, Encoding encoding)
        {
            string temp = string.Empty;

            int n = 0;

            foreach (string key in collection)
            {
                if (n == collection.Count - 1)
                {
                    temp += string.Format("{0}={1}", key, HttpUtility.UrlEncode(collection[key]));
                }
                else
                {
                    temp += string.Format("{0}={1}&", key, HttpUtility.UrlEncode(collection[key], encoding));
                }
                n++;
            }

            return temp;
        }
        public string Request(MultiParts mp, Encoding encoding)
        {
            try
            {
                MemoryStream ms = new MemoryStream();
                mp.ToStream(ms, encoding);
                HttpWebRequest webRequest = CreateRequest(mp.Url);
            this.Logger.Info(mp.Url);
                webRequest.ReadWriteTimeout = Timeout;
                if (!string.IsNullOrEmpty(HttpRequest.Accept))
                {
                    webRequest.Accept = HttpRequest.Accept;
                }
                if (!string.IsNullOrEmpty(this.HttpRequest.Referer))
                {
                    webRequest.Referer = this.HttpRequest.Referer;
                }
                foreach (string key in HttpRequest.Headers.Keys)
                {
                    webRequest.Headers.Add(key, HttpRequest.Headers.Get(key));
                }
                webRequest.Method = HttpMethod.POST.ToString();
                webRequest.UserAgent = HttpRequest.UserAgent;
                webRequest.SendChunked = false;
                webRequest.KeepAlive = true;
                webRequest.ServicePoint.Expect100Continue = false;
                webRequest.ServicePoint.ConnectionLimit = 100;
                webRequest.ContentType = mp.ContentType;
                webRequest.CookieContainer = this.HttpRequest.CookieContainer;
                byte[] bytes = ms.ToArray();
                webRequest.ContentLength = bytes.Length;
                Stream rs = webRequest.GetRequestStream();
                rs.Write(bytes, 0, bytes.Length);
                rs.Flush();
                rs.Close();
                try
                {
                    using (WebResponse res = webRequest.GetResponse())
                    {
                        this.HttpRequest.CookieContainer = webRequest.CookieContainer;
                        using (StreamReader sr = new StreamReader(res.GetResponseStream(), encoding))
                        {
                            return sr.ReadToEnd();
                        }
                    }
                }
                catch (WebException e)
                {
                    string responseText = e.Message;
                    using (WebResponse response = e.Response)
                    {
                        HttpWebResponse httpResponse = (HttpWebResponse)response;
                        Console.WriteLine("Error code: {0}", httpResponse.StatusCode);
                        using (Stream responseData = response.GetResponseStream())
                        using (var reader = new StreamReader(responseData))
                        {
                            responseText = reader.ReadToEnd();
                        }
                    }
                    throw new WebException(string.IsNullOrEmpty(responseText) ? e.Status.ToString() : responseText, e);
                }
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }
    }
}
