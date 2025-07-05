using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LSL.ViewModels;

namespace LSL.Services
{
    // 用于服务层通信的事件总线
    #region 事件总线
    public sealed class EventBus
    {
        // 字典，用于存储事件类型和对应的委托列表  
        private readonly ConcurrentDictionary<Type, (object _lock, List<Delegate> _delegates)> _handlers = [];

        // 私有构造函数，使用了单例模式
        private EventBus()
        {
            Debug.WriteLine("EventBus initialized");
        }

        // 使用 Lazy<T> 实现线程安全的单例
        private static readonly Lazy<EventBus> _instance = new(() => new EventBus());

        public static EventBus Instance => _instance.Value;

        // 订阅事件（在构造函数中使用）
        public bool Subscribe<TEvent>(Action<TEvent> handler)
        {
            try
            {
                var (_lock, _delegates) = _handlers.GetOrAdd(typeof(TEvent), t => (new object(), new List<Delegate>()));
                lock (_lock)
                {
                    _delegates.Add(handler);
                }
            }
            catch { return false; }
            return true;
        }

        // 取消订阅事件（谨慎使用）
        public bool Unsubscribe<TEvent>(Action<TEvent> handler)
        {
            try
            {
                if (_handlers.TryGetValue(typeof(TEvent), out var entry))
                {
                    lock (entry._lock)
                    {
                        entry._delegates.Remove(handler);
                    }
                    // 如果订阅者列表为空，则移除该事件类型  
                    if (entry._delegates.Count == 0)
                    {
                        _handlers.TryRemove(typeof(TEvent), out _);
                    }
                    return true; // 成功取消订阅
                }
                else return false;
            }
            catch { return false; }
        }

        // 发布事件
        public bool Publish<TEvent>(TEvent e)
        {
            try
            {
                if (_handlers.TryGetValue(typeof(TEvent), out var entry))
                {
                    // 创建副本，避免同步操作耗费过长时间
                    List<Delegate> snapshot;
                    lock (entry._lock)
                    {
                        snapshot = new(entry._delegates);
                    }
                    // 使用委托的DynamicInvoke方法来避免显式类型转换  
                    foreach (var handler in snapshot.Cast<Action<TEvent>>())
                    {
                        // 使用PublishAsync可以异步处理事件，避免阻塞主线程
                        // 比较适用于把东西一丢就不管的情况
                        // 但请注意，这并不会改变事件处理的顺序，只是并行执行  
                        handler(e); // 同步执行  
                    }
                }
            }
            catch (Exception ex)
            {
                // 处理异常
                Console.WriteLine($"Error publishing event: {ex.Message}");
                return false;
            }
            return true;
        }
        // 异步发布事件（其实比较常用）
        public async Task<bool> PublishAsync<TEvent>(TEvent e)
        {
            try
            {
                if (_handlers.TryGetValue(typeof(TEvent), out var entry))
                {
                    // 创建副本，避免同步操作耗费过长时间
                    List<Delegate> snapshot;
                    lock (entry._lock)
                    {
                        snapshot = new(entry._delegates);
                    }
                    // 使用委托的DynamicInvoke方法来避免显式类型转换  
                    foreach (var handler in snapshot.Cast<Action<TEvent>>())
                    {
                        await Task.Run(() => handler(e)); // 异步执行  
                    }
                }
            }
            catch (Exception ex)
            {
                // 处理异常
                Console.WriteLine($"Error publishing event: {ex.Message}");
                return false;
            }
            return true;
        }
    }
    #endregion

    /*
    使用说明：
    1. 创建一个事件类，继承自EventArgs
    public class 【类名】
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

    #region 带返回值的远程调用
    /* 这个类其实没设么用
     * 等到时候IPC搞出来直接JSON通信
     * 有用的估计也就是EventBus了
     */
    public sealed class InvokeBus
    {
        private InvokeBus() { }
        private static readonly Lazy<InvokeBus> _instance = new(() => new InvokeBus());
        public static InvokeBus Instance => _instance.Value;
        private readonly ConcurrentDictionary<Type, (Type RTType, Delegate Handler)> _handlers = new();
        // 注册事件处理器
        public bool TryRegister<TEvent, TResult>(Func<TEvent, TResult> handler, bool force = false)
        {
            if (handler is null) return false;
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
        public bool TryRegisterAsync<TEvent, TResult>(Func<TEvent, Task<TResult>> handler, bool force = false)
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
        public bool TryRemove<TEvent>()
            => _handlers.TryRemove(typeof(TEvent), out _);
        // 调用事件处理器
        public TResult? Invoke<TEvent, TResult>(TEvent args)
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
        public async Task<TResult?> InvokeAsync<TEvent, TResult>(TEvent args)
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
