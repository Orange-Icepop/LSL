namespace LSL.ViewModels;

using Avalonia.Controls;
using LSL.Views;
using LSL.Views.Main;
using LSL.Views.Settings;
using ReactiveUI;
using System.Windows.Input;

public class MainViewModel : ViewModelBase
{
    //创建两个可变动视图
    public UserControl LeftView { get; private set; }
    public UserControl RightView { get; private set; }
    //声明切换的视图
    private UserControl _mainLeftView;
    private UserControl _serverLeftView;
    private UserControl _downloadLeftView;
    private UserControl _settingsLeftView;

    public MainViewModel()
    {
        // 设置默认的两个视图  
        LeftView = new MainLeft();
        RightView = new MainRight();
        // 创建视图的实例
        _mainLeftView = new MainLeft();
        _serverLeftView = new ServerLeft();
        _downloadLeftView = new DownloadLeft();
        _settingsLeftView = new SettingsLeft();  

        // 添加一个命令或方法来切换视图  
        this.SwitchToLeftViewCommand = ReactiveCommand.Create(() =>
        {
            LeftView = _settingsLeftView; // 切换视图  
        });
    }
    public ICommand SwitchToLeftViewCommand { get; }
}
