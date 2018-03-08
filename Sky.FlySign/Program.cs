using System;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;
using Quartz;
using Quartz.Impl;
using Sky.FlySign.Job;
using Sky.Job;

namespace Sky.FlySign
{
    class Program
    {
       
        static void Main(string[] args)
        {           
            new Run();
            Console.Title = "Fly社区自动签到!";
            Console.WriteLine("Service Working...");


            // 
            TestRun().GetAwaiter().GetResult();

            //var o = new FlySignIn();
            //o.GetIpStr();


            //var info = ConfigHelper.GetBasicConfig().FlyCfg;
            //var o = new FlySignIn(info.Email, info.Pwd);
            //o.StartSignIn();
            Console.ReadKey();
        }
        static async Task TestRun()
        {
            try
            {
                // Grab the Scheduler instance from the Factory
                NameValueCollection props = new NameValueCollection
                {
                    { "quartz.serializer.type", "binary" }
                };
                StdSchedulerFactory factory = new StdSchedulerFactory(props);
                IScheduler scheduler = await factory.GetScheduler();

                // and start it off
                await scheduler.Start();

                // define the job and tie it to our HelloJob class
                IJobDetail job = JobBuilder.Create<TestRunJob>()
                    .WithIdentity("job1", "group1")
                    .Build();

                // Trigger the job to run now, and then repeat every 10 seconds
                ITrigger trigger = TriggerBuilder.Create()
                    .WithIdentity("trigger1", "group1")
                    .WithCronSchedule("34 24 14 * * ?")
                    .Build();

                // Tell quartz to schedule the job using our trigger
                await scheduler.ScheduleJob(job, trigger);

                // some sleep to show what's happening
                await Task.Delay(TimeSpan.FromSeconds(60));

                // and last shut down the scheduler when you are ready to close your program
                await scheduler.Shutdown();
            }
            catch (SchedulerException se)
            {
                Console.WriteLine(se);
            }
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
