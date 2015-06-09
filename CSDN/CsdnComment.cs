using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;

namespace CSDN
{
    internal class CsdnComment
    {
        private EasyWebRequest request;

        public DownloadList Downloads { get; private set; }

        public bool LoginState { get; private set; }

        public CsdnComment()
        {
            request = new EasyWebRequest();
            LoginState = false;
            Downloads = new DownloadList();
        }

        public bool Login(string username, string password)
        {
            const string url = "http://passport.csdn.net/account/login";

            string html = request.Request(url, RequestMethod.GET);

            LoginParam loginParam = new LoginParam(username, password);
            loginParam.GetParamByHtml(html);
            byte[] postData = Encoding.UTF8.GetBytes(loginParam.ToString());

            html = request.Request(url, RequestMethod.POST, postData);

            CheckLoginState(html);

            Console.WriteLine(LoginState ? "登录成功" : "登录失败");

            return LoginState;
        }

        private bool CheckLoginState(string html)
        {
            LoginState = html.IndexOf("redirect_back") > -1;
            return LoginState;
        }

        public void GetMyDownloads()
        {
            if (LoginState)
            {
                const string url = "http://download.csdn.net/my/downloads";
                string html = request.Request(url, RequestMethod.GET);

                Downloads = new DownloadList();

                Console.WriteLine("正在读取页码.");

                if (!GetPageInfo(html))
                {
                    throw new Exception("获取页码信息失败。");
                }

                Console.WriteLine("页码读取完毕,共{0}页{1}条数据.", Downloads.PageCount, Downloads.Count);
                Console.WriteLine("准备读取下载列表.");

                GetDownList();
                if (Downloads.Detail.Count == 0)
                {
                    throw new Exception("获取下载资源列表失败。");
                }

                Console.WriteLine("下载列表读取完毕.其中未评价的共{0}条.", Downloads.Detail.Count(m => m.State == 0));

            }
            else
            {
                throw new Exception("请先登录。");
            }
        }

        private bool GetPageInfo(string html)
        {
            const string pattern = @"共(\d+)个.+共(\d+)页";

            MatchCollection matches = Regex.Matches(html, pattern);

            if (matches.Count > 0)
            {
                string scount = matches[0].Groups[1].Value;
                string spagecount = matches[0].Groups[2].Value;

                if (!string.IsNullOrWhiteSpace(scount) && !string.IsNullOrWhiteSpace(spagecount))
                {
                    int count, pagecount;
                    if (int.TryParse(scount, out count) && int.TryParse(spagecount, out pagecount))
                    {
                        Downloads.Count = count;
                        Downloads.PageCount = pagecount;

                        return true;
                    }
                }

            }

            return false;
        }

        private void GetDownList()
        {
            Downloads.Detail = new List<DownloadDetail>();

            for (int i = 0; i < Downloads.PageCount; i++)
            {
                Console.WriteLine("正在读取第{0}页数据.", i + 1);

                const string url = "http://download.csdn.net/my/downloads/{0}";
                string html = request.Request(string.Format(url, i + 1), RequestMethod.GET);

                const string pattern = @"<dt>\s+?[\s\S]+?<div class=""btns"">[\s\S]*?<[\s\S]+?>(.+)<[\s\S]+?<h3>[\s\S]+?=""(.+?)"">(.+?)</a>[\s\S]*?>(.*?)<[\s\S]*?</dt>";
                MatchCollection matches = Regex.Matches(html, pattern);
                if (matches.Count > 0)
                {
                    foreach (Match match in matches)
                    {
                        string state = match.Groups[1].Value;
                        string detailurl = match.Groups[2].Value;
                        string name = match.Groups[3].Value;
                        float integral;
                        float? integral2;
                        if (float.TryParse(match.Groups[4].Value, out integral))
                        {
                            integral2 = integral;
                        }
                        else
                        {
                            integral2 = null;
                        }

                        string sourceid = detailurl.Split('/').Length == 4 ? detailurl.Split('/')[3] : "";

                        Downloads.Detail.Add(new DownloadDetail
                        {
                            State = DownloadDetail.GetState(state),
                            Name = name,
                            UrlPart = detailurl,
                            Sourceid = sourceid,
                            Integral = integral2
                        });
                    }
                }

                Console.WriteLine("第{0}页数据读取完毕.", i + 1);
            }
        }

        public void Comment()
        {
            CommontCommon(Downloads.Detail.FindAll(m => m.State == 0 && m.Integral == 0), SourceType.ZeroIntegral);
        }

        private void CommontCommon(List<DownloadDetail> details, SourceType type)
        {
            if (LoginState)
            {
                if (Downloads.Detail.Count > 0)
                {
                    const string cup1 = "http://download.csdn.net/index.php/comment/post_comment?{0}";

                    string cu, html;
                    Random random = new Random();

                    int i = 0;
                    foreach (DownloadDetail detail in details)
                    {
                        Console.WriteLine("正在评论 {0} >>> {1} >>> 进度 {2}/{3}", GetSourceTypeName(type), detail.Name, ++i, details.Count);

                        string content = GengrateComment(detail, type);
                        CommentParam cp = new CommentParam
                        {
                            content = content,
                            jsonpcallback = "jsonp" + DateTime.Now.getTime(),
                            rating = random.Next(4, 6).ToString(),
                            sourceid = detail.Sourceid,
                            t = DateTime.Now.getTime().ToString()
                        };
                        cu = string.Format(cup1, cp);

                        html = request.Request(cu, RequestMethod.GET);

                        Console.WriteLine(CheckCommentState(html) ? "{0} >>>评论成功。" : "{0} >>>评论失败。", detail.Name);

                        Thread.Sleep(1000*70);
                    }
                }
                else
                {
                    throw new Exception("请先获取下载列表。");
                }
            }
            else
            {
                throw new Exception("请先登录。");
            }
        }

        public bool CheckCommentState(string html)
        {
            return html.IndexOf("\"succ\":1") > -1;
        }

        private string GengrateComment(DownloadDetail detail, SourceType type)
        {
            return "";
        }

        private string GetSourceTypeName(SourceType type)
        {
            string retstr = "";
            switch (type)
            {
                case SourceType.ZeroIntegral:
                    retstr = "0积分资源";
                    break;
                case SourceType.NotZeroIntegral:
                    retstr = "非0积分资源";
                    break;
                case SourceType.All:
                    retstr = "全部资源";
                    break;
            }
            return retstr;
        }

    }

    #region DownloadList

    public class DownloadList
    {
        public int Count { get; set; }

        public int PageCount { get; set; }

        public List<DownloadDetail> Detail { get; set; }

    }

    #endregion

    #region DownloadDetail

    public class DownloadDetail
    {
        public int State { get; set; }

        public string UrlPart { get; set; }

        public string Sourceid { get; set; }

        public string Name { get; set; }

        public float? Integral { get; set; }

        public static int GetState(string state)
        {
            int s;
            switch (state)
            {
                case "立即评价，通过可返分":
                    s = 0;
                    break;
                case "已评价":
                    s = 1;
                    break;
                default:
                    s = -1;
                    break;
            }
            return s;
        }
    }

    #endregion

    #region LoginParam

    public class LoginParam
    {
        public string username { get; set; }

        public string password { get; set; }

        public string lt { get; set; }

        public string execution { get; set; }

        public string _eventId { get; set; }

        public LoginParam(string username, string password)
        {
            this.username = username;
            this.password = password;
        }

        public void GetParamByHtml(string html)
        {
            Regex regex = new Regex("<input type=\"hidden\" name=\"(.+)\" value=\"(.+)\" />");

            MatchCollection matches = regex.Matches(html);

            if (matches.Count == 0)
            {
                throw new Exception("未能从登录页面找到隐藏登录参数。");
            }

            foreach (Match match in matches)
            {
                switch (match.Groups[1].Value)
                {
                    case "lt":
                        lt = match.Groups[2].Value;
                        break;
                    case "execution":
                        execution = match.Groups[2].Value;
                        break;
                    case "_eventId":
                        _eventId = match.Groups[2].Value;
                        break;
                }
            }
            if (!CheckHiddenParam())
            {
                throw new Exception("匹配隐藏登录参数失败。");
            }
        }

        public bool CheckHiddenParam()
        {
            if (string.IsNullOrEmpty(lt) || string.IsNullOrEmpty(execution) || string.IsNullOrEmpty(_eventId))
            {
                return false;
            }
            return true;
        }

        public override string ToString()
        {
            string us = HttpUtility.UrlEncode(username, Encoding.GetEncoding("GB2312"));
            string pa = HttpUtility.UrlEncode(password, Encoding.GetEncoding("GB2312"));
            string l = HttpUtility.UrlEncode(lt, Encoding.GetEncoding("GB2312"));
            string ex = HttpUtility.UrlEncode(execution, Encoding.GetEncoding("GB2312"));
            string ev = HttpUtility.UrlEncode(_eventId, Encoding.GetEncoding("GB2312"));

            return string.Format("username={0}&password={1}&lt={2}&execution={3}&_eventId={4}", us, pa, l, ex, ev);
        }
    }

    #endregion

    #region CommentParam

    public class CommentParam
    {
        public string content { get; set; }

        public string jsonpcallback { get; set; }

        public string rating { get; set; }

        public string sourceid { get; set; }

        public string t { get; set; }

        public override string ToString()
        {
            string co = HttpUtility.UrlEncode(content, Encoding.GetEncoding("GB2312"));
            return string.Format("jsonpcallback={0}&sourceid={1}&content={2}&rating={3}&t={4}", jsonpcallback, sourceid, co, rating, t);
        }
    }

    #endregion

    #region SourceType

    public enum SourceType
    {
        ZeroIntegral,
        NotZeroIntegral,
        All
    }

    #endregion
}