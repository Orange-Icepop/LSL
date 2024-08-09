using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using System.Diagnostics;
using LSL.ViewModels;
using LSL.Services;
using LSL.Views.Server;

namespace LSL.Views.Download.ASViews
{
    public partial class AddCore : UserControl
    {
        public AddCore()
        {
            InitializeComponent();
            ReadJavaList();
        }

        #region 文件选择
        private static FilePickerFileType CoreFileType { get; } = new("Minecraft服务器核心文件")
        {
            Patterns = new[] { "*.jar" },
            MimeTypes = new[] { "application/java-archive" }
        };
        public async void OpenFileCmd(object sender, RoutedEventArgs args)
        {
            var mainViewModel = (MainViewModel)this.DataContext;
            // 从当前控件获取 TopLevel。此处使用 Window 引用。
            var topLevel = TopLevel.GetTopLevel(this);
            // 启动异步操作以打开对话框。
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "打开Minecraft核心文件",
                AllowMultiple = false,
                FileTypeFilter = new[] { CoreFileType },
            });

            if (files.Count >= 1)
            {
                var URI = files[0].Path;
                string localPath = URI.LocalPath;
                mainViewModel.SaveFilePath(localPath, "CorePath");
            }
        }
        #endregion

        #region Java列表读取（从配置文件读取）
        public void ReadJavaList()
        {
            int count = 0;
            Java.Items.Clear();
            try
            {
                while (true)
                {
                    string KeyPath = $"$.{count}.version";
                    Java.Items.Add(JsonHelper.ReadJson(ConfigManager.JavaConfigPath, KeyPath));
                    count++;
                }
            }
            catch { }
            Java.SelectedIndex = 0;
        }
        #endregion

    }
}