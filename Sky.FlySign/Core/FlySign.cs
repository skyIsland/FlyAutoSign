using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        /// 签到活跃榜地址
        /// </summary>
        private string _TopResultUrl { get; set; } = "http://fly.layui.com/top/signin/";

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
        /// Cookie数据
        /// </summary>
        private Dictionary<string, Cookie> _cookiesDic = new Dictionary<string, Cookie>();

        private string _logPath = @"\Log";
        #endregion

        #region 构造函数
        public FlySignIn()
        {

        }

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

        #region 登录

        /// <summary>
        /// 登录
        /// </summary>
        /// <param name="loginName"></param>
        /// <param name="loginPwd"></param>
        /// <returns></returns>
        private bool Login(string loginName, string loginPwd)
        {
            var isLoginSuccess = false;
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
            }
            else
            {
                isLoginSuccess = true;
            }

            return isLoginSuccess;
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
                    var firstIndexof = p.IndexOf("加") + 1;
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

        #region 检测是否需要签到
        /// <summary>
        /// 是否需要签到
        /// </summary>
        /// <returns></returns>
        private Result CheckIsNeedSign()
        {
            // 这里的 status 是请求是否成功
            var signResult = GetStatus(_statusUrl, GetCookieStr());

            return signResult;
        }
        #endregion

        #region 签到状态

        /// <summary>
        /// 获取签到状态
        /// </summary>
        /// <param name="url">请求地址</param>
        /// <param name="cookieData">cookie</param>
        /// <returns>是否需要签到</returns>
        private Result GetStatus(string url, string cookieData)
        {
            //bool flag = false;
            var resultStr = DownloadString(url, true, "", cookieData);
            //var resultObj = resultStr.ToJsonEntity<Result>();
            //if (resultObj.status == 0)
            //{
            //    // 赋值token
            //    _token = "token=" + resultObj.data.token;
            //    flag = resultObj.data.signed;
            //}
            //else
            //{
            //    SendEmail($"获取签到状态失败:{resultObj.msg}");
            //}
            return resultStr.ToJsonEntity<Result>();
        }

        #endregion

        #region 签到请求

        /// <summary>
        /// 签到
        /// </summary>
        /// <param name="url"></param>
        /// <param name="parameter">token参数</param>
        /// <param name="cookieData">cookie</param>
        private Result SignIn(string url, string parameter, string cookieData)
        {

            var resultStr = DownloadString(url, true, parameter, cookieData);

            var resultObj = resultStr.ToJsonEntity<Result>();

            return resultObj;
        }
        #endregion

        #region 签到
        /*
         * 1.检测是否需要签到
         *   1.1 需要登录 -> 登录更新 _cookiesDic
         *   1.2 不需要登录-> 签到
         * 2.
         *   2.1 需要签到 -> 签到 返回签到结果
         *   2.2 不需要签到 -> 返回签到成功的结果
         *   
         */
        /// <summary>
        /// 签到        
        /// </summary>
        public void StartSignIn()
        {
            // 1.
            var checkSignResult = CheckIsNeedSign();

            if (checkSignResult.status == 0)
            {
                // 1.1
                if (checkSignResult.msg?.Contains("登入") ?? true)
                {
                    var loginResult = Login(_loginName, _loginPwd);
                    if (loginResult)
                    {
                        checkSignResult = CheckIsNeedSign();
                        _token = checkSignResult.data.token;
                    }
                }

                var signed = checkSignResult.data.signed;

                // 2.1
                if (!signed)
                {
                    _isTrackCookies = false; // 关闭跟踪Cookie todo:需要了解清楚登录之后 Cookie是怎么返回到响应的
                    var cookieStr = GetCookieStr();
                    var signResult = SignIn(_signUrl, "token=" + _token, cookieStr);

                    var msg = string.Format(
                        "<br>签到 {0}",
                        signResult.status == 0 ? "成功" : "失败");

                    msg += "<br> " + GetOrderStr();
                    WriteLog(msg);
                    SendEmail(msg);

                }
                else // 2.2
                {
                    var msg = "已经签到成功了,无需再签到!";
                    msg += "<br> " + GetOrderStr();
                    WriteLog(msg);
                }

            }
            else
            {
                WriteLog($"检测是否需要登录发生错误,原因: {checkSignResult.msg}");
            }
        }

        #endregion

        #region 获取签到活跃榜信息
        /// <summary>
        /// 获取签到活跃榜信息
        /// </summary>
        /// <returns></returns>
        private TopResult GetTopResult()
        {
            var resultStr = DownloadString(_TopResultUrl);
            var resultObj = resultStr.ToJsonEntity<TopResult>();
            return resultObj;
        }

        public string GetOrderStr()
        {
            var topResult = GetTopResult();
            var info = topResult.data[1].ToList();
            var mySignInfo = info.Where(p => p.uid == 2098488).ToList(); //todo:这里写死了
            var msg = string.Empty;
            if (mySignInfo.Count > 0)
            {
                var signInfo = mySignInfo.First();
                msg = $"签到时间 {signInfo.time:yyyy-MM-dd HH:mm:ss} 签到排名:{info.IndexOf(signInfo) + 1} 签到天数{signInfo.days}";
            }
            else
            {
                msg = "签到排名暂未进入前20!";
            }
            return msg;
        }

        public void GetIpStr()
        {
            var ipInfoList = new List<UserIpInfo>();
            var topResult = GetTopResult();
            if (topResult.status == 0)
            {
                var data = topResult.data;

                foreach (var item in data)
                {
                    foreach (var u in item)
                    {
                        var ip = u.info.ToJsonEntity<IpInfo>();
                        var userInfo = u.user;
                        var userName = userInfo.username;
                        if (ipInfoList.Count(p => p.UserName == userName) == 0)
                        {
                            //var ipAddress = ip.ip.IPToAddress();todo:newlife.core nethelper有bug
                            var ipAddress = GetPosition(ip.ip);
                            ipInfoList.Add(new UserIpInfo { UserName = userName, IpAddress = ipAddress });
                        }
                    }

                }
            }
            var sb = new StringBuilder();
            sb.Append("<br>");
            ipInfoList.ForEach(p =>
            {
                sb.Append("用户名:" + p.UserName + "&nbsp;&nbsp;&nbsp;&nbsp;");
                sb.Append("Ip地址:" + p.IpAddress + "&nbsp;&nbsp;&nbsp;&nbsp;");
                sb.Append("<br>");
            });

            SendEmail(sb.ToString());
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

        /// <summary>
        /// 签到活跃榜
        /// </summary>
        private class TopResult
        {
            public int status { get; set; }
            public SignInfo[][] data { get; set; }
        }

        /// <summary>
        /// 第一个数组最新签到 第二个数组今日最快 第三个数组总签到榜
        /// </summary>
        private class SignInfo
        {
            public int uid { get; set; }
            /// <summary>
            /// 连续签到天数
            /// </summary>
            public int days { get; set; }
            /// <summary>
            /// 签到时间
            /// </summary>
            public DateTime time { get; set; }
            public long msec { get; set; }
            public string token { get; set; }
            /// <summary>
            /// 签到的ip地址
            /// </summary>
            public string info { get; set; }
            public User user { get; set; }
        }


        private class User
        {
            public string username { get; set; }
            public string avatar { get; set; }
        }

        private class UserIpInfo
        {
            public string UserName { get; set; }

            public string IpAddress { get; set; }
        }

        private class IpInfo
        {
            public string ip { get; set; }
        }

        /// <summary>
        /// 接口返回类
        /// </summary>
        public class IpPosition
        {
            /// <summary>
            /// 返回结果状态值
            /// </summary>
            public string status { get; set; }
            /// <summary>
            /// 返回状态说明
            /// </summary>
            public string info { get; set; }
            /// <summary>
            /// 状态码
            /// </summary>
            public string infocode { get; set; }
            /// <summary>
            /// 省份名称
            /// </summary>
            public string province { get; set; }
            /// <summary>
            /// 城市名称
            /// </summary>
            public string city { get; set; }
            /// <summary>
            /// 城市的adcode编码
            /// </summary>
            public string adcode { get; set; }
            /// <summary>
            /// 所在城市矩形区域范围
            /// </summary>
            public string rectangle { get; set; }
        }
        #endregion

        #region Ip地址
        /// <summary>  
        /// Ip解析  
        /// </summary>  
        /// <param name="strIp">需要解析的IP地址</param>  
        /// <param name="key">调用接口的key</param>  
        /// <returns></returns>  
        public static string GetPosition(string strIp, string key = "237ccec56bac36ee4bc22740357655f3")
        {
            var msg = string.Empty;
            var url = "http://restapi.amap.com/v3/ip?ip=" + strIp + "&key=" + key;
            var http = new WebClient { Encoding = Encoding.UTF8 };
            var response = http.DownloadString(url);
            try
            {
                var result = response.ToJsonEntity<IpPosition>();
                if (result.status.Equals("1"))
                {
                    msg = result.province + "," + result.city;
                }
            }
            catch (Exception)
            {
                msg = "错误,无法解析Ip来源!";
            }

            return msg;
        }
        #endregion

    }
}