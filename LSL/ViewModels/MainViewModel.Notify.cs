using System;
using System.Collections.Generic;
using ReactiveUI;
using Avalonia.Controls.Notifications;
using System.Windows.Input;
using LSL.Services;

namespace LSL.ViewModels
{
	public partial class MainViewModel 
	{
		public ICommand NotifyCommand { get; }
		//type:0-Info,1-Success,2-Warn,3-Error
		public static void Notify(int type, string title, string message)
		{
			EventBus.Instance.Publish(new NotifyArgs { Type = type, Title = title, Message = message });
		}
	}
}