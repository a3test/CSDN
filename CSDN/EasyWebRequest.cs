using System;
using System.IO;
using System.IO.Compression;
using System.Net;

namespace CSDN
{
    public class EasyWebRequest
    {
        public CookieContainer cookies { get; set; }

        private HttpWebRequest _req;
        private HttpWebResponse _res;

        public EasyWebRequest()
        {
            cookies = new CookieContainer();
        }

        public string Request(Uri url, RequestMethod method, byte[] postData = null)
        {
            _req = (HttpWebRequest) WebRequest.Create(url);
            _req.Method = method.ToString();
            _req.ProtocolVersion = HttpVersion.Version11;
            _req.Host = url.Host;
            _req.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:38.0) Gecko/20100101 Firefox/38.0";
            _req.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            _req.Headers.Add(HttpRequestHeader.AcceptLanguage, "zh-CN,zh;q=0.8,en-US;q=0.5,en;q=0.3");
            _req.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip, deflate");
            _req.KeepAlive = true;

            _req.CookieContainer = cookies;

            if (method == RequestMethod.POST)
            {
                _req.ContentType = "application/x-www-form-urlencoded";
                if (postData != null && postData.Length > 0)
                {
                    using (Stream tmp = _req.GetRequestStream())
                    {
                        tmp.Write(postData, 0, postData.Length);
                    }
                }
            }

            _res = (HttpWebResponse) _req.GetResponse();

            Stream stream = null;
            switch (_res.ContentEncoding.ToLower())
            {
                case "gzip":
                    stream = new GZipStream(_res.GetResponseStream(), CompressionMode.Decompress);
                    break;
                case "deflate":
                    stream = new DeflateStream(_res.GetResponseStream(), CompressionMode.Decompress);
                    break;
                default:
                    stream = _res.GetResponseStream();
                    break;
            }
            string html;
            using (StreamReader sr = new StreamReader(stream))
            {
                html = sr.ReadToEnd();
            }

            return html;
        }

        public string Request(string url, RequestMethod method, byte[] postData = null)
        {
            return Request(new Uri(url), method, postData);
        }
    }

    public enum RequestMethod
    {
        POST,
        GET
    }
}