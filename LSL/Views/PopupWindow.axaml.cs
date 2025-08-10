using System;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.ReactiveUI;
using LSL.Components;
using LSL.ViewModels;
using ReactiveUI;

namespace LSL.Views
{
    public partial class PopupWindow : ReactiveWindow<ShellViewModel>
    {
        public PopupWindow()
        {
            InitializeComponent();
            var _topic = this.Get<TextBlock>("Topic");
            var _message = this.Get<TextBlock>("Message");
            var _border = this.Get<Border>("PopupBorder");
            var _buttons = this.Get<StackPanel>("Buttons");
            _topic.Text = "空弹窗";
            _message.Text = $"我是一个空的弹窗！{Environment.NewLine}Avalonia parameterless constructor popup window";
            SetButton(PopupType.Info_Confirm, _border, _buttons);
        }
        public PopupWindow(PopupType type, string title, string content)
        {
            InitializeComponent();

            var _topic = this.Get<TextBlock>("Topic");
            var _message = this.Get<TextBlock>("Message");
            var _border = this.Get<Border>("PopupBorder");
            var _buttons = this.Get<StackPanel>("Buttons");
            
            _topic.Text = title;
            _message.Text = content;
            SetButton(type, _border, _buttons);
        }

        private void SetButton(PopupType type, Border border, StackPanel buttons)
        {
            switch (type)
            {
                case PopupType.Info_Confirm:
                {
                    this.Title = "提示";
                    border.BorderBrush = new SolidColorBrush(Color.Parse("#019eff"));
                    AddButton(buttons, "Highlight", "确定", ReactiveCommand.Create(() => Close(PopupResult.Confirm)));
                    break;
                }
                case PopupType.Info_YesNo:
                {
                    this.Title = "提示";
                    border.BorderBrush = new SolidColorBrush(Color.Parse("#019eff"));
                    AddButton(buttons, "Highlight", "否", ReactiveCommand.Create(() => Close(PopupResult.No)));
                    AddButton(buttons, "Default", "是", ReactiveCommand.Create(() => Close(PopupResult.Yes)));
                    break;
                }
                case PopupType.Error_Confirm:
                {
                    this.Title = "错误";
                    border.BorderBrush = new SolidColorBrush(Colors.Red);
                    AddButton(buttons, "Red", "确定", ReactiveCommand.Create(() => Close(PopupResult.Confirm)));
                    break;
                }
                case PopupType.Warning_YesNo:
                {
                    this.Title = "警告";
                    border.BorderBrush = new SolidColorBrush(Colors.Yellow);
                    AddButton(buttons, "Default", "否", ReactiveCommand.Create(() => Close(PopupResult.No)));
                    AddButton(buttons, "Highlight", "是", ReactiveCommand.Create(() => Close(PopupResult.Yes)));
                    break;
                }
                case PopupType.Warning_YesNoCancel:
                {
                    this.Title = "警告";
                    border.BorderBrush = new SolidColorBrush(Colors.Yellow);
                    AddButton(buttons, "Default", "取消", ReactiveCommand.Create(() => Close(PopupResult.Cancel)));
                    AddButton(buttons, "Default", "否", ReactiveCommand.Create(() => Close(PopupResult.No)));
                    AddButton(buttons, "Highlight", "是", ReactiveCommand.Create(() => Close(PopupResult.Yes)));
                    break;
                }
                case PopupType.Warning_Confirm:
                {
                    this.Title = "警告";
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
    }
}