using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using OATControl.ViewModels;
using OATCommunications;
using System.Text.RegularExpressions;
using System.Windows;
using System.Threading.Tasks;
using MahApps.Metro.Controls;

namespace OATControl.ViewModels
{
	public class NinaPolarAlignLogProcessor : PolarAlignLogProcessorBase
	{

		protected override string LogFolder
		{
			get
			{
				var folder = AppSettings.Instance.NinaLogFolder;
				if (string.IsNullOrEmpty(folder))
				{
					folder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NINA", "Logs");
					AppSettings.Instance.NinaLogFolder=folder;
					AppSettings.Instance.Save();
				}
				return folder;
			}
		}

		protected override string Name => "NINA";

		public override void LogfileWasReset()
		{
			_polarAlignState = "Idle";
		}

		protected override void ProcessLogLines()
		{
			int lineCount = 0;
			lock (_allTextList)
			{
				lineCount = _allTextList.Count;
			}

			while (lineCount > _examinedLines)
			{
				if (_allTextList.FindIndex(_examinedLines, l => l.Contains("ERROR|PolarAlignment.cs|Solve|")) > 0)
				{
					_examinedLines = lineCount;
					ShowDialogStatus("Succeeded", "Polar alignment cancelled by user.");
					return;
				}

				switch (_polarAlignState)
				{
					case "Idle":
						var startLine = _allTextList.FindIndex(_examinedLines, l => l.Contains("PolarAlignment.cs") && l.Contains("Starting"));
						if (startLine > 0)
						{
							_examinedLines = startLine;
							_numCalculatedErrors = 0;
							_polarAlignState = "Starting";
							ShowDialogStatus("Started", "Polar alignment started.");
						}
						else
						{
							_examinedLines = lineCount;
						}
						break;
					case "Starting":
						{
							var calcLine = _allTextList.FindIndex(_examinedLines, l => l.Contains("PolarAlignment.cs") && l.Contains("First measurement"));
							if (calcLine > 0)
							{
								ShowDialogStatus("Measure", _allTextList[calcLine]);
							}
							calcLine = _allTextList.FindIndex(_examinedLines, l => l.Contains("PolarAlignment.cs") && l.Contains("Second measurement"));
							if (calcLine > 0)
							{
								ShowDialogStatus("Measure", _allTextList[calcLine]);
							}
							calcLine = _allTextList.FindIndex(_examinedLines, l => l.Contains("PolarAlignment.cs") && l.Contains("Third measurement"));
							_examinedLines = lineCount;
							if (calcLine > 0)
							{
								ShowDialogStatus("Measure", _allTextList[calcLine]);
								_polarAlignState = "Calculating";
								_examinedLines = calcLine + 1;
							}
						}
						break;
					case "Calculating":
						{
							var calcLine = _allTextList.FindIndex(_examinedLines, l => l.Contains("PolarAlignment.cs") && l.Contains("Calculated Error"));
							if (calcLine > 0)
							{
								_examinedLines = calcLine + 1;
								_numCalculatedErrors++;
								Regex regex = new Regex(@"Calculated Error: ([+-]?\d+)[°\s]+(\d+)[\'\s]+(\d+)[\""\s]*$");
								var error = _allTextList[calcLine].Substring(_allTextList[calcLine].IndexOf("Calculated Error:") + 17).Trim();
								var errors = error.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
								var azError = errors[0].Substring(3).Trim();
								var altError = errors[1].Substring(5).Trim();
								var totalError = errors[2].Substring(5).Trim();
								ShowDialogStatus("CalculateSettle", $"{azError}|{altError}|{totalError}|({_numCalculatedErrors}/2)");
								if (_numCalculatedErrors > 1)
								{
									ShowDialogStatus("Adjust", _allTextList[calcLine]);
									var azAdjust = (AppSettings.Instance.InvertAZCorrections ? -1 : 1) * ParseMinutes(azError, @"^([+-]?\d+)[+°\s]+(\d+)[\'\s]+(\d+)[\""\s]*$");
									var altAdjust = (AppSettings.Instance.InvertALTCorrections ? -1 : 1) * ParseMinutes(altError, @"^([+-]?\d+)[°\s]+(\d+)[\'\s]+(\d+)[\""\s]*$");
									if ((Math.Abs(azAdjust) > 60 * 3) || (Math.Abs(altAdjust) > 60 * 3))
									{
										string msg = "";
										if ((Math.Abs(azAdjust) > 60 * 3) && Math.Abs(altAdjust) < 60 * 3)
											msg = "Azimuth error is too large for automatic adjustment, please move mount manually to within 3 degrees.";
										else if ((Math.Abs(azAdjust) < 60 * 3) && Math.Abs(altAdjust) > 60 * 3)
											msg = "Altitude error is too large for automatic adjustment, please move mount manually to within 3 degrees.";
										else
											msg = "Both Azimuth and Altitude errors are too large for automatic adjustment, please move mount manually to within 3 degrees.";
										ShowDialogStatus("Error", msg);
										_polarAlignState = "Idle";
										return;
									}
									RaiseCorrectionRequired(new PolarAlignCorrectionEventArgs(altAdjust, azAdjust));
									_polarAlignState = "Adjusting";
								}
							}
							else
							{
								bool alignmentIsComplete = _allTextList.FindIndex(_examinedLines, l => l.Contains("PolarAlignment.cs") && l.Contains("Total Error is below alignment tolerance")) > 0;
								string message = "Polar alignment succeeded.";
								if (_allTextList.FindIndex(_examinedLines, l => l.Contains("ERROR|PolarAlignment.cs|Solve|")) > 0)
								{
									alignmentIsComplete = true;
									message = "Polar alignment cancelled by user.";
								}
								alignmentIsComplete |= _allTextList.FindIndex(_examinedLines, l => l.Contains("Received message to stop polar alignment")) > 0;
								if (alignmentIsComplete)
								{
									_examinedLines = lineCount;
									_polarAlignState = "Complete";
									ShowDialogStatus("Succeeded", message);
									return;
								}
								_examinedLines = lineCount;
							}
						}
						break;
					case "Adjusting":
						{
							// Start polling the mount asynchronously, but only if not already polling
							if (_sendMountCommandAsync != null && !_isPollingAdjustment)
							{
								_isPollingAdjustment = true;
								_ = PollAdjustmentCompleteAsync();
							}
							// Exit the loop, the polling will handle state transition
							return;
						}
					case "Complete":
						{
							_examinedLines = lineCount;
						}
						break;
				}

				bool alignmentComplete = _allTextList.FindIndex(_examinedLines, l => l.Contains("PolarAlignment.cs") && l.Contains("Total Error is below alignment tolerance")) > 0;
				alignmentComplete |= _allTextList.FindIndex(_examinedLines, l => l.Contains("Received message to stop polar alignment")) > 0;
				if (alignmentComplete)
				{
					_examinedLines = lineCount;
					_polarAlignState = "Complete";
					ShowDialogStatus("Succeeded", "Polar alignment succeeded.");
					return;
				}
			}
		}

		private async Task PollAdjustmentCompleteAsync()
		{
			bool azRunning = false;
			bool altRunning = false;
			bool posValid = false;
			do
			{
				string gxResult = await _sendMountCommandAsync(":GX#,#");
				if (!string.IsNullOrEmpty(gxResult))
				{
					var parts = gxResult.Split(',');
					if (parts.Length > 1)
					{
						posValid = true;
						azRunning = (parts[1].Length > 3 && parts[1][3] != '-');
						altRunning = (parts[1].Length > 4 && parts[1][4] != '-');
					}
				}
				await Task.Delay(250);
			}
			while (posValid && (azRunning || altRunning));

			// Adjustment is complete, so start the adjustment loop again.
			_polarAlignState = "Calculating";
			_numCalculatedErrors = 0;
			ShowDialogStatus("ResetLoop", "Adjustment complete, restarting calculation loop.");
			_isPollingAdjustment = false;
			// Re-process log lines to continue the state machine
			ProcessLogLines();
		}

		protected override IPolarAlignDialog CreateAlignmentDialog(Action closeAction)
		{
			return new DlgNinaPolarAlignment(closeAction);
		}
	}
}