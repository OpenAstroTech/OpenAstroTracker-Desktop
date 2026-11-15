using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using MdXaml;
using OATCommunications.WPF;
using OATControl.ViewModels;

namespace OATControl
{
    public partial class DlgUpdateAvailable : Controls.ThemedWindow, INotifyPropertyChanged
    {
        private readonly UpdateCheckResult _result;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private bool _isDownloading;
        private int _downloadProgress;
        private string _downloadStatus;
        private bool _hasError;
        private string _errorMessage;
        private DelegateCommand _skipCommand;
        private DelegateCommand _skipVersionCommand;
        private DelegateCommand _upgradeCommand;
        private DelegateCommand _cancelCommand;

        public event PropertyChangedEventHandler PropertyChanged;

        private readonly MountVM _mount; // needed for firmware updates, which open the firmware dialog

        public DlgUpdateAvailable(UpdateCheckResult result, MountVM mount = null)
        {
            _result = result;
            _mount = mount;
            this.Owner = Application.Current.MainWindow;
            this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            InitializeComponent();
            this.DataContext = this;

            var themedStyle = new Style(typeof(FlowDocument), MarkdownStyle.Standard);
            themedStyle.Setters.Add(new Setter(FlowDocument.ForegroundProperty, FindResource("AppForegroundBrush")));
            themedStyle.Setters.Add(new Setter(FlowDocument.BackgroundProperty, Brushes.Transparent));
            themedStyle.Setters.Add(new Setter(FlowDocument.FontSizeProperty, 13.0));

            themedStyle.Resources = new ResourceDictionary();
            var foreground = FindResource("AppForegroundBrush");

            var paragraphStyle = new Style(typeof(Paragraph));
            paragraphStyle.Triggers.Add(CreateHeadingTrigger("Heading1", foreground, 20, FontWeights.Bold));
            paragraphStyle.Triggers.Add(CreateHeadingTrigger("Heading2", foreground, 18, FontWeights.SemiBold));
            paragraphStyle.Triggers.Add(CreateHeadingTrigger("Heading3", foreground, 16, FontWeights.Normal));
            paragraphStyle.Triggers.Add(CreateHeadingTrigger("Heading4", foreground, 14, FontWeights.Normal));
            themedStyle.Resources[typeof(Paragraph)] = paragraphStyle;

            MarkdownViewer.MarkdownStyle = themedStyle;

            _skipCommand = new DelegateCommand(s => Close());
            _skipVersionCommand = new DelegateCommand(s =>
            {
                if (_result.IsFirmware)
                {
                    AppSettings.Instance.SkippedFirmwareVersion = _result.SkipKey;
                }
                else
                {
                    AppSettings.Instance.SkippedDesktopVersion = _result.LatestVersion;
                }

                AppSettings.Instance.Save();
                Close();
            });
            _upgradeCommand = new DelegateCommand(s =>
            {
                if (_result.IsFirmware)
                {
                    OpenFirmwareDialog();
                }
                else
                {
                    StartDownload();
                }
            });
            _cancelCommand = new DelegateCommand(s => CancelDownload());
        }

        public string VersionInfo => _result.IsFirmware
            ? $"Firmware {_result.LatestVersion} is now available (your mount has {_result.CurrentVersion})"
            : $"OATControl {_result.LatestVersion} is now available (you have {_result.CurrentVersion})";

        private void OpenFirmwareDialog()
        {
            var owner = Owner;
            Close();
            var dlg = new DlgFirmwareFlasher(_mount, _result) { Owner = owner, WindowStartupLocation = WindowStartupLocation.CenterOwner };
            dlg.ShowDialog();
        }
        public string Changelog => _result.Changelog;

        public bool IsDownloading
        {
            get => _isDownloading;
            set { _isDownloading = value; OnPropertyChanged(nameof(IsDownloading)); }
        }

        public int DownloadProgress
        {
            get => _downloadProgress;
            set { _downloadProgress = value; OnPropertyChanged(nameof(DownloadProgress)); }
        }

        public string DownloadStatus
        {
            get => _downloadStatus;
            set { _downloadStatus = value; OnPropertyChanged(nameof(DownloadStatus)); }
        }

        public bool HasError
        {
            get => _hasError;
            set { _hasError = value; OnPropertyChanged(nameof(HasError)); }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(nameof(ErrorMessage)); }
        }

        public System.Windows.Input.ICommand SkipCommand => _skipCommand;
        public System.Windows.Input.ICommand SkipVersionCommand => _skipVersionCommand;
        public System.Windows.Input.ICommand UpgradeCommand => _upgradeCommand;
        public System.Windows.Input.ICommand CancelCommand => _cancelCommand;

        private async void StartDownload()
        {
            if (string.IsNullOrEmpty(_result.DownloadUrl))
            {
                HasError = true;
                ErrorMessage = "No download URL available.";
                return;
            }

            IsDownloading = true;
            HasError = false;

            try
            {
                var tempPath = Path.Combine(Path.GetTempPath(), "OATControlSetup.exe");

                using (var client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) })
                using (var response = await client.GetAsync(_result.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, _cts.Token))
                {
                    response.EnsureSuccessStatusCode();
                    var totalBytes = response.Content.Headers.ContentLength ?? -1L;

                    using (var stream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        var buffer = new byte[8192];
                        long bytesRead = 0;
                        int read;

                        while ((read = await stream.ReadAsync(buffer, 0, buffer.Length, _cts.Token)) > 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, read, _cts.Token);
                            bytesRead += read;

                            if (totalBytes > 0)
                            {
                                var progress = (int)(bytesRead * 100 / totalBytes);
                                DownloadProgress = progress;
                                DownloadStatus = $"{bytesRead / 1024} KB / {totalBytes / 1024} KB ({progress}%)";
                            }
                            else
                            {
                                DownloadStatus = $"{bytesRead / 1024} KB downloaded";
                            }
                        }
                    }

                    Process.Start(new ProcessStartInfo(tempPath) { UseShellExecute = true });
                    Application.Current.Shutdown();
                }
            }
            catch (OperationCanceledException)
            {
                IsDownloading = false;
                DownloadProgress = 0;
                DownloadStatus = "";
            }
            catch (Exception ex)
            {
                IsDownloading = false;
                HasError = true;
                ErrorMessage = $"Download failed: {ex.Message}";
            }
        }

        private void CancelDownload()
        {
            _cts.Cancel();
        }

        private static Trigger CreateHeadingTrigger(string tag, object foreground, double fontSize, FontWeight fontWeight)
        {
            var trigger = new Trigger { Property = Paragraph.TagProperty, Value = tag };
            trigger.Setters.Add(new Setter(Paragraph.ForegroundProperty, foreground));
            trigger.Setters.Add(new Setter(Paragraph.FontSizeProperty, fontSize));
            trigger.Setters.Add(new Setter(Paragraph.FontWeightProperty, fontWeight));
            return trigger;
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
