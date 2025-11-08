using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LSL.Services;
// 用于服务层通信的事件总线

#region 事件总线

public sealed class EventBus
{
    // 字典，用于存储事件类型和对应的委托列表  
    private readonly ConcurrentDictionary<Type, (object _lock, List<Delegate> _delegates)> _handlers = [];

    // 单例
    private static readonly Lazy<EventBus> s_instance = new(() => new EventBus());
    public static EventBus Instance => s_instance.Value;

    // 订阅事件（支持同步和异步处理程序）
    public bool Subscribe<TEvent>(Action<TEvent> handler) => SubscribeInternal<TEvent>(handler);
    public bool Subscribe<TEvent>(Func<TEvent, Task> handler) => SubscribeInternal<TEvent>(handler);

    private bool SubscribeInternal<TEvent>(Delegate handler)
    {
        try
        {
            var (locker, delegates) = _handlers.GetOrAdd(typeof(TEvent), _ => (new object(), []));
            lock (locker)
            {
                delegates.Add(handler);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    // 取消订阅事件
    public bool Unsubscribe<TEvent>(Delegate handler)
    {
        try
        {
            if (_handlers.TryGetValue(typeof(TEvent), out var entry))
            {
                lock (entry._lock)
                {
                    return entry._delegates.Remove(handler);
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    // 同步发布（确保所有处理程序执行完成）
    public bool Publish<TEvent>(TEvent e)
    {
        if (!_handlers.TryGetValue(typeof(TEvent), out var entry))
            return true;

        List<Delegate> snapshot;
        lock (entry._lock)
        {
            snapshot = new List<Delegate>(entry._delegates);
        }

        var exceptions = new List<Exception>();

        foreach (var handler in snapshot)
        {
            try
            {
                switch (handler)
                {
                    case Action<TEvent> syncHandler:
                        syncHandler(e);
                        break;
                    case Func<TEvent, Task> asyncHandler:
                        // 同步等待异步处理
                        asyncHandler(e).GetAwaiter().GetResult();
                        break;
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
                // 记录错误但继续执行其他处理程序
                Console.WriteLine($"Error in event handler: {ex.Message}");
            }
        }

        return exceptions.Count == 0;
    }

    // 异步发布（确保所有处理程序执行完成）
    public async Task<bool> PublishAsync<TEvent>(TEvent e)
    {
        if (!_handlers.TryGetValue(typeof(TEvent), out var entry))
            return true;

        List<Delegate> snapshot;
        lock (entry._lock)
        {
            snapshot = new List<Delegate>(entry._delegates);
        }

        var tasks = new List<Task>();
        var exceptions = new List<Exception>();

        foreach (var handler in snapshot)
        {
            try
            {
                switch (handler)
                {
                    case Action<TEvent> syncHandler:
                        // 同步方法包装为异步任务
                        tasks.Add(Task.Run(() => syncHandler(e)));
                        break;
                    case Func<TEvent, Task> asyncHandler:
                        tasks.Add(asyncHandler(e));
                        break;
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
                Console.WriteLine($"Error starting event handler: {ex.Message}");
            }
        }

        try
        {
            // 等待所有任务完成（即使有失败）
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch (AggregateException ae)
        {
            exceptions.AddRange(ae.Flatten().InnerExceptions);
        }
        catch (Exception ex)
        {
            exceptions.Add(ex);
        }

        // 记录所有异常
        foreach (var ex in exceptions)
        {
            Console.WriteLine($"Error in event handler: {ex.Message}");
        }

        return exceptions.Count == 0;
    }

    // Fire-and-Forget发布（不等待完成）
    public void Fire<TEvent>(TEvent e, Action<Exception>? errorHandler = null)
    {
        if (!_handlers.TryGetValue(typeof(TEvent), out var entry))
            return;

        List<Delegate> snapshot;
        lock (entry._lock)
        {
            snapshot = new List<Delegate>(entry._delegates);
        }

        foreach (var handler in snapshot)
        {
            // 为每个处理程序启动独立任务
            _ = Task.Run(async () =>
            {
                try
                {
                    switch (handler)
                    {
                        case Action<TEvent> syncHandler:
                            syncHandler(e);
                            break;
                        case Func<TEvent, Task> asyncHandler:
                            await asyncHandler(e).ConfigureAwait(false);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    // 自定义错误处理或默认日志
                    errorHandler?.Invoke(ex);
                    Console.WriteLine($"Fire-and-forget error: {ex.Message}");
                }
            });
        }
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
    private InvokeBus()
    {
    }

    private static readonly Lazy<InvokeBus> s_instance = new(() => new InvokeBus());
    public static InvokeBus Instance => s_instance.Value;

    private readonly ConcurrentDictionary<Type, (Type RTType, Delegate Handler)> _handlers = new();

    // 注册事件处理器
    public bool TryRegister<TEvent, TResult>(Func<TEvent, TResult> handler, bool force = false)
    {
        var key = typeof(TEvent);
        var value = (RTType: typeof(TResult), (Delegate)handler);
        if (force)
        {
            _handlers.AddOrUpdate(key, value, (_, _) => value);
            return true;
        }

        return _handlers.TryAdd(key, value);
    }

    // 注册异步事件处理器
    public bool TryRegisterAsync<TEvent, TResult>(Func<TEvent, Task<TResult>> handler, bool force = false)
    {
        var key = typeof(TEvent);
        var value = (typeof(Task<TResult>), (Delegate)handler);
        if (force)
        {
            _handlers.AddOrUpdate(key, value, (_, _) => value);
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