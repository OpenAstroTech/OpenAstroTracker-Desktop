using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Windows;
using System.IO;
using System.Reflection;
using System.Windows.Media;

namespace OATSimulation.ViewModels
{
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

    class AppSettings
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
            _settingsLocation = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OpenAstroTracker", "OATSimulation.Settings");
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

        private Color ToColor(string val)
        {
            var parts = val.Split('|');
            return Color.FromRgb(byte.Parse(parts[0]), byte.Parse(parts[1]), byte.Parse(parts[2]));
        }

        private string FromColor(Color val)
        {
            return $"{val.R}|{val.G}|{val.B}";
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

        [DefaultValueAttribute("1")]
        public int LightPreset
        {
            get { return int.Parse(this["LightPreset"]); }
            set { this["LightPreset"] = value.ToString(); }
        }

        [DefaultValueAttribute("50")]
        public int MountAngle
        {
            get { return int.Parse(this["MountAngle"]); }
            set { this["MountAngle"] = value.ToString(); }
        }

        [DefaultValueAttribute("100|100")]
        public Point WindowPos
        {
            get { return ToPoint(this["WindowPos"]); }
            set { this["WindowPos"] = FromPoint(value); }
        }

        [DefaultValueAttribute("False")]
        public bool KeepWindowOnTop
        {
            get { return Convert.ToBoolean(this["KeepWindowOnTop"]); }
            set { this["KeepWindowOnTop"] = value.ToString(); }
        }

        [DefaultValueAttribute("70|70|70")]
        public Color AlbedoColorBase
        {
            get { return ToColor(this["AlbedoColorBase"]); }
            set { this["AlbedoColorBase"] = FromColor(value); }
        }

        [DefaultValueAttribute("70|70|70")]
        public Color AlbedoColorRA
        {
            get { return ToColor(this["AlbedoColorRA"]); }
            set { this["AlbedoColorRA"] = FromColor(value); }
        }

        [DefaultValueAttribute("70|70|70")]
        public Color AlbedoColorDEC
        {
            get { return ToColor(this["AlbedoColorDEC"]); }
            set { this["AlbedoColorDEC"] = FromColor(value); }
        }

        [DefaultValueAttribute("70|70|70")]
        public Color AlbedoColorGuider
        {
            get { return ToColor(this["AlbedoColorGuider"]); }
            set { this["AlbedoColorGuider"] = FromColor(value); }
        }
    }
}
