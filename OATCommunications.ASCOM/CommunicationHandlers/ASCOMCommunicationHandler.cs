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

		public override bool Connected { get { return _oat.Connected; } }

		long requestIndex = 1;

		protected override void RunJob(Job job)
		{
			CommandResponse response = null;

			if (_logJobs) Log.WriteLine("[{0:0000}] ASCOM: [{1}] Processing Job", requestIndex, job.Command);
			if (Connected)
			{
				if (_logJobs) Log.WriteLine("[{0:0000}] ASCOM: [{1}] Connected! Discarding any bytes in input buffer", requestIndex, job.Command);
				requestIndex++;
				string reply;
				try
				{
					if (_logJobs) Log.WriteLine("[{0:0000}] ASCOM: [{1}] Sending command", requestIndex, job.Command);
					string command = job.Command;
					switch (job.ResponseType)
					{
						case ResponseType.NoResponse:
							if (_logJobs) Log.WriteLine("[{0:0000}] ASCOM: [{1}] Expecting no response for command", requestIndex, job.Command);
							break;

						case ResponseType.DigitResponse:
							if (_logJobs) Log.WriteLine("[{0:0000}] ASCOM: [{1}] Expecting single digit response for command", requestIndex, job.Command);
							command += ",n";
							break;

						case ResponseType.FullResponse:
							if (_logJobs) Log.WriteLine("[{0:0000}] ASCOM: [{1}] Expecting #-delimited response for Command...", requestIndex, job.Command);
							command += ",#";
							break;
						case ResponseType.DoubleFullResponse:
							command += ",##";
							if (_logJobs) Log.WriteLine("[{0:0000}] ASCOM: [{1}] Expecting two #-delimited responses for Command...", requestIndex, job.Command);
							break;
					}

					reply = _oat.Action("Serial:PassThroughCommand", command);
					if (_logJobs) Log.WriteLine("[{0:0000}] ASCOM: [{1}] Received reply: '{2}'", requestIndex, job.Command, reply);
					response = new CommandResponse(reply, true);
				}
				catch (Exception ex)
				{
					Log.WriteLine("[{0:0000}] ASCOM: [{1}] Failed to send command. {2}", requestIndex, job.Command, ex.Message);
					job.OnFulFilled(new CommandResponse(string.Empty, false, $"Unable to execute command. " + ex.Message));
					return;
				}
			}
			else
			{
				Log.WriteLine("[{0:0000}] ASCOM: Telescope is not connected!", requestIndex);
				response = new CommandResponse(string.Empty, false, $"Unable to connect to telescope");
			}

			if (_logJobs) Log.WriteLine("[{0:0000}] ASCOM: Calling OnFulFilled lambda", requestIndex);
			job.OnFulFilled(response);
			if (_logJobs) Log.WriteLine("[{0:0000}] ASCOM: Job completed", requestIndex);
		}

		public override bool Connect()
		{
			_oat.Connected = true;
			StopJobsProcessor();
			StartJobsProcessor();
			return _oat.Connected;
		}

		public override void Disconnect()
		{
			Log.WriteLine("ASCOM: Stopping Jobs processor.");
			StopJobsProcessor();
			_oat.Connected = false;
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
			_oat.SetupDialog();
			_oat.Dispose();
			_oat = null;
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
