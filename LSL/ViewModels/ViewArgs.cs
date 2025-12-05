using System;
using LSL.Models;

namespace LSL.ViewModels;

#region MessageBus事件类

public record NotifyArgs(int Type, string? Title, string? Message); // 通知条事件
// 0消息，1成功，2警告，3错误

public record InvokePopupArgs(PopupType PType, string PTitle, string PContent);
public enum PopupType
{
    InfoConfirm,
    InfoYesNo,
    WarningYesNoCancel,
    WarningYesNo,
    WarningConfirm,
    ErrorConfirm,
}

public enum PopupResult
{
    Confirm,
    Yes,
    No,
    Cancel,
}

public class ViewBroadcastArgs(Type target, string msg)
{
    public Type Target { get; } = target;
    public string Message { get; } = msg;
}

public class NavigateArgs
{
    public BarState BarTarget { get; init; } = BarState.Undefined;
    public GeneralPageState LeftTarget { get; init; } = GeneralPageState.Undefined;
    public RightPageState RightTarget { get; init; } = RightPageState.Undefined;
}

public class NavigateCommand(NavigateCommandType cType)
{
    public NavigateCommandType CommandType { get; } = cType;
}

public class PopupArgs(int type, string title, string message)
{
    public int Type { get; set; } = type;
    public string Title { get; set; } = title;
    public string Message { get; set; } = message;
}

public enum WindowOperationArgType
{
    Hide, // to MainWindow only
    Show, // to MainWindow only
    RequestClose, // to MainWindow only
    CheckForClose,// from MainWindow to ShellVM only
    ConfirmClose, // from ShellVM to MainWindow only
    ForceClose, // to MainWindow only
}

public class WindowOperationArgs(WindowOperationArgType cType)
{
    public WindowOperationArgType Body { get; } = cType;
}

#endregion

#region EventHandler事件类
public record GeneralMetricsEventArgs(double CpuUsage, double RamUsage, long RamValue);
#endregion