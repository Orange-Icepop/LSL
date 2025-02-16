﻿using Avalonia.Controls;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSL.ViewModels
{
    public class LeftRegionVM : RegionalVMBase
    {
        private UserControl _currentView;
        public UserControl CurrentView
        {
            get => _currentView;
            private set => this.RaiseAndSetIfChanged(ref _currentView, value);
        }
        public LeftRegionVM(AppStateLayer appState) : base(appState) 
        {
            CurrentView = ViewFactory.CreateView("HomeLeft");
        }
    }
}
