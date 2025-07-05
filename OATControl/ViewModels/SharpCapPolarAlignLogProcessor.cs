using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Interop;
using MahApps.Metro.Controls;
using OATControl.ViewModels;

namespace OATControl.ViewModels
{
	public class SharpCapPolarAlignLogProcessor : PolarAlignLogProcessorBase
	{
		protected override string LogFolder
		{
			get
			{
				var folder = AppSettings.Instance.SharpCapLogFolder;
				if (string.IsNullOrEmpty(folder))
				{
					folder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SharpCap", "Logs");
					AppSettings.Instance.SharpCapLogFolder = folder;
					AppSettings.Instance.Save();
				}
				return folder;
			}
		}

		public override void LogfileWasReset()
		{
			_polarAlignState = "Idle";
		}

		float CalcAltitudeError(string error, string pole)
		{
			return ParseDegrees(pole, @"^([+-]?\d+):(\d+):(\d+)$") - ParseDegrees(error, @"^([+-]?\d+):(\d+):(\d+)$");
		}

		float CalcAzimuthError(string error, string pole)
		{
			float poleErr = ParseDegrees(pole, @"^([+-]?\d+):(\d+):(\d+)$") + 180;
			if (poleErr > 360)
			{
				poleErr -= 360;
			}
			poleErr -= 180;

			float errorErr = ParseDegrees(error, @"^([+-]?\d+):(\d+):(\d+)$") + 180;
			if (errorErr > 360)
			{
				errorErr -= 360;
			}
			errorErr -= 180;

			return poleErr - errorErr;
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
				if (_allTextList.FindIndex(_examinedLines, l => l.Contains("Selecting transform None")) > 0)
				{
					_examinedLines = lineCount;
					ShowDialogStatus("Succeeded", "Polar alignment completed.");
					return;
				}

				switch (_polarAlignState)
				{
					case "Idle":
						var startLine = _allTextList.FindIndex(_examinedLines, l => l.Contains("Polar Align executing") || l.Contains("Polar Alignment ready"));
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
							var calcLine = _allTextList.FindIndex(_examinedLines, l => l.Contains("Final First Frame"));
							if (calcLine > 0)
							{
								ShowDialogStatus("Measure", _allTextList[calcLine]);
							}
							calcLine = _allTextList.FindIndex(_examinedLines, l => l.Contains("Final Second Frame"));
							if (calcLine > 0)
							{
								ShowDialogStatus("Measure", _allTextList[calcLine]);
							}
							_examinedLines = lineCount;
							if (calcLine > 0)
							{
								_polarAlignState = "Calculating";
								_examinedLines = calcLine + 1;
							}
						}
						break;
					case "Calculating":
						{
							var calcLine = _allTextList.FindIndex(_examinedLines, l => l.Contains("PolarAligner.CalcAltAzOffset()"));
							if (calcLine > 0)
							{
								_examinedLines = calcLine + 1;
								Regex regex = new Regex(@"Info\s+(\d{2}:\d{2}:\d{2}\.\d{6}).*?AltAzCor=Alt=([^,]+),Az=([^ ]+)\s+AltAzPole=Alt=([^,]+),Az=([^,]+),\s+AltAzOffset=.*?");
								Match match = regex.Match(_allTextList[calcLine]);
								if (match.Success)
								{
									_numCalculatedErrors++;
									float azError = CalcAzimuthError(match.Groups[2].Value, match.Groups[4].Value);
									float altError = CalcAltitudeError(match.Groups[3].Value, match.Groups[5].Value);
									float totalError = (float)Math.Sqrt(azError * azError + altError * altError);
									ShowDialogStatus("CalculateSettle", $"{ToDegreeString(azError)}|{ToDegreeString(altError)}|{ToDegreeString(totalError)}|({_numCalculatedErrors}/2)");
									if (_numCalculatedErrors > 1)
									{
										ShowDialogStatus("Adjust", "");

										var azAdjust = (AppSettings.Instance.InvertAZCorrections ? -1 : 1) * azError * 60.0f;
										var altAdjust = (AppSettings.Instance.InvertALTCorrections ? -1 : 1) * altError * 60.0f;
										if ((Math.Abs(azAdjust) > 60 * 3) || (Math.Abs(altAdjust) > 60 * 3))
										{
											string msg = "";
											if ((Math.Abs(azAdjust) > 60 * 3) && Math.Abs(altAdjust) < 60 * 3)
												msg = "Azimuth error is too large for automatic adjustment, please move mount manually to within 3 degrees.";
											else if ((Math.Abs(azAdjust) < 60 * 3) && Math.Abs(altAdjust) > 60 * 3)
												msg = "Altitude error is too large for automatic adjustment, please move mount manually to within 3 degrees.";
											else
												msg = "Both Azimuth and Altitude errors are too large for automatic adjustment, please move mount manually to within 3 degrees.";
											ShowDialogStatus("Error", msg+" Will keep monitoring error...");
											// We'll still keep looping to see if we get a smaller error
											_numCalculatedErrors = 0;

											return;
										}
										else
										{
											// Clear any error we may have had
											ShowDialogStatus("Error", "");
										}
										RaiseCorrectionRequired(new PolarAlignCorrectionEventArgs(altAdjust, azAdjust));
										_polarAlignState = "Adjusting";
									}
								}
							}
							else
							{
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
			return new DlgSharpCapPolarAlignment(closeAction);
		}
	}
}