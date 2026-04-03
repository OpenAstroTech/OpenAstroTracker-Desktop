using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace OATControl.ViewModels
{
    public enum ChecklistShowOn { OnStartup, OnConnect, OnDemand }
    public static class ChecklistShowOnEnumHelper
    {
        public static Array ChecklistShowOnValues => Enum.GetValues(typeof(ChecklistShowOn));
    }

    public class UpgradeEventArgs : EventArgs
    {
        public UpgradeEventArgs(long loaded, long current) { LoadedVersion = loaded; CurrentVersion = current; }
        public long LoadedVersion { get; }
        public long CurrentVersion { get; }
    }

    public class AppSettings
    {
        class DefaultValueAttribute : Attribute
        {
            public DefaultValueAttribute(string val) { DefaultValue = val; }
            public string DefaultValue { get; private set; }
        }

        public EventHandler<UpgradeEventArgs>? UpgradeVersion;
        private static AppSettings? _instance;
        private Dictionary<string, string> _dict = new Dictionary<string, string>();
        private readonly string _settingsLocation;
        private List<SlewPoint>? _slewPoints;

        AppSettings()
        {
            var entry = Assembly.GetExecutingAssembly().GetName().Version!;
            CurrentVersion = ((entry.Major * 100 + entry.Minor) * 100 + entry.Build) * 100 + entry.Revision;
            _settingsLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OpenAstroTracker", "OATControl.Settings");
            Directory.CreateDirectory(Path.GetDirectoryName(_settingsLocation)!);
        }

        public static AppSettings Instance => _instance ??= new AppSettings();

        public void Load()
        {
            if (File.Exists(_settingsLocation))
            {
                var doc = XDocument.Load(_settingsLocation, LoadOptions.PreserveWhitespace);
                LoadedVersion = long.Parse(doc.Element("Settings")!.Attribute("Version")!.Value);
                foreach (var setting in doc.Element("Settings")!.Elements("Setting"))
                    _dict[setting.Attribute("Key")!.Value] = setting.Attribute("Value")!.Value;
                if (LoadedVersion != CurrentVersion) { OnUpgradeSettings(); Save(); }
            }
        }

        void OnUpgradeSettings() => UpgradeVersion?.Invoke(this, new UpgradeEventArgs(LoadedVersion, CurrentVersion));

        public void Save()
        {
            var doc = new XDocument(new XElement("Settings",
                new XAttribute("Version", CurrentVersion),
                _dict.Select(kv => new XElement("Setting", new XAttribute("Key", kv.Key), new XAttribute("Value", kv.Value)))));
            doc.Save(_settingsLocation);
        }

        public long CurrentVersion { get; private set; }
        public long LoadedVersion { get; private set; }

        private string? this[string? key]
        {
            get
            {
                if (key == null) return null;
                if (_dict.TryGetValue(key, out string? val)) return val;
                var prop = GetType().GetProperty(key);
                if (prop == null) return null;
                var defaultProp = prop.GetCustomAttributes(false).FirstOrDefault() as DefaultValueAttribute;
                return defaultProp?.DefaultValue;
            }
            set { if (key != null && value != null) _dict[key] = value; }
        }

        private (double X, double Y) ToPoint(string? val)
        {
            if (val == null) return (0, 0);
            var parts = val.Split('|');
            return (double.Parse(parts[0]), double.Parse(parts[1]));
        }
        private string FromPoint((double X, double Y) val) => $"{val.X}|{val.Y}";

        private (double W, double H) ToSize(string? val)
        {
            if (val == null) return (100, 100);
            var parts = val.Split('|');
            return (double.Parse(parts[0]), double.Parse(parts[1]));
        }
        private string FromSize((double W, double H) val) => $"{val.W}|{val.H}";

        private string FromSlewPoints(List<SlewPoint>? points)
        {
            if (points == null || !points.Any()) return string.Empty;
            var serializer = new XmlSerializer(typeof(List<SlewPoint>));
            using var writer = new StringWriter();
            serializer.Serialize(writer, points);
            return writer.ToString();
        }

        private List<SlewPoint> ToSlewPoints(string? xml)
        {
            if (string.IsNullOrEmpty(xml)) return new List<SlewPoint>();
            var serializer = new XmlSerializer(typeof(List<SlewPoint>));
            try { using var reader = new StringReader(xml); return (List<SlewPoint>)serializer.Deserialize(reader)!; }
            catch { return new List<SlewPoint>(); }
        }

        [DefaultValueAttribute("")]
        public List<SlewPoint> SlewPoints
        {
            get => _slewPoints ??= ToSlewPoints(this["SlewPoints"]);
            set { _slewPoints = value; this["SlewPoints"] = FromSlewPoints(value); }
        }

        [DefaultValueAttribute("45")]
        public float SiteLatitude
        {
            get => float.Parse(this["SiteLatitude"]!);
            set => this["SiteLatitude"] = value.ToString();
        }

        [DefaultValueAttribute("45")]
        public float SiteLongitude
        {
            get => float.Parse(this["SiteLongitude"]!);
            set => this["SiteLongitude"] = value.ToString();
        }

        [DefaultValueAttribute("100")]
        public float SiteAltitude
        {
            get => float.Parse(this["SiteAltitude"]!);
            set => this["SiteAltitude"] = value.ToString();
        }

        [DefaultValueAttribute("100|100")]
        public (double X, double Y) WindowPos
        {
            get => ToPoint(this["WindowPos"]);
            set => this["WindowPos"] = FromPoint(value);
        }

        [DefaultValueAttribute("0|0")]
        public (double X, double Y) MiniControllerPos
        {
            get => ToPoint(this["MiniControllerPos"]);
            set => this["MiniControllerPos"] = FromPoint(value);
        }

        [DefaultValueAttribute("0|0")]
        public (double X, double Y) SlewPointsWindowPos
        {
            get => ToPoint(this["SlewPointsWindowPos"]);
            set => this["SlewPointsWindowPos"] = FromPoint(value);
        }

        [DefaultValueAttribute("400|300")]
        public (double W, double H) SlewPointsWindowSize
        {
            get => ToSize(this["SlewPointsWindowSize"]);
            set => this["SlewPointsWindowSize"] = FromSize(value);
        }

        [DefaultValueAttribute("0|50")]
        public (double X, double Y) TargetChooserPos
        {
            get => ToPoint(this["TargetChooserPos"]);
            set => this["TargetChooserPos"] = FromPoint(value);
        }

        [DefaultValueAttribute("670|910")]
        public (double W, double H) TargetChooserSize
        {
            get => ToSize(this["TargetChooserSize"]);
            set => this["TargetChooserSize"] = FromSize(value);
        }

        [DefaultValueAttribute("False")]
        public bool ShowDecLimits
        {
            get => Convert.ToBoolean(this["ShowDecLimits"]);
            set => this["ShowDecLimits"] = value.ToString();
        }

        [DefaultValueAttribute("-90")]
        public float LowerDecLimit
        {
            get => float.Parse(this["LowerDecLimit"]!);
            set => this["LowerDecLimit"] = value.ToString();
        }

        [DefaultValueAttribute("180")]
        public float UpperDecLimit
        {
            get => float.Parse(this["UpperDecLimit"]!);
            set => this["UpperDecLimit"] = value.ToString();
        }

        [DefaultValueAttribute("19200")]
        public string BaudRate
        {
            get => this["BaudRate"]!;
            set => this["BaudRate"] = value;
        }

        [DefaultValueAttribute("Sidereal")]
        public string TrackingRate
        {
            get => this["TrackingRate"]!;
            set => this["TrackingRate"] = value;
        }

        [DefaultValueAttribute("False")]
        public bool KeepMiniControlOnTop
        {
            get => Convert.ToBoolean(this["KeepMiniControlOnTop"]);
            set => this["KeepMiniControlOnTop"] = value.ToString();
        }

        [DefaultValueAttribute("0")]
        public Single DECHomeOffset
        {
            get => Convert.ToSingle(this["DECHomeOffset"]);
            set => this["DECHomeOffset"] = value.ToString();
        }

        [DefaultValueAttribute("0")]
        public Single RAHomeOffset
        {
            get => Convert.ToSingle(this["RAHomeOffset"]);
            set => this["RAHomeOffset"] = value.ToString();
        }

        [DefaultValueAttribute("<CustomCommands />")]
        public string CustomCommands
        {
            get => this["CustomCommands"]!;
            set => this["CustomCommands"] = value;
        }

        [DefaultValueAttribute("False")]
        public bool RunAutoHomeRAOnConnect
        {
            get => Convert.ToBoolean(this["RunAutoHomeRAOnConnect"]);
            set => this["RunAutoHomeRAOnConnect"] = value.ToString();
        }

        [DefaultValueAttribute("False")]
        public bool RunAutoHomeDECOnConnect
        {
            get => Convert.ToBoolean(this["RunAutoHomeDECOnConnect"]);
            set => this["RunAutoHomeDECOnConnect"] = value.ToString();
        }

        [DefaultValueAttribute("False")]
        public bool RunDECOffsetHomingOnConnect
        {
            get => Convert.ToBoolean(this["RunDECOffsetHomingOnConnect"]);
            set => this["RunDECOffsetHomingOnConnect"] = value.ToString();
        }

        [DefaultValueAttribute("East")]
        public string AutoHomeRaDirection
        {
            get => this["AutoHomeRaDirection"]!;
            set => this["AutoHomeRaDirection"] = value;
        }

        [DefaultValueAttribute("15")]
        public float AutoHomeRaDistance
        {
            get => Convert.ToSingle(this["AutoHomeRaDistance"]);
            set => this["AutoHomeRaDistance"] = value.ToString();
        }

        [DefaultValueAttribute("North")]
        public string AutoHomeDecDirection
        {
            get => this["AutoHomeDecDirection"]!;
            set => this["AutoHomeDecDirection"] = value;
        }

        [DefaultValueAttribute("15")]
        public float AutoHomeDecDistance
        {
            get => Convert.ToSingle(this["AutoHomeDecDistance"]);
            set => this["AutoHomeDecDistance"] = value.ToString();
        }

        [DefaultValueAttribute("OnDemand")]
        public ChecklistShowOn ShowChecklist
        {
            get => (ChecklistShowOn)Enum.Parse(typeof(ChecklistShowOn), this["ShowChecklist"]!);
            set => this["ShowChecklist"] = value.ToString();
        }

        [DefaultValueAttribute("False")]
        public bool ShowChecklistOnConnect
        {
            get => Convert.ToBoolean(this["ShowChecklistOnConnect"]);
            set => this["ShowChecklistOnConnect"] = value.ToString();
        }

        [DefaultValueAttribute("Checklist")]
        public string ChecklistTitle
        {
            get => this["ChecklistTitle"]!;
            set => this["ChecklistTitle"] = value;
        }

        [DefaultValueAttribute("False")]
        public bool MonitorNinaPA
        {
            get => Convert.ToBoolean(this["MonitorNinaPA"]);
            set => this["MonitorNinaPA"] = value.ToString();
        }

        [DefaultValueAttribute("False")]
        public bool MonitorSharpCapPA
        {
            get => Convert.ToBoolean(this["MonitorSharpCapPA"]);
            set => this["MonitorSharpCapPA"] = value.ToString();
        }

        [DefaultValueAttribute("False")]
        public bool InvertAZCorrections
        {
            get => Convert.ToBoolean(this["InvertAZCorrections"]);
            set => this["InvertAZCorrections"] = value.ToString();
        }

        [DefaultValueAttribute("False")]
        public bool InvertALTCorrections
        {
            get => Convert.ToBoolean(this["InvertALTCorrections"]);
            set => this["InvertALTCorrections"] = value.ToString();
        }

        [DefaultValueAttribute("")]
        public string NinaLogFolder
        {
            get => this["NinaLogFolder"]!;
            set => this["NinaLogFolder"] = value;
        }

        [DefaultValueAttribute("")]
        public string SharpCapLogFolder
        {
            get => this["SharpCapLogFolder"]!;
            set => this["SharpCapLogFolder"] = value;
        }

        [DefaultValueAttribute("3")]
        public float AZLimit
        {
            get => Convert.ToSingle(this["AZLimit"] ?? "3");
            set => this["AZLimit"] = value.ToString();
        }

        [DefaultValueAttribute("3")]
        public float ALTLimit
        {
            get => Convert.ToSingle(this["ALTLimit"] ?? "3");
            set => this["ALTLimit"] = value.ToString();
        }

        [DefaultValueAttribute("20")]
        public float PolarAlignmentMinimumTotalError
        {
            get => Convert.ToSingle(this["PolarAlignmentMinimumTotalError"] ?? "20");
            set => this["PolarAlignmentMinimumTotalError"] = value.ToString();
        }

        [DefaultValueAttribute("")]
        public string LastConnectedDevice
        {
            get => this["LastConnectedDevice"] ?? string.Empty;
            set => this["LastConnectedDevice"] = value;
        }

        [DefaultValueAttribute("False")]
        public bool AlwaysShowConnectDialog
        {
            get => Convert.ToBoolean(this["AlwaysShowConnectDialog"] ?? "False");
            set => this["AlwaysShowConnectDialog"] = value.ToString();
        }

        [DefaultValueAttribute("localhost")]
        public string IndiServerHost
        {
            get => this["IndiServerHost"] ?? "localhost";
            set => this["IndiServerHost"] = value;
        }

        [DefaultValueAttribute("7624")]
        public int IndiServerPort
        {
            get => int.TryParse(this["IndiServerPort"], out var p) ? p : 7624;
            set => this["IndiServerPort"] = value.ToString();
        }

        [DefaultValueAttribute("False")]
        public bool IndiForceAltAzControls
        {
            get => Convert.ToBoolean(this["IndiForceAltAzControls"] ?? "False");
            set => this["IndiForceAltAzControls"] = value.ToString();
        }

        public (double Left, double Top, double Width, double Height) EnsureRectIsOnScreen(string posPropertyName, object? unused)
        {
            (double X, double Y) pos = posPropertyName switch
            {
                "MiniControllerPos" => MiniControllerPos,
                "SlewPointsWindowPos" => SlewPointsWindowPos,
                _ => (0.0, 0.0)
            };
            // Simple passthrough - just return the stored position with default size
            return (pos.X, pos.Y, 300, 200);
        }
    }
}
