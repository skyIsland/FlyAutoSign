﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Quartz;
using Sky.FlySign.Config;

namespace Sky.FlySign.Core
{
    public class RunJob : IJob
    {
        public Task Execute(IJobExecutionContext context)
        {

            var info = ConfigHelper.GetBasicConfig().FlyCfg;
            var o = new FlySignIn(info.Email, info.Pwd);
           
            for (int i = 0; i < 5; i++)
            {
               o.Start();
            }
            return Task.CompletedTask;
        }
    }
}
