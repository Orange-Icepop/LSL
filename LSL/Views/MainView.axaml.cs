using Avalonia.Controls;
using LSL.ViewModels;

namespace LSL.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        this.DataContext = new MainViewModel(); // 设置DataContext为MainViewModel的实例
    }
}
