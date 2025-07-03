using System;
using System.Threading.Tasks;
using Avalonia.Threading;
using LSL.Common.Contracts;
using ReactiveUI.Fody.Helpers;

namespace LSL.ViewModels;

public class MonitorViewModel : RegionalVMBase
{
    [Reactive] public RangedObservableCollection<uint> CpuMetrics { get; set; } = new(30, 0);

    public MonitorViewModel(AppStateLayer appState, ServiceConnector connector) : base(appState, connector)
    {
        Task.Run(RandomAdd);
    }

    private async Task RandomAdd()
    {
        var rand = new Random();
        while (true)
        {
            int val = rand.Next(0, 101);
            await Dispatcher.UIThread.InvokeAsync(() => { CpuMetrics.Add((uint)val); });
            await Task.Delay(1000);
        }
    }
}