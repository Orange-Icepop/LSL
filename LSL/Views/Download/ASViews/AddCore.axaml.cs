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
            if (mainViewModel == null)
            {
                QuickHandler.ThrowError("添加核心页面的DataContext为null，无法通过对话框选择文件。");
            }
            // 从当前控件获取 TopLevel。此处使用 Window 引用。
            var topLevel = MainWindow.Instance;
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

    }
}