using Avalonia.Animation;
using Avalonia.Media;
using Avalonia.Threading;
using LSL.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace LSL.ViewModels
{
    public class PopupViewModel
    {
        public PopupViewModel(PopupInteraction interaction)
        {
            //InvokeBus.Instance.TryRegisterAsync<PopupArgs, PopupResult>(ShowPopup, true);
            interaction.RegisterHandler(async args =>
            {
                var result = await ShowPopup(args.Input);
                args.SetOutput(result);
            });
            Title = "";
            Content = "";
            MainColor = Brushes.Black;
            IsVisible = false;
            Opacity = 0;
            SetResCmd = ReactiveCommand.Create<PopupResult>(res =>
            {
                CurrentPopupTcs?.TrySetResult(res);
            });
            PopupConfigs = new()
            {
                [PopupType.Info_Confirm] = new PopupConfig("提示", "", new SolidColorBrush(Color.Parse("#33e0e5")), [new PopupButton("确认", SetResCmd, PopupResult.Confirm)]),
                [PopupType.Info_YesNo] = new PopupConfig("提示", "", new SolidColorBrush(Color.Parse("#33e0e5")), [new PopupButton("是", SetResCmd, PopupResult.Yes), new PopupButton("否", SetResCmd, PopupResult.No)]),
                [PopupType.Warning_YesNoCancel] = new PopupConfig("警告", "", Brushes.Yellow, [new PopupButton("是", SetResCmd, PopupResult.Yes), new PopupButton("否", SetResCmd, PopupResult.No), new PopupButton("取消", SetResCmd, PopupResult.Cancel)]),
                [PopupType.Warning_YesNo] = new PopupConfig("警告", "", Brushes.Yellow, [new PopupButton("是", SetResCmd, PopupResult.Yes), new PopupButton("否", SetResCmd, PopupResult.No)]),
                [PopupType.Error_Confirm] = new PopupConfig("错误", "", Brushes.Red, [new PopupButton("确认", SetResCmd, PopupResult.Confirm)]),
            };
        }

        #region Popup属性字段
        [Reactive] public string Title { get; set; }
        [Reactive] public string Content { get; set; }
        [Reactive] public ISolidColorBrush MainColor { get; set; }
        [Reactive] public bool IsVisible { get; set; }
        [Reactive] public double Opacity { get; set; }
        public ObservableCollection<PopupButton> Buttons { get; set; } = [];
        private readonly ConcurrentQueue<PopupRequest> RequestQueue = new();
        public ICommand SetResCmd { get; set; }
        #endregion

        #region 公共方法
        public Task<PopupResult> ShowPopup(PopupArgs args)
        {
            return ShowPopup((PopupType)args.Type, args.Title, args.Message);
        }
        public Task<PopupResult> ShowPopup(PopupType type, string? title, string? content)
        {
            if (!PopupConfigs.TryGetValue(type, out var conf)) throw new NotImplementedException("Unknown popup type");
            title ??= conf.Title;
            content ??= conf.Content;
            TaskCompletionSource<PopupResult> tcs = new();
            RequestQueue.Enqueue(new(type, title, content, tcs));
            ProcessQueue();
            return tcs.Task;
        }
        #endregion

        #region 弹窗处理机制
        private async Task ProcessQueue()
        {
            await _queLock.WaitAsync().ConfigureAwait(false);
            if (IsProcessing) return;
            await Dispatcher.UIThread.InvokeAsync(() => IsProcessing = true);
            try
            {
                while (!RequestQueue.IsEmpty)
                {
                    if (RequestQueue.TryDequeue(out var request))
                    {
                        await Task.Run(async () =>
                        {
                            await ProcessPopup(request).ConfigureAwait(false);
                            await request.RTcs.Task.ConfigureAwait(false); // 避免死锁
                            await ResetPopup().ConfigureAwait(false);
                        });
                    }
                }
            }
            finally
            {
                _queLock.Release();
                await Dispatcher.UIThread.InvokeAsync(() => IsProcessing = false);
                await ResetPopup();
            }
        }
        private async Task ProcessPopup(PopupRequest request)
        {
            try
            {
                if (!PopupConfigs.TryGetValue(request.RType, out var config))
                {
                    request.RTcs.SetException(new KeyNotFoundException("Invalid popup type"));
                    return;
                }
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    Title = request.RTitle;
                    Content = request.RContent;
                    MainColor = config.MainColor;
                    Buttons.Clear();
                    foreach (var item in config.Buttons) Buttons.Add(item);
                    CurrentPopupTcs = request.RTcs;
                    IsVisible = true;
                    Opacity = 1;
                });
            }
            catch (Exception ex)
            {
                request.RTcs.SetException(ex);
            }
        }
        private async Task ResetPopup()
        {
            await Dispatcher.UIThread.InvokeAsync(() => Opacity = 0);
            await Task.Delay(200).ConfigureAwait(false);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsVisible = false;
                Title = "";
                Content = "";
                MainColor = Brushes.Black;
                Buttons.Clear();
            });
        }
        #endregion

        private TaskCompletionSource<PopupResult> CurrentPopupTcs { get; set; } = new();
        private bool IsProcessing;
        private readonly SemaphoreSlim _queLock = new(1, 1);

        #region Popup所用类，枚举与配置
        public record PopupButton(string Text, ICommand Command, PopupResult Parameter);// 按钮的文本与命令
        private record PopupConfig(string Title, string Content, ISolidColorBrush MainColor, PopupButton[] Buttons);// 弹窗的标题、内容、颜色与按钮配置单元
        private readonly Dictionary<PopupType, PopupConfig> PopupConfigs;// 弹窗配置字典
        private record PopupRequest(PopupType RType, string RTitle, string RContent, TaskCompletionSource<PopupResult> RTcs);

        #endregion
    }
    public enum PopupType
    {
        Info_Confirm,
        Info_YesNo,
        Warning_YesNoCancel,
        Warning_YesNo,
        Error_Confirm,
    }
    public enum PopupResult
    {
        Confirm,
        Yes,
        No,
        Cancel,
    }
    public class PopupInteraction : Interaction<PopupArgs, PopupResult> { }
}
