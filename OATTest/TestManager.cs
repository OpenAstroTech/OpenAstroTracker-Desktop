using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using OATCommunications;
using OATCommunications.Model;

namespace OATTest
{
	public class TestManager
	{
		// Yeah, I know I'm mixing Models and ViewModels.... not true MVVM, more like VVM
		ObservableCollection<CommandTest> _tests;
		string _testSuiteFile;

		public TestManager()
		{
			_tests = new ObservableCollection<CommandTest>();
		}

		public long FirmwareVersion { get; set; }
		public DateTime UseDate { get; set; }
		public DateTime UseTime { get; set; }

		public void LoadTestSuite(string testXmlFile)
		{
			_testSuiteFile = testXmlFile;
			_tests.Clear();
			XDocument doc = XDocument.Load(testXmlFile);
			foreach (var testXml in doc.Element("TestSuites").Element("TestSuite").Elements("Test"))
			{
				var test = new CommandTest(testXml);
				_tests.Add(test);
			}
		}

		public ObservableCollection<CommandTest> Tests { get { return _tests; } }

		internal void ResetAllTests()
		{
			LoadTestSuite(_testSuiteFile);

			foreach (var test in _tests)
			{
				test.Reset();
			}
		}

		public void PrepareForRun()
		{
			foreach (var test in _tests)
			{
				test.Command = ReplaceMacros(test.Command);
				test.ExpectedReply = ReplaceMacros(test.ExpectedReply);
			}
		}

		public async Task RunAllTests(ICommunicationHandler handler, Action<string> debugOut)
		{
			foreach (var test in _tests)
			{
				try
				{
					if (FirmwareVersion < test.MinFirmwareVersion)
					{
						test.Status = CommandTest.StatusType.Skipped;
						continue;
					}
					if ((test.MaxFirmwareVersion != -1) && FirmwareVersion > test.MaxFirmwareVersion)
					{
						test.Status = CommandTest.StatusType.Skipped;
						continue;
					}

					var commandCompleteEvent = new AsyncAutoResetEvent();
					test.Status = CommandTest.StatusType.Running;
					var command = ReplaceMacros(test.Command);
					bool success = false;
					string reply = string.Empty;
					if (test.CommandType == "Mount")
					{
						switch (test.ExpectedReplyType)
						{
							case CommandTest.ReplyType.Number:
								handler.SendCommandConfirm(command, (data) =>
								{
									success = data.Success;
									reply = data.Data;
									commandCompleteEvent.Set();
								});
								break;

							case CommandTest.ReplyType.HashDelimited:
								handler.SendCommand(command, (data) =>
								{
									success = data.Success;
									reply = data.Data;
									commandCompleteEvent.Set();
								});
								break;


							case CommandTest.ReplyType.DoubleHashDelimited:
								handler.SendCommandDoubleResponse(command, (data) =>
								{
									success = data.Success;
									reply = data.Data;
									commandCompleteEvent.Set();
								});
								break;

							case CommandTest.ReplyType.None:
								handler.SendBlind(command, (data) =>
								{
									success = data.Success;
									commandCompleteEvent.Set();
								});
								break;
						}

						await commandCompleteEvent.WaitAsync();
					}
					else if (test.CommandType == "Builtin")
					{
						string pattern = @"^(\w+),(\d+)(\w{1})$";
						var match = Regex.Match(command, pattern, RegexOptions.Singleline);
						if (match.Success)
						{
							long num;
							long.TryParse(match.Groups[2].Value, out num);
							long factor = GetTimeFactorFromUnit(match.Groups[3].Value);
							switch (match.Groups[1].Value.ToUpper())
							{
								case "DELAY":
								{
									await Task.Delay(TimeSpan.FromSeconds(num * factor));
									success = true;
									reply = string.Empty;
									break;
								}
								default:
									throw new ArgumentException("Unrecognized built-in command '" + match.Groups[1].Value + "'.");
							}
						}
					}

					if (success)
					{
						// Did we get a reply?
						if (!string.IsNullOrEmpty(reply))
						{
							// Yes, so set it
							test.ReceivedReply = reply;
							// Did we expect a reply?
							if (!string.IsNullOrEmpty(test.ExpectedReply))
							{
								// Yes, so set fail or success according to match
								test.Status = (test.ExpectedReply == test.ReceivedReply) ? CommandTest.StatusType.Success : CommandTest.StatusType.Failed;
							}
							else
							{
								// No, so ignore reply and set as complete (could do Success here)
								test.Status = CommandTest.StatusType.Complete;
							}
						}
						else
						{
							// No reply. If that's expected, set complete (or success?), otherwise it's failed.
							test.Status = test.ExpectedReplyType == CommandTest.ReplyType.None ? CommandTest.StatusType.Complete : CommandTest.StatusType.Failed;
						}
					}
					else
					{
						test.Status = CommandTest.StatusType.Failed;
					}
				}
				catch (Exception ex)
				{
					debugOut($"Exception caught in Test '{test.Description}': {ex.Message}");
				}
			}
		}

		private long GetTimeFactorFromUnit(string unit)
		{
			long ret = 0;
			switch (unit.ToUpper())
			{
				case "S": ret = 1; break;
				case "M": ret = 60; break;
				case "H": ret = 3600; break;
				case "D": ret = 24 * 3600; break;
				default:
					throw new ArgumentException("Unknown unit '" + unit + "'.");
			}
			return ret;
		}

		private string ReplaceMacros(string command)
		{
			string pattern = @"(^:?\w*)\{(.*),?}(.*)$";
			RegexOptions options = RegexOptions.Multiline;

			var match = Regex.Match(command, pattern, options);

			if (match.Success)
			{
				command = match.Groups[1].Value ?? string.Empty;
				TimeSpan offset = TimeSpan.FromSeconds(0);
				var parts = match.Groups[2].Value.Split(',');
				var macro = parts[0];
				var format = parts[1];
				string macroMathPattern = @"^(\w+)([\+\-]{1})(\d+)(\w+)$";
				var mathMatch = Regex.Match(macro, macroMathPattern, options);
				if (mathMatch.Success)
				{
					var oper = mathMatch.Groups[2].Value;
					long sign = 1;
					if (oper == "-")
					{
						sign = -1;
					}

					long num;
					long.TryParse(mathMatch.Groups[3].Value, out num);
					long seconds = GetTimeFactorFromUnit(mathMatch.Groups[4].Value);
					offset = TimeSpan.FromSeconds(sign * seconds * num);
					macro = mathMatch.Groups[1].Value;
				}

				switch (macro.ToUpper())
				{
					case "TIME":
					{
						command += UseDate.Add(offset).ToString(format);
					}
					break;
					default:
					{

					}
					throw new ArgumentException("Unknown macro '" + macro + "'");
				}
				command += match.Groups[3]?.Value ?? string.Empty;
			}
			return command;
		}
	}
}

