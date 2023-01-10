using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
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
		public string SetupWarning { get; internal set; }

		public TestSuite(TestManager manager, XElement suite)
		{
			Tests = new List<CommandTest>();
			Name = suite.Attribute("Name").Value;
			Description = suite.Attribute("Description").Value;
			FixedDateTime = DateTime.Parse(suite.Attribute("FixedDateTime")?.Value ?? "03/28/22 23:00:00");
			SetupWarning = suite.Attribute("SetupWarning")?.Value ?? string.Empty;
			foreach (var testXml in suite.Elements("Test"))
			{
				if (testXml.Attribute("IncludedName") != null)
				{
					var incSuite = manager.TestSuites.FirstOrDefault(ts => ts.Name == testXml.Attribute("IncludedName").Value);
					incSuite.Tests.ForEach(tst => Tests.Add(tst));
				}
				else
				{
					var test = new CommandTest(testXml);
					Tests.Add(test);
				}
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
		Dictionary<string, string> _variables;
		HashSet<string> _loadedFilenames;
		string _activeSuite = string.Empty;
		private bool _abortTestRun;
		private string _testFolder;
		AsyncAutoResetEvent _commandCompleteEvent = new AsyncAutoResetEvent();

		public TestManager()
		{
			_testSuites = new ObservableCollection<TestSuite>();
			_tests = new ObservableCollection<CommandTest>();
			_variables = new Dictionary<string, string>();
			_loadedFilenames = new HashSet<string>();

			LoadAllTests();
		}

		void LoadTestSuitesFile(string file)
		{
			XDocument doc = XDocument.Load(file);
			var suites = doc.Element("TestSuites");
			foreach (var include in suites.Elements("IncludeSuite"))
			{
				var filename = Path.Combine(_testFolder, include.Attribute("Filename").Value);
				if (File.Exists(filename))
				{
					if (!_loadedFilenames.Contains(filename))
					{
						_loadedFilenames.Add(filename);
						LoadTestSuitesFile(filename);
					}
				}
			}
			foreach (var suite in suites.Elements("TestSuite"))
			{
				_testSuites.Add(new TestSuite(this, suite));
			}
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
			_loadedFilenames.Clear();
			foreach (var file in Directory.GetFiles(_testFolder, "*.xml"))
			{
				LoadTestSuitesFile(file);
			}
		}

		public long FirmwareVersion { get; set; }
		public DateTime UseDate { get; set; }
		public DateTime UseTime { get; set; }

		public ObservableCollection<CommandTest> Tests { get { return _tests; } }

		public bool AreTestsRunning { get; internal set; }
		public IList<TestSuite> TestSuites { get { return _testSuites; } }

		public bool StopOnError { get; internal set; }

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
			_variables.Clear();
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
			_abortTestRun = false;
			foreach (var test in _tests)
			{
				test.Command = ReplaceMacros(test.Command);
				test.ExpectedReply = ReplaceMacros(test.GetExpectedReply(FirmwareVersion));
			}
		}

		public async Task RunAllTests(ICommunicationHandler handler, Func<CommandTest, CommandTest.StatusType, Task<bool>> testResult, Action<string> debugOut)
		{
			var suite = _testSuites.FirstOrDefault(ts => ts.Name == _activeSuite);
			if (!string.IsNullOrEmpty(suite.SetupWarning))
			{
				var dialogResult = MessageBox.Show(suite.SetupWarning + "\n\nDo you want to continue with the test?", "Pre-run warning", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
				if (dialogResult == MessageBoxResult.No)
				{
					debugOut("TEST: User cancelled test run.");
					return;
				}
			}
			AreTestsRunning = true;
			foreach (var test in _tests)
			{
				if (_abortTestRun)
				{
					test.Status = CommandTest.StatusType.Skipped;
					await testResult(test, CommandTest.StatusType.Skipped);
					continue;
				}

				try
				{
					if (FirmwareVersion < test.MinFirmwareVersion)
					{
						debugOut($"TEST: Skipping test '{test.Description}' because firmware too old");
						test.Status = CommandTest.StatusType.Skipped;
						await testResult(test, CommandTest.StatusType.Skipped);
						continue;
					}
					if ((test.MaxFirmwareVersion != -1) && FirmwareVersion > test.MaxFirmwareVersion)
					{
						debugOut($"TEST: Skipping test '{test.Description}' because firmware too new");
						test.Status = CommandTest.StatusType.Skipped;
						await testResult(test, CommandTest.StatusType.Skipped);
						continue;
					}

					debugOut($"TEST: Running test '{test.Description}'...");
					test.Status = CommandTest.StatusType.Running;
					await testResult(test, CommandTest.StatusType.Running);
					var command = ReplaceMacros(test.Command);
					bool success = false;
					string reply = string.Empty;
					if (test.CommandType == "Mount")
					{
						switch (test.ExpectedReplyType)
						{
							case CommandTest.ReplyType.Number:
								debugOut($"TEST: Sending command '{command}', expecting number reply...");
								handler.SendCommandConfirm(command, (data) =>
								{
									success = data.Success;
									reply = data.Data;
									_commandCompleteEvent.Set();
								});
								break;

							case CommandTest.ReplyType.HashDelimited:
								debugOut($"TEST: Sending command '{command}', expecting delimited reply...");
								handler.SendCommand(command, (data) =>
								{
									success = data.Success;
									reply = data.Data;
									_commandCompleteEvent.Set();
								});
								break;


							case CommandTest.ReplyType.DoubleHashDelimited:
								debugOut($"TEST: Sending command '{command}', expecting double delimited reply...");
								handler.SendCommandDoubleResponse(command, (data) =>
								{
									success = data.Success;
									reply = data.Data;
									_commandCompleteEvent.Set();
								});
								break;

							case CommandTest.ReplyType.None:
								debugOut($"TEST: Sending command '{command}', expecting no reply...");
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
										debugOut($"TEST: Executing built in 'Delay' command for {num * factor}ms ...");
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
											debugOut($"TEST: Send GIS");
											handler.SendCommandConfirm(":GIS#", (data) =>
											{
												debugOut($"TEST: GIS returned {data.Success} and {data.Data}");
												success = data.Success;
												reply = data.Data;
												_commandCompleteEvent.Set();
											});
											await _commandCompleteEvent.WaitAsync();
											if ((reply == "0") || !success || _abortTestRun)
											{
												if (reply == "0") debugOut($"TEST: GIS loop terminating since OAT is idle.");
												else if (!success) debugOut($"TEST: GIS loop terminating since GIS failed.");
												else if (_abortTestRun) debugOut($"TEST: GIS loop terminating since Test run aborted.");
												break;
											}
										}
										while (true);

										// If comms failed, try send a blind command to clear buffers.
										if (!success)
										{
											debugOut($"TEST: GIS loop failed, sending Blind command.");
											handler.SendBlind(":RS#", (data) =>
											{
												success = data.Success;
												_commandCompleteEvent.Set();
											});
											await _commandCompleteEvent.WaitAsync();
											debugOut($"TEST: Blind command complete.");
										}
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
							debugOut($"TEST: Command completed successfully. Reply was '{reply}'.");
							// Yes, so set it
							test.ReceivedReply = reply;
							// Did we expect a reply?
							if (!string.IsNullOrEmpty(test.ExpectedReply))
							{
								var expected = ReplaceMacros(test.ExpectedReply);
								// Yes, so set fail or success according to match
								
								test.Status = test.IsReceivedReplyEqualToExpectedReply(reply,expected) ? CommandTest.StatusType.Success : CommandTest.StatusType.Failed;
							}
							else
							{
								// No, so ignore reply and set as complete (could do Success here)
								test.Status = CommandTest.StatusType.Complete;
							}
							// Did we need to hold onto this reply?
							foreach (var varName in test.StoreAs)
							{
								_variables.Add(varName, reply);
							}
						}
						else
						{
							debugOut($"TEST: Command completed successfully. No reply was received.");
							// No reply. If that's expected, set complete (or success?), otherwise it's failed.
							test.Status = test.ExpectedReplyType == CommandTest.ReplyType.None ? CommandTest.StatusType.Complete : CommandTest.StatusType.Failed;
						}
					}
					else
					{
						debugOut($"TEST: Command failed.");
						test.Status = CommandTest.StatusType.Failed;
					}

					if (!await testResult(test, test.Status))
					{
						_abortTestRun = true;
					}
				}
				catch (Exception ex)
				{
					debugOut($"TEST: Exception caught in Test '{test.Description}': {ex.Message}");
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
			// Look for opening curly, macro (maybe with math) and comma and more
			string pattern = @"(^:?\w*)\{(.*?)}(.*)$";
			RegexOptions options = RegexOptions.Multiline;

			Match match;
			string initialCommand = command;
			while ((match = Regex.Match(command, pattern, options)).Success)
			{
				// Pre macro text
				command = match.Groups[1].Value ?? string.Empty;

				var parts = match.Groups[2].Value.Split(',');
				var macro = parts[0];
				switch (macro.ToUpper())
				{
					case "TIME":
						{
							TimeSpan offset = TimeSpan.FromSeconds(0);
							var format = parts[1];
							if (parts.Length == 3)
							{
								var math = parts[1];
								format = parts[2];
								string macroMathPattern = @"^([\+\-]{1})(\d+)(\w+)$";
								var mathMatch = Regex.Match(math, macroMathPattern, options);
								if (mathMatch.Success)
								{
									var oper = mathMatch.Groups[1].Value;
									long sign = 1;
									if (oper == "-")
									{
										sign = -1;
									}

									long num;
									long.TryParse(mathMatch.Groups[2].Value, out num);
									long milliSeconds = GetMsFactorFromUnit(mathMatch.Groups[3].Value);
									offset = TimeSpan.FromMilliseconds(sign * milliSeconds * num);
								}
							}
							command += UseDate.Add(offset).ToString(format);
						}
						break;

					case "VAR":
						{
							if (_variables.TryGetValue(parts[1], out string varValue))
							{
								command += varValue;
							}
							else
							{
								command += '{' + match.Groups[2].Value + '}';
							}
						}
						break;

					case "CALC":
						{
							string expr = parts[1];

							try
							{
								var result = ExpressionEvaluator.Evaluate(parts[1], (vname) => _variables[vname]);
								command += result.ToString();
							}
							catch
							{
								command += '{' + match.Groups[2].Value + '}';
							}
						}
						break;

					default:
						{

						}
						throw new ArgumentException("Unknown macro '" + macro + "'");
				}
				// Post macro text
				command += match.Groups[3]?.Value ?? string.Empty;

				if (command == initialCommand)
					break;
			}
			return command;
		}
	}
}

