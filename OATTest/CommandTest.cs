using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using OATCommunications.Model;
using OATCommunications.WPF;

namespace OATTest
{
	public class CommandTest : NotifyPropertyChanged
	{
		// We compare DEC slew results with an accuracy of plusminus 8 arcsecs
		public static double ArcSecondResolution = 8.0 / 60.0 / 60.0;
		public static double TwoSeconds = 2.0 / 60.0 / 60.0;

		public enum StatusType
		{
			Ready,
			Running,
			Failed,
			Success,
			Skipped,
			Complete
		};

		public enum ReplyType
		{
			None,
			Number,
			HashDelimited,
			DoubleHashDelimited
		};

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
				var reply = node.Attribute("Type").Value ?? "None";
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

			public string Expected { get { return _expected; } }
			public ReplyType ReplyType { get { return _replyType; } }
			public long MinVersion { get { return _minVersion; } }
			public long MaxVersion { get { return _maxVersion; } }
			public string StoreAs { get { return _storeAs; } }
			public string FuzzyType { get { return _fuzzyType; } }
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
			_command = testElem.Element("Command").Value;
			_commandType = testElem.Element("Command").Attribute("Type")?.Value ?? "Mount";
			var reply = testElem.Elements("ExpectedReply");
			if (reply.Any())
			{
				_replies.AddRange(reply.Select(n => new Reply(n)));
			}
			_expected = string.Empty;
			_label = testElem.Attribute("Description").Value;
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
			get { return _status; }
			set { SetProperty(ref _status, value, "Status"); }
		}

		public string CommandType { get { return _commandType; } }
		public string Command
		{
			get { return _command; }
			set { SetProperty(ref _command, value, "Command"); }
		}

		public string Description { get { return _label; } }

		internal string GetExpectedReply(long version)
		{
			// If we only have one, return it
			if (_replies.Count == 1)
			{
				return _replies[0].Expected;
			}

			// If we have multiple, find the first one that can run on the given firmware
			foreach (var reply in _replies)
			{
				bool canRun = true;
				if (reply.MinVersion != -1)
				{
					if (version < reply.MinVersion)
					{
						canRun = false;
					}
				}
				if (reply.MaxVersion != -1)
				{
					if (version > reply.MaxVersion)
					{
						canRun = false;
					}
				}
				if (canRun)
				{
					return reply.Expected;
				}
			}
			return string.Empty;
		}

		public string ExpectedReply
		{
			get { return _expected; }
			set { SetProperty(ref _expected, value, "ExpectedReply"); }
		}

		public string ReceivedReply
		{
			get { return _received; }
			set { SetProperty(ref _received, value, "ReceivedReply"); }
		}

		public bool RunOnFirmware(long version)
		{
			bool run = true;
			if (_minVersion != -1)
			{
				if (version < _minVersion)
				{
					run = false;
				}
			}
			if (_maxVersion != -1)
			{
				if (version > _maxVersion)
				{
					run = false;
				}
			}
			return run;
		}

		public long MinFirmwareVersion { get { return _minVersion; } }
		public long MaxFirmwareVersion { get { return _maxVersion; } }

		public ReplyType ExpectedReplyType { get { return _replies.FirstOrDefault()?.ReplyType ?? ReplyType.None; } }

		public IEnumerable<string> StoreAs
		{
			get
			{
				foreach (var reply in _replies)
				{
					if (!string.IsNullOrWhiteSpace(reply.StoreAs))
					{
						yield return reply.StoreAs;
					}
				}

			}
		}

		internal bool IsReceivedReplyEqualToExpectedReply(string reply, string expected)
		{
			string fuzzyCompare = (_replies.FirstOrDefault()?.FuzzyType ?? "").ToLower();
			if (string.IsNullOrEmpty(fuzzyCompare))
			{
				return string.Equals(reply, expected);
			}
			if (fuzzyCompare == "degrees")
			{
				if (Parsers.TryParseDec(reply, out double replyDec) && Parsers.TryParseDec(expected, out double expectedDec))
				{
					return Math.Abs(replyDec - expectedDec) < ArcSecondResolution;
				}
			}
			else if (fuzzyCompare.StartsWith("time"))
			{
				if (Parsers.TryParseRA(reply, out double replyRa) && Parsers.TryParseRA(expected, out double expectedRa))
				{
					return Math.Abs(replyRa - expectedRa) < TwoSeconds;
				}
			}


			return false;
		}
	}
}
