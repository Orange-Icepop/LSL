using System.Collections.Generic;
using System.Collections.Immutable;
using System.Windows.Input;
using Avalonia;
using Avalonia.Media;
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
                        this.Title = "��ʾ";
                        this.PopupBorder.BorderBrush = new SolidColorBrush(Color.Parse("#019eff"));
                        AddButton("Highlight", "ȷ��", ReactiveCommand.Create(() => Close(PopupResult.Confirm)));
                        break;
                    }
                case PopupType.Error_Confirm:
                    {
                        this.Title = "����";
                        this.PopupBorder.BorderBrush = new SolidColorBrush(Colors.Red);
                        AddButton("Red", "ȷ��", ReactiveCommand.Create(() => Close(PopupResult.Confirm)));
                        break;
                    }
                case PopupType.Warning_YesNo:
                    {
                        this.Title = "����";
                        this.PopupBorder.BorderBrush = new SolidColorBrush(Colors.Yellow);
                        AddButton("Default", "��", ReactiveCommand.Create(() => Close(PopupResult.No)));
                        AddButton("Highlight", "��", ReactiveCommand.Create(() => Close(PopupResult.Yes)));
                        break;
                    }
                case PopupType.Warning_YesNoCancel:
                    {
                        this.Title = "����";
                        this.PopupBorder.BorderBrush = new SolidColorBrush(Colors.Yellow);
                        AddButton("Default", "ȡ��", ReactiveCommand.Create(() => Close(PopupResult.Cancel)));
                        AddButton("Default", "��", ReactiveCommand.Create(() => Close(PopupResult.No)));
                        AddButton("Highlight", "��", ReactiveCommand.Create(() => Close(PopupResult.Yes)));
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
