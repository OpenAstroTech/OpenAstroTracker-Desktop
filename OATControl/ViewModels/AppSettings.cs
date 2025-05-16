using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Windows;
using System.IO;
using System.Reflection;
using OATControl;

namespace OATControl.ViewModels
{
	public enum ChecklistShowOn { OnStartup, OnConnect, OnDemand };
	public static class ChecklistShowOnEnumHelper
	{
		public static Array ChecklistShowOnValues => Enum.GetValues(typeof(ChecklistShowOn));
	}

	public class UpgradeEventArgs : EventArgs
	{
		public UpgradeEventArgs(long loaded, long current)
		{
			LoadedVersion = loaded;
			CurrentVersion = current;
		}

		public long LoadedVersion { get; }
		public long CurrentVersion { get; }
	}

	public class AppSettings
	{
		class DefaultValueAttribute : Attribute
		{
			public DefaultValueAttribute(string val)
			{
				DefaultValue = val;
			}
			public string DefaultValue { get; private set; }
		}

		public EventHandler<UpgradeEventArgs> UpgradeVersion;
		private static AppSettings _instance;
		private Dictionary<string, string> _dict = new Dictionary<string, string>();
		private string _settingsLocation;

		AppSettings()
		{
			var entry = Assembly.GetExecutingAssembly().GetName().Version;
			CurrentVersion = ((entry.Major * 100 + entry.Minor) * 100 + entry.Build) * 100 + entry.Revision;
			_settingsLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OpenAstroTracker", "OATControl.Settings");
		}

		public static AppSettings Instance
		{
			get { return _instance == null ? (_instance = new AppSettings()) : _instance; }
		}

		public void Load()
		{
			if (File.Exists(_settingsLocation))
			{
				var doc = XDocument.Load(_settingsLocation, LoadOptions.PreserveWhitespace);
				LoadedVersion = long.Parse(doc.Element("Settings").Attribute("Version").Value);
				foreach (var setting in doc.Element("Settings").Elements("Setting"))
				{
					var key = setting.Attribute("Key").Value;
					_dict[key] = setting.Attribute("Value").Value;
				}

				if (LoadedVersion != CurrentVersion)
				{
					// This will execute on every new release. Allows clients to upgrade settings if needed.
					OnUpgradeSettings();
					Save();
				}
			}
		}

		void OnUpgradeSettings()
		{
			var handler = UpgradeVersion;
			if (handler != null)
			{
				handler(this, new UpgradeEventArgs(LoadedVersion, CurrentVersion));
			}
		}

		public void Save()
		{
			var doc = new XDocument(new XElement("Settings", new XAttribute("Version", CurrentVersion), _dict.Select(kv => new XElement("Setting", new XAttribute("Key", kv.Key), new XAttribute("Value", kv.Value.ToString())))));
			doc.Save(_settingsLocation);
		}

		public long CurrentVersion { get; private set; }
		public long LoadedVersion { get; private set; }

		private Point ToPoint(string val)
		{
			var parts = val.Split('|');
			return new Point(int.Parse(parts[0]), int.Parse(parts[1]));
		}

		private string FromPoint(Point val)
		{
			return $"{val.X}|{val.Y}";

		}

		private Size ToSize(string val)
		{
			var parts = val.Split('|');
			return new Size(int.Parse(parts[0]), int.Parse(parts[1]));
		}

		private string FromSize(Size val)
		{
			return $"{val.Width}|{val.Height}";

		}

		private string this[string key]
		{
			get
			{
				if (_dict.TryGetValue(key, out string val))
				{
					return val;
				}
				var prop = this.GetType().GetProperty(key);
				var defaultProp = prop.GetCustomAttributes(false).FirstOrDefault() as DefaultValueAttribute;

				return defaultProp.DefaultValue;
			}
			set
			{
				_dict[key] = value;
				//_dirty=true;
			}
		}

		[DefaultValueAttribute("45")]
		public float SiteLatitude
		{
			get { return float.Parse(this["SiteLatitude"]); }
			set { this["SiteLatitude"] = value.ToString(); }
		}

		[DefaultValueAttribute("45")]
		public float SiteLongitude
		{
			get { return float.Parse(this["SiteLongitude"]); }
			set { this["SiteLongitude"] = value.ToString(); }
		}

		[DefaultValueAttribute("0|0")]
		public Point MiniControllerPos
		{
			get { return ToPoint(this["MiniControllerPos"]); }
			set { this["MiniControllerPos"] = FromPoint(value); }
		}

		[DefaultValueAttribute("0|50")]
		public Point ChecklistPos
		{
			get { return ToPoint(this["ChecklistPos"]); }
			set { this["ChecklistPos"] = FromPoint(value); }
		}

		[DefaultValueAttribute("0|50")]
		public Size ChecklistSize
		{
			get { return ToSize(this["ChecklistSize"]); }
			set { this["ChecklistSize"] = FromSize(value); }
		}

		[DefaultValueAttribute("100|100")]
		public Point WindowPos
		{
			get { return ToPoint(this["WindowPos"]); }
			set { this["WindowPos"] = FromPoint(value); }
		}

