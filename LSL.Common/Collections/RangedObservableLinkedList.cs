using System.Collections;
using System.Collections.Specialized;

namespace LSL.Common.Collections;

public class RangedObservableLinkedList<T> : IEnumerable<T>, INotifyCollectionChanged
{
    private readonly LinkedList<T> _list;
    private readonly ReaderWriterLockSlim _lock;

    public RangedObservableLinkedList(int maxLength, bool notify = true)
    {
        if (maxLength <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxLength), "The maxLength must be greater than zero.");
        _list = [];
        _lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        MaxLength = maxLength;
        Notifiable = notify;
    }

    public RangedObservableLinkedList(int maxLength, T defaultValue, bool notify = true) : this(maxLength, notify)
    {
        for (var i = maxLength - 1; i >= 0; i--) _list.AddLast(defaultValue);
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    public T? FirstItem
    {
        get
        {
            _lock.EnterReadLock();
            try
            {
                return _list.First is null ? default : _list.First.Value;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }

    public T? LastItem
    {
        get
        {
            _lock.EnterReadLock();
            try
            {
                return _list.Last is null ? default : _list.Last.Value;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }

    public int Count
    {
        get
        {
            _lock.EnterReadLock();
            try
            {
                return _list.Count;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }

    public int MaxLength { get; }

    public bool Notifiable { get; }

    public IEnumerator<T> GetEnumerator()
    {
        _lock.EnterReadLock();
        try
        {
            // 创建链表快照避免长时间持有锁
            var snapshot = new List<T>(_list);
            return snapshot.GetEnumerator();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    private void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (Notifiable) CollectionChanged?.Invoke(this, e);
    }

    public void Add(T item)
    {
        NotifyCollectionChangedEventArgs? args;
        var reset = _list.Count >= MaxLength;
        _lock.EnterWriteLock();
        try
        {
            _list.AddLast(item);
            if (reset)
            {
                _list.RemoveFirst();
                args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
            }
            else
            {
                args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, _list.Count - 1);
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        OnCollectionChanged(args);
    }

    public void Clear()
    {
        _lock.EnterWriteLock();
        try
        {
            _list.Clear();
        }
        finally
        {
            _lock.ExitWriteLock();
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}