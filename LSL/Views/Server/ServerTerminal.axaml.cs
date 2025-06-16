using System;
using System.Reactive.Linq;
using Avalonia.ReactiveUI;
using LSL.Services;
using LSL.ViewModels;
using ReactiveUI;

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
            MessageBus.Current.Listen<ViewBroadcastArgs>()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(ForceScroll);
        }
        public void TurnToEnd()
        {
            double tolerance = 10.0;
            if (Math.Abs(TerminalScroll.Offset.Y + TerminalScroll.Viewport.Height - TerminalScroll.Extent.Height) < tolerance) TerminalScroll.ScrollToEnd();
        }
        public void ForceScroll(ViewBroadcastArgs args)
        {
            if (args.Target == typeof(ServerTerminal) && args.Message == "ScrollToEnd")
            {
                TerminalScroll.ScrollToEnd();
            }
        }
    }
}
