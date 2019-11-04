using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace HZ.Crawler.Common.Net
{
    public interface IHttpResponse
    {
        string StatusDescription { get; set; }
        HttpStatusCode StatusCode { get; set; }
        WebHeaderCollection Header { get; set; }
        string Html { get; set; }
        string ResponseUri { get; set; }
        string RedirectUrl { get; }
    }
}
