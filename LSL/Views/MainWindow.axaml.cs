using Avalonia;
using Avalonia.Controls;
using LSL.ViewModels;

namespace LSL.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        this.DataContext = new MainViewModel(); // 设置DataContext为MainViewModel的实例
    }
}
