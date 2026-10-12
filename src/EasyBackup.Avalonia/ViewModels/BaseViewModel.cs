using Avalonia.Controls;
using EasyBackupAvalonia.Helpers;
using EasyBackupAvalonia.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace EasyBackupAvalonia.ViewModels
{
    class BaseViewModel : ChangeNotifier, IChangeViewModel
    {
        private IChangeViewModel _viewModelChanger;

        public BaseViewModel(IChangeViewModel viewModelChanger)
        {
            ViewModelChanger = viewModelChanger;
        }

        public IChangeViewModel ViewModelChanger
        {
            get { return _viewModelChanger; }
            set { _viewModelChanger = value; }
        }

        #region IChangeViewModel

        public void PopViewModel()
        {
            _viewModelChanger?.PopViewModel();
        }

        public void PushViewModel(BaseViewModel model)
        {
            _viewModelChanger?.PushViewModel(model);
        }

        public TopLevel GetTopLevel()
        {
            return _viewModelChanger?.GetTopLevel();
        }

        #endregion
    }
}
