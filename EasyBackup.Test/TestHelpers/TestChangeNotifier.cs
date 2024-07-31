using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EasyBackup.Helpers;

namespace EasyBackup.Tests.TestHelpers
{
    public class TestChangeNotifier : ChangeNotifier
    {
        private string _testProperty = "";
        public string TestProperty
        {
            get => _testProperty;
            set
            {
                if (_testProperty != value)
                {
                    _testProperty = value;
                    NotifyPropertyChanged();
                }
            }
        }
    }
}