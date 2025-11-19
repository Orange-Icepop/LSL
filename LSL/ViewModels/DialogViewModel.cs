using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using LSL.Controls;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace LSL.ViewModels;

public class DialogViewModel : ViewModelBase
{
    public DialogViewModel(ILogger<DialogViewModel> logger, InteractionUnits interactionUnits) : base(logger)
    {
        _popupConfigs = new Dictionary<PopupType, ButtonsConfig>
        {
            {
                PopupType.InfoConfirm,
                new ButtonsConfig(Color.Parse("#019eff"),
                    CreateButtonCollection([CreateButton(MyButtonColorType.Highlight, "确定", PopupResult.Confirm)]))
            },
            {
                PopupType.InfoYesNo,
                new ButtonsConfig(Color.Parse("#019eff"),
                CreateButtonCollection([
                    CreateButton(MyButtonColorType.Highlight, "否", PopupResult.No),
                    CreateButton(MyButtonColorType.Default, "是", PopupResult.Yes)
                ]))
            },
            {
                PopupType.ErrorConfirm,
                new ButtonsConfig(Colors.Red,
                    CreateButtonCollection([CreateButton(MyButtonColorType.Red, "确定", PopupResult.Confirm)]))
            },
            {
                PopupType.WarningYesNo,
                new ButtonsConfig(Colors.Yellow,
                CreateButtonCollection([
                    CreateButton(MyButtonColorType.Default, "否", PopupResult.No),
                    CreateButton(MyButtonColorType.Highlight, "是", PopupResult.Yes)
                ]))
            },
            {
                PopupType.WarningYesNoCancel,
                new ButtonsConfig(Colors.Yellow,
                CreateButtonCollection([
                    CreateButton(MyButtonColorType.Default, "取消", PopupResult.Cancel),
                    CreateButton(MyButtonColorType.Default, "否", PopupResult.No),
                    CreateButton(MyButtonColorType.Highlight, "是", PopupResult.Yes)
                ]))
            },
            {
                PopupType.WarningConfirm,
                new ButtonsConfig(Colors.Yellow,
                    CreateButtonCollection([CreateButton(MyButtonColorType.Highlight, "确定", PopupResult.Confirm)]))
            }
        }.ToFrozenDictionary();
        Title = string.Empty;
        Message = string.Empty;
        IsVisible = false;
        IsOpen = false;
        Height = 20;
        Opacity = 0;
        Buttons = new StackPanel();
        BorderColor = new SolidColorBrush(Color.Parse("#019eff"));
        _ = Task.Run((() => HandlePopup(_popupCts.Token)));
        interactionUnits.PopupInteraction.RegisterHandler(AddPopupTask);
    }

    [Reactive] public string Title { get; private set; }
    [Reactive] public string Message { get; private set; }
    [Reactive] public bool IsVisible { get; private set; }
    [Reactive] public bool IsOpen { get; private set; }
    [Reactive] public double Height { get; private set; }
    [Reactive] public double Opacity { get; private set; }
    [Reactive] public SolidColorBrush BorderColor { get; private set; }
    [Reactive] public StackPanel Buttons { get; private set; }

    private readonly CancellationTokenSource _popupCts = new();

    private readonly Channel<(IInteractionContext<InvokePopupArgs, PopupResult> interaction, TaskCompletionSource tcs)>
        _popupChannel =
            Channel.CreateUnbounded<(IInteractionContext<InvokePopupArgs, PopupResult>, TaskCompletionSource)>();

    private async Task HandlePopup(CancellationToken token)
    {
        try
        {
            await foreach (var (interaction, tcs) in _popupChannel.Reader.ReadAllAsync(token))
            {
                await PopupTaskProcessor(interaction, tcs);
            }
        }
        catch (OperationCanceledException)
        {
            Logger.LogInformation("Popup handling queue cancelled.");
        }
        catch (Exception e)
        {
            Logger.LogError(e, "An error occured in popup handling task.");
        }
    }

    private async Task PopupTaskProcessor(IInteractionContext<InvokePopupArgs, PopupResult> interaction,
        TaskCompletionSource tcs)
    {
        var args = interaction.Input;
        var task = ShowAsync(args.PType, args.PTitle, args.PContent);
        await ShowDialog();
        var result = await task;
        await HideDialog();
        interaction.SetOutput(result);
        tcs.TrySetResult();
    }

    private Task AddPopupTask(IInteractionContext<InvokePopupArgs, PopupResult> interaction)
    {
        var tcs = new TaskCompletionSource();
        _popupChannel.Writer.TryWrite((interaction, tcs));
        return tcs.Task;
    }

    private async Task ShowDialog()
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            IsVisible = true;
            Opacity = 1;
        });
        await Task.Delay(500);
    }

    private async Task HideDialog()
    {
        await Dispatcher.UIThread.InvokeAsync(() => Opacity = 0);
        await Task.Delay(500);
        await Dispatcher.UIThread.InvokeAsync(() => IsVisible = false);
    }

    private TaskCompletionSource<PopupResult>? _tcs;

    private void SetButton(PopupType type)
    {
        if (!_popupConfigs.TryGetValue(type, out var config)) throw new ArgumentException("未处理的弹窗类型，开发错误");
        BorderColor = new SolidColorBrush(config.BorderColor);
        Buttons = config.Buttons;
    }

    public Task<PopupResult> ShowAsync(PopupType type, string title, string content)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            Title = title;
            Message = content;
            SetButton(type);
        });
        _tcs = new TaskCompletionSource<PopupResult>();
        return _tcs.Task;
    }

    private void Close(PopupResult result)
    {
        _tcs?.TrySetResult(result);
        _tcs = null;
    }

    // 按钮配置字典
    private record ButtonsConfig(Color BorderColor, StackPanel Buttons);

    private readonly FrozenDictionary<PopupType, ButtonsConfig> _popupConfigs;

    private MyButton CreateButton(MyButtonColorType color, string content, PopupResult resultType) =>
        new(color, content, ReactiveCommand.Create(() => Close(resultType))){Padding = new Thickness(5)};

    private static StackPanel CreateButtonCollection(Avalonia.Controls.Controls content)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, FlowDirection = FlowDirection.RightToLeft };
        panel.Children.AddRange(content);
        return panel;
    }
}