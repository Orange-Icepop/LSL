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

        #region �ļ�ѡ��
        private static FilePickerFileType CoreFileType { get; } = new("Minecraft�����������ļ�")
        {
            Patterns = new[] { "*.jar" },
            MimeTypes = new[] { "application/java-archive" }
        };
        public async void OpenFileCmd(object sender, RoutedEventArgs args)
        {
            var mainViewModel = (MainViewModel)this.DataContext;
            // �ӵ�ǰ�ؼ���ȡ TopLevel���˴�ʹ�� Window ���á�
            var topLevel = TopLevel.GetTopLevel(this);
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

        #region Java�б��ȡ���������ļ���ȡ��
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