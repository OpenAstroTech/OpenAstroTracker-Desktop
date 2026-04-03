using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using OATCommunications;
using OATCommunications.CommunicationHandlers;
using OATCommunications.Utilities;

namespace OATCommunications.Avalonia.CommunicationHandlers
{
    public static class CommunicationHandlerFactory
    {
        static List<ICommunicationHandler> _handlers = new List<ICommunicationHandler>();
        static ObservableCollection<string> _available = new ObservableCollection<string>();

        public static void AddHandler(ICommunicationHandler handler) => _handlers.Add(handler);

        public static void Initialize()
        {
            AddHandler(new SerialCommunicationHandler());
            AddHandler(new TcpCommunicationHandler());
            AddHandler(new INDICommunicationHandler());
        }

        public static void DiscoverDevices()
        {
            Log.WriteLine("COMMFACTORY: Device Discovery initiated.");
            _available.Clear();
            _handlers.ForEach(handler => handler.DiscoverDeviceInstances(device =>
            {
                Log.WriteLine("COMMFACTORY: Device found: " + device);
                AvaloniaUtilities.RunOnUiThread(() => _available.Add(device));
            }));
        }

        public static ObservableCollection<string> AvailableDevices => _available;
        public static List<ICommunicationHandler> AvailableHandlers => _handlers;

        public static ICommunicationHandler? ConnectToDevice(string device)
        {
            Log.WriteLine($"COMMFACTORY: Attempting to connect to device {device}...");
            if (string.IsNullOrEmpty(device)) return null;
            var useHandler = _handlers.FirstOrDefault(h => h.IsDriverForDevice(device));
            if (useHandler == null)
            {
                Log.WriteLine($"COMMFACTORY: No handler found for device {device}...");
                return null;
            }
            return useHandler.CreateHandler(device);
        }
    }
}
