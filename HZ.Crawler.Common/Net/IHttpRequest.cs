using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;
using System.Net;

namespace HZ.Crawler.Common.Net
{
    public interface IHttpRequest
    {
        CookieContainer CookieContainer { get; set; }
        string Accept { get; set; }
        string Referer { get; set; }
        NameValueCollection Headers { get; set; }
        string UserAgent { get; set; }
    }
}
