using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
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
        public string SetupWarning { get; internal set; }

        public TestSuite(TestManager manager, XElement suite)
        {
            Tests = new List<CommandTest>();
            Name = suite.Attribute("Name")!.Value;
            Description = suite.Attribute("Description")!.Value;
            FixedDateTime = DateTime.Parse(suite.Attribute("FixedDateTime")?.Value ?? "03/28/22 23:00:00", System.Globalization.CultureInfo.InvariantCulture);
            SetupWarning = suite.Attribute("SetupWarning")?.Value ?? string.Empty;
            foreach (var testXml in suite.Elements("Test"))
            {
                if (testXml.Attribute("IncludedName") != null)
                {
                    var incSuite = manager.TestSuites.FirstOrDefault(ts => ts.Name == testXml.Attribute("IncludedName")!.Value);
                    incSuite?.Tests.ForEach(tst => Tests.Add(tst));
                }
                else
                {
                    Tests.Add(new CommandTest(testXml));
                }
            }
        }

        public override string ToString() => $"{Name} - {Description}";
    }

    public class TestManager
    {
        ObservableCollection<TestSuite> _testSuites;
        ObservableCollection<CommandTest> _tests;
        Dictionary<string, string> _variables;
        HashSet<string> _loadedFilenames;
        string _activeSuite = string.Empty;
        private bool _abortTestRun;
        private string _testFolder = string.Empty;
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
            var suites = doc.Element("TestSuites")!;
            foreach (var include in suites.Elements("IncludeSuite"))
            {
                var filename = Path.Combine(_testFolder, include.Attribute("Filename")!.Value);
                if (File.Exists(filename) && !_loadedFilenames.Contains(filename))
                {
                    _loadedFilenames.Add(filename);
                    LoadTestSuitesFile(filename);
                }
            }
            foreach (var suite in suites.Elements("TestSuite"))
                _testSuites.Add(new TestSuite(this, suite));
        }

        public void LoadAllTests()
        {
            string location = Assembly.GetExecutingAssembly().Location;
            _testFolder = Path.Combine(location, "Tests");
            while (!Directory.Exists(_testFolder))
            {
                location = Path.GetDirectoryName(location)!;
                if (location == null)
                    throw new ApplicationException("Installation corrupt. No Tests folder found.");
                _testFolder = Path.Combine(location, "Tests");
            }
            _testSuites.Clear();
            _loadedFilenames.Clear();
            foreach (var file in Directory.GetFiles(_testFolder, "*.xml"))
                LoadTestSuitesFile(file);
        }

        public long FirmwareVersion { get; set; }
        public DateTime UseDate { get; set; }
        public DateTime UseTime { get; set; }
        public ObservableCollection<CommandTest> Tests => _tests;
        public bool AreTestsRunning { get; internal set; }
        public IList<TestSuite> TestSuites => _testSuites;
        public bool StopOnError { get; internal set; }

        public void SetActiveTestSuite(string name)
        {
            _activeSuite = name;
            _tests.Clear();
            if (!string.IsNullOrEmpty(_activeSuite))
            {
                var suite = _testSuites.First(ts => ts.Name == name);
                foreach (var test in suite.Tests)
                    _tests.Add(test);
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
                    test.Reset();
            }
            else
            {
                _tests.Clear();
            }
        }

        public void PrepareForRun()
        {
            _abortTestRun = false;
            foreach (var test in _tests.ToList())
            {
                test.Command = ReplaceMacros(test.Command);
                test.ExpectedReply = ReplaceMacros(test.GetExpectedReply(FirmwareVersion));
            }
        }

        // confirmAction: shown when suite has a SetupWarning; returns true to continue
        public async Task RunAllTests(
            ICommunicationHandler handler,
            Func<CommandTest, CommandTest.StatusType, Task<bool>> testResult,
            Action<string> debugOut,
            Func<string, Task<bool>> confirmAction)
        {
            var suite = _testSuites.FirstOrDefault(ts => ts.Name == _activeSuite);
            if (suite != null && !string.IsNullOrEmpty(suite.SetupWarning))
            {
                bool proceed = await confirmAction(suite.SetupWarning + "\n\nDo you want to continue with the test?");
                if (!proceed)
                {
                    debugOut("TEST: User cancelled test run.");
                    return;
                }
            }

            AreTestsRunning = true;
            var testsToRun = _tests.ToList();
            foreach (var test in testsToRun)
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
                    if (test.MaxFirmwareVersion != -1 && FirmwareVersion > test.MaxFirmwareVersion)
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
                                handler.SendCommandConfirm(command, data => { success = data.Success; reply = data.Data; _commandCompleteEvent.Set(); });
                                break;
                            case CommandTest.ReplyType.HashDelimited:
                                handler.SendCommand(command, data => { success = data.Success; reply = data.Data; _commandCompleteEvent.Set(); });
                                break;
                            case CommandTest.ReplyType.DoubleHashDelimited:
                                handler.SendCommandDoubleResponse(command, data => { success = data.Success; reply = data.Data; _commandCompleteEvent.Set(); });
                                break;
                            case CommandTest.ReplyType.None:
                                handler.SendBlind(command, data => { success = data.Success; _commandCompleteEvent.Set(); });
                                break;
                        }
                        await _commandCompleteEvent.WaitAsync();
                    }
                    else if (test.CommandType == "Builtin")
                    {
                        int commaPos = command.IndexOf(',');
                        string verb = commaPos == -1 ? command.ToUpper() : command.Substring(0, commaPos).ToUpper();
                        string arguments = commaPos == -1 ? string.Empty : command.Substring(commaPos + 1);
                        switch (verb)
                        {
                            case "DELAY":
                                var match = Regex.Match(arguments, @"(\d+)(\w{1,2})", RegexOptions.Singleline);
                                if (match.Success)
                                {
                                    long.TryParse(match.Groups[1].Value, out long num);
                                    long factor = GetMsFactorFromUnit(match.Groups[2].Value);
                                    await Task.Delay(TimeSpan.FromMilliseconds(num * factor));
                                    success = true;
                                }
                                break;

                            case "WAITFORSLEWEND":
                                do
                                {
                                    await Task.Delay(TimeSpan.FromSeconds(0.5));
                                    handler.SendCommandConfirm(":GIS#", data => { success = data.Success; reply = data.Data; _commandCompleteEvent.Set(); });
                                    await _commandCompleteEvent.WaitAsync();
                                    if (reply == "0" || !success || _abortTestRun) break;
                                } while (true);
                                if (!success)
                                {
                                    handler.SendBlind(":RS#", data => { success = data.Success; _commandCompleteEvent.Set(); });
                                    await _commandCompleteEvent.WaitAsync();
                                }
                                reply = string.Empty;
                                break;

                            case "WAITFORSTATUS":
                                do
                                {
                                    await Task.Delay(TimeSpan.FromSeconds(0.5));
                                    handler.SendCommand(":GX#,#", data => { success = data.Success; reply = data.Data; _commandCompleteEvent.Set(); });
                                    await _commandCompleteEvent.WaitAsync();
                                    string status = reply.Split(',')[0];
                                    if (status.ToUpper() == arguments.ToUpper() || !success || _abortTestRun) break;
                                } while (true);
                                if (!success)
                                {
                                    handler.SendBlind(":RS#", data => { success = data.Success; _commandCompleteEvent.Set(); });
                                    await _commandCompleteEvent.WaitAsync();
                                }
                                reply = string.Empty;
                                break;

                            default:
                                throw new ArgumentException("Unrecognized built-in command '" + verb + "'.");
                        }
                    }

                    if (success)
                    {
                        if (!string.IsNullOrEmpty(reply))
                        {
                            test.ReceivedReply = reply;
                            if (!string.IsNullOrEmpty(test.ExpectedReply))
                            {
                                var expected = ReplaceMacros(test.ExpectedReply);
                                test.Status = test.IsReceivedReplyEqualToExpectedReply(reply, expected)
                                    ? CommandTest.StatusType.Success
                                    : CommandTest.StatusType.Failed;
                            }
                            else
                            {
                                test.Status = CommandTest.StatusType.Complete;
                            }
                            foreach (var varName in test.StoreAs)
                                _variables[varName] = reply;
                        }
                        else
                        {
                            test.Status = test.ExpectedReplyType == CommandTest.ReplyType.None
                                ? CommandTest.StatusType.Complete
                                : CommandTest.StatusType.Failed;
                        }
                    }
                    else
                    {
                        test.Status = CommandTest.StatusType.Failed;
                    }

                    if (!await testResult(test, test.Status))
                        _abortTestRun = true;
                }
                catch (Exception ex)
                {
                    debugOut($"TEST: Exception caught in Test '{test.Description}': {ex.Message}");
                }
            }
            AreTestsRunning = false;
        }

        internal void AbortRun() => _abortTestRun = true;

        private long GetMsFactorFromUnit(string unit) => unit.ToUpper() switch
        {
            "MS" => 1,
            "S" => 1000,
            "M" => 60 * 1000,
            "H" => 60 * 60 * 1000,
            "D" => 24 * 60 * 60 * 1000,
            _ => throw new ArgumentException("Unknown unit '" + unit + "'.")
        };

        private string ReplaceMacros(string command)
        {
            string pattern = @"(^:?\w*)\{(.*?)}(.*)$";
            RegexOptions options = RegexOptions.Multiline;
            Match match;
            string initialCommand = command;
            while ((match = Regex.Match(command, pattern, options)).Success)
            {
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
                                var mathMatch = Regex.Match(math, @"^([\+\-]{1})(\d+)(\w+)$", options);
                                if (mathMatch.Success)
                                {
                                    long sign = mathMatch.Groups[1].Value == "-" ? -1 : 1;
                                    long.TryParse(mathMatch.Groups[2].Value, out long num);
                                    long ms = GetMsFactorFromUnit(mathMatch.Groups[3].Value);
                                    offset = TimeSpan.FromMilliseconds(sign * ms * num);
                                }
                            }
                            command += UseDate.Add(offset).ToString(format);
                        }
                        break;
                    case "VAR":
                        command += _variables.TryGetValue(parts[1], out string? varValue)
                            ? varValue
                            : '{' + match.Groups[2].Value + '}';
                        break;
                    case "CALC":
                        try { command += ExpressionEvaluator.Evaluate(parts[1], vname => _variables[vname]).ToString(); }
                        catch { command += '{' + match.Groups[2].Value + '}'; }
                        break;
                    default:
                        throw new ArgumentException("Unknown macro '" + macro + "'");
                }
                command += match.Groups[3]?.Value ?? string.Empty;
                if (command == initialCommand) break;
            }
            return command;
        }
    }
}
