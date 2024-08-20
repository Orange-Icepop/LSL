﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
        public static EventBus Instance { get; } = new EventBus();

        // 订阅事件（在构造函数中使用）
        public void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : EventArgs
        {
            _lock.EnterWriteLock();
            try
            {
                if (!_handlers.ContainsKey(typeof(TEvent)))
                {
                    _handlers[typeof(TEvent)] = new List<Delegate>();
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
                        // 可以在这里使用Task.Run来异步执行事件处理器  
                        // 但请注意，这并不会改变事件处理的顺序，只是并行执行  
                        // handler(e); // 同步执行  
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

    // 使用说明：
    // 1. 创建一个事件类，继承自EventArgs
    // public class 【类名】 : EventArgs
    // {
    //     public 【参数类型】 【参数名称】 { get; set; }
    // }
    // 2. 发布事件
    // EventBus.Instance.Publish(new 【事件类名】 { 【事件参数名称】 = 【值】 });
    // 3. 事件处理器类（可选）
    // 事件处理器可以统一设置，也可以在需要的地方单独设置
    // 如果是统一设置的非静态事件处理器，需要引入该类的实例
    // public void 【事件处理器方法名】(【事件类名】 e)
    // 注：AI给的示例中还包含object sender参数，但如果加上会报错，就很神奇
    // 4. 订阅事件
    // EventBus.Instance.Subscribe<【事件类名】>(【事件处理器方法】);
    #endregion

    #region 事件类
    public class TerminalOutputArgs : EventArgs
    {
        public string ServerId { get; set; }
        public string Output { get; set; }
    }
    public class PopupMessageArgs : EventArgs
    {
        public string Type { get; set; }
        public string Message { get; set; }
    }
    #endregion
}