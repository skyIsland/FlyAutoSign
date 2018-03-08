using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Quartz;
using Sky.Core;

namespace Sky.FlySign.Job
{
    public class TestRunJob:IJob
    {
        public Task Execute(IJobExecutionContext context)
        {
            new FlySignIn().GetIpStr();

            return Task.CompletedTask;
        }
    }
}
