using System.Windows.Input;

namespace OATCommunications.Avalonia
{
    public class DeviceDriver
    {
        public DeviceDriver(string name, bool hasSetup, ICommand runSetup)
        {
            DeviceName = name;
            SupportsSetup = hasSetup;
            RunSetupCommand = runSetup;
        }

        public string DeviceName { get; set; }
        public bool SupportsSetup { get; set; }
        public ICommand RunSetupCommand { get; set; }
    }
}
