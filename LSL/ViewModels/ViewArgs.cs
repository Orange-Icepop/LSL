namespace LSL.ViewModels;

#region 事件类

public record NotifyArgs(int Type, string? Title, string? Message); // 通知条事件
// 0消息，1成功，2警告，3错误

public record InvokePopupArgs(PopupType PType, string PTitle, string PContent);
public enum PopupType
{
    Info_Confirm,
    Info_YesNo,
    Warning_YesNoCancel,
    Warning_YesNo,
    Warning_Confirm,
    Error_Confirm,
}

public enum PopupResult
{
    Confirm,
    Yes,
    No,
    Cancel,
}


public class ViewBroadcastArgs // 广播事件
{
    public required string Target { get; set; }
    public required string Message { get; set; }
}

public class NavigateArgs
{
    public required BarState BarTarget { get; set; } = BarState.Undefined;
    public required GeneralPageState LeftTarget { get; set; } = GeneralPageState.Undefined;
    public required RightPageState RightTarget { get; set; } = RightPageState.Undefined;
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

#endregion