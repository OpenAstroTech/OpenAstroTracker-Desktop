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

			foreach (var action in _oat.SupportedActions)
			{
				Log.WriteLine($"COMMFACTORY: ASCOM handler supported action: {action} ...");
			}
			_oat.Action("Serial:PassThroughCommand", "I#");
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
			// _oat can be null here if RunSetupDialog() was called since this handler was
			// created (it previously left _oat null after the setup dialog closed), so
			// re-create it defensively rather than risk an unhandled NullReferenceException
			// taking down the whole app.
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
			_oat = new ASCOM.DriverAccess.Telescope("ASCOM.OpenAstroTracker.Telescope");
			try
			{
				_oat.SetupDialog();
			}
			finally
			{
				_oat.Dispose();
				// Re-create the handler's driver instance rather than leaving _oat null:
				// the setup dialog may have connected/disconnected this instance, and a
				// subsequent Connect() on this same handler must not dereference null.
				_oat = new ASCOM.DriverAccess.Telescope("ASCOM.OpenAstroTracker.Telescope");
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
