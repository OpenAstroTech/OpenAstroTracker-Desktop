using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using OATControl.ViewModels;

namespace OATControl
{
    public class UpdateCheckResult
    {
        public bool UpdateAvailable { get; set; }
        public string LatestVersion { get; set; }
        public string CurrentVersion { get; set; }
        public string Changelog { get; set; }
        public string DownloadUrl { get; set; }
        public string ReleasePageUrl { get; set; }
        public bool Failed { get; set; }      // the check could not be made (no internet, GitHub unreachable)
        public bool IsFirmware { get; set; }
        public string SkipKey { get; set; }   // what "Skip This Version" remembers: the release tag, or the develop commit
        public bool Skipped { get; set; }     // the user chose to skip this one: no popup

        public static UpdateCheckResult NoUpdate => new UpdateCheckResult();
    }

    public static class UpdateChecker
    {
        private const string DesktopRepoUrl = "https://api.github.com/repos/OpenAstroTech/OpenAstroTracker-Desktop/releases/latest";
        private const string FirmwareRepoUrl = "https://api.github.com/repos/OpenAstroTech/OpenAstroTracker-Firmware/releases/latest";

        private const string FirmwareCompareUrl = "https://api.github.com/repos/OpenAstroTech/OpenAstroTracker-Firmware/compare/{0}...develop";

        private static DateTime _lastDesktopCheck = DateTime.MinValue;
        private static DateTime _lastFirmwareCheck = DateTime.MinValue;
        private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(24);

        private static readonly HttpClient _httpClient = new HttpClient(new HttpClientHandler())
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        static UpdateChecker()
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "OATControl");
        }

        public static async Task<UpdateCheckResult> CheckForDesktopUpdateAsync()
        {
            if (DateTime.UtcNow - _lastDesktopCheck < CheckInterval)
                return UpdateCheckResult.NoUpdate;

            try
            {
                var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
                var release = await FetchLatestReleaseAsync(DesktopRepoUrl);
                if (release == null)
                    return UpdateCheckResult.NoUpdate;

                var tagName = UppercaseVersion(release["tag_name"]?.ToString());
                var tagVersion = ParseVersionTag(release["tag_name"]?.ToString());
                if (tagVersion == null || currentVersion == null)
                    return UpdateCheckResult.NoUpdate;

                if (tagVersion > currentVersion)
                {
                    if (AppSettings.Instance.SkippedDesktopVersion == tagName)
                        return UpdateCheckResult.NoUpdate;

                    _lastDesktopCheck = DateTime.UtcNow;
                    return new UpdateCheckResult
                    {
                        UpdateAvailable = true,
                        LatestVersion = tagName,
                        CurrentVersion = $"V{currentVersion}",
                        Changelog = release["body"]?.ToString() ?? "",
                        DownloadUrl = release["assets"]?[0]?["browser_download_url"]?.ToString(),
                        ReleasePageUrl = release["html_url"]?.ToString()
                    };
                }

                return UpdateCheckResult.NoUpdate;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateChecker desktop check failed: {ex.Message}");
                return new UpdateCheckResult { Failed = true };
            }
        }

        public static void ResetDesktopCheckTimer()
        {
            _lastDesktopCheck = DateTime.MinValue;
        }

        static string UppercaseVersion(string tag)
        {
            if (!string.IsNullOrEmpty(tag) && tag.StartsWith("v"))
            {
                tag= 'V' + tag.Substring(1);
            }
            return tag;
        }

        public static async Task<UpdateCheckResult> CheckForFirmwareUpdateAsync(string currentFirmwareVersion)
        {
            if (DateTime.UtcNow - _lastFirmwareCheck < CheckInterval)
                return UpdateCheckResult.NoUpdate;

            try
            {
                var currentVersion = ParseVersionTag(currentFirmwareVersion);
                if (currentVersion == null)
                    return UpdateCheckResult.NoUpdate;

                var release = await FetchLatestReleaseAsync(FirmwareRepoUrl);
                if (release == null)
                    return UpdateCheckResult.NoUpdate;

                var tagName = UppercaseVersion(release["tag_name"].ToString());
                var tagVersion = ParseVersionTag(tagName);
                if (tagVersion == null)
                    return UpdateCheckResult.NoUpdate;

                
                if (tagVersion > currentVersion)
                {
                    _lastFirmwareCheck = DateTime.UtcNow;
                    return new UpdateCheckResult
                    {
                        UpdateAvailable = true,
                        IsFirmware = true,
                        LatestVersion = tagName,
                        CurrentVersion = UppercaseVersion(currentFirmwareVersion),
                        Changelog = release["body"]?.ToString() ?? "",
                        DownloadUrl = release["zipball_url"]?.ToString(),
                        ReleasePageUrl = release["html_url"]?.ToString(),
                        SkipKey = tagName,
                        Skipped = AppSettings.Instance.SkippedFirmwareVersion == tagName
                    };
                }

                if (AppSettings.Instance.NotifyFirmwareBeta)
                {
                    // The develop branch reports the same version as the latest release, so the mount can't tell us
                    // whether it already runs it. Notify once per develop commit instead ("Skip This Version").
                    var compare = await FetchLatestReleaseAsync(string.Format(FirmwareCompareUrl, release["tag_name"]));
                    var commits = compare["commits"] as JArray;
                    var ahead = (int?)compare["ahead_by"] ?? 0;
                    if (ahead > 0 && commits != null && commits.Count > 0)
                    {
                        var head = commits.Last["sha"]?.ToString();
                        _lastFirmwareCheck = DateTime.UtcNow;
                        return new UpdateCheckResult
                        {
                            UpdateAvailable = true,
                            IsFirmware = true,
                            LatestVersion = $"develop ({ahead} unreleased change{(ahead == 1 ? "" : "s")} after {tagName})",
                            CurrentVersion = UppercaseVersion(currentFirmwareVersion),
                            Changelog = string.Join("\n", commits.Reverse().Select(c => "- " + c["commit"]?["message"]?.ToString().Split('\n')[0].Trim())),
                            DownloadUrl = Firmware.FirmwareBuilder.DevelopZipUrl,
                            ReleasePageUrl = compare["html_url"]?.ToString(),
                            SkipKey = head,
                            Skipped = AppSettings.Instance.SkippedFirmwareVersion == head
                        };
                    }
                }

                return UpdateCheckResult.NoUpdate;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateChecker firmware check failed: {ex.Message}");
                return UpdateCheckResult.NoUpdate;
            }
        }

        private static async Task<JObject> FetchLatestReleaseAsync(string url)
        {
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JObject.Parse(json);
        }

        private static Version ParseVersionTag(string tag)
        {
            if (string.IsNullOrEmpty(tag))
                return null;

            // Strip leading 'V' or 'v'
            var versionStr = tag.TrimStart('V', 'v');

            Version result;
            if (Version.TryParse(versionStr, out result))
                return result;

            return null;
        }
    }
}