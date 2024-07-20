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
    public partial class DaemonService : BackgroundService
    {
        private readonly CancellationToken _cancellationToken;
        public DaemonService(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
        }
        public async Task StartAsync()
        {
            try
            {
                while (!_cancellationToken.IsCancellationRequested)
                {
                    // 在这里执行您的后台逻辑
                    await Task.Delay(1000, _cancellationToken);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Debug.WriteLine($"DaemonService error: {ex.Message}");
            }
        }

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
