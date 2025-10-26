using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using LSL.Components;
using LSL.ViewModels;
using ReactiveUI;

namespace LSL.Views
{
    public partial class MessageDialog : ReactiveUserControl<ShellViewModel>
    {
        private TaskCompletionSource<PopupResult>? _tcs;

        public MessageDialog()
        {
            InitializeComponent();
        }

        private void SetButton(PopupType type, Border border, StackPanel buttons)
        {
            switch (type)
            {
                case PopupType.InfoConfirm:
                {
                    border.BorderBrush = new SolidColorBrush(Color.Parse("#019eff"));
                    AddButton(buttons, "Highlight", "确定", ReactiveCommand.Create(() => Close(PopupResult.Confirm)));
                    break;
                }
                case PopupType.InfoYesNo:
                {
                    border.BorderBrush = new SolidColorBrush(Color.Parse("#019eff"));
                    AddButton(buttons, "Highlight", "否", ReactiveCommand.Create(() => Close(PopupResult.No)));
                    AddButton(buttons, "Default", "是", ReactiveCommand.Create(() => Close(PopupResult.Yes)));
                    break;
                }
                case PopupType.ErrorConfirm:
                {
                    border.BorderBrush = new SolidColorBrush(Colors.Red);
                    AddButton(buttons, "Red", "确定", ReactiveCommand.Create(() => Close(PopupResult.Confirm)));
                    break;
                }
                case PopupType.WarningYesNo:
                {
                    border.BorderBrush = new SolidColorBrush(Colors.Yellow);
                    AddButton(buttons, "Default", "否", ReactiveCommand.Create(() => Close(PopupResult.No)));
                    AddButton(buttons, "Highlight", "是", ReactiveCommand.Create(() => Close(PopupResult.Yes)));
                    break;
                }
                case PopupType.WarningYesNoCancel:
                {
                    border.BorderBrush = new SolidColorBrush(Colors.Yellow);
                    AddButton(buttons, "Default", "取消", ReactiveCommand.Create(() => Close(PopupResult.Cancel)));
                    AddButton(buttons, "Default", "否", ReactiveCommand.Create(() => Close(PopupResult.No)));
                    AddButton(buttons, "Highlight", "是", ReactiveCommand.Create(() => Close(PopupResult.Yes)));
                    break;
                }
                case PopupType.WarningConfirm:
                {
                    border.BorderBrush = new SolidColorBrush(Colors.Yellow);
                    AddButton(buttons, "Default", "确定", ReactiveCommand.Create(() => Close(PopupResult.Confirm)));
                    break;
                }
                default:
                    throw new ArgumentException("未处理的弹窗类型，开发错误");
            }
        }

        private static void AddButton(StackPanel panel, string color, string content, ICommand command)
        {
            panel.Children.Add(new MyButton(color, content, command)
                { Width = 100, Height = 30, Margin = Thickness.Parse("10") });
        }

        public Task<PopupResult> ShowAsync(PopupType type, string title, string content)
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                Topic.Text = title;
                Message.Text = content;
                SetButton(type, PopupBorder, Buttons);
            });
            _tcs = new TaskCompletionSource<PopupResult>();
            return _tcs.Task;
        }

        private void Close(PopupResult result)
        {
            _tcs?.TrySetResult(result);
            _tcs = null;
        }
    }
}