using System;

namespace OATControl.ViewModels
{
    public class PolarAlignStatusEventArgs : EventArgs
    {
        public string StatusType { get; set; } // e.g., "Started", "Measure", "Adjust", "Error", "Succeeded"
        public string Message { get; set; }
        public object Data { get; set; } // Optional: for extra info (e.g., error values)

        public PolarAlignStatusEventArgs(string statusType, string message, object data = null)
        {
            StatusType = statusType;
            Message = message;
            Data = data;
        }
    }
} 