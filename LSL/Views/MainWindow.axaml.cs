using Avalonia;
using Avalonia.Controls;
using LSL.ViewModels;
using LSL.Services;
namespace LSL.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        //初始化配置文件，虽然放在这里有些不应该，但是App里面没有初始化相关代码，只能这么干了
        ConfigurationManager.WriteInitialConfig();
    }
}
