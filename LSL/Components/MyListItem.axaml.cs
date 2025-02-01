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
    public partial class MyListItem : Button
    {
        private static readonly StyledProperty<string> TitleProperty =
            AvaloniaProperty.Register<MyCard, string>(nameof(Title), defaultBindingMode: BindingMode.OneWay);
        private static readonly StyledProperty<string> InfoProperty =
            AvaloniaProperty.Register<MyCard, string>(nameof(Info), defaultBindingMode: BindingMode.OneWay);
        public static readonly StyledProperty<string> LogoProperty =
            AvaloniaProperty.Register<MyCard, string>(nameof(Logo), defaultBindingMode: BindingMode.OneWay);
        public static readonly StyledProperty<Bitmap?> LogoImageProperty =
            AvaloniaProperty.Register<MyCard, Bitmap?>(nameof(LogoImage), defaultBindingMode: BindingMode.OneWay);

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
        public string Logo
        {
            get => GetValue(LogoProperty);
            set => SetValue(LogoProperty, value);
        }

        private Bitmap? _logoImage;
        public Bitmap? LogoImage
        {
            get => _logoImage;
            set => SetValue(LogoImageProperty, value);
        }

        public MyListItem()
        {
            InitializeComponent();
            this.Loaded += LoadResources;
        }

        private void LoadResources(object? sender, RoutedEventArgs e)
        {
            var tmp = new string(Logo);
            if (!string.IsNullOrEmpty(tmp) && !string.IsNullOrEmpty(tmp))
            {
                Task.Run(() => LoadImage(tmp));
            }
        }

        #region ´ÓÂ·¾¶¼ÓÔØÍ¼Æ¬
        private async void LoadImage(string path)
        {
            try
            {
                if (path.StartsWith("http") || path.StartsWith("https"))
                {
                    var bitmap = await LoadFromWeb(new Uri(path));
                    await Dispatcher.UIThread.InvokeAsync(() => LogoImage = bitmap);
                }
                else
                {
                    var bitmap = LoadFromResource(new Uri(path));
                    await Dispatcher.UIThread.InvokeAsync(() => LogoImage = bitmap);
                }
            }
            catch { }
        }
        private static Bitmap LoadFromResource(Uri resourceUri)
        {
            return new Bitmap(AssetLoader.Open(resourceUri));
        }

        private static async Task<Bitmap?> LoadFromWeb(Uri url)
        {
            using var httpClient = new HttpClient();
            try
            {
                var response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var data = await response.Content.ReadAsByteArrayAsync();
                return new Bitmap(new MemoryStream(data));
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"An error occurred while downloading image '{url}' : {ex.Message}");
                return null;
            }
        }
        #endregion
    }
}