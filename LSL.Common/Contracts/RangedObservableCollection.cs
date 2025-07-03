using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace LSL.Common.Contracts;
/// <summary>
/// A custom ObservableCollection class used to record a ranged sequence.
/// Not thread-safe, just like its base class.
/// </summary>
/// <typeparam name="T">Any type parameter that can be used in ObservableCollection.</typeparam>
public class RangedObservableCollection<T> : ObservableCollection<T>
{
    private bool _suppressUpdate;
    private readonly uint _range;
    public RangedObservableCollection(uint range)
    {
        _range = range;
        _suppressUpdate = false;
    }

    public RangedObservableCollection(uint range, T defaultValue)
    {
        _range = range;
        _suppressUpdate = false;
        for (int i = 0; i < range; i++)
        {
            base.Add(defaultValue);
        }
    }

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (!_suppressUpdate) base.OnCollectionChanged(e);
    }

    public new void Add(T item)
    {
        if(this.Count < _range) base.Add(item);
        else
        {
            _suppressUpdate = true;
            try
            {
                base.RemoveAt(0);
                base.Add(item);
            }
            finally
            {
                _suppressUpdate = false;
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }
    }
}