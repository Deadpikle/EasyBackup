using Avalonia.Controls;
using EasyBackupAvalonia.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace EasyBackupAvalonia.Interfaces
{
    interface IChangeViewModel
    {
        void PushViewModel(BaseViewModel model);
        void PopViewModel();
        TopLevel GetTopLevel();
    }
}
