using MahApps.Metro.Controls;
using OATCommunications.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace OATControl.ViewModels
{
	public abstract class PolarAlignLogProcessorBase : IPolarAlignLogProcessor
	{
		public event EventHandler<PolarAlignStatusEventArgs> StatusChanged;
		public event EventHandler<PolarAlignCorrectionEventArgs> CorrectionRequired;

		protected List<string> _allTextList = new List<string>(5000);
		protected int _examinedLines;
		private FileSystemWatcher _logWatcher;
		protected string _polarAlignState;
		protected string _latestLogfile;
		protected bool _monitoring = false;
		protected long _lastLogPosition = 0;
		protected private int _numCalculatedErrors;
		protected Func<string, Task<string>> _sendMountCommandAsync;
		private IPolarAlignDialog _polarAlignmentDlg;
		protected bool _isPollingAdjustment = false;


		protected abstract string LogFolder { get; }
		protected abstract void ProcessLogLines();

		public void SetSendMountCommandDelegate(Func<string, Task<string>> sendCommand)
		{
			_sendMountCommandAsync = sendCommand;
		}

		protected void RaiseStatusChanged(PolarAlignStatusEventArgs args)
		{
			StatusChanged?.Invoke(this, args);
		}

		protected void RaiseCorrectionRequired(PolarAlignCorrectionEventArgs args)
		{
			CorrectionRequired?.Invoke(this, args);
		}

		public void Start()
		{
			if (_monitoring) return;
			_monitoring = true;
			string logFolder = LogFolder;
			if (string.IsNullOrEmpty(logFolder) || !Directory.Exists(logFolder))
				return;
			var logFiles = Directory.EnumerateFiles(logFolder, "*.log", SearchOption.TopDirectoryOnly).ToList();
			logFiles.Sort((f1, f2) => new FileInfo(f1).LastWriteTimeUtc.CompareTo(new FileInfo(f2).LastWriteTimeUtc));
			var latestLogfile = logFiles.LastOrDefault();
			if (latestLogfile != null)
			{
				Log.WriteLine("MOUNT: Latest NINA log file is " + latestLogfile + ". Reading and skipping existing lines...");
				_latestLogfile = latestLogfile;
				lock (_allTextList)
				{
					_examinedLines = 0;
					_numCalculatedErrors = 0;
					_allTextList.Clear();
					_lastLogPosition = 0;
					using (FileStream fs = File.Open(_latestLogfile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
					{
						fs.Seek(_lastLogPosition, SeekOrigin.Begin);
						using (StreamReader sr = new StreamReader(fs))
						{
							string line;
							while ((line = sr.ReadLine()) != null)
							{
								_allTextList.Add(line);
								_examinedLines++;
							}
							_lastLogPosition = fs.Position;
						}
					}
				}
				Log.WriteLine("MOUNT: Skipped " + _examinedLines + " lines (" + _lastLogPosition + " bytes)...");
				LogfileWasReset();
			}
			_logWatcher = new FileSystemWatcher(logFolder) { EnableRaisingEvents = true, NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size, Filter = "*.log" };
			_logWatcher.Changed += OnLogFileChanged;
		}

		public void Stop()
		{
			if (!_monitoring) return;
			_monitoring = false;
			if (_logWatcher != null)
			{
				_logWatcher.Changed -= OnLogFileChanged;
				_logWatcher.Dispose();
				_logWatcher = null;
			}
		}

		private void OnLogFileChanged(object sender, FileSystemEventArgs e)
		{
			var changedFile = Path.Combine(LogFolder, e.Name);
			if (changedFile != _latestLogfile)
			{
				_latestLogfile = changedFile;
				_lastLogPosition = 0;
				lock (_allTextList)
				{
					_examinedLines = 0;
					_allTextList.Clear();
				}
			}

			using (FileStream fs = File.Open(_latestLogfile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
			{
				fs.Seek(_lastLogPosition, SeekOrigin.Begin);
				using (StreamReader sr = new StreamReader(fs))
				{
					string line;
					lock (_allTextList)
					{
						while ((line = sr.ReadLine()) != null)
						{
							_allTextList.Add(line);
						}
						_lastLogPosition = fs.Position;
					}
				}
			}

			ProcessLogLines();
		}

		public virtual void LogfileWasReset()
		{
			throw new NotImplementedException();
		}

		protected float ParseMinutes(string input, string regexPattern)
		{
			input = input.Trim();
			var regex = new Regex(regexPattern);
			var match = regex.Match(input);
			if (!match.Success)
				throw new FormatException("Input string not in expected format: " + input);
			int sign = match.Groups[1].Value.StartsWith("-") ? -1 : 1;
			int degrees = Math.Abs(int.Parse(match.Groups[1].Value));
			int minutes = int.Parse(match.Groups[2].Value);
			int seconds = int.Parse(match.Groups[3].Value);
			float result = sign * (degrees * 60.0f + minutes + seconds / 60.0f);
			if (result > 180f*60f)
			{
				result = result- 360.0f*60f;
			}
			if (result < -180f * 60f)
			{
				result =  360.0f*60f + result;
			}

			return result;
		}

		protected float ParseDegrees(string input, string regexPattern)
		{
			return ParseMinutes(input, regexPattern) / 60.0f;
		}

		protected string ToDegreeString(float angle)
		{
			float sign = angle < 0 ? -1 : 1;
			float degrees = Math.Abs(angle);

			int d = (int)Math.Floor(degrees);
			degrees = (degrees - d) * 60f;
			int m = (int)Math.Floor(degrees);
			degrees = (degrees - m) * 60f;
			int s = (int)Math.Floor(degrees);

			return $"{d * sign:00}° {m:00}' {s:00}\"";
		}


		protected abstract IPolarAlignDialog CreateAlignmentDialog(Action closeCallback);

		// Default dialog/status handler 
		protected void ShowDialogStatus(string statusType, string message)
		{
			Application.Current.Dispatcher.Invoke(() =>
			{
				switch (statusType)
				{
					case "Started":
						if (_polarAlignmentDlg == null)
						{
							_polarAlignmentDlg = CreateAlignmentDialog(() =>
							{
								_polarAlignmentDlg?.Close();
								_polarAlignmentDlg = null;
								_polarAlignState = "Idle";
							});
							if (_polarAlignmentDlg is Window win)
							{
								win.WindowStartupLocation = WindowStartupLocation.CenterOwner;
								win.Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault();
							}
							_polarAlignmentDlg.Show();
						}
						break;
					case "Measure":
						_polarAlignmentDlg?.SetStatus("Measure", message);
						break;
					case "CalculateSettle":
						_polarAlignmentDlg?.SetStatus("CalculateSettle", message);
						break;
					case "Adjust":
						_polarAlignmentDlg?.SetStatus("Adjust", message);
						break;
					case "Error":
						_polarAlignmentDlg?.SetStatus("Error", message);
						break;
					case "Succeeded":
						_polarAlignmentDlg?.SetStatus("Succeeded", message);
						break;
					case "ResetLoop":
						_polarAlignmentDlg?.SetStatus("ResetLoop", message);
						break;
					default:
						break;
				}
			});
		}
	}
} 