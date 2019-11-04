using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;
using System.Net;

namespace HZ.Crawler.Common.Net
{
    class HttpRequest : IHttpRequest
    {
        public CookieContainer CookieContainer { get; set; }
        public string Accept { get; set; }
        public string Referer { get; set; }
        public NameValueCollection Headers { get; set; }

        public string UserAgent { get; set; }
        public HttpRequest()
        {
            this.CookieContainer = new CookieContainer(128, 128, 4096);
            this.Headers = new NameValueCollection();
        }

    }
}
