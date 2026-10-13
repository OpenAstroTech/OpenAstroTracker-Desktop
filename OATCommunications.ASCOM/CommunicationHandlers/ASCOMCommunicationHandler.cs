using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ASCOM.DeviceInterface;

using OATCommunications.CommunicationHandlers;
using OATCommunications.Utilities;

namespace OATCommunications.CommunicationHandlers
{
	public class ASCOMCommunicationHandler : CommunicationHandler
	{
		ASCOM.DriverAccess.Telescope _oat;

		public ASCOMCommunicationHandler()
		{
		}

		public ASCOMCommunicationHandler(string device)
		{
			Log.WriteLine($"COMMFACTORY: Creating ASCOM handler for {device} ...");
			_oat = new ASCOM.DriverAccess.Telescope("ASCOM.OpenAstroTracker.Telescope");

			// This probe runs before Connect() is ever called on this handler, so _oat is not
			// yet connected: the :I# passthrough normally throws NotConnectedException here
			// unless some other still-open connection happens to leave the shared serial port
			// open underneath it. It's a best-effort early log/probe, not a required step, so
			// a failure here must not crash the app - Connect() does the real connection.
			try
			{
				foreach (var action in _oat.SupportedActions)
				{
					Log.WriteLine($"COMMFACTORY: ASCOM handler supported action: {action} ...");
				}
				_oat.Action("Serial:PassThroughCommand", "I#");
			}
			catch (Exception ex)
			{
				Log.WriteLine($"COMMFACTORY: ASCOM handler initial probe failed (device not yet connected?): {ex.Message}");
			}
		}

		public override string Name => "ASCOM";

		public override bool Connected { get { return _oat?.Connected ?? false; } }

		protected override void RunJob(Job job)
		{
			CommandResponse response = null;

			if (_logJobs) Log.WriteLine("ASCOM: {0:0000}: [{1}] Processing Job", job.Number, job.Command);
			if (Connected)
			{
				if (_logJobs) Log.WriteLine("ASCOM: {0:0000}: [{1}] Connected! Discarding any bytes in input buffer", job.Number, job.Command);
				string reply;
				try
				{
					if (_logJobs) Log.WriteLine("ASCOM: {0:0000}: [{1}] Sending command", job.Number, job.Command);
					string command = job.Command;
					switch (job.ResponseType)
					{
						case ResponseType.NoResponse:
							if (_logJobs) Log.WriteLine("ASCOM: {0:0000}: [{1}] Expecting no response for command", job.Number, job.Command);
							break;

						case ResponseType.DigitResponse:
							if (_logJobs) Log.WriteLine("ASCOM: {0:0000}: [{1}] Expecting single digit response for command", job.Number, job.Command);
							command += ",n";
							break;

						case ResponseType.FullResponse:
							if (_logJobs) Log.WriteLine("ASCOM: {0:0000}: [{1}] Expecting #-delimited response for Command...", job.Number, job.Command);
							command += ",#";
							break;
						case ResponseType.DoubleFullResponse:
							command += ",##";
							if (_logJobs) Log.WriteLine("ASCOM: {0:0000}: [{1}] Expecting two #-delimited responses for Command...", job.Number, job.Command);
							break;
					}

					reply = _oat.Action("Serial:PassThroughCommand", command);
					if (_logJobs) Log.WriteLine("ASCOM: {0:0000}: [{1}] Received reply: '{2}'", job.Number, job.Command, reply);
					response = new CommandResponse(reply, true);
				}
				catch (Exception ex)
				{
					Log.WriteLine("ASCOM: {0:0000}: [{1}] Failed to send command. {2}", job.Number, job.Command, ex.Message);
					job.OnFulFilled(new CommandResponse(string.Empty, false, $"Unable to execute command. " + ex.Message));
					return;
				}
			}
			else
			{
				Log.WriteLine("ASCOM: {0:0000}: Telescope is not connected!", job.Number);
				response = new CommandResponse(string.Empty, false, $"Unable to connect to telescope");
			}

			if (_logJobs) Log.WriteLine("ASCOM: {0:0000}: Calling OnFulFilled lambda", job.Number);
			job.OnFulFilled(response);
			job.Succeeded = response.Success;
			if (_logJobs) Log.WriteLine("ASCOM: {0:0000}: Job completed", job.Number);
		}

		public override bool Connect()
		{
			// Defensive: _oat is normally set by the constructor, but RunSetupDialog() can
			// leave it null on whichever handler instance it was called on. Re-create it
			// here rather than risk an unhandled NullReferenceException taking down the
			// whole app if this ever runs on an instance whose _oat is null.
			if (_oat == null)
			{
				_oat = new ASCOM.DriverAccess.Telescope("ASCOM.OpenAstroTracker.Telescope");
			}
			_oat.Connected = true;
			StopJobsProcessor();
			StartJobsProcessor();
			return _oat.Connected;
		}

		public override void Disconnect()
		{
			Log.WriteLine("ASCOM: Stopping Jobs processor.");
			StopJobsProcessor();
			if (_oat != null)
			{
				_oat.Connected = false;
			}
		}

		public override void DiscoverDeviceInstances(Action<string> addDevice)
		{
			addDevice("ASCOM: OpenAstroTracker");
		}

		public override bool IsDriverForDevice(string device)
		{
			return device.StartsWith("ASCOM: ");
		}

		public override bool SupportsSetupDialog
		{
			get
			{
				return true;
			}
		}

		public override bool RunSetupDialog()
		{
			// This handler instance is the long-lived template from
			// CommunicationHandlerFactory's static handler list, not the instance that
			// Connect() is later called on (ConnectToDevice() creates a fresh handler via
			// CreateHandler() for that). So _oat here must NOT be left holding a live COM
			// reference after this method returns: the ASCOM local server only exits itself
			// once every served object's reference count drops to zero, and a reference
			// pinned in this template for the rest of the app's lifetime would prevent
			// that, leaving the local server process running in the background even after
			// the real connection (on the other handler instance) is fully disconnected.
			// Null it out instead; Connect()'s own null-check lazily creates a fresh
			// instance on whichever handler instance actually needs one.
			_oat = new ASCOM.DriverAccess.Telescope("ASCOM.OpenAstroTracker.Telescope");
			try
			{
				_oat.SetupDialog();
			}
			finally
			{
				_oat.Dispose();
				_oat = null;
			}
			return true;
		}

		public override ICommunicationHandler CreateHandler(string device)
		{
			var regex = new System.Text.RegularExpressions.Regex(@"([A-z]+:\s*)([A-z]+)@?(\d+)?");
			var result = regex.Match(device);
			if (result.Success)
			{
				device = result.Groups[2].Value;
			}
			return new ASCOMCommunicationHandler(device);
		}
	}
}
