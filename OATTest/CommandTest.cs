using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using OATCommunications.WPF;

namespace OATTest
{
	public class CommandTest : NotifyPropertyChanged
	{
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

		string _command = string.Empty;
		string _expected = string.Empty;
		string _received = string.Empty;
		string _label = string.Empty;
		StatusType _status;
		ReplyType _replyType;

		long _minVersion;
		long _maxVersion;
		private string _commandType;

		public CommandTest(XElement testElem)
		{
			_command = testElem.Element("Command").Value;
			_commandType = testElem.Element("Command").Attribute("Type")?.Value ?? "Mount";
			var reply = testElem.Element("ExpectedReply")?.Attribute("Type").Value ?? "None";
			if (reply == "n") reply = "Number";
			if (reply == "#") reply = "HashDelimited";
			if (reply == "##") reply = "DoubleHashDelimited";
			_replyType = (ReplyType)Enum.Parse(typeof(ReplyType), reply);
			_expected = testElem.Element("ExpectedReply")?.Value ?? string.Empty;
			_label = testElem.Attribute("Description").Value;
			_status = StatusType.Ready;
			_minVersion = long.Parse(testElem.Attribute("MinFirmware")?.Value ?? "-1");
			_maxVersion = long.Parse(testElem.Attribute("MaxFirmware")?.Value ?? "-1");
		}

		internal void Reset()
		{
			Status = StatusType.Ready;
			ReceivedReply = string.Empty;
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

		public ReplyType ExpectedReplyType { get { return _replyType; } }
	}
}
