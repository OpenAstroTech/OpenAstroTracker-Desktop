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
		private Thread _jobProcessingThread;
		private bool _processJobs;

		protected void StartJobsProcessor()
		{
			Log.WriteLine("COMMS: Start Jobs processor");
			_jobs = new Queue<Job>();
			_processJobs = true;
			_jobProcessingThread = new Thread(ProcessJobQueue);
			_jobProcessingThread.Start();
		}

		protected void StopJobsProcessor()
		{
			Log.WriteLine("COMMS: Stop Jobs processor");
			_processJobs = false;
			_jobsAvailable.Set();
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

		private void SendCommand(string command, ResponseType needsResponse, Action<CommandResponse> onFullFilledAction)
		{
			lock (_jobsQueue)
			{
				Log.WriteLine("JOBPROC: Adding Job [{0}] to queue, setting signal", command);
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
				Log.WriteLine("JOBPROC: Wait for Job");
				_jobsAvailable.WaitOne();

				if (!_processJobs)
				{
					Log.WriteLine("JOBPROC: Quit requested");
					break;
				}

				Log.WriteLine("JOBPROC: Job available, getting Job");
				lock (_jobsQueue)
				{
					job = _jobs.Dequeue();
					if (!_jobs.Any())
					{
						Log.WriteLine("JOBPROC: No more Jobs available, resetting signal.");
						_jobsAvailable.Reset();
					}
				}

				Log.WriteLine("JOBPROC: Running job [{0}]", job.Command);
				RunJob(job);
				Log.WriteLine("JOBPROC: Completed job [{0}]", job.Command);
			}
			while (_processJobs);
			Log.WriteLine("JOBPROC: End Jobs thread");
		}

		protected abstract void RunJob(Job job);

		public abstract bool Connected { get; }

		public abstract void Disconnect();
	}
}