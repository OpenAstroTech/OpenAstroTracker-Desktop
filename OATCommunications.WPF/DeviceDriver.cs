using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace OATCommunications.WPF
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
