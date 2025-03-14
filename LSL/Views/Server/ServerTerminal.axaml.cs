using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.ReactiveUI;
using LSL.Services;
using LSL.ViewModels;
using ReactiveUI;
using System;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace LSL.Views.Server
{
    public partial class ServerTerminal : ReactiveUserControl<ShellViewModel>
    {
        public ServerTerminal()
        {
            InitializeComponent();
            /*
            this.WhenActivated(disposables =>
            {
                this.ViewModel.MainVM.ScrollTerminal
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(_ => TurnToEnd())
                    .DisposeWith(disposables);
            });
            */
            EventBus.Instance.Subscribe<ViewBroadcastArgs>(ForceScroll);
        }
        public void TurnToEnd()
        {
            double tolerance = 10.0;
            if (Math.Abs(TerminalScroll.Offset.Y + TerminalScroll.Viewport.Height - TerminalScroll.Extent.Height) < tolerance) TerminalScroll.ScrollToEnd();
        }
        public void ForceScroll(ViewBroadcastArgs args)
        {
            if (args.Target == "ServerTerminal.axaml.cs" && args.Message == "ScrollToEnd")
            {
                TerminalScroll.ScrollToEnd();
            }
        }
    }
}
