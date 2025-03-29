using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;
using LSL.ViewModels;

namespace LSL.Services
{
    #region 事件总线
    public sealed class EventBus
    {
        // 锁对象，用于同步对事件字典的访问  
        private readonly ReaderWriterLockSlim _lock = new();

        // 字典，用于存储事件类型和对应的委托列表  
        private readonly Dictionary<Type, List<Delegate>> _handlers = [];

        // 私有构造函数，使用了单例模式
        private EventBus() { }

        // 使用 Lazy<T> 实现线程安全的单例
        private static readonly Lazy<EventBus> _instance = new(() => new EventBus());

        public static EventBus Instance => _instance.Value;

        // 订阅事件（在构造函数中使用）
        public void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : EventArgs
        {
            _lock.EnterWriteLock();
            try
            {
                if (!_handlers.ContainsKey(typeof(TEvent)))
                {
                    _handlers[typeof(TEvent)] = [];
                }

                _handlers[typeof(TEvent)].Add(handler);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        // 取消订阅事件（谨慎使用）
        public void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : EventArgs
        {
            _lock.EnterWriteLock();
            try
            {
                if (_handlers.TryGetValue(typeof(TEvent), out var handlers))
                {
                    handlers.Remove(handler);

                    // 如果订阅者列表为空，则移除该事件类型  
                    if (handlers.Count == 0)
                    {
                        _handlers.Remove(typeof(TEvent));
                    }
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        // 发布事件
        public void Publish<TEvent>(TEvent e) where TEvent : EventArgs
        {
            _lock.EnterReadLock();
            try
            {
                if (_handlers.TryGetValue(typeof(TEvent), out var handlers))
                {
                    // 使用委托的DynamicInvoke方法来避免显式类型转换  
                    foreach (var handler in handlers.Cast<Action<TEvent>>())
                    {
                        // 使用PublishAsync可以异步处理事件，避免阻塞主线程
                        // 比较适用于把东西一丢就不管的情况
                        // 但请注意，这并不会改变事件处理的顺序，只是并行执行  
                        handler(e); // 同步执行  
                    }
                }
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
        // 异步发布事件（其实比较常用）
        public async Task PublishAsync<TEvent>(TEvent e) where TEvent : EventArgs
        {
            _lock.EnterReadLock();
            try
            {
                if (_handlers.TryGetValue(typeof(TEvent), out var handlers))
                {
                    // 使用委托的DynamicInvoke方法来避免显式类型转换  
                    foreach (var handler in handlers.Cast<Action<TEvent>>())
                    {
                        // 同步执行请用Publish方法
                        await Task.Run(() => handler(e)); // 异步执行  
                    }
                }
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }
    #endregion

    /*
    使用说明：
    1. 创建一个事件类，继承自EventArgs
    public class 【类名】 : EventArgs
    {
        public 【参数类型】 【参数名称】 { get; set; }
    }
    2. 发布事件
    EventBus.Instance.Publish(new 【事件类名】 { 【事件参数名称】 = 【值】 });
    3. 事件处理器类（可选）
    public void 【事件处理器方法名】(【事件类名】 args)
    4. 订阅事件
    EventBus.Instance.Subscribe<【事件类名】>(【事件处理器方法】);
    */

    #region 事件类
    public class TerminalOutputArgs : EventArgs// 终端输出事件
    {
        public required string ServerId { get; set; }
        public required string Output { get; set; }
    }
    public class ColorOutputArgs : EventArgs// 彩色终端输出事件
    {
        public required string ServerId { get; set; }
        public required ISolidColorBrush Color { get; set; }
        public required string Output { get; set; }
    }
    public class PopupMessageArgs : EventArgs// 弹窗事件
    {
        public required int Type { get; set; }
        public required string Title { get; set; }
        public required string Message { get; set; }
    }

    public class NotifyArgs : EventArgs// 通知条事件
    {
        public int? Type { get; set; }
        public string? Title { get; set; }
        public string? Message { get; set; }
    }

    public class UpdateTerminalArgs : EventArgs// 更新终端文本事件
    {
        public required string Type { get; set; }
    }

    public class PlayerMessageArgs : EventArgs// 服务器消息事件
    {
        public required string ServerId { get; set; }
        public required string Message { get; set; }
    }

    public class PlayerUpdateArgs : EventArgs// 玩家列表更新事件
    {
        public required string ServerId { get; set; }
        public required string UUID { get; set; }
        public required string PlayerName { get; set; }
        public required bool Entering { get; set; }
    }

    public class ServerStatusArgs : EventArgs// 服务器状态更新事件
    {
        public required string ServerId { get; set; }
        public required bool Running { get; set; }
        public required bool Online { get; set; }
    }

    public class ClosingArgs : EventArgs { }// 窗体关闭事件

    public class ViewBroadcastArgs : EventArgs// 广播事件
    {
        public required string Target { get; set; }
        public required string Message { get; set; }
    }
    #endregion

    public static class QuickHandler// 快捷的消息处理方式（手动狗头）
    {
        public static void ThrowError(string message)
        {
            EventBus.Instance.Publish(new PopupMessageArgs { Type = 4, Title = "非致命错误", Message = message });
        }
        public static void SendNotify(int type, string title, string message)
        {
            EventBus.Instance.Publish(new NotifyArgs { Type = type, Title = title, Message = message });
        }
    }

    #region ReactiveUI事件类
    public class NavigateArgs : EventArgs
    {
        public required BarState BarTarget { get; set; } = BarState.Undefined;
        public required GeneralPageState LeftTarget { get; set; } = GeneralPageState.Undefined;
        public required RightPageState RightTarget { get; set; } = RightPageState.Undefined;
    }

    public class NavigateCommand : EventArgs
    {
        public NavigateCommandType Type { get; set; } = NavigateCommandType.None;
    }

    public class PopupArgs : EventArgs
    {
        public int Type { get; set; } = 0;
        public string Title { get; set; } = "空弹窗";
        public string Message { get; set; } = "我是一个空的弹窗！";
    }
    #endregion

    #region 带返回值的远程调用
    public sealed class InvokeBus
    {
        private InvokeBus() { }
        private static readonly Lazy<InvokeBus> _instance = new(() => new InvokeBus());
        public static InvokeBus Instance => _instance.Value;
        private readonly ConcurrentDictionary<Type, (Type RTType, Delegate Handler)> _handlers = new();
        // 注册事件处理器
        public bool TryRegister<TEvent, TResult>(Func<TEvent, TResult> handler, bool force = false) where TEvent : EventArgs
        {
            if(handler is null) return false;
            var key = typeof(TEvent);
            var value = (RTType: typeof(TResult), (Delegate)handler);
            if (force)
            {
                _handlers.AddOrUpdate(key, value, (k, old) => value);
                return true;
            }
            return _handlers.TryAdd(key, value);
        }
        // 注册异步事件处理器
        public bool TryRegisterAsync<TEvent, TResult>(Func<TEvent, Task<TResult>> handler, bool force = false) where TEvent : EventArgs
        {
            if (handler is null) return false;
            var key = typeof(TEvent);
            var value = (typeof(Task<TResult>), (Delegate)handler);
            if (force)
            {
                _handlers.AddOrUpdate(key, value, (k, old) => value);
                return true;
            }
            return _handlers.TryAdd(key, value);
        }
        // 移除事件处理器
        public bool TryRemove<TEvent>() where TEvent : EventArgs
            => _handlers.TryRemove(typeof(TEvent), out _);
        // 调用事件处理器
        public TResult? Invoke<TEvent, TResult>(TEvent args) where TEvent : EventArgs
        {
            ArgumentNullException.ThrowIfNull(args);
            if (_handlers.TryGetValue(typeof(TEvent), out var pair))
            {
                if (typeof(TResult) == pair.RTType)
                {
                    var handler = (Func<TEvent, TResult>)pair.Handler;
                    return handler(args);
                }
                else throw new InvalidCastException("Return type mismatch");
            }
            else return default;
        }
        // 异步调用事件处理器
        public async Task<TResult?> InvokeAsync<TEvent, TResult>(TEvent args) where TEvent : EventArgs
        {
            ArgumentNullException.ThrowIfNull(args);
            if (_handlers.TryGetValue(typeof(TEvent), out var pair))
            {
                if (pair.RTType == typeof(Task<TResult>))
                {
                    var asyncHandler = (Func<TEvent, Task<TResult>>)pair.Handler;
                    return await asyncHandler(args).ConfigureAwait(false);
                }
                else if (pair.RTType == typeof(TResult))
                {
                    var syncHandler = (Func<TEvent, TResult>)pair.Handler;
                    return await Task.Run(() => syncHandler(args)).ConfigureAwait(false);
                }
                else throw new InvalidCastException("Return type mismatch");
            }
            else return default;
        }
    }
    #endregion
}
