using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Parser.Html;
using NewLife.Serialization;
using Sky.FlySign.Config;

namespace Sky.FlySign.Core
{

    /// <summary>
    /// Fly社区签到
    /// </summary>
    public class FlySignIn
    {

        #region 相关字段

        /// <summary>
        /// 登录账号
        /// </summary>
        private string _loginName;

        /// <summary>
        /// 登录密码
        /// </summary>
        private string _loginPwd;

        /// <summary>
        /// 登录地址
        /// </summary>
        private string _loginUrl = "http://fly.layui.com/user/login/";

        /// <summary>
        /// 签到地址
        /// </summary>
        private string _signUrl = "http://fly.layui.com/sign/in";

        /// <summary>
        /// 签到状态地址
        /// </summary>
        private string _statusUrl = "http://fly.layui.com/sign/status";

        /// <summary>
        /// token todo:获取token (sign/status 该链接可得到当前签到状态以及token信息(值)) 
        /// </summary>
        private string _token { get; set; }

        ///// <summary>
        ///// Cookie
        ///// </summary>
        //private string CookieData { get; set; }

        /// <summary>
        /// 是否跟踪cookie
        /// </summary>
        private bool _isTrackCookies = false;

        /// <summary>
        /// Cookie字典
        /// </summary>
        private Dictionary<string, Cookie> _cookiesDic = new Dictionary<string, Cookie>();

        private string _logPath = @"\Log";
        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数传参
        /// </summary>
        /// <param name="loginName"></param>
        /// <param name="loginPwd"></param>
        public FlySignIn(string loginName, string loginPwd)
        {
            this._loginName = loginName;
            this._loginPwd = loginPwd;
        }

        #endregion

        #region Main入口

        /// <summary>
        /// 程序入口
        /// </summary>
        public void Start()
        {
            Login(_loginName, _loginPwd);

        }

        #endregion

        #region 登录

        private void Login(string loginName, string loginPwd)
        {
            _isTrackCookies = true;

            var str = DownloadString(_loginUrl, false);

            // parser
            var document = new HtmlParser().Parse(str);

            // get vercodeText
            var vercodeText = document
                .QuerySelector("#LAY_ucm > div > div > form > div:nth-child(3) > div.layui-form-mid > span")
                .TextContent;

            // write vercodeText
            WriteLog($"当前人类验证题目:{vercodeText}");

            var answer = GetAnswer(vercodeText);

            // login
            var cookieStr = GetCookieStr();
            var parameter = $"email={loginName}&pass={loginPwd}&vercode={answer}";
            var response = DownloadString(_loginUrl, true, parameter, cookieStr).ToJsonEntity<Result>();
            if (response.status == 1)
            {
                var message = $"登录失败,原因:{response.msg},当前人类验证题目:{vercodeText}";
                WriteLog(message);
                SendEmail(message);

                return;
            }

            // getSignStatus
            _isTrackCookies = false;

            cookieStr = GetCookieStr();
            var signed = GetStatus(_statusUrl, GetCookieStr());
            if (!signed)
            {
                SignIn(_signUrl, _token, cookieStr);
            }
            else
            {
                WriteLog("已经签到成功了!无需再签到!");
            }
            // 

        }

        /// <summary>
        /// 从题库中获取人类验证问题答案
        /// </summary>
        /// <param name="vercodeText">人类验证问题</param>
        /// <returns>人类验证问题答案</returns>
        private string GetAnswer(string vercodeText)
        {
            string result;
            if (vercodeText.Contains("请在输入框填上") || vercodeText.Contains("请在输入框填上字符"))
            {
                result = vercodeText.Split("：")[1];
            }
            else if (vercodeText.Contains("加") && vercodeText.Contains("等于几"))
            {
                var firstNumber = vercodeText.Split("加")[0].ToInt();
                Func<string, int> op = p =>
                {
                    var firstIndexof = p.IndexOf("加")+1;
                    var lastIndexof = p.IndexOf("等于几");
                    var length = lastIndexof - firstIndexof;
                    return p.Substring(firstIndexof, length).ToInt();
                };

                result = (firstNumber + op(vercodeText)).ToString();
            }
            else
            {
                result = _vercodeBook[vercodeText];
            }
            return result;
        }

