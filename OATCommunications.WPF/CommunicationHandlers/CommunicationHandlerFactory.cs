using System;
using System.Collections.Generic;
using OATCommunications.CommunicationHandlers;
using System.Collections.ObjectModel;
using OATCommuncations.WPF;
using OATCommunications.Utilities;
using System.Linq;

namespace OATCommunications.WPF.CommunicationHandlers
{
	public static class CommunicationHandlerFactory
	{
		static List<ICommunicationHandler> _handlers = new List<ICommunicationHandler>();
		static ObservableCollection<string> _available = new ObservableCollection<string>();

		public static void AddHandler(ICommunicationHandler handler)
		{
			_handlers.Add(handler);
		}

		public static void Initialize()
		{
			// Add the built-in handlers for serial and wifi
			AddHandler(new SerialCommunicationHandler());
			AddHandler(new TcpCommunicationHandler());
		}

		public static void DiscoverDevices()
		{
			Log.WriteLine("COMMFACTORY: Device Discovery initiated.");
			_available.Clear();
			_handlers.ForEach(handler => handler.DiscoverDeviceInstances((device) =>
			{
				Log.WriteLine("COMMFACTORY: Device found: " + device);
				WpfUtilities.RunOnUiThread(() =>
				{
					_available.Add(device);
				},
				System.Windows.Application.Current.Dispatcher);
			}));
		}

		public static ObservableCollection<String> AvailableDevices { get { return _available; } }
		public static List<ICommunicationHandler> AvailableHandlers { get { return _handlers; } }

		public static ICommunicationHandler ConnectToDevice(string device)
		{
			Log.WriteLine($"COMMFACTORY: Attempting to connect to device {device}...");
			if (string.IsNullOrEmpty(device))
			{
				return null;
			}

			ICommunicationHandler useHandler = _handlers.FirstOrDefault(handler => handler.IsDriverForDevice(device));
			if (useHandler == null)
			{
				Log.WriteLine($"COMMFACTORY: None of the available handles indicated they could handle device {device}...");
				return null;
			}

			return useHandler.CreateHandler(device);
		}
	}
}
