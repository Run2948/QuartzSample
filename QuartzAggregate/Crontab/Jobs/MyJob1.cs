using Microsoft.Extensions.Logging;
using Quartz;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace QuartzAggregate.Crontab.Jobs
{
    public class MyJob1 : IJob
    {
        /// <summary>
        /// 日志
        /// </summary>
        private readonly ILogger<MyJob1> _logger;

        public MyJob1(ILogger<MyJob1> logger)
        {
            _logger = logger;
        }

        public Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation($"{DateTime.Now}  执行了我，我是 【job1】");

            return Task.CompletedTask;
        }
    }
}