        /// <summary>
        /// 人类验证题库
        /// </summary>
        private readonly Dictionary<string, string> _vercodeBook = new Dictionary<string, string>
        {
            {"a和c之间的字母是？","b" },
            {"layui 的作者是谁？","贤心" },
            {"\"100\" > \"2\" 的结果是 true 还是 false？","false" },// 正确答案应该是true 但是fly社区的答案是false
            //{"请在输入框填上字符：ejd1egzl5688gdk7s1exw29","ejd1egzl5688gdk7s1exw29" },
            {"贤心是男是女？","男" },
            {"爱Fly社区吗？请回答：爱","爱" },
            //{ "请在输入框填上：我爱layui","我爱layui" },
            { "Fly社区采用 Node.js 编写，yes or no？","yes" },
            { "\"1 3 2 4 6 5 7 __\" 请写出\"__\"处的数字","9" },
            { "Node.js 诞生于哪一年？","2009" }
            //{ "68加20等于几？","88" },
            
        };
        #endregion

        #region 签到状态

        /// <summary>
        /// 获取签到状态
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="cookieData">cookie</param>
        /// <returns>是否需要签到</returns>
        private bool GetStatus(string url, string cookieData)
        {
            bool flag = false;
            var resultStr = DownloadString(url, true, "", cookieData);
            var resultObj = resultStr.ToJsonEntity<Result>();
            if (resultObj.status == 0)
            {
                // 赋值token
                _token = "token=" + resultObj.data.token;
                flag = resultObj.data.signed;
            }
            else
            {
                SendEmail($"获取签到状态失败:{resultObj.msg}");
            }
            return flag;
        }

        #endregion

        #region 签到请求

        /// <summary>
        /// 签到
        /// </summary>
        /// <param name="url"></param>
        /// <param name="parameter">token参数</param>
        /// <param name="cookieData">cookie</param>
        private void SignIn(string url, string parameter, string cookieData)
        {

            var resultStr = DownloadString(url, true, parameter, cookieData);

            var resultObj = resultStr.ToJsonEntity<Result>();

            // 
            var msg = string.Format("Fly社区签到信息如下:<br>签到 {0},消息: {1}", resultObj.status == 0 ? "成功" : "失败",
                resultObj.msg);

            WriteLog(msg);

            // 发送邮件
            SendEmail(msg);
        }
        #endregion

        #region Http

        /// <summary>
        /// 简单的fly社区获取数据和html
        /// </summary>
        /// <param name="url">请求链接</param>
        /// <param name="isPost">是否为post请求</param>
        /// <param name="parameter">附加参数</param>
        /// <param name="cookieData">cookie</param>
        /// <returns></returns>
        private string DownloadString(string url, bool isPost = true, string parameter = "", string cookieData = "")
        {
            string resultStr = "";
            var request = (HttpWebRequest)WebRequest.Create(url);



            // cookie 
            if (!string.IsNullOrWhiteSpace(cookieData))
            {
                request.Headers.Add(HttpRequestHeader.Cookie, cookieData);
            }
            request.Referer = "http://fly.layui.com/";
            request.UserAgent =
                "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/50.0.2661.102 Safari/537.36";

            // post数据相关
            if (isPost)
            {
                // 定义相关请求头
                request.Accept = "application/json, text/javascript, */*; q=0.01";
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";

                // 需要传参数
                if (!parameter.IsNullOrWhiteSpace())
                {
                    byte[] byteData = Encoding.UTF8.GetBytes(parameter);
                    request.ContentLength = byteData.Length;
                    using (Stream reqStream = request.GetRequestStream())
                    {
                        reqStream.Write(byteData, 0, byteData.Length);
                    }
                }
            }
            else
            {
                request.Method = "GET";
            }



            // 发出请求
            using (var response = (HttpWebResponse)request.GetResponse())
            {
                // isTrackCookies
                if (_isTrackCookies)
                {
                    // TrackCookies(response.Cookies);
                    CookieCollection cc = new CookieCollection();
                    string cookieString = response.Headers[HttpResponseHeader.SetCookie];
                    if (!string.IsNullOrWhiteSpace(cookieString))
                    {
                        var spilit = cookieString.Split(';');
                        foreach (string item in spilit)
                        {
                            var kv = item.Split('=');
                            if (kv.Length == 2)
                                cc.Add(new Cookie(kv[0].Trim(), kv[1].Trim()));
                        }
                    }
                    TrackCookies(cc);
                }
                using (StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.Default))
                {
                    resultStr = reader.ReadToEnd();

                }
            }
            return resultStr;
        }

