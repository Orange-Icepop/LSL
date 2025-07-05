using System.Collections;

namespace LSL.Common.Contracts;
/// <summary>
/// A thread-safe enumerator class for LinkedList.
/// MUST BE DISPOSED MANUALLY IF NOT USING foreach() or using()!
/// </summary>
/// <typeparam name="T">The type of objects to enumerate.</typeparam>
public sealed class SafeLinkedListEnumerator<T> : IEnumerator<T>
{
    private readonly LinkedList<T> _source;
    private readonly ReaderWriterLockSlim _lock;
    private IEnumerator<T>? _inner;
    private bool _disposed;

    public SafeLinkedListEnumerator(LinkedList<T> source, ReaderWriterLockSlim lockObj)
    {
        _source = source;
        _lock = lockObj;
        _lock.EnterReadLock();
    }

    public T Current
    {
        get
        {
            ThrowIfDisposed();
            if (_inner is null) throw new InvalidOperationException("Enumeration has not started. Call MoveNext first.");
            return _inner.Current;
        }
    }

    object IEnumerator.Current => Current!;

    public bool MoveNext()
    {
        ThrowIfDisposed();
            
        _inner ??= _source.GetEnumerator();
            
        return _inner.MoveNext();
    }

    public void Reset()
    {
        ThrowIfDisposed();
        _inner?.Reset();
    }

    public void Dispose()
    {
        if (_disposed) return;
            
        _inner?.Dispose();
        _lock.ExitReadLock();
        _disposed = true;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}