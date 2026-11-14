using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using OATCommunications.Utilities;
using OATControl.Controls;
using OATControl.Firmware;
using OATControl.ViewModels;

namespace OATControl
{
	/// <summary>
	/// Test version of configuring and flashing the mount firmware from OATControl: runs the config.openastrotech.com
	/// wizard natively, then downloads the firmware and builds/uploads it with PlatformIO (like OATFWGUI).
	/// </summary>
	public partial class DlgFirmwareFlasher : ThemedWindow
	{
		private readonly MountVM _mount;
		private FirmwareConfigWizard _wizard;
		private readonly Dictionary<string, TextBox> _textInputs = new Dictionary<string, TextBox>();
		private CancellationTokenSource _cts;
		private string _firmwareDir;
		private FirmwareRelease _downloadedRelease;
		private bool _busy;
		private bool _loadingReleases;
		private string _autoSelectedFwVersion;

		public DlgFirmwareFlasher(MountVM mount)
		{
			_mount = mount;
			InitializeComponent();
			Closing += OnClosing;
			_wizard = new FirmwareConfigWizard(FirmwareConfigWizard.LoadEmbeddedSchema());
			ShowStep();
		}

		private void ShowStep()
		{
			RefreshAnswers();
			if (_wizard.IsComplete)
			{
				ShowFlashPage();
				return;
			}

			WizardPage.Visibility = Visibility.Visible;
			FlashPage.Visibility = Visibility.Collapsed;
			BackButton.IsEnabled = _wizard.CanGoBack;

			var step = _wizard.CurrentStep;
			StepTitleText.Text = (string)step["title"];
			StepLabelText.Text = (string)step["label"];
			ChoicesPanel.Children.Clear();
			_textInputs.Clear();

			if ((string)step["control"]["type"] == "textinput")
			{
				foreach (var choice in _wizard.EnabledChoices(step))
				{
					var row = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };
					var label = new TextBlock { Text = (string)choice["label"], Width = 260, VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap };
					label.SetResourceReference(StyleProperty, "MetroTextBlock");
					var box = new TextBox { Text = _wizard.DisplayDefaultValue(choice), Height = 26, MaxWidth = 300, HorizontalAlignment = HorizontalAlignment.Left, MinWidth = 200, VerticalContentAlignment = VerticalAlignment.Center };
					DockPanel.SetDock(label, Dock.Left);
					row.Children.Add(label);
					row.Children.Add(box);
					ChoicesPanel.Children.Add(row);
					_textInputs[(string)choice["key"]] = box;
				}

				var next = new Button { Content = "Next", Width = 90, Height = 28, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(260, 8, 0, 0) };
				next.SetResourceReference(StyleProperty, "MainButtonStyle");
				next.Click += OnTextNext;
				ChoicesPanel.Children.Add(next);
				_textInputs.Values.FirstOrDefault()?.Focus();
			}
			else
			{
				foreach (var choice in _wizard.EnabledChoices(step))
				{
					var key = (string)choice["key"];
					var button = new Button
					{
						Content = (string)choice["value"],
						Height = 32,
						Margin = new Thickness(0, 0, 0, 6),
						HorizontalContentAlignment = HorizontalAlignment.Left,
						Padding = new Thickness(10, 0, 10, 0),
					};
					button.SetResourceReference(StyleProperty, "MainButtonStyle");
					button.Click += (s, e) => { _wizard.Select(key); ShowStep(); };
					ChoicesPanel.Children.Add(button);
				}
			}
		}

