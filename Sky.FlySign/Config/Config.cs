namespace Sky.FlySign.Config
{
    /// <summary>
    /// 配置信息
    /// </summary>
    public class Config
    {
        public EmailConfig EmailCfg { get; set; } = new EmailConfig();
      
         public FlyConfig FlyCfg { get; set; } = new FlyConfig();
        /// <summary>
        /// 邮箱信息
        /// </summary>
        public class EmailConfig
        {
            /// <summary> SMTP服务器地址 </summary>
            public string SmtpHost { get; set; } = "Smtp.163.com";

            /// <summary> 登录账号 </summary>
            public string SmtpUser { get; set; } = "";

            /// <summary> 登录密码 </summary>
            public string SmtpPassword { get; set; } = "";
        }

        /// <summary>
        /// 账户信息
        /// </summary>
        public class FlyConfig
        {
            /// <summary> 登录账号 </summary>
            public string Email { get; set; } = "";

            /// <summary> 登录密码 </summary>
            public string Pwd { get; set; } = "";
        }
    }
}
