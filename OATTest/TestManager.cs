using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using OATCommunications;
using OATCommunications.Model;

namespace OATTest
{
	public class TestSuite
	{
		public string Name { get; private set; }
		public string Description { get; private set; }
		public DateTime FixedDateTime { get; private set; }
		public List<CommandTest> Tests { get; private set; }

		public TestSuite(XElement suite)
		{
			Tests = new List<CommandTest>();
			Name = suite.Attribute("Name").Value;
			Description = suite.Attribute("Description").Value;
			FixedDateTime = DateTime.Parse(suite.Attribute("FixedDateTime")?.Value ?? "03/28/22 23:00:00");
			foreach (var testXml in suite.Elements("Test"))
			{
				var test = new CommandTest(testXml);
				Tests.Add(test);
			}
		}

		public override string ToString()
		{
			return $"{Name} - {Description}";
		}
	}

	public class TestManager
	{
		// Yeah, I know I'm mixing Models and ViewModels.... not true MVVM, more like VVM
		ObservableCollection<TestSuite> _testSuites;
		ObservableCollection<CommandTest> _tests;
		string _activeSuite = string.Empty;
		private bool _abortTestRun;
		private string _testFolder;
		AsyncAutoResetEvent _commandCompleteEvent = new AsyncAutoResetEvent();

		public TestManager()
		{
			_testSuites = new ObservableCollection<TestSuite>();
			_tests = new ObservableCollection<CommandTest>();

			LoadAllTests();
		}

		public void LoadAllTests()
		{
			string location = Assembly.GetExecutingAssembly().Location;
			_testFolder = Path.Combine(location, "Tests");

			while (!Directory.Exists(_testFolder))
			{
				location = Path.GetDirectoryName(location);
				if (location == null)
				{
					throw new ApplicationException("Installation corrupt. No Tests folder found.");
				}
				_testFolder = Path.Combine(location, "Tests");
			}
			_testSuites.Clear();
			foreach (var file in Directory.GetFiles(_testFolder, "*.xml"))
			{
				XDocument doc = XDocument.Load(file);
				var suites = doc.Element("TestSuites");
				foreach (var suite in suites.Elements("TestSuite"))
				{
					_testSuites.Add(new TestSuite(suite));
				}
			}
		}

		public long FirmwareVersion { get; set; }
		public DateTime UseDate { get; set; }
		public DateTime UseTime { get; set; }

		public ObservableCollection<CommandTest> Tests { get { return _tests; } }

		public bool AreTestsRunning { get; internal set; }
		public IList<TestSuite> TestSuites { get { return _testSuites; } }

		public void SetActiveTestSuite(string name)
		{
			_activeSuite = name;
			_tests.Clear();
			if (!string.IsNullOrEmpty(_activeSuite))
			{
				var suite = _testSuites.First(ts => ts.Name == name);
				foreach (var test in suite.Tests)
				{
					_tests.Add(test);
				}
			}
		}

		internal void ResetAllTests()
		{
			LoadAllTests();
			if (_testSuites.FirstOrDefault(ts => ts.Name == _activeSuite) != null)
			{
				SetActiveTestSuite(_activeSuite);

				foreach (var test in _tests)
				{
					test.Reset();
				}
			}
			else
			{
				_tests.Clear();
			}
		}

		public void PrepareForRun()
		{
			foreach (var test in _tests)
			{
				test.Command = ReplaceMacros(test.Command);
				test.ExpectedReply = ReplaceMacros(test.GetExpectedReply(FirmwareVersion));
			}
		}

