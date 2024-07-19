using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LSL.Services
{
    public partial class Daemon : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // 执行后台任务  
                // ...  

                await Task.Delay(1000, stoppingToken); // 模拟长时间运行的任务  
            }
        }
    }
}
