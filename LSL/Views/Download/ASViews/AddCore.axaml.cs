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

        #region �ļ�ѡ��
        private static FilePickerFileType CoreFileType { get; } = new("Minecraft�����������ļ�")
        {
            Patterns = new[] { "*.jar" },
            MimeTypes = new[] { "application/java-archive" }
        };
        public async void OpenFileCmd(object sender, RoutedEventArgs args)
        {
            var mainViewModel = (MainViewModel)this.DataContext;
            if (mainViewModel == null)
            {
                QuickHandler.ThrowError("��Ӻ���ҳ���DataContextΪnull���޷�ͨ���Ի���ѡ���ļ���");
            }
            // �ӵ�ǰ�ؼ���ȡ TopLevel���˴�ʹ�� Window ���á�
            var topLevel = MainWindow.Instance;
            // �����첽�����Դ򿪶Ի���
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "��Minecraft�����ļ�",
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