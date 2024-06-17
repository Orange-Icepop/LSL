namespace LSL.ViewModels;

using Avalonia.Controls;
using LSL.Views;
using LSL.Views.Main;

public class MainViewModel : ViewModelBase
{
    //创建两个可变动视图
    public UserControl LeftView { get; private set; }
    public UserControl RightView { get; private set; }

    public MainViewModel()
    {
        // 设置默认的两个视图  
        LeftView = new MainLeft();
        RightView = new MainRight();
    }
}
