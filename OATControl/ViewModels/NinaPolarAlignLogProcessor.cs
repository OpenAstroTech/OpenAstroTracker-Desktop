using OATCommunications;
using OATCommunications.Utilities;
using OATControl.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

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
					AppSettings.Instance.NinaLogFolder = folder;
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
					Log.WriteLine($"NINALOG: Looks like the user cancelled PA in NINA within {lineCount} lines, back to Idle.");

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
							Log.WriteLine($"NINALOG: Looks like the user initiatest PA in NINA within {lineCount} lines, move to Starting.");

							ShowDialogStatus("Started", "Polar alignment started.");
						}
						else
						{
							startLine = _allTextList.FindIndex(_examinedLines, l => l.Contains("ImageSolver.cs") && l.Contains("Platesolve successful"));
							if (startLine > 0)
							{
								// Platesolve event found outside of polar alignment, so raise the event
								Log.WriteLine($"NINALOG: Looks like a NINA platesolve succeeded at line {startLine}.");
								Regex regex = new Regex(@".*Coordinates: RA: (\d{2}):(\d{2}):(\d{2}); Dec: ([+-]?\d+)[°\s]+(\d+)['\s]+(\d+)\"";.*$");
								var matches = regex.Match(_allTextList[startLine]);
								if (matches.Success && matches.Groups.Count == 7)
								{
									int raH = int.Parse(matches.Groups[1].Value);
									int raM = int.Parse(matches.Groups[2].Value);
									int raS = int.Parse(matches.Groups[3].Value);
									int decD = int.Parse(matches.Groups[4].Value);
									int decM = int.Parse(matches.Groups[5].Value);
									int decS = int.Parse(matches.Groups[6].Value);
									float raHours = raH + (raM / 60.0f) + (raS / 3600.0f);
									float decDegrees = Math.Abs(decD) + (decM / 60.0f) + (decS / 3600.0f);
									if (decD < 0)
									{
										decDegrees = -decDegrees;
									}
									Log.WriteLine($"NINALOG: The coordinates were RA: {raHours}h, DEC:{decDegrees}deg.");

									RaisePlatesolveCompleted(new PlatesolveEventArgs(raHours, decDegrees));
								}
								_examinedLines = lineCount;
							}
							_examinedLines = lineCount;
						}
						break;
					case "Starting":
						{
							var calcLine = _allTextList.FindIndex(_examinedLines, l => l.Contains("PolarAlignment.cs") && l.Contains("First measurement"));
							if (calcLine > 0)
							{
								Log.WriteLine($"NINALOG: Found first measurement at line {calcLine}");
								ShowDialogStatus("Measure", _allTextList[calcLine]);
							}
							calcLine = _allTextList.FindIndex(_examinedLines, l => l.Contains("PolarAlignment.cs") && l.Contains("Second measurement"));
							if (calcLine > 0)
							{
								Log.WriteLine($"NINALOG: Found second measurement at line {calcLine}");
								ShowDialogStatus("Measure", _allTextList[calcLine]);
							}
							calcLine = _allTextList.FindIndex(_examinedLines, l => l.Contains("PolarAlignment.cs") && l.Contains("Third measurement"));
							_examinedLines = lineCount;
							if (calcLine > 0)
							{
								Log.WriteLine($"NINALOG: Found third measurement at line {calcLine}");
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
								Log.WriteLine($"NINALOG: Found calculated error at line {calcLine}");
								_examinedLines = calcLine + 1;
								_numCalculatedErrors++;
								Regex regex = new Regex(@"Calculated Error: ([+-]?\d+)[°\s]+(\d+)[\'\s]+(\d+)[\""\s]*$");
								var error = _allTextList[calcLine].Substring(_allTextList[calcLine].IndexOf("Calculated Error:") + 17).Trim();
								var errors = error.Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
								var azError = errors[0].Substring(3).Trim();
								var altError = errors[1].Substring(5).Trim();
								var totalError = errors[2].Substring(5).Trim();
								ShowDialogStatus("CalculateSettle", $"{azError}|{altError}|{totalError}|({_numCalculatedErrors}/2)");
								Log.WriteLine($"NINALOG: Errors are AZ: {azError}, ALT: {altError}");
								if (_numCalculatedErrors > 1)
								{
									Log.WriteLine($"NINALOG: Have more than one errors, so move to Adjusting");
									ShowDialogStatus("Adjust", _allTextList[calcLine]);
									var azAdjust = (AppSettings.Instance.InvertAZCorrections ? -1 : 1) * ParseMinutes(azError, @"^([+-]?\d+)[+°\s]+(\d+)[\'\s]+(\d+)[\""\s]*$");
									var altAdjust = (AppSettings.Instance.InvertALTCorrections ? -1 : 1) * ParseMinutes(altError, @"^([+-]?\d+)[°\s]+(\d+)[\'\s]+(\d+)[\""\s]*$");

									Log.WriteLine($"NINALOG: Required adjustment is AZ:{azAdjust}, ALT: {altAdjust}.");
									Log.WriteLine($"NINALOG: Adjustment AZ Limits: {60 * AppSettings.Instance.AZLimit}");

									if (Math.Abs(azAdjust) > 60 * AppSettings.Instance.AZLimit)
									{
										Log.WriteLine($"NINALOG: AZ Adjustment is too large! (Requested ${azAdjust}m, but limit is ${60 * AppSettings.Instance.AZLimit}m)");
										string msg = "";
										msg = $"Azimuth error is too large for automatic adjustment, please move mount manually to within {AppSettings.Instance.AZLimit.ToString("F1")} degrees horizontally.";
										ShowDialogStatus("Error", msg);
										_numCalculatedErrors = 0;
										return;
									}

									Log.WriteLine($"NINALOG: Adjustment ALT Limits: {60 * AppSettings.Instance.ALTLimit}");

									if (Math.Abs(altAdjust) > 60 * AppSettings.Instance.ALTLimit)
									{
										string msg = "";
										Log.WriteLine($"NINALOG: ALT Adjustment is too large! (Requested ${altAdjust}m, but limit is ${60 * AppSettings.Instance.ALTLimit}m)");
										msg = $"Altitude error is too large for automatic adjustment, please move mount manually to within {AppSettings.Instance.ALTLimit.ToString("F1")} degrees vertically.";
										ShowDialogStatus("Error", msg);
										_numCalculatedErrors = 0;
										return;
									}

									var totalAdjust = ParseMinutes(totalError, @"^([+-]?\d+)[°\s]+(\d+)[\'\s]+(\d+)[\""\s]*$");
									if (totalAdjust * 60 < AppSettings.Instance.PolarAlignmentMinimumTotalError)
									{
										Log.WriteLine($"NINALOG: Total error is below {AppSettings.Instance.PolarAlignmentMinimumTotalError} arcsecs, alignment succeeded!!");
										_examinedLines = lineCount;
										_polarAlignState = "Complete";
										ShowDialogStatus("Succeeded", $"Polar Alignment succeeded, error below {AppSettings.Instance.PolarAlignmentMinimumTotalError} arcsecs.");
										return;

									}

									Log.WriteLine($"NINALOG: Sending Adjustment to mount");
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
									Log.WriteLine($"NINALOG: Alignment cancelled by user!");
									alignmentIsComplete = true;
									message = "Polar alignment cancelled by user.";
								}
								alignmentIsComplete |= _allTextList.FindIndex(_examinedLines, l => l.Contains("Received message to stop polar alignment")) > 0;
								if (alignmentIsComplete)
								{
									Log.WriteLine($"NINALOG: Alignment succeeded!!");
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
					Log.WriteLine($"NINALOG: Alignment is complete!");
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