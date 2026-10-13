using Avalonia.Controls;
using YasashiiBackupAvalonia.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace YasashiiBackupAvalonia.Interfaces;

interface IChangeViewModel
{
    void PushViewModel(BaseViewModel model);
    void PopViewModel();
    TopLevel GetTopLevel();
}
