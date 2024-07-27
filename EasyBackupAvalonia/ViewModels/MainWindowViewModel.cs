using Avalonia.Controls;
using EasyBackupAvalonia.Helpers;
using EasyBackupAvalonia.Interfaces;
using EasyBackupAvalonia.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace EasyBackupAvalonia.ViewModels
{
    class MainWindowViewModel : ChangeNotifier, IChangeViewModel
    {
        private BaseViewModel _currentViewModel;
        private Stack<BaseViewModel> _viewModels;
        private TopLevel _topLevel;

        public MainWindowViewModel(TopLevel topLevel)
        {
            Settings.Init();
            _viewModels = new Stack<BaseViewModel>();
            var initialViewModel = new SetupBackupViewModel(this)
            {
            };
            _viewModels.Push(initialViewModel);
            CurrentViewModel = initialViewModel;
            _topLevel = topLevel;
        }

        public BaseViewModel CurrentViewModel
        {
            get { return _currentViewModel; }
            set { _currentViewModel = value; NotifyPropertyChanged(); }
        }

        #region IChangeViewModel

        public void PushViewModel(BaseViewModel model)
        {
            _viewModels.Push(model);
            CurrentViewModel = model;
        }

        public void PopViewModel()
        {
            if (_viewModels.Count > 1)
            {
                _viewModels.Pop();
                CurrentViewModel = _viewModels.Peek();
            }
        }

        public TopLevel GetTopLevel()
        {
            return _topLevel;
        }

        #endregion
    }
}
