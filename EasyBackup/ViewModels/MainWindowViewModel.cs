using EasyBackup.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EasyBackup.Helpers;

namespace EasyBackup.ViewModels
{
    class MainWindowViewModel : ChangeNotifier, IChangeViewModel
    {
        BaseViewModel _currentViewModel;
        Stack<BaseViewModel> _viewModels;

        public MainWindowViewModel()
        {
            _viewModels = new Stack<BaseViewModel>();
            var initialViewModel = new SetupBackupViewModel(this);
            _viewModels.Push(initialViewModel);
            CurrentViewModel = initialViewModel;
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

        #endregion
    }
}