		[DefaultValueAttribute("100")]
		public float SiteAltitude
		{
			get { return float.Parse(this["SiteAltitude"]); }
			set { this["SiteAltitude"] = value.ToString(); }
		}

		[DefaultValueAttribute("0|0")]
		public Point TargetChooserPos
		{
			get { return ToPoint(this["TargetChooserPos"]); }
			set { this["TargetChooserPos"] = FromPoint(value); ; }
		}

		[DefaultValueAttribute("670|910")]
		public Size TargetChooserSize
		{
			get { return ToSize(this["TargetChooserSize"]); }
			set { this["TargetChooserSize"] = FromSize(value); }
		}

		[DefaultValueAttribute("False")]
		public bool ShowDecLimits
		{
			get { return Convert.ToBoolean(this["ShowDecLimits"]); }
			set { this["ShowDecLimits"] = value.ToString(); }
		}

		[DefaultValueAttribute("-90")]
		public float LowerDecLimit
		{
			get { return float.Parse(this["LowerDecLimit"]); }
			set { this["LowerDecLimit"] = value.ToString(); }
		}

		[DefaultValueAttribute("180")]
		public float UpperDecLimit
		{
			get { return float.Parse(this["UpperDecLimit"]); }
			set { this["UpperDecLimit"] = value.ToString(); }
		}

		[DefaultValueAttribute("19200")]
		public string BaudRate
		{
			get { return this["BaudRate"]; }
			set { this["BaudRate"] = value; }
		}

		[DefaultValueAttribute("Sidereal")]
		public string TrackingRate
		{
			get { return this["TrackingRate"]; }
			set { this["TrackingRate"] = value; }
		}

		[DefaultValueAttribute("False")]
		public bool KeepMiniControlOnTop
		{
			get { return Convert.ToBoolean(this["KeepMiniControlOnTop"]); }
			set { this["KeepMiniControlOnTop"] = value.ToString(); }
		}

		[DefaultValueAttribute("0")]
		public Single DECHomeOffset
		{
			get { return Convert.ToSingle(this["DECHomeOffset"]); }
			set { this["DECHomeOffset"] = value.ToString(); }
		}

		[DefaultValueAttribute("0")]
		public Single RAHomeOffset
		{
			get { return Convert.ToSingle(this["RAHomeOffset"]); }
			set { this["RAHomeOffset"] = value.ToString(); }
		}

		[DefaultValueAttribute("<CustomCommands />")]
		public string CustomCommands
		{
			get { return this["CustomCommands"]; }
			set { this["CustomCommands"] = value; }
		}

		[DefaultValueAttribute("False")]
		public bool RunAutoHomeRAOnConnect
		{
			get { return Convert.ToBoolean(this["RunAutoHomeRAOnConnect"]); }
			set { this["RunAutoHomeRAOnConnect"] = value.ToString(); }
		}

		[DefaultValueAttribute("False")]
		public bool RunAutoHomeDECOnConnect
		{
			get { return Convert.ToBoolean(this["RunAutoHomeDECOnConnect"]); }
			set { this["RunAutoHomeDECOnConnect"] = value.ToString(); }
		}

		[DefaultValueAttribute("False")]
		public bool RunDECOffsetHomingOnConnect
		{
			get { return Convert.ToBoolean(this["RunDECOffsetHomingOnConnect"]); }
			set { this["RunDECOffsetHomingOnConnect"] = value.ToString(); }
		}

		[DefaultValueAttribute("East")]
		public string AutoHomeRaDirection
		{
			get { return this["AutoHomeRaDirection"]; }
			set { this["AutoHomeRaDirection"] = value; }
		}

		[DefaultValueAttribute("15")]
		public float AutoHomeRaDistance
		{
			get { return Convert.ToSingle(this["AutoHomeRaDistance"]); }
			set { this["AutoHomeRaDistance"] = value.ToString(); }
		}

		[DefaultValueAttribute("North")]
		public string AutoHomeDecDirection
		{
			get { return this["AutoHomeDecDirection"]; }
			set { this["AutoHomeDecDirection"] = value; }
		}

		[DefaultValueAttribute("15")]
		public float AutoHomeDecDistance
		{
			get { return Convert.ToSingle(this["AutoHomeDecDistance"]); }
			set { this["AutoHomeDecDistance"] = value.ToString(); }
		}

		[DefaultValueAttribute("false")]
		public bool ShowChecklistOnConnect
		{
			get { return Convert.ToBoolean(this["ShowChecklistOnConnect"]); }
			set { this["ShowChecklistOnConnect"] = value.ToString(); }
		}

		[DefaultValueAttribute("OnDemand")]
		public ChecklistShowOn ShowChecklist
		{
			get { return (ChecklistShowOn)Enum.Parse(typeof(ChecklistShowOn), this["ShowChecklist"]); }
			set { this["ShowChecklist"] = value.ToString(); }
		}

		[DefaultValueAttribute("Checklist")]
		public string ChecklistTitle
		{
			get { return this["ChecklistTitle"]; }
			set { this["ChecklistTitle"] = value; }
		}
	}
}
