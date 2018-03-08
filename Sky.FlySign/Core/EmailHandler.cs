using System;
using System.Net.Mail;
using System.Threading.Tasks;

namespace Sky.Core
{
    /// <summary>
    /// 邮件处理 todo:有轮子 http://www.1234.sh/post/pomelo-extensions-smtp
    /// </summary>
    public class EmailHandler
    {
        public class SendAcccount
        {
            /// <summary> SMTP服务器地址 </summary>
            public string SmtpHost { get; set; } = "";

            /// <summary> 登录账号 </summary>
            public string SmtpUser { get; set; } = "";

            /// <summary> 登录密码 </summary>
            public string SmtpPassword { get; set; } = "";

        }
        /// <summary>
        /// 发送邮件
        /// </summary>
        /// <param name="strto">接收邮件地址,多个地址用英文逗号隔开</param>
        /// <param name="strSubject">主题</param>
        /// <param name="strBody">内容</param>
        /// <param name="sendAcccount">发件人账户信息</param>
        public static void SendSmtpEMail(string strto, string strSubject, string strBody, SendAcccount sendAcccount)
        {
            // SMTP服务器地址
            string smtpHost = sendAcccount.SmtpHost;

            // 登录账号
            string smtpUser = sendAcccount.SmtpUser;

            // 登录密码
            string smtpPassword = sendAcccount.SmtpPassword;

            Task.Factory.StartNew(() =>
            {
                try
                {
                    SmtpClient client = new SmtpClient(smtpHost);
                    client.UseDefaultCredentials = false;
                    client.Credentials = new System.Net.NetworkCredential(smtpUser, smtpPassword);
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;
                    MailMessage msg = new MailMessage(smtpUser, strto, strSubject, strBody);
                    msg.BodyEncoding = System.Text.Encoding.UTF8;
                    msg.IsBodyHtml = true;
                    client.EnableSsl = false;
                    client.Port = 25;
                    msg.Priority = MailPriority.High;
                    client.Send(msg);
                }
                catch (Exception ex)
                {
                    NewLife.Log.XTrace.WriteLine("发送邮件错误。接收邮箱：" + strto + ",错误信息：" + ex.Message);
                }
            });
        }
    }
}
