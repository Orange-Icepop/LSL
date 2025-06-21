using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System;
using Avalonia.Media;
using Avalonia.Threading;
using System.Diagnostics;
using Avalonia.Interactivity;

namespace LSL.Components
{
    public class MyListItem : Button
    {
        public static readonly StyledProperty<string> TitleProperty =
            AvaloniaProperty.Register<MyListItem, string>(nameof(Title));
        public static readonly StyledProperty<string> InfoProperty =
            AvaloniaProperty.Register<MyListItem, string>(nameof(Info));
        public static readonly StyledProperty<IImage?> LogoProperty =
            AvaloniaProperty.Register<MyListItem, IImage?>(nameof(Logo));

        public string Title
        {
            get => GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }
        public string Info
        {
            get => GetValue(InfoProperty);
            set => SetValue(InfoProperty, value);
        }
        public IImage? Logo
        {
            get => GetValue(LogoProperty);
            set => SetValue(LogoProperty, value);
        }
        
        public MyListItem()
        {
            Padding = new Thickness(10, 5);
        }
    }
}