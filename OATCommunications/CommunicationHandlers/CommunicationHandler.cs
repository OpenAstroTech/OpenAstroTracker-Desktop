using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using OATCommunications.Utilities;

namespace OATCommunications.CommunicationHandlers
{
	public class Job
	{
		public Job(string command, ResponseType responseType, Action<CommandResponse> onFulFilled)
		{
			Command = command;
			ResponseType = responseType;
			OnFulFilled = onFulFilled;
		}
		public string Command { get; set; }
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
		protected bool _logJobs = true;

		protected void StartJobsProcessor()
		{
			Log.WriteLine("COMMS: Start Jobs processor");
			_jobs = new Queue<Job>();
			_processJobs = true;
			_jobsProcessorStopped.Reset();
			_jobProcessingThread = new Thread(ProcessJobQueue);
			_jobProcessingThread.Start();
		}

		protected void StopJobsProcessor()
		{
			Log.WriteLine("COMMS: Stop Jobs processor");
			_processJobs = false;
			_jobsAvailable.Set();
			_jobsProcessorStopped.WaitOne();
			_jobsProcessorStopped.Reset();
			_jobProcessingThread.Join();
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
				if (_logJobs) Log.WriteLine("JOBPROC: Adding Job [{0}] to queue, {1} jobs, setting signal", command, _jobs.Count + 1);
				_jobs.Enqueue(new Job(command, needsResponse, onFullFilledAction));
				_jobsAvailable.Set();
			}
		}

		protected void ProcessJobQueue(object obj)
		{
			Log.WriteLine("JOBPROC: Start Jobs thread");

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
					if (_logJobs) Log.WriteLine("JOBPROC: {0} Jobs available, getting Job", _jobs.Count);
					job = _jobs.Dequeue();
					if (!_jobs.Any())
					{
						if (_logJobs) Log.WriteLine("JOBPROC: No more Jobs available, resetting signal.");
						_jobsAvailable.Reset();
					}
				}

				if (_logJobs) Log.WriteLine("JOBPROC: Running job [{0}]", job.Command);
				RunJob(job);
				if (_logJobs) Log.WriteLine("JOBPROC: Completed job [{0}]", job.Command);
			}
			while (_processJobs);
			Log.WriteLine("JOBPROC: End Jobs thread");
			_jobsProcessorStopped.Set();
		}

		protected abstract void RunJob(Job job);

		public abstract bool Connected { get; }

		public abstract void Disconnect();
	}
}