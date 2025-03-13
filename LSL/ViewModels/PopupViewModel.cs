using Avalonia.Media;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace LSL.ViewModels
{
    public class PopupViewModel
    {
        public PopupViewModel()
        {
            Title = "";
            Content = "";
            Color = Brushes.White;
            IsVisible = false;
            Opacity = 0;
        }

        [Reactive] public string Title { get; set; }
        [Reactive] public string Content { get; set; }
        [Reactive] public ISolidColorBrush Color { get; set; }
        [Reactive] public bool IsVisible { get; set; }
        [Reactive] public double Opacity { get; set; }
        public ObservableCollection<PopupButton> Buttons { get; set; } = [];
    }

    public record PopupButton(string Text, ICommand Command, object? Parameter=null);
}
