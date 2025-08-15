using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using ReactiveUI;
using LSL.Views;
using LSL.Views.Download;
using LSL.Views.Download.ASViews;
using LSL.Views.Home;
using LSL.Views.Server;
using LSL.Views.Settings;
using Microsoft.Extensions.Logging;

namespace LSL.ViewModels
{
    public partial class ShellViewModel
    {
        #region 导航相关

        //创建切换触发方法
        public ICommand LeftViewCmd { get; }
        public ICommand RightViewCmd { get; }
        public ICommand FullViewCmd { get; set; }
        public ICommand FullViewBackCmd { get; set; }

        //这一部分是多参数导航按钮的部分，由于设置别的VM会导致堆栈溢出且暂时没找到替代方案，所以先摆了
        //还有就是本来希望可以创建一个方法来传递两个参数的，但是太麻烦了，还是先搁置了
        public ICommand PanelConfigCmd { get; }
        public ICommand DownloadConfigCmd { get; }
        public ICommand CommonConfigCmd { get; }

        #endregion

        #region 左视图切换命令

        public async Task NavigateLeftView(string viewName, bool dislink = false)
        {
            var gps = GeneralPageState.Undefined;
            var rps = RightPageState.Undefined;
            switch (viewName)
            {
                case "HomeLeft":
                    gps = GeneralPageState.Home;
                    if (!dislink)
                        rps = RightPageState.HomeRight;
                    break;
                case "ServerLeft":
                    gps = GeneralPageState.Server;
                    if (!dislink)
                        rps = RightPageState.ServerGeneral;
                    break;
                case "DownloadsLeft":
                    gps = GeneralPageState.Downloads;
                    if (!dislink)
                        rps = RightPageState.AddServer;//TODO
                    break;
                case "SettingsLeft":
                    gps = GeneralPageState.Settings;
                    if (!dislink)
                        rps = RightPageState.CommonSettings;
                    break;
            }

            await NavigateToPage(gps, rps);
        }

        #endregion

        #region 右视图切换命令

        public async Task NavigateRightView(string viewName, bool force = false)
        {
            if (Enum.TryParse<RightPageState>(viewName, out var RV))
            {
                await NavigateToPage(GeneralPageState.Undefined, RV, force);
            }
            else _logger.LogError("Unknown right page name {rps}", viewName);
        }

        #endregion

        #region 视图切换命令

        public async Task NavigateToPage(GeneralPageState gps, RightPageState rps, bool force = false)
        {
            // 检查左视图重合
            if (gps == AppState.CurrentGeneralPage && !force) return;
            // 检查右视图重合
            if (rps == AppState.CurrentRightPage && !force) return;
            // 自动保存配置
            if (AppState.CurrentGeneralPage == GeneralPageState.Settings) await ConfigVM.ConfirmConfigAsync();
            // 新视图预操作
            if (gps == GeneralPageState.Settings) await ConfigVM.GetConfigAsync();
            // 导航
            MessageBus.Current.SendMessage(new NavigateArgs
                { BarTarget = BarState.Undefined, LeftTarget = gps, RightTarget = rps });
            _logger.LogDebug("Page Switched: {Left}, {Right}",gps.ToString(),rps.ToString());
        }

        #endregion

        #region 全屏视图切换命令

        public void NavigateFullScreenView(string viewName)
        {
            FullViewBackCmd = ReactiveCommand.Create(() =>
                MessageBus.Current.SendMessage(new NavigateCommand(NavigateCommandType.FS2Common)));
            if (!Enum.TryParse<RightPageState>(viewName, out var RV)) return;
            if (RV is RightPageState.AddCore or RightPageState.EditSC or RightPageState.AddFolder)
            {
                MessageBus.Current.SendMessage(new NavigateArgs
                    { BarTarget = BarState.FullScreen, LeftTarget = GeneralPageState.Empty, RightTarget = RV });
                if (RV is RightPageState.AddCore or RightPageState.EditSC or RightPageState.AddFolder) FormVM.ClearForm(RV);
                _logger.LogDebug("Successfully navigated to {FullScreen}.", viewName);
            }
            else _logger.LogError("This view is not a fullscreen view: {name}.", viewName);
        }

        #endregion

        #region 右视图强制刷新命令

        public void RefreshRightView()
        {
            var original = AppState.CurrentRightPage;
            MessageBus.Current.SendMessage(new NavigateArgs
                { BarTarget = BarState.Undefined, LeftTarget = GeneralPageState.Undefined, RightTarget = original });
            MessageBus.Current.SendMessage(new ViewBroadcastArgs(typeof(ServerTerminal), "ScrollToEnd"));
        }

        #endregion
    }

    #region 页面状态枚举

    public enum BarState
    {
        Common,
        FullScreen,
        Undefined
    }

    public enum GeneralPageState
    {
        Home,
        Server,
        Downloads,
        Settings,
        Empty,
        Undefined
    }

    public enum RightPageState
    {
        HomeRight,

        //Server
        ServerGeneral,
        ServerStat,
        ServerTerminal,
        ServerConf,

        //Downloads
        AutoDown,
        ManualDown,
        AddServer,
        ModDown,

        //Settings
        CommonSettings,
        DownloadSettings,
        PanelSettings,
        StyleSettings,
        About,

        //FullScreen
        EditSC,
        AddCore,
        AddFolder,

        //Others
        Empty,
        Undefined,
        Hold
    }

    public enum NavigateCommandType
    {
        None,
        Refresh,
        FS2Common
    }

    #endregion
    
    #region ViewFactory穷举并创建所有视图
    public static class ViewFactory
    {
        private static readonly HomeLeft HomeLeftView = new();
        private static readonly ServerLeft ServerLeftView = new();
        private static readonly DownloadsLeft DownloadsLeftView = new();
        private static readonly SettingsLeft SettingsLeftView = new();
        public static UserControl CreateView(string viewName)
        {
            return viewName switch
            {
                //Bar
                "Bar" => new Bar(),
                "FSBar" => new FSBar(),

                //Top
                "HomeLeft" => HomeLeftView,
                "ServerLeft" => ServerLeftView,
                "DownloadsLeft" => DownloadsLeftView,
                "SettingsLeft" => SettingsLeftView,
                //Home
                "HomeRight" => new HomeRight(),
                //Server
                "ServerGeneral" => new ServerGeneral(),
                "ServerStat" => new ServerStat(),
                "ServerTerminal" => new ServerTerminal(),
                "ServerConf" => new ServerConf(),
                "EditSC" => new EditSC(),
                //Download
                "AutoDown" => new AutoDown(),
                "ManualDown" => new ManualDown(),
                "AddServer" => new AddServer(),
                "ModDown" => new ModDown(),
                //ASViews
                "AddCore" => new AddCore(),
                "AddFolder" => new AddFolder(),
                //Settings
                "CommonSettings" => new CommonSettings(),
                "DownloadSettings" => new DownloadSettings(),
                "PanelSettings" => new PanelSettings(),
                "StyleSettings" => new StyleSettings(),
                "About" => new About(),
                _ => throw new ArgumentException($"未找到视图: {viewName}，应用程序可能已经损坏。"),
            };
        }
    }
    #endregion

}