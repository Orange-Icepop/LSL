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
            this.Title = title;
            this.Message.Text = content;
            SetButton(type);
        }
        private void SetButton(PopupType type)
        {
            switch (type)
            {
                case PopupType.Info_Confirm:
                    {
                        this.Buttons.Children.Add(new MyButton("Highlight", "确定", ReactiveCommand.Create(() => Close(PopupResult.Confirm))));
                        break;
                    }
                case PopupType.Error_Confirm:
                    {
                        this.Buttons.Children.Add(new MyButton("Red", "确定", ReactiveCommand.Create(() => Close(PopupResult.Confirm))));
                        break;
                    }
                case PopupType.Warning_YesNo:
                    {
                        this.Buttons.Children.Add(new MyButton("Highlight", "是", ReactiveCommand.Create(() => Close(PopupResult.Yes))));
                        this.Buttons.Children.Add(new MyButton("Default", "否", ReactiveCommand.Create(() => Close(PopupResult.No))));
                        break;
                    }
                case PopupType.Warning_YesNoCancel:
                    {
                        this.Buttons.Children.Add(new MyButton("Highlight", "是", ReactiveCommand.Create(() => Close(PopupResult.Yes))));
                        this.Buttons.Children.Add(new MyButton("Default", "否", ReactiveCommand.Create(() => Close(PopupResult.No))));
                        this.Buttons.Children.Add(new MyButton("Default", "取消", ReactiveCommand.Create(() => Close(PopupResult.Cancel))));
                        break;
                    }
            }
        }
    }
}
