using ReactiveUI;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace LSL.ViewModels;
public class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
public class HomeLeftViewModel : ViewModelBase;
public class ServerLeftViewModel : ViewModelBase;
public class DownloadLeftViewModel : ViewModelBase;
public class SettingsLeftViewModel : ViewModelBase;
/*public class ViewModelBase : ReactiveObject
{
}*/
