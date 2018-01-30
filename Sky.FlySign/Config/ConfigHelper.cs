using System;
using System.IO;
using System.Text;
using NewLife.Serialization;

namespace Sky.FlySign.Config
{
    /// <summary>
    /// 取出配置信息
    /// </summary>
    public class ConfigHelper
    {
        /// <summary>
        /// 获取配置信息
        /// </summary>
        /// <returns></returns>
        public static Config GetBasicConfig()
        {
            string result;
            var fileName = "Config.json";

            // 不存在文件 则自动生成
            if (!File.Exists(fileName))
            {
                var fs = File.Create(fileName);
                fs.Close();
                var obj = new Config();
               var jsonStr = obj.ToJson();
                using (StreamWriter sw = new StreamWriter(fileName))
                {
                    sw.Write(jsonStr);
                }               
            }
            using (var sw=new StreamReader(fileName,Encoding.UTF8))
            {
                 result = sw.ReadToEnd();
            }
            return result.ToJsonEntity<Config>(); ;
        }
        /// <summary>
        /// 保存配置信息
        /// </summary>
        /// <param name="config"></param>
        public static void SetBasicConfig(Config config)
        {
            var path = "Config.json";
            var fs = File.Create(path);
            fs.Close();
            var jsonStr = config.ToJson();
            var fr = new StreamWriter(path);
            fr.Write(jsonStr);
            fr.Close();
        }       
    }
}
