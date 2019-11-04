using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace HZ.Crawler.Common.Net
{
    public class MultiParts
    {
        // 边界符
        private string Boundary;
        // 边界符
        private string BeginBoundary;
        // 最后的结束符
        private string EndBoundary;

        private List<HttpPart> List = new List<HttpPart>();
        public string ContentType
        {
            get
            {
                return string.Format("multipart/form-data; boundary={0}", Boundary);
            }
        }

        public string Url
        {
            get;
            set;
        }

        public MultiParts(string url)
        {
            this.Url = url;
            // 边界符
            this.Boundary = "----WebKitFormBoundarymOI4BzLWbEqLlrbC";//string.Format("{0}", DateTime.Now.Ticks.ToString("x"));
            // 边界符
            this.BeginBoundary = string.Format("--{0}\r\n", Boundary);
            // 最后的结束符 
            this.EndBoundary = string.Format("--{0}--\r\n", Boundary);
        }

        public void AddPart(HttpPart part)
        {
            List.Add(part);
        }
        public void AddParts(params HttpPart[] parts)
        {
            List.AddRange(parts);
        }

        public void Clear()
        {
            List.Clear();
        }

        public void RemovePart(HttpPart part)
        {
            List.Remove(part);
        }

        public void ToStream(Stream stream, Encoding encoding)
        {
            foreach (HttpPart part in this.List)
            {
                part.ToStream(stream, encoding, Boundary);
                HttpPart.StringToStream(stream, encoding, "\r\n");
            }
            HttpPart.StringToStream(stream, encoding, EndBoundary);

            stream.Flush();
        }
    }
    public abstract class HttpPart
    {
        protected HttpPart() { }

        public static void StringToStream(Stream stream, Encoding encoding, string data)
        {
            byte[] st = encoding.GetBytes(data);
            stream.Write(st, 0, st.Length);
            /*using (StreamWriter sw = new StreamWriter(stream, encoding))
            {
                sw.Write(data);
                //sw.Flush();
                //sw.Close();
            }*/
        }
        public virtual void ToStream(Stream stream, Encoding encoding, string boundary) { }
    }

    public class TxtPart : HttpPart
    {
        private string Data;
        private string Name;
        public TxtPart(string name, string data)
        {
            this.Name = name;
            this.Data = data;
        }
        public override void ToStream(Stream stream, Encoding encoding, string boundary)
        {
            string header = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"\r\n\r\n{2}",
                boundary, this.Name, this.Data);
            HttpPart.StringToStream(stream, encoding, header);
        }
    }

    public class FilePart : HttpPart
    {
        private byte[] Data;
        private string Name;
        private string FileName;
        public FilePart(byte[] data, string name, string filename)
        {
            this.Name = name;
            this.FileName = filename;
            this.Data = data;
        }
        public override void ToStream(Stream stream, Encoding encoding, string boundary)
        {
            string contentType = "application/octet-stream";
            if (FileName.EndsWith("txt"))
            {
                contentType = "text/plain";
            }
            else if (FileName.EndsWith("smil"))
            {
                contentType = "text/xml";
            }
            else if (FileName.EndsWith("jpg"))
            {
                contentType = "image/jpeg";
            }
            else if (FileName.EndsWith("png"))
            {
                contentType = "image/png";
            }
            string header = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"; filename=\"{2}\"\r\nContent-Type:{3}\r\n\r\n",
                boundary, this.Name, this.FileName, contentType);
            HttpPart.StringToStream(stream, encoding, header);
            stream.Write(this.Data, 0, this.Data.Length);
            //HttpPart.StringToStream(stream, encoding, "\r\n");
        }
    }
}
