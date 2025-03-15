using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LSL.ViewModels;

namespace LSL.Services
{
    #region 事件总线
    public class EventBus
    {
        // 锁对象，用于同步对事件字典的访问  
        private readonly ReaderWriterLockSlim _lock = new();

        // 字典，用于存储事件类型和对应的委托列表  
        private readonly Dictionary<Type, List<Delegate>> _handlers = [];

        // 私有构造函数，使用了单例模式
        private EventBus() { }

        // 单例属性  
        private static EventBus _instance;

        public static EventBus Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new EventBus();
                }
                return _instance;
            }
        }

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
        public void PublishAsync<TEvent>(TEvent e) where TEvent : EventArgs
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
                        Task.Run(() => handler(e)); // 异步执行  
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
    public class BarChangedEventArgs : EventArgs// 导航栏改变事件
    {
        public required string NavigateTarget { get; set; }
    }

    public class LeftChangedEventArgs : EventArgs// 左侧栏改变事件
    {
        public required string LeftView { get; set; }
        public required string LeftTarget { get; set; }
    }

    public class TerminalOutputArgs : EventArgs// 终端输出事件
    {
        public required string ServerId { get; set; }
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
    public interface IMessageArgs;
    public class NavigateArgs : IMessageArgs
    {
        public required BarState BarTarget { get; set; } = BarState.Undefined;
        public required GeneralPageState LeftTarget { get; set; } = GeneralPageState.Undefined;
        public required RightPageState RightTarget { get; set; } = RightPageState.Undefined;
    }

    public class NavigateCommand : IMessageArgs
    {
        public NavigateCommandType Type { get; set; } = NavigateCommandType.None;
    }

    public class PopupRequest : IMessageArgs
    {
        public int Type { get; set; } = 0;
        public string Title { get; set; } = "空弹窗";
        public string Message { get; set; } = "我是一个空的弹窗！";
    }
    #endregion
}
