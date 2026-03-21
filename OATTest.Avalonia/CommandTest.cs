using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using OATCommunications.Model;
using OATCommunications.Avalonia;

namespace OATTest
{
    public class CommandTest : NotifyPropertyChanged
    {
        public static double ArcSecondResolution = 8.0 / 60.0 / 60.0;
        public static double TwoSeconds = 2.0 / 60.0 / 60.0;

        public enum StatusType { Ready, Running, Failed, Success, Skipped, Complete }
        public enum ReplyType { None, Number, HashDelimited, DoubleHashDelimited }

        private class Reply
        {
            string _expected = string.Empty;
            string _storeAs = string.Empty;
            string _fuzzyType = string.Empty;
            long _minVersion = -1;
            long _maxVersion = -1;
            ReplyType _replyType;

            public Reply(XElement node)
            {
                var reply = node.Attribute("Type")!.Value ?? "None";
                if (reply == "n") reply = "Number";
                if (reply == "#") reply = "HashDelimited";
                if (reply == "##") reply = "DoubleHashDelimited";
                _replyType = (ReplyType)Enum.Parse(typeof(ReplyType), reply);
                _expected = node.Value ?? string.Empty;
                _minVersion = long.Parse(node.Attribute("MinFirmware")?.Value ?? "-1");
                _maxVersion = long.Parse(node.Attribute("MaxFirmware")?.Value ?? "-1");
                _storeAs = node.Attribute("StoreAs")?.Value ?? string.Empty;
                _fuzzyType = node.Attribute("Fuzzy")?.Value ?? string.Empty;
            }

            public string Expected => _expected;
            public ReplyType ReplyType => _replyType;
            public long MinVersion => _minVersion;
            public long MaxVersion => _maxVersion;
            public string StoreAs => _storeAs;
            public string FuzzyType => _fuzzyType;
        }

        string _command = string.Empty;
        string _expected = string.Empty;
        string _received = string.Empty;
        string _label = string.Empty;
        StatusType _status;
        List<Reply> _replies;
        long _minVersion;
        long _maxVersion;
        private string _commandType;

        public CommandTest(XElement testElem)
        {
            _replies = new List<Reply>();
            _command = testElem.Element("Command")!.Value;
            _commandType = testElem.Element("Command")!.Attribute("Type")?.Value ?? "Mount";
            var reply = testElem.Elements("ExpectedReply");
            if (reply.Any())
                _replies.AddRange(reply.Select(n => new Reply(n)));
            _expected = string.Empty;
            _label = testElem.Attribute("Description")!.Value;
            _status = StatusType.Ready;
            _minVersion = long.Parse(testElem.Attribute("MinFirmware")?.Value ?? "-1");
            _maxVersion = long.Parse(testElem.Attribute("MaxFirmware")?.Value ?? "-1");
        }

        internal void Reset()
        {
            Status = StatusType.Ready;
            ReceivedReply = string.Empty;
            ExpectedReply = string.Empty;
        }

        public StatusType Status
        {
            get => _status;
            set => SetProperty(ref _status, value, "Status");
        }

        public string CommandType => _commandType;

        public string Command
        {
            get => _command;
            set => SetProperty(ref _command, value, "Command");
        }

        public string Description => _label;

        internal string GetExpectedReply(long version)
        {
            if (_replies.Count == 1) return _replies[0].Expected;
            foreach (var r in _replies)
            {
                bool canRun = true;
                if (r.MinVersion != -1 && version < r.MinVersion) canRun = false;
                if (r.MaxVersion != -1 && version > r.MaxVersion) canRun = false;
                if (canRun) return r.Expected;
            }
            return string.Empty;
        }

        public string ExpectedReply
        {
            get => _expected;
            set => SetProperty(ref _expected, value, "ExpectedReply");
        }

        public string ReceivedReply
        {
            get => _received;
            set => SetProperty(ref _received, value, "ReceivedReply");
        }

        public bool RunOnFirmware(long version)
        {
            if (_minVersion != -1 && version < _minVersion) return false;
            if (_maxVersion != -1 && version > _maxVersion) return false;
            return true;
        }

        public long MinFirmwareVersion => _minVersion;
        public long MaxFirmwareVersion => _maxVersion;
        public ReplyType ExpectedReplyType => _replies.FirstOrDefault()?.ReplyType ?? ReplyType.None;

        public IEnumerable<string> StoreAs
        {
            get
            {
                foreach (var r in _replies)
                    if (!string.IsNullOrWhiteSpace(r.StoreAs))
                        yield return r.StoreAs;
            }
        }

        internal bool IsReceivedReplyEqualToExpectedReply(string reply, string expected)
        {
            string fuzzyCompare = (_replies.FirstOrDefault()?.FuzzyType ?? "").ToLower();
            if (string.IsNullOrEmpty(fuzzyCompare))
                return string.Equals(reply, expected);
            if (fuzzyCompare == "degrees")
            {
                if (Parsers.TryParseDec(reply, out double replyDec) && Parsers.TryParseDec(expected, out double expectedDec))
                    return Math.Abs(replyDec - expectedDec) < ArcSecondResolution;
            }
            else if (fuzzyCompare.StartsWith("time"))
            {
                if (Parsers.TryParseRA(reply, out double replyRa) && Parsers.TryParseRA(expected, out double expectedRa))
                    return Math.Abs(replyRa - expectedRa) < TwoSeconds;
            }
            return false;
        }
    }
}
