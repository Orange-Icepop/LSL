using System.Collections;
using System.Collections.Specialized;

namespace LSL.Common.Contracts;

public class RangedObservableLinkedList<T> : IEnumerable<T>, INotifyCollectionChanged
{
    private readonly LinkedList<T> _list;
    private readonly int _maxLength;
    private readonly bool _notifiable;
    private readonly ReaderWriterLockSlim _lock;
    public event NotifyCollectionChangedEventHandler? CollectionChanged;
    
    private bool _suppressNotification = false;

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
    public int MaxLength => _maxLength;
    public bool Notifiable => _notifiable;

    public RangedObservableLinkedList(int maxLength, bool notify = true)
    {
        if (maxLength <= 0) throw new ArgumentOutOfRangeException(nameof(maxLength), "The maxLength must be greater than zero.");
        _list = new LinkedList<T>();
        _lock = new ReaderWriterLockSlim();
        _maxLength = maxLength;
        _notifiable = notify;
    }

    public RangedObservableLinkedList(int maxLength, T defaultValue, bool notify = true) : this(maxLength, notify)
    {
        _suppressNotification = true;
        try
        {
            for (int i = maxLength - 1; i >= 0; i--) _list.AddLast(defaultValue);
        }
        finally
        {
            _suppressNotification = false;
        }
    }

    public void Add(T item)
    {
        NotifyCollectionChangedEventArgs? args = null;
        bool reset = _list.Count >= _maxLength;;
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
                args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add);
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
        if (args is not null) OnCollectionChanged(args);
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
        }
    }
    
    protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (_notifiable && !_suppressNotification) CollectionChanged?.Invoke(this, e);
    }

    public IEnumerator<T> GetEnumerator() => new SafeLinkedListEnumerator<T>(_list, _lock);
    
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}