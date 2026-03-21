using System.Windows.Input;
using OATCommunications.Avalonia;

namespace OATControl.Avalonia.ViewModels
{
    // Thin wrapper so the ListBox DataTemplate can use x:DataType compiled bindings
    public class DeviceDriverVM
    {
        private readonly DeviceDriver _driver;

        public DeviceDriverVM(DeviceDriver driver) => _driver = driver;

        public string DeviceName => _driver.DeviceName;
        public bool SupportsSetup => _driver.SupportsSetup;
        public ICommand RunSetupCommand => _driver.RunSetupCommand;

        public DeviceDriver Inner => _driver;
    }
}
