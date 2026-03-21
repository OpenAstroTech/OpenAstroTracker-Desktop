using System;

namespace OATControl.ViewModels
{
    public class PolarAlignStatusEventArgs : EventArgs
    {
        public PolarAlignStatusEventArgs(string statusType, string message = "")
        {
            StatusType = statusType;
            Message = message;
        }

        public string StatusType { get; }
        public string Message { get; }
    }
}