		private void RefreshAnswers()
		{
			AnswersPanel.Children.Clear();
			foreach (var answer in _wizard.AnsweredSteps())
			{
				var title = new TextBlock { Text = answer.Key, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 4, 0, 0) };
				title.SetResourceReference(StyleProperty, "MetroTextBlock");
				var value = new TextBlock { Text = answer.Value, TextWrapping = TextWrapping.Wrap, FontSize = 11 };
				value.SetResourceReference(StyleProperty, "MetroTextBlock");
				AnswersPanel.Children.Add(title);
				AnswersPanel.Children.Add(value);
			}
		}

		private void OnTextNext(object sender, RoutedEventArgs e)
		{
			// Only fields that differ from what was shown count as edits, like the website's change events.
			var step = _wizard.CurrentStep;
			var edited = new Dictionary<string, string>();
			foreach (var choice in _wizard.EnabledChoices(step))
			{
				var key = (string)choice["key"];
				if (_textInputs.TryGetValue(key, out var box) && box.Text != _wizard.DisplayDefaultValue(choice))
				{
					edited[key] = box.Text.Trim();
				}
			}

			_wizard.SubmitText(edited);
			ShowStep();
		}

		private void OnBack(object sender, RoutedEventArgs e)
		{
			if (_busy)
			{
				return;
			}

			_wizard.GoBack();
			ShowStep();
		}

		private void OnRestart(object sender, RoutedEventArgs e)
		{
			if (_busy)
			{
				return;
			}

			_wizard = new FirmwareConfigWizard(FirmwareConfigWizard.LoadEmbeddedSchema());
			ShowStep();
		}

		private void OnSaveConfig(object sender, RoutedEventArgs e)
		{
			var dlg = new SaveFileDialog { FileName = "Configuration_local.hpp", Filter = "C++ header (*.hpp)|*.hpp|All files (*.*)|*.*" };
			if (dlg.ShowDialog(this) == true)
			{
				System.IO.File.WriteAllText(dlg.FileName, _wizard.GenerateFileContent());
			}
		}

		private async void ShowFlashPage()
		{
			WizardPage.Visibility = Visibility.Collapsed;
			FlashPage.Visibility = Visibility.Visible;
			ConfigText.Text = _wizard.GenerateFileContent();
			FooterText.Text = $"Wizard steps from config.openastrotech.com v{_wizard.SourceVersion}. Firmware is downloaded to {FirmwareBuilder.WorkDirectory}.";

			var pio = FirmwareBuilder.FindPlatformIO();
			if (pio == null)
			{
				SetStatus("PlatformIO was not found. This test version needs PlatformIO Core installed (pio.exe on the PATH or in %USERPROFILE%\\.platformio).");
				SetBusy(false, pioAvailable: false);
				return;
			}

			var releases = ReleaseCombo.ItemsSource as List<FirmwareRelease>;
			if (releases == null)
			{
				await LoadReleasesAsync();
			}
			else if (_autoSelectedFwVersion != _wizard.GetValue("fwversion"))
			{
				// The firmware line answer changed after going back: pick the matching release again.
				SelectReleaseForWizard(releases);
				SelectEnvironmentForBoard();
			}
			else
			{
				SelectEnvironmentForBoard();
			}

			await RefreshPortsAsync();
		}

		private async Task LoadReleasesAsync()
		{
			SetStatus("Getting firmware versions from GitHub...");
			_loadingReleases = true;
			FlashBackButton.IsEnabled = RestartButton.IsEnabled = false;
			try
			{
				var releases = await FirmwareBuilder.GetReleasesAsync();
				ReleaseCombo.ItemsSource = releases;
				SelectReleaseForWizard(releases);
			}
			catch (Exception ex)
			{
				SetStatus("Could not get firmware versions: " + ex.Message);
			}
			finally
			{
				_loadingReleases = false;
				FlashBackButton.IsEnabled = RestartButton.IsEnabled = !_busy;
			}
		}

		private void SelectReleaseForWizard(List<FirmwareRelease> releases)
		{
			// The OAT wizard asks whether to target the official V1.11 line; otherwise use the newest stable release.
			_autoSelectedFwVersion = _wizard.GetValue("fwversion");
			var stable = releases.Where(r => !r.IsPrerelease).ToList();
			var wanted = _autoSelectedFwVersion == "O" ? stable.FirstOrDefault(r => r.Name.Contains("1.11.")) : null;
			ReleaseCombo.SelectedItem = wanted ?? stable.FirstOrDefault() ?? releases.First();
		}

		private async void OnReleaseChanged(object sender, SelectionChangedEventArgs e)
		{
			var release = ReleaseCombo.SelectedItem as FirmwareRelease;
			if (release == null || release.ZipUrl == _downloadedRelease?.ZipUrl)
			{
				SelectEnvironmentForBoard();
				return;
			}

			await RunBusyAsync(async token =>
			{
				EnvCombo.ItemsSource = null;
				_firmwareDir = null;
				_downloadedRelease = null;
				try
				{
					_firmwareDir = await FirmwareBuilder.DownloadAsync(release, AppendLog, token);
				}
				catch
				{
					// Clear the selection so picking the same release again retries the download.
					ReleaseCombo.SelectedItem = null;
					throw;
				}

				_downloadedRelease = release;
				EnvCombo.ItemsSource = FirmwareBuilder.ReadEnvironments(_firmwareDir);
				SelectEnvironmentForBoard();
				SetStatus($"Firmware {release.Name} downloaded.");
			});
		}

		private void SelectEnvironmentForBoard()
		{
			var board = _wizard.GetChosenDefineValue("board");
			var environments = EnvCombo.ItemsSource as List<PioEnvironment>;
			EnvCombo.SelectedItem = environments?.FirstOrDefault(env => env.Board == board);
		}

		private async void OnRefreshPorts(object sender, RoutedEventArgs e)
		{
			await RefreshPortsAsync();
		}

		private async Task RefreshPortsAsync()
		{
			var pio = FirmwareBuilder.FindPlatformIO();
			if (pio == null)
			{
				return;
			}

			var previous = (PortCombo.SelectedItem as SerialPortEntry)?.Port;
			List<SerialPortEntry> ports;
			try
			{
				ports = await FirmwareBuilder.ListSerialPortsAsync(pio);
			}
			catch (Exception ex)
			{
				Log.WriteLine("FIRMWARE: Listing ports with PlatformIO failed: {0}", ex);
				ports = System.IO.Ports.SerialPort.GetPortNames().Distinct().Select(p => new SerialPortEntry { Port = p }).ToList();
			}

			PortCombo.ItemsSource = ports;
			PortCombo.SelectedItem = ports.FirstOrDefault(p => p.Port == previous) ?? (ports.Count == 1 ? ports[0] : null);
		}

		private async void OnBuild(object sender, RoutedEventArgs e)
		{
			await BuildAsync(upload: false);
		}

		private async void OnFlash(object sender, RoutedEventArgs e)
		{
			await BuildAsync(upload: true);
		}

		private async Task BuildAsync(bool upload)
		{
			var env = EnvCombo.SelectedItem as PioEnvironment;
			var port = PortCombo.SelectedItem as SerialPortEntry;
			if (_firmwareDir == null || env == null)
			{
				SetStatus("Select a firmware version and board first.");
				return;
			}

			if (upload && port == null)
			{
				SetStatus("Select the serial port of the board.");
				return;
			}

			var board = _wizard.GetChosenDefineValue("board");
			if (board != null && env.Board != null && board != env.Board)
			{
				SetStatus($"Board '{env.Name}' does not match the configuration ({board}); the firmware would refuse to build.");
				return;
			}

			if (upload && _mount.MountConnected)
			{
				SetStatus("OATControl is connected to the mount. Disconnect first so the serial port is free for flashing.");
				return;
			}

			var pio = FirmwareBuilder.FindPlatformIO();
			await RunBusyAsync(async token =>
			{
				FirmwareBuilder.WriteConfiguration(_firmwareDir, _wizard.GenerateFileContent());
				FirmwareBuilder.ApplyHotPatches(_firmwareDir, env, AppendLog);
				SetStatus(upload ? $"Building and flashing to {port.Port}... (the first build downloads compilers and can take several minutes)" : "Building... (the first build downloads compilers and can take several minutes)");
				var exitCode = await FirmwareBuilder.BuildAsync(pio, _firmwareDir, env, upload ? port.Port : null, AppendLog, token);
				if (token.IsCancellationRequested)
				{
					SetStatus("Cancelled.");
				}
				else if (exitCode == 0)
				{
					SetStatus(upload ? "Firmware flashed successfully." : "Firmware built successfully.");
				}
				else
				{
					SetStatus($"PlatformIO failed (exit code {exitCode}). See the log above.");
				}
			});
		}

		private void OnCancel(object sender, RoutedEventArgs e)
		{
			_cts?.Cancel();
		}

		private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (!_busy)
			{
				return;
			}

			// Killing an upload half way can leave the board without working firmware, so ask first.
			var answer = MessageBox.Show(this, "A firmware download, build or upload is still running. Stopping an upload part way can leave the board without working firmware.\n\nStop it and close anyway?",
				"Firmware", MessageBoxButton.YesNo, MessageBoxImage.Warning);
			if (answer == MessageBoxResult.Yes)
			{
				_cts?.Cancel();
			}
			else
			{
				e.Cancel = true;
			}
		}

		private async Task RunBusyAsync(Func<CancellationToken, Task> action)
		{
			if (_busy)
			{
				return;
			}

			_cts = new CancellationTokenSource();
			SetBusy(true);
			try
			{
				await action(_cts.Token);
			}
			catch (OperationCanceledException)
			{
				SetStatus("Cancelled.");
			}
			catch (Exception ex)
			{
				Log.WriteLine("FIRMWARE: Failed: {0}", ex);
				SetStatus("Failed: " + ex.Message);
			}
			finally
			{
				SetBusy(false);
				_cts.Dispose();
				_cts = null;
			}
		}

		private void SetBusy(bool busy, bool pioAvailable = true)
		{
			_busy = busy;
			BuildButton.IsEnabled = !busy && pioAvailable;
			FlashButton.IsEnabled = !busy && pioAvailable;
			ReleaseCombo.IsEnabled = !busy && pioAvailable;
			EnvCombo.IsEnabled = !busy && pioAvailable;
			PortCombo.IsEnabled = !busy && pioAvailable;
			RefreshButton.IsEnabled = !busy && pioAvailable;
			FlashBackButton.IsEnabled = !busy && !_loadingReleases;
			RestartButton.IsEnabled = !busy && !_loadingReleases;
			CancelButton.IsEnabled = busy;
		}

		private void SetStatus(string text)
		{
			Log.WriteLine("FIRMWARE: {0}", text);
			StatusText.Text = text;
		}

		private void AppendLog(string line)
		{
			Dispatcher.BeginInvoke(new Action(() =>
			{
				LogText.AppendText(line + Environment.NewLine);
				LogText.ScrollToEnd();
			}));
		}
	}
}
