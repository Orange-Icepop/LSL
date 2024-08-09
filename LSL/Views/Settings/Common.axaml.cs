using Avalonia.Controls;
using LSL.Services;
using LSL.ViewModels;
using System.Collections.Generic;

namespace LSL.Views.Settings
{
    
    public partial class Common : UserControl
    {
        #region Java�б��ȡ���������ļ���ȡ��
        public void ReadJavaList()
        {
            int count = 1;
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

        public Common()
        {
            InitializeComponent();
            this.DataContext = new ConfigViewModel();
            ReadJavaList();
        }
    }
}
