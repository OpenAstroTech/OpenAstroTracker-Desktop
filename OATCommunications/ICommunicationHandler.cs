using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using OATCommunications.CommunicationHandlers;

namespace OATCommunications
{
    public interface ICommunicationHandler {

        // Get the name of the communication layer
        string Name { get; }

        // Find any instances of this type available on the system
        void DiscoverDeviceInstances(Action<string> addDevice);

        // Can this handler process the given device identifier?
        bool IsDriverForDevice(string device);

        // Does this handler allow a supprot dialog to be shown
        bool SupportsSetupDialog { get; }

        // Tell the handler to run the support dialog. Return false to cancel connection.
        bool RunSetupDialog();

        // Creates another instance of this handler
        ICommunicationHandler CreateHandler(string device);

        // Connect the device (for devices that keep an open connection).
        bool Connect();

        // Disconnect from the device
        void Disconnect();

        // Are we connected?
        bool Connected { get; }

        // Send a command, no response expected
        void SendBlind(string command, Action<CommandResponse> onFullFilledAction);

        // Send a command, expect a '#' terminated response
        void SendCommand(string command, Action<CommandResponse> onFullFilledAction);

        // Send a command, expect two '#' terminated responses (only first one gets passed to action). 
        // Note: this is to handle the :SC command, which Meade decided should return two #-delimited strings for some bizarre reason.
        void SendCommandDoubleResponse(string command, Action<CommandResponse> onFullFilledAction);

        // Send a command, expect a single digit response
        void SendCommandConfirm(string command, Action<CommandResponse> onFullFilledAction);
	}
}