		public async Task RunAllTests(ICommunicationHandler handler, Action<CommandTest.StatusType> testResult, Action<string> debugOut)
		{
			AreTestsRunning = true;
			foreach (var test in _tests)
			{
				if (_abortTestRun)
				{
					test.Status = CommandTest.StatusType.Skipped;
					testResult(0);
					continue;
				}

				try
				{
					if (FirmwareVersion < test.MinFirmwareVersion)
					{
						debugOut($"Skipping test '{test.Description}' because firmware too old");
						test.Status = CommandTest.StatusType.Skipped;
						testResult(CommandTest.StatusType.Skipped);
						continue;
					}
					if ((test.MaxFirmwareVersion != -1) && FirmwareVersion > test.MaxFirmwareVersion)
					{
						debugOut($"Skipping test '{test.Description}' because firmware too new");
						test.Status = CommandTest.StatusType.Skipped;
						testResult(CommandTest.StatusType.Skipped);
						continue;
					}

					debugOut($"Running test '{test.Description}'...");
					test.Status = CommandTest.StatusType.Running;
					var command = ReplaceMacros(test.Command);
					bool success = false;
					string reply = string.Empty;
					if (test.CommandType == "Mount")
					{
						switch (test.ExpectedReplyType)
						{
							case CommandTest.ReplyType.Number:
								debugOut($"Sending command '{command}', expecting number reply...");
								handler.SendCommandConfirm(command, (data) =>
								{
									success = data.Success;
									reply = data.Data;
									_commandCompleteEvent.Set();
								});
								break;

							case CommandTest.ReplyType.HashDelimited:
								debugOut($"Sending command '{command}', expecting delimited reply...");
								handler.SendCommand(command, (data) =>
								{
									success = data.Success;
									reply = data.Data;
									_commandCompleteEvent.Set();
								});
								break;


							case CommandTest.ReplyType.DoubleHashDelimited:
								debugOut($"Sending command '{command}', expecting double delimited reply...");
								handler.SendCommandDoubleResponse(command, (data) =>
								{
									success = data.Success;
									reply = data.Data;
									_commandCompleteEvent.Set();
								});
								break;

							case CommandTest.ReplyType.None:
								debugOut($"Sending command '{command}', expecting no reply...");
								handler.SendBlind(command, (data) =>
								{
									success = data.Success;
									_commandCompleteEvent.Set();
								});
								break;
						}

						await _commandCompleteEvent.WaitAsync();
					}
					else if (test.CommandType == "Builtin")
					{
						string pattern = @"^(\w+)(?:,(\d+)(\w{1,2}))*$";
						var match = Regex.Match(command, pattern, RegexOptions.Singleline);
						if (match.Success)
						{
							long num = 0;
							long factor = 1000;
							if (!string.IsNullOrEmpty(match.Groups[2].Value))
							{
								long.TryParse(match.Groups[2].Value, out num);
								factor = GetMsFactorFromUnit(match.Groups[3].Value);
							}

							switch (match.Groups[1].Value.ToUpper())
							{
								case "DELAY":
								{
									debugOut($"Executing built in 'Delay' command for {num * factor}ms ...");
									await Task.Delay(TimeSpan.FromMilliseconds(num * factor));
									success = true;
									reply = string.Empty;
									break;
								}
								case "WAITFORIDLE":
								{
									debugOut($"Executing built in 'WaitForIdle' command...");
									do
									{
										await Task.Delay(TimeSpan.FromSeconds(0.5));
										handler.SendCommandConfirm(":GIS#", (data) =>
										{
											success = data.Success;
											reply = data.Data;
											_commandCompleteEvent.Set();
										});
										await _commandCompleteEvent.WaitAsync();
										if ((reply == "0") || !success || _abortTestRun)
											break;
									}
									while (true);
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
							debugOut($"Command completed successfully. Reply was '{reply}'.");
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
							debugOut($"Command completed successfully. No reply was received.");
							// No reply. If that's expected, set complete (or success?), otherwise it's failed.
							test.Status = test.ExpectedReplyType == CommandTest.ReplyType.None ? CommandTest.StatusType.Complete : CommandTest.StatusType.Failed;
						}
					}
					else
					{
						debugOut($"Command failed.");
						test.Status = CommandTest.StatusType.Failed;
					}
					testResult(test.Status);
				}
				catch (Exception ex)
				{
					debugOut($"Exception caught in Test '{test.Description}': {ex.Message}");
				}
			}
			AreTestsRunning = false;
		}

		internal void AbortRun()
		{
			_abortTestRun = true;
		}

		private long GetMsFactorFromUnit(string unit)
		{
			long ret;
			switch (unit.ToUpper())
			{
				case "MS": ret = 1; break;
				case "S": ret = 1000; break;
				case "M": ret = 60 * 1000; break;
				case "H": ret = 60 * 60 * 1000; break;
				case "D": ret = 24 * 60 * 60 * 1000; break;
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
					long milliSeconds = GetMsFactorFromUnit(mathMatch.Groups[4].Value);
					offset = TimeSpan.FromMilliseconds(sign * milliSeconds * num);
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

