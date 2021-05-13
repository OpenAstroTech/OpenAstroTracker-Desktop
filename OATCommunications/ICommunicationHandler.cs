using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using OATCommunications.CommunicationHandlers;

namespace OATCommunications
{
    public interface ICommunicationHandler {
        
        // Connect the device (for devices that keep an open connection.
        bool Connect();

        // Disconnect from the device
        void Disconnect();

        // Are we connected
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