        /// <summary>
        /// 跟踪cookies
        /// </summary>
        /// <param name="cookies"></param>
        private void TrackCookies(CookieCollection cookies)
        {
            if (!_isTrackCookies) return;
            if (cookies == null) return;
            foreach (Cookie c in cookies)
            {
                if (_cookiesDic.ContainsKey(c.Name))
                {
                    _cookiesDic[c.Name] = c;
                }
                else
                {
                    _cookiesDic.Add(c.Name, c);
                }
            }

        }

        /// <summary>
        /// 格式cookies
        /// </summary>
        private string GetCookieStr()
        {
            StringBuilder sb = new StringBuilder();
            foreach (KeyValuePair<string, Cookie> item in _cookiesDic)
            {
                if (!item.Value.Expired)
                {
                    if (sb.Length == 0)
                    {
                        sb.Append(item.Key).Append("=").Append(item.Value.Value);
                    }
                    else
                    {
                        sb.Append("; ").Append(item.Key).Append(" = ").Append(item.Value.Value);
                    }
                }
            }
            return sb.ToString();

        }
        #endregion

        #region SendEmail & WriteLog

        /// <summary>
        /// 发送邮件
        /// </summary>
        /// <param name="message"></param>
        private void SendEmail(string message)
        {
            try
            {
                var emailCfg = ConfigHelper.GetBasicConfig().EmailCfg;
                EmailHandler.SendSmtpEMail("ismatch@qq.com", "Fly社区签到结果", "Fly社区签到:<br> &nbsp;&nbsp;" + message,
                    new EmailHandler.SendAcccount
                    {
                        SmtpHost = emailCfg.SmtpHost,
                        SmtpUser = emailCfg.SmtpUser,
                        SmtpPassword = emailCfg.SmtpPassword
                    });
            }
            catch (Exception ex)
            {
                WriteLog($"发送邮件错误,{ex.Message}!");
            }
        }

        /// <summary>
        /// 日志
        /// </summary>
        /// <param name="message"></param>
        private void WriteLog(string message)
        {
            NewLife.Log.XTrace.LogPath = _logPath;
            NewLife.Log.XTrace.WriteLine(message);
        }
        #endregion

        #region Fly社区Post请求返回结果类

        private class Result
        {
            /// <summary>
            /// 返回状态 0 成功 1 失败
            /// </summary>
            public int status { get; set; }

            /// <summary>
            /// 返回信息
            /// </summary>
            public string msg { get; set; }

            public Data data { get; set; }

            public class Data
            {
                /// <summary>
                /// 连续签到天数
                /// </summary>
                public int days { get; set; }

                /// <summary>
                /// 飞吻
                /// </summary>
                public int experience { get; set; }

                /// <summary>
                /// 签到状态
                /// </summary>
                public bool signed { get; set; }

                public string token { get; set; }
            }
        }


        #endregion

    }
}