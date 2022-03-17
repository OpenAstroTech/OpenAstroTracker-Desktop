using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using OATCommunications.Utilities;

namespace OATCommunications.CommunicationHandlers
{
	public class Job
	{
		public Job(long number, string command, ResponseType responseType, Action<CommandResponse> onFulFilled)
		{
			Number = number;
			Command = command;
			ResponseType = responseType;
			Succeeded = false;
			OnFulFilled = onFulFilled;
		}
		public string Command { get; set; }
		public long Number { get; set; }
		public bool Succeeded { get; set; }
		public ResponseType ResponseType { get; set; }

		public Action<CommandResponse> OnFulFilled { get; set; }
	}

	public abstract class CommunicationHandler : ICommunicationHandler
	{
		private Queue<Job> _jobs;
		private Object _jobsQueue = new object();
		private ManualResetEvent _jobsAvailable = new ManualResetEvent(false);
		private ManualResetEvent _jobsProcessorStopped = new ManualResetEvent(false);
		private Thread _jobProcessingThread;
		private bool _processJobs;
		private static long jobNr = 1;
		private static long jobsSucceeded = 0;
		private static long jobsFailed = 0;
#if DEBUG
		protected bool _logJobs = true;
#else
		protected bool _logJobs = false;
#endif

		public void EnableLogging(bool state)
		{
			_logJobs = state;
		}

		protected void StartJobsProcessor()
		{
			if (_logJobs) Log.WriteLine("COMMS: Start Jobs processor");
			_jobs = new Queue<Job>();
			_processJobs = true;
			_jobsProcessorStopped.Reset();
			_jobProcessingThread = new Thread(ProcessJobQueue);
			_jobProcessingThread.Start();
		}

		protected void StopJobsProcessor()
		{
			if (_logJobs) Log.WriteLine("COMMS: Stop Jobs processor");
			if (_jobProcessingThread != null)
			{
				if (_logJobs) Log.WriteLine("COMMS: Stop Jobs processor, setting jobsAvailable event");
				_processJobs = false;
				_jobsAvailable.Set();
				if (_logJobs) Log.WriteLine("COMMS: Waiting for Jobs processor stop event");
				_jobsProcessorStopped.WaitOne();
				_jobsProcessorStopped.Reset();
				if (_logJobs) Log.WriteLine("COMMS: Waiting for Jobs processor thread end");
				_jobProcessingThread.Join();
				_jobProcessingThread = null;
				if (_logJobs) Log.WriteLine("COMMS: Done stopping Jobs processor");
			}
			else
			{
				if (_logJobs) Log.WriteLine("COMMS: Jobs processor is not running!");
			}
		}

		public void SendBlind(string command, Action<CommandResponse> onFullFilledAction)
		{
			SendCommand(command, ResponseType.NoResponse, onFullFilledAction);
		}

		public void SendCommand(string command, Action<CommandResponse> onFullFilledAction)
		{
			SendCommand(command, ResponseType.FullResponse, onFullFilledAction);
		}

		public void SendCommandConfirm(string command, Action<CommandResponse> onFullFilledAction)
		{
			SendCommand(command, ResponseType.DigitResponse, onFullFilledAction);
		}

		public void SendCommandDoubleResponse(string command, Action<CommandResponse> onFullFilledAction)
		{
			SendCommand(command, ResponseType.DoubleFullResponse, onFullFilledAction);
		}

		private void SendCommand(string command, ResponseType needsResponse, Action<CommandResponse> onFullFilledAction)
		{
			lock (_jobsQueue)
			{
				if (_logJobs) Log.WriteLine("JOBPROC: Job {2:0000} [{0}] added to queue, {1} jobs, setting signal", command, _jobs.Count + 1, jobNr);
				_jobs.Enqueue(new Job(jobNr++, command, needsResponse, onFullFilledAction));
				_jobsAvailable.Set();
			}
		}

		protected void ProcessJobQueue(object obj)
		{
			if (_logJobs) Log.WriteLine("JOBPROC: Start Jobs thread");

			do
			{
				Job job = null;
				if (_logJobs) Log.WriteLine("JOBPROC: Wait for Job...");
				_jobsAvailable.WaitOne();

				if (!_processJobs)
				{
					if (_logJobs) Log.WriteLine("JOBPROC: Quit requested");
					break;
				}

				lock (_jobsQueue)
				{
					job = _jobs.Dequeue();
					if (_logJobs) Log.WriteLine("JOBPROC: There are {0} Jobs available, getting Job {1:0000}", _jobs.Count + 1, job.Number);
					if (!_jobs.Any())
					{
						if (_logJobs) Log.WriteLine("JOBPROC: No more Jobs available, resetting signal.");
						_jobsAvailable.Reset();
					}
				}

				if (_logJobs) Log.WriteLine("JOBPROC: Job {1:0000} [{0}] to be run now", job.Command, job.Number);
				RunJob(job);
				if (job.Succeeded) jobsSucceeded++; else jobsFailed++;
				if (_logJobs) Log.WriteLine("JOBPROC: Job {1:0000} [{0}] completed {2}", job.Command, job.Number, job.Succeeded ? "successfully" : "with failure");
			}
			while (_processJobs);
			if (_logJobs) Log.WriteLine("JOBPROC: End Jobs thread. Processed {0} jobs, {1} failures", jobsSucceeded + jobsFailed, jobsFailed);
			_jobsProcessorStopped.Set();
		}


		public abstract string Name { get; }
		public abstract bool Connected { get; }
		public abstract void Disconnect();
		public abstract bool Connect();
		public abstract bool IsDriverForDevice(string device);
		public abstract ICommunicationHandler CreateHandler(string device);
		public abstract void DiscoverDeviceInstances(Action<string> addDevice);
		public virtual bool SupportsSetupDialog { get { return false; } }
		public virtual bool RunSetupDialog() { return true; }

		protected abstract void RunJob(Job job);
	}
}