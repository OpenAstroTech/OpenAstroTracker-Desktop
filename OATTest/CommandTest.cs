using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace OATTest
{
	public class CommandTest
	{
		public enum StatusType
		{
			Ready,
			Running,
			Failed,
			Succees,
			Complete
		};

		public enum ReplyType
		{
			None,
			Number,
			HashDelimited,
			DoubleHashDelimited
		};

		string _command;
		string _expected;
		string _received;
		string _label;
		StatusType _status;
		ReplyType _replyType;

		long _minVersion;
		long _maxVersion;

		public CommandTest(XElement testElem)
		{
			_command = testElem.Element("Command").Value;
			var reply = testElem.Element("ExpectedReply")?.Attribute("Type").Value ?? "None";
			if (reply == "n") reply = "Number";
			if (reply == "#") reply = "HashDelimited";
			if (reply == "##") reply = "DoubleHashDelimited";
			_replyType = (ReplyType)Enum.Parse(typeof(ReplyType), reply);
			_expected = testElem.Element("ExpectedReply")?.Value ?? string.Empty;
			_label = testElem.Attribute("Description").Value;
			_status = StatusType.Ready;
			_minVersion = long.Parse(testElem.Element("Command").Attribute("MinFirmware")?.Value ?? "-1");
			_maxVersion = long.Parse(testElem.Element("Command").Attribute("MaxFirmware")?.Value ?? "-1");
		}

		internal void Reset()
		{
			_status = StatusType.Ready;
			_received = string.Empty;
		}

		public StatusType Status { get { return _status; } set { _status = value; } }
		public string Command { get { return _command; } }
		public string Description { get { return _label; } }
		public string ExpectedReply { get { return _expected; } }
		public string ReceivedReply { get { return _received; } }

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

		public bool IsReplyExpected { get { return !string.IsNullOrEmpty(_expected); } }
	}
}
