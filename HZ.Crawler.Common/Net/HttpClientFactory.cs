using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HZ.Crawler.Common.Net
{
    public class HttpClientFactory
    {
        public static IHttpClient Create()
        {
            return new HttpClient();
        }        
    }
}
