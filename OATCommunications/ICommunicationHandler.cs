using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using OATCommunications.CommunicationHandlers;

namespace OATCommunications
{
    public interface ICommunicationHandler {
        // Send a command, no response expected
        void SendBlind(string command, Action<CommandResponse> onFullFilledAction);

        // Send a command, expect a '#' terminated response
        void SendCommand(string command, Action<CommandResponse> onFullFilledAction);

        // Send a command, expect a single digit response
        void SendCommandConfirm(string command, Action<CommandResponse> onFullFilledAction);

        bool Connected { get; }

        void Disconnect();

        
    }
}
