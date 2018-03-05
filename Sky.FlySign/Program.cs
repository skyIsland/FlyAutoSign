using System;
using System.Threading;
using Quartz;
using Quartz.Impl;
using Sky.FlySign.Config;
using Sky.FlySign.Core;

namespace Sky.FlySign
{
    class Program
    {
       
        static void Main(string[] args)
        {           
            //new Run();
            Console.Title = "Fly社区自动签到!";
            Console.WriteLine("Service Working...");

            //
            // var o = new FlySignIn();
            //o.GetIpStr();


            var info = ConfigHelper.GetBasicConfig().FlyCfg;
            var o = new FlySignIn(info.Email, info.Pwd);
            o.StartSignIn();
            Console.ReadKey();
        }

    }

    public class Run
    {
        public Run()
        {
            // 创建一个调度器
            var factory = new StdSchedulerFactory();
            var scheduler = factory.GetScheduler();
            scheduler.Result.Start();

            // 创建一个任务对象
            IJobDetail job = JobBuilder.Create<RunJob>().WithIdentity("job1", "group1").Build();

            // 创建一个触发器
            //DateTimeOffset runTime = DateBuilder.EvenMinuteDate(DateTimeOffset.UtcNow);
            ITrigger trigger = TriggerBuilder.Create()
                .WithIdentity("trigger1", "group1")
                .WithCronSchedule("58 59 23 * * ?")     //凌晨23:59:58触发 58 59 23 * * ? 0/5 * * * * ?
                                                        //.StartAt(runTime)
                .Build();

            // 将任务与触发器添加到调度器中
            scheduler.Result.ScheduleJob(job, trigger);
            // 开始执行
            scheduler.Result.Start();
        }
    }
}
