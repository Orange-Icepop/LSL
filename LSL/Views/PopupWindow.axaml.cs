using System.Windows.Input;
using Avalonia;
using Avalonia.ReactiveUI;
using LSL.Components;
using LSL.ViewModels;
using ReactiveUI;

namespace LSL.Views
{
    public partial class PopupWindow : ReactiveWindow<ShellViewModel>
    {
        public PopupWindow(PopupType type, string title, string content)
        {
            InitializeComponent();
            this.Icon = null;
            this.Title = title;
            this.Topic.Text = title;
            this.Message.Text = content;
            SetButton(type);
        }
        private void SetButton(PopupType type)
        {
            switch (type)
            {
                case PopupType.Info_Confirm:
                    {
                        AddButton("Highlight", "确定", ReactiveCommand.Create(() => Close(PopupResult.Confirm)));
                        break;
                    }
                case PopupType.Error_Confirm:
                    {
                        AddButton("Red", "确定", ReactiveCommand.Create(() => Close(PopupResult.Confirm)));
                        break;
                    }
                case PopupType.Warning_YesNo:
                    {
                        AddButton("Default", "否", ReactiveCommand.Create(() => Close(PopupResult.No)));
                        AddButton("Highlight", "是", ReactiveCommand.Create(() => Close(PopupResult.Yes)));
                        break;
                    }
                case PopupType.Warning_YesNoCancel:
                    {
                        AddButton("Default", "取消", ReactiveCommand.Create(() => Close(PopupResult.Cancel)));
                        AddButton("Default", "否", ReactiveCommand.Create(() => Close(PopupResult.No)));
                        AddButton("Highlight", "是", ReactiveCommand.Create(() => Close(PopupResult.Yes)));
                        break;
                    }
            }
        }
        private void AddButton(string color, string content, ICommand command)
        {
            this.Buttons.Children.Add(new MyButton(color, content, command) { Width = 100, Height = 30, Margin = Thickness.Parse("10") });
        }
    }
}
