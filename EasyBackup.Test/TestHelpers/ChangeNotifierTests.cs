using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace EasyBackup.Tests.TestHelpers
{
    public class ChangeNotifierTests
    {
        [Fact]
        public void TestProperty_ShouldRaisePropertyChangedEvent()
        {
            // Arrange
            var testChangeNotifier = new TestChangeNotifier();
            bool eventRaised = false;

            testChangeNotifier.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(TestChangeNotifier.TestProperty))
                {
                    eventRaised = true;
                }
            };

            // Act
            testChangeNotifier.TestProperty = "New Value";

            // Assert
            Assert.True(eventRaised);
        }
    }
}
