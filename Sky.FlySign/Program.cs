using System;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;
using Quartz;
using Quartz.Impl;
using Sky.Core;
using Sky.FlySign.Job;
using Sky.Job;

namespace Sky.FlySign
{
    class Program
    {
       
        static void Main(string[] args)
        {           
            Run.RunTask();
            Console.Title = "Fly社区自动签到!";
            Console.WriteLine("Service Working...");

            //var info = ConfigHelper.GetBasicConfig().FlyCfg;
            //var o = new FlySignIn(info.Email, info.Pwd);
            //o.StartSignIn();

            //Test();
            Console.ReadKey();
        }

        static void Test()
        {
            var sign = new FlySignIn();
            Console.WriteLine(sign.Login("",""));
        }
    }

    public class Run
    {
        public static void RunTask()
        {
            // 创建一个调度器
            var factory = new StdSchedulerFactory();
            var scheduler = factory.GetScheduler();
            scheduler.Result.Start();

            // 创建一个任务对象
            IJobDetail job = JobBuilder.Create<RunJob>().WithIdentity("job1", "group1").Build();
            // 再来一个任务对象
            var jobByIp = JobBuilder.Create<TestRunJob>().WithIdentity("job2", "group1").Build();

            // 创建一个触发器
            //DateTimeOffset runTime = DateBuilder.EvenMinuteDate(DateTimeOffset.UtcNow);
            ITrigger trigger = TriggerBuilder.Create()
                .WithIdentity("trigger1", "group1")
                .WithCronSchedule("58 59 23 * * ?")     //凌晨23:59:58触发 58 59 23 * * ? 0/5 * * * * ? .StartAt(runTime)
                .Build();
            // 再来一个触发器
            var triggerByIp = TriggerBuilder
                        .Create()
                        .WithIdentity("trigger2", "group1")
                        .WithCronSchedule("0 02 18 * * ?").Build();

            // 将任务与触发器添加到调度器中
            scheduler.Result.ScheduleJob(job, trigger);
            scheduler.Result.ScheduleJob(jobByIp, triggerByIp);

            // 开始执行
            scheduler.Result.Start();
        }
    }
}
