using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Collections.Specialized;

namespace HZ.Crawler.Common.Net
{
    public interface IHttpClient
    {
        IHttpRequest HttpRequest { get; }
        IHttpResponse HttpResponse { get; }
        string Request(string url, HttpMethod method, string data, Encoding encoding, string contentType = null, CredentialCache mycache = null, NameValueCollection moreHeader = null);
        string Request(MultiParts mp, Encoding encoding);
    }
    public enum HttpMethod
    {
        GET = 1, POST = 2
    }
}
