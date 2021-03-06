﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Quartz;
using Sky.Config;
using Sky.Core;

namespace Sky.Job
{
    public class RunJob : IJob
    {
        public Task Execute(IJobExecutionContext context)
        {

            var info = ConfigHelper.GetBasicConfig().FlyCfg;
            var o = new FlySignIn(info.Email, info.Pwd);

            for (int i = 0; i < 10; i++)
            {
                //if (i == 0)
                //{
                //    // 第一次执行慢一点
                //    Thread.Sleep(500);
                //}
                o.StartSignIn();
            }
            return Task.CompletedTask;
        }
    }
}
