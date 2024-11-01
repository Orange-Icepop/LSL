using Avalonia;
using Avalonia.Controls;
using System.ComponentModel;
using LSL.ViewModels;
using LSL.Services;
using System;
using System.Diagnostics;
namespace LSL.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        this.Closing += MainWindow_Closing;// 重定向关闭窗口事件
        this.Loaded += InitializeViews;
        EventBus.Instance.Subscribe<ViewBroadcastArgs>(BroadcastHandler);
    }
    private void InitializeViews(object sender, EventArgs e)
    {
        var mainViewModel = (MainViewModel)this.DataContext;
        mainViewModel.InitializeMainWindow();
    }
    private void MainWindow_Closing(object sender, CancelEventArgs e)
    {
        EventBus.Instance.Publish(new ClosingArgs());
        bool EnableDaemon = (bool)ConfigManager.CurrentConfigs["daemon"];
        if (EnableDaemon == true)
        {
            e.Cancel = true;
            this.Hide();
        }
        else
        {
            e.Cancel = false;
        }
    }

    private void BroadcastHandler(ViewBroadcastArgs args)
    {
        if (args.Target == "MainWindow.axaml.cs" && args.Message == "Show")
        {
            this.Show();
        }
    }
}
