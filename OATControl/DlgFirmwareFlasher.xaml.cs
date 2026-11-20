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
		private FirmwareRelease _downloadedRelease; // the release _firmwareDir holds; the custom folder entry when building in place
		private FirmwareRelease _customRelease;
		private bool _busy;
		private bool _loadingReleases;
		private string _autoSelectedFwVersion;
		private string _folderConfigDir; // the custom folder whose Configuration_local.hpp is in the editor; null while the wizard's is shown
		private string _folderConfigSaved; // that file's content as last read or written
		private string _wizardWritten; // the wizard configuration a build here wrote into a folder that had none
		private bool _askingToSave;
		private string _pio; // the pio.exe to build with; null until a usable one is found or installed
		private bool _pioSearched;
		private Task<string> _pioSearch; // started once; a second ShowFlashPage awaits the same search
		private bool ToolsMissing => _pio == null || FlashTools.NeedsGit; // the firmware's libraries are git URLs

		private readonly UpdateCheckResult _update; // set when opened from a firmware update notification
		private Newtonsoft.Json.Linq.JObject _steps; // the wizard step definitions, loaded when the window opens
		private string _stepsSource;
		private string _selectZipUrl; // that update's firmware, to select when the list has loaded

		public DlgFirmwareFlasher(MountVM mount, UpdateCheckResult update = null)
		{
			_mount = mount;
			_update = update;
			_selectZipUrl = update?.DownloadUrl;
			InitializeComponent();
			Closing += OnClosing;
			Loaded += OnLoaded;
			StepTitleText.Text = "Loading the configuration steps...";
			BackButton.IsEnabled = RestartButton.IsEnabled = false;
		}

		private async void OnLoaded(object sender, RoutedEventArgs e)
		{
			// Off the UI thread: .NET Framework resolves the proxy and DNS synchronously before the timeout applies.
			(_steps, _stepsSource) = await Task.Run(() => WizardSteps.LoadAsync(message => Log.WriteLine("FIRMWARE: {0}", message)));
			if (!IsVisible)
			{
				return; // closed while loading
			}

			RestartButton.IsEnabled = true;
			try
			{
				_wizard = new FirmwareConfigWizard(_steps);
				LoadState();
				ShowStep();
			}
			catch (Exception ex)
			{
				// Validation runs only one path per tracker; another branch of a downloaded file can still fail.
				Log.WriteLine("FIRMWARE: The wizard steps ({0}) failed, using the built-in copy: {1}", _stepsSource, ex);
				_steps = FirmwareConfigWizard.LoadEmbeddedSchema();
				_stepsSource = "copy built into OATControl, the downloaded steps failed";
				_wizard = new FirmwareConfigWizard(_steps);
				LoadState();
				ShowStep();
			}
		}

		private static string StateFile => System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OpenAstroTracker", "FirmwareConfig.json");

		/// <summary>Restores the answers (and custom firmware folder) from the last time the dialog was used.</summary>
		private void LoadState()
		{
			try
			{
				if (!System.IO.File.Exists(StateFile))
				{
					return;
				}

				var state = Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(StateFile));
				try
				{
					if (state["answers"] is Newtonsoft.Json.Linq.JArray answers)
					{
						_wizard.ReplayAnswers(answers);
					}
				}
				catch (Exception ex)
				{
					// Malformed answers: keep what was replayed so far and still restore the folder below.
					Log.WriteLine("FIRMWARE: Could not restore all saved answers: {0}", ex);
				}

				var folder = (string)state["folder"];
				if (!string.IsNullOrEmpty(folder) && System.IO.File.Exists(System.IO.Path.Combine(folder, "platformio.ini")))
				{
					// Selecting the entry makes OnReleaseChanged use the folder; the releases are added when they load.
					_customRelease = new FirmwareRelease { Name = "Folder: " + folder, LocalPath = folder, IsPrerelease = true };
					ReleaseCombo.ItemsSource = new List<FirmwareRelease> { _customRelease };
					ReleaseCombo.SelectedItem = _customRelease;
				}
			}
			catch (Exception ex)
			{
				Log.WriteLine("FIRMWARE: Could not restore the saved configuration: {0}", ex);
			}
		}

		private void SaveState()
		{
			try
			{
				var state = new Newtonsoft.Json.Linq.JObject
				{
					["answers"] = _wizard.ExportAnswers(),
					["folder"] = _downloadedRelease?.LocalPath,
				};
				System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(StateFile));
				System.IO.File.WriteAllText(StateFile, state.ToString());
			}
			catch (Exception ex)
			{
				Log.WriteLine("FIRMWARE: Could not save the configuration: {0}", ex);
			}
		}

		private void ShowStep()
		{
			SaveState();
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
			if (_busy || _wizard == null)
			{
				return;
			}

			_wizard.GoBack();
			ShowStep();
		}

		private void OnRestart(object sender, RoutedEventArgs e)
		{
			if (_busy || _wizard == null)
			{
				return;
			}

			if (_wizard.CanGoBack && MessageBox.Show(this, "Discard your answers and start the configuration again?", "Firmware",
				MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes)
			{
				return;
			}

			_wizard = new FirmwareConfigWizard(_steps);
			ShowStep();
		}

		private void OnSaveConfig(object sender, RoutedEventArgs e)
		{
			var dlg = new SaveFileDialog { FileName = "Configuration_local.hpp", Filter = "C++ header (*.hpp)|*.hpp|All files (*.*)|*.*" };
			if (dlg.ShowDialog(this) == true)
			{
				try
				{
					System.IO.File.WriteAllText(dlg.FileName, ConfigText.Text);
				}
				catch (Exception ex)
				{
					SetStatus("Could not save the file: " + ex.Message);
				}
			}
		}

		private bool ConfigDirty => _folderConfigDir != null && ConfigText.Text != _folderConfigSaved;

		/// <summary>Shows the selected folder's own Configuration_local.hpp (editable) if it has one, else the wizard's.</summary>
		private void UpdateConfigView()
		{
			var dir = _downloadedRelease?.LocalPath;
			if (dir != null && dir == _folderConfigDir)
			{
				return; // already in the editor: keep any unsaved edits
			}

			string existing = null;
			if (dir != null)
			{
				try
				{
					existing = FirmwareBuilder.ReadConfiguration(dir);
				}
				catch (Exception ex)
				{
					Log.WriteLine("FIRMWARE: Could not read the folder's configuration: {0}", ex);
				}
			}

			ShowConfig(existing != null ? dir : null, existing);
		}

		private void ShowConfig(string folderDir, string folderContent)
		{
			_folderConfigDir = folderDir;
			_folderConfigSaved = folderContent;
			var text = folderContent ?? (_wizard.IsComplete ? _wizard.GenerateFileContent() : ""); // also called while the wizard page is still up
			if (ConfigText.Text != text)
			{
				ConfigText.Text = text;
			}

			ConfigText.IsReadOnly = folderDir == null;
			ConfigTitleText.Text = folderDir == null ? "Configuration_local.hpp" : "Configuration_local.hpp (from the folder, editable)";
			SaveConfigButton.Visibility = UseWizardButton.Visibility = folderDir == null ? Visibility.Collapsed : Visibility.Visible;
		}

		private bool SaveFolderConfig()
		{
			try
			{
				if (FirmwareBuilder.ReadConfiguration(_folderConfigDir) != _folderConfigSaved && MessageBox.Show(this,
					"Configuration_local.hpp in the firmware folder was changed by another program. Overwrite it with your changes from this window?",
					"Firmware", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) != MessageBoxResult.Yes)
				{
					SetStatus("Configuration_local.hpp was not saved.");
					return false;
				}

				FirmwareBuilder.WriteConfiguration(_folderConfigDir, ConfigText.Text);
				_folderConfigSaved = ConfigText.Text;
				SetStatus($"Saved Configuration_local.hpp in {_folderConfigDir}.");
				return true;
			}
			catch (Exception ex)
			{
				SetStatus("Could not save Configuration_local.hpp: " + ex.Message);
				return false;
			}
		}

		private void OnSaveFolderConfig(object sender, RoutedEventArgs e)
		{
			if (_busy)
			{
				SetStatus("Busy. Save again when the current operation has finished.");
			}
			else if (_folderConfigDir != null)
			{
				SaveFolderConfig();
			}
		}

		private void OnUseWizardConfig(object sender, RoutedEventArgs e)
		{
			// Only fills the editor; the folder's file changes on Save or a build.
			ConfigText.Text = _wizard.GenerateFileContent();
			SelectEnvironmentForBoard();
		}

		private async void ShowFlashPage()
		{
			WizardPage.Visibility = Visibility.Collapsed;
			FlashPage.Visibility = Visibility.Visible;
			UpdateConfigView();
			var wizardContent = _wizard.GenerateFileContent();
			if (_folderConfigDir != null && _wizardWritten != null && ConfigText.Text == _wizardWritten && wizardContent != _wizardWritten)
			{
				// The folder's file is the wizard configuration a build here wrote, unedited: follow the changed answers.
				ConfigText.Text = _wizardWritten = wizardContent;
			}

			FooterText.Text = $"Wizard steps v{_wizard.SourceVersion} ({_stepsSource}). Firmware is downloaded to {FirmwareBuilder.WorkDirectory}.";

			SetBusy(_busy);
			if (!_pioSearched)
			{
				SetStatus("Looking for PlatformIO...");
				_pioSearch = _pioSearch ?? FlashTools.FindPlatformIOAsync(AppendLog);
				var found = await _pioSearch;
				_pio = _pio ?? found;
				_pioSearched = true;
				SetStatus(ToolsMissing ? "Flash tools are missing. Use Download Flash Tools below." : $"Using PlatformIO at {_pio}");
				SetBusy(_busy);
			}

			var releases = ReleaseCombo.ItemsSource as List<FirmwareRelease>;
			if (releases == null || releases.All(r => r.LocalPath != null))
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
			FlashBackButton.IsEnabled = RestartButton.IsEnabled = AllBranchesCheck.IsEnabled = false;
			try
			{
				var releases = await FirmwareBuilder.GetReleasesAsync(AllBranchesCheck.IsChecked == true);
				var customSelected = IsCustomSelected;
				var previousUrl = (ReleaseCombo.SelectedItem as FirmwareRelease)?.ZipUrl;
				var previous = previousUrl == null ? null : releases.FirstOrDefault(r => r.ZipUrl == previousUrl);
				if (_customRelease != null)
				{
					releases.Add(_customRelease);
				}

				var requested = _selectZipUrl == null ? null : releases.FirstOrDefault(r => r.ZipUrl == _selectZipUrl);
				if (requested == null && _selectZipUrl != null)
				{
					// The announced firmware isn't in the list (for example a release GitHub marks as latest that isn't the newest): add it.
					requested = new FirmwareRelease { Name = "Firmware " + _update.LatestVersion, ZipUrl = _selectZipUrl, IsPrerelease = true };
					releases.Insert(0, requested);
				}

				_selectZipUrl = null;
				ReleaseCombo.ItemsSource = releases;
				if (requested != null)
				{
					// Opened from an update notification: that firmware wins over a saved folder or the wizard's choice.
					_autoSelectedFwVersion = _wizard.GetValue("fwversion");
					ReleaseCombo.SelectedItem = requested;
				}
				else if (customSelected)
				{
					ReleaseCombo.SelectedItem = _customRelease;
					_autoSelectedFwVersion = _wizard.GetValue("fwversion");
					SetStatus($"Using firmware folder {_customRelease.LocalPath}.");
				}
				else if (previous != null)
				{
					// The list was reloaded (all branches switched on or off): keep what was selected.
					ReleaseCombo.SelectedItem = previous;
					SetStatus($"Firmware {previous.Name} selected.");
				}
				else
				{
					SelectReleaseForWizard(releases);
				}
			}
			catch (Exception ex)
			{
				SetStatus("Could not get firmware versions: " + ex.Message);
			}
			finally
			{
				_loadingReleases = false;
				FlashBackButton.IsEnabled = RestartButton.IsEnabled = AllBranchesCheck.IsEnabled = !_busy;
			}
		}

		private async void OnAllBranchesChanged(object sender, RoutedEventArgs e)
		{
			if (!_busy && !_loadingReleases)
			{
				await LoadReleasesAsync();
			}
		}

		private void SelectReleaseForWizard(List<FirmwareRelease> releases)
		{
			// The OAT wizard asks whether to target the official V1.11 line; otherwise use the newest stable release.
			_autoSelectedFwVersion = _wizard.GetValue("fwversion");
			if (IsCustomSelected)
			{
				// A custom folder was picked on purpose; don't switch away from it.
				return;
			}

			var stable = releases.Where(r => !r.IsPrerelease).ToList();
			var wanted = _autoSelectedFwVersion == "O" ? stable.FirstOrDefault(r => r.Name.Contains("1.11.")) : null;
			ReleaseCombo.SelectedItem = wanted ?? stable.FirstOrDefault(); // never an unreleased version automatically
		}

		private bool IsCustomSelected => _customRelease != null && ReleaseCombo.SelectedItem == _customRelease;

		private void OnCustomFolder(object sender, RoutedEventArgs e)
		{
			if (_busy)
			{
				return;
			}

			using (var dlg = new System.Windows.Forms.FolderBrowserDialog())
			{
				dlg.Description = "Select the firmware folder to build (the one containing platformio.ini). It is built in place.";
				dlg.ShowNewFolderButton = false;
				dlg.SelectedPath = _customRelease?.LocalPath ?? string.Empty;
				if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK)
				{
					return;
				}

				if (_busy)
				{
					// A download started while the folder dialog was open; it would overwrite this choice when it finishes.
					SetStatus("Busy. Select the folder again when the current operation has finished.");
					return;
				}

				if (!System.IO.File.Exists(System.IO.Path.Combine(dlg.SelectedPath, "platformio.ini")))
				{
					SetStatus($"{dlg.SelectedPath} is not a firmware folder: it has no platformio.ini.");
					return;
				}

				var workDir = System.IO.Path.GetFullPath(FirmwareBuilder.WorkDirectory).TrimEnd('\\') + "\\";
				if ((System.IO.Path.GetFullPath(dlg.SelectedPath).TrimEnd('\\') + "\\").StartsWith(workDir, StringComparison.OrdinalIgnoreCase))
				{
					SetStatus($"Folders inside {FirmwareBuilder.WorkDirectory} can't be used: they are deleted when a firmware version is downloaded. Copy the folder elsewhere first.");
					return;
				}

				// Replace any earlier custom entry and select the new one, which makes OnReleaseChanged use the folder.
				var releases = (ReleaseCombo.ItemsSource as List<FirmwareRelease> ?? new List<FirmwareRelease>()).Where(r => r.LocalPath == null).ToList();
				_customRelease = new FirmwareRelease { Name = "Folder: " + dlg.SelectedPath, LocalPath = dlg.SelectedPath, IsPrerelease = true };
				releases.Add(_customRelease);
				ReleaseCombo.ItemsSource = releases;
				ReleaseCombo.SelectedItem = _customRelease;
			}
		}

		private async void OnReleaseChanged(object sender, SelectionChangedEventArgs e)
		{
			if (_askingToSave)
			{
				return; // the list changed under the question below; the handler asking it picks up the selection afterwards
			}

			var release = ReleaseCombo.SelectedItem as FirmwareRelease;
			if (release != null && release.LocalPath != _folderConfigDir && ConfigDirty)
			{
				// Switching away from the folder whose configuration was edited here.
				_askingToSave = true;
				try
				{
					var question = $"Save your changes to Configuration_local.hpp in {_folderConfigDir}?";
					while (MessageBox.Show(this, question, "Firmware", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) == MessageBoxResult.Yes && !SaveFolderConfig())
					{
						question = StatusText.Text + "\n\nTry again? No discards your changes.";
					}
				}
				finally
				{
					_askingToSave = false;
				}

				release = ReleaseCombo.SelectedItem as FirmwareRelease;
			}

			if (release?.LocalPath != null)
			{
				try
				{
					EnvCombo.ItemsSource = FirmwareBuilder.ReadEnvironments(release.LocalPath);
					_firmwareDir = release.LocalPath;
					_downloadedRelease = release;
					UpdateConfigView();
					SelectEnvironmentForBoard();
					SetStatus($"Using firmware folder {release.LocalPath}.");
					SaveState();
				}
				catch (Exception ex)
				{
					EnvCombo.ItemsSource = null;
					_firmwareDir = null;
					_downloadedRelease = null;
					if (!ConfigDirty)
					{
						UpdateConfigView(); // unsaved edits stay in the editor, where Save still works
					}

					SetStatus("Could not read the firmware folder: " + ex.Message);
				}

				return;
			}

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
				UpdateConfigView();
				SaveState(); // a custom folder is no longer the choice, even if the download fails
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
			if (_folderConfigDir != null)
			{
				// The folder's configuration decides, not the wizard answers.
				var match = System.Text.RegularExpressions.Regex.Match(ConfigText.Text, @"^[ \t]*#define[ \t]+BOARD[ \t]+(\w+)", System.Text.RegularExpressions.RegexOptions.Multiline);
				board = match.Success ? match.Groups[1].Value : null;
			}

			var environments = EnvCombo.ItemsSource as List<PioEnvironment>;
			EnvCombo.SelectedItem = environments?.FirstOrDefault(env => env.Board == board);
		}

		private async void OnRefreshPorts(object sender, RoutedEventArgs e)
		{
			await RefreshPortsAsync();
		}

		private async Task RefreshPortsAsync()
		{
			var pio = _pio;
			var previous = (PortCombo.SelectedItem as SerialPortEntry)?.Port;
			List<SerialPortEntry> ports;
			try
			{
				ports = pio == null ? System.IO.Ports.SerialPort.GetPortNames().Distinct().Select(p => new SerialPortEntry { Port = p }).ToList() : await FirmwareBuilder.ListSerialPortsAsync(pio);
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

			if (ToolsMissing)
			{
				SetStatus("Flash tools are needed to build. Use Download Flash Tools first.");
				return;
			}

			if (upload && port == null)
			{
				SetStatus("Select the serial port of the board.");
				return;
			}

			if (upload && _mount?.MountConnected == true)
			{
				// This dialog is modal, so the user can't disconnect in the main window: offer to do it here.
				if (MessageBox.Show(this, "OATControl is connected to the mount, which keeps the serial port busy.\n\nDisconnect from the mount now?", "Firmware",
					MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes)
				{
					_mount.Disconnect();
				}

				if (_mount.MountConnected)
				{
					SetStatus("OATControl is connected to the mount. Disconnect first so the serial port is free for flashing.");
					return;
				}
			}

			if (upload)
			{
				// Over ASCOM the driver keeps the port while any other client is connected, so test it for real.
				string portError = null;
				await RunBusyAsync(async token => portError = await FirmwareBuilder.CheckPortFreeAsync(port.Port));
				if (portError != null)
				{
					SetStatus(portError);
					return;
				}
			}

			// A custom folder's own Configuration_local.hpp is used as is, with any edits made here; the wizard's
			// configuration is only written to a folder that has none.
			var content = _wizard.GenerateFileContent();
			var custom = _downloadedRelease?.LocalPath != null;
			var writeConfig = true;
			if (custom)
			{
				string existing;
				try
				{
					existing = FirmwareBuilder.ReadConfiguration(_firmwareDir);
				}
				catch (Exception ex)
				{
					SetStatus("Could not read the firmware folder: " + ex.Message);
					return;
				}

				if (!ConfigDirty)
				{
					// Pick up the file as it is now: it may have been changed, added or deleted in another editor.
					ShowConfig(existing != null ? _firmwareDir : null, existing);
				}
				else if (existing != _folderConfigSaved)
				{
					var answer = MessageBox.Show(this, "Configuration_local.hpp in the firmware folder was changed by another program, and you have unsaved changes here.\n\n" +
						"Yes: overwrite the file with your changes from this window.\nNo: discard your changes and build with the file in the folder.",
						"Firmware", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning, MessageBoxResult.Cancel);
					if (answer == MessageBoxResult.Cancel || answer == MessageBoxResult.None)
					{
						return;
					}

					if (answer == MessageBoxResult.No)
					{
						ShowConfig(existing != null ? _firmwareDir : null, existing);
					}
				}

				if (_folderConfigDir != null)
				{
					content = ConfigText.Text;
					writeConfig = content != existing; // unchanged: leave the file untouched
				}
			}

			// Not checked for a folder's own configuration: a Configuration_local_<board>.hpp there can override it.
			var board = _wizard.GetChosenDefineValue("board");
			if (_folderConfigDir == null && board != null && env.Board != null && board != env.Board)
			{
				SetStatus($"Board '{env.Name}' does not match the configuration ({board}); the firmware would refuse to build.");
				return;
			}

			var pio = _pio;
			await RunBusyAsync(async token =>
			{
				if (writeConfig)
				{
					FirmwareBuilder.WriteConfiguration(_firmwareDir, content);
					if (custom)
					{
						if (_folderConfigDir == null)
						{
							_wizardWritten = content;
						}

						ShowConfig(_firmwareDir, content); // the folder has this file now: show it as the folder's
					}
				}
				else
				{
					AppendLog("Building with the Configuration_local.hpp already in the folder");
				}

				if (custom)
				{
					// No hot patches either: they are for old releases and would edit the user's platformio.ini.
					foreach (var name in FirmwareBuilder.BoardSpecificConfigurations(_firmwareDir))
					{
						AppendLog($"Note: the folder contains {name}, which the firmware uses instead of Configuration_local.hpp for its board");
					}
				}
				else
				{
					FirmwareBuilder.ApplyHotPatches(_firmwareDir, env, AppendLog);
				}

				SetStatus(upload ? $"Building and flashing to {port.Port}... (the first build downloads compilers and can take several minutes)" : "Building... (the first build downloads compilers and can take several minutes)");
				var exitCode = await FirmwareBuilder.BuildAsync(pio, _firmwareDir, env, upload ? port.Port : null, AppendLog, token);
				if (token.IsCancellationRequested)
				{
					SetStatus("Cancelled.");
				}
				else if (exitCode == 0)
				{
					SetStatus(upload ? "Firmware flashed successfully." : "Firmware built successfully.");
					if (upload)
					{
						// A develop build reports the release's version, so the mount can't tell us what it runs. Remember
						// the develop commit flashed here, so the "unreleased changes" notice doesn't announce it again.
						// Flashing anything else forgets it: the mount no longer has that commit.
						var settings = OATControl.ViewModels.AppSettings.Instance;
						var isDevelop = _downloadedRelease?.ZipUrl == FirmwareBuilder.DevelopZipUrl && _downloadedRelease.CommitSha != null;
						var wasCommit = System.Text.RegularExpressions.Regex.IsMatch(settings.SkippedFirmwareVersion ?? "", "^[0-9a-f]{40}$");
						if (isDevelop || wasCommit)
						{
							settings.SkippedFirmwareVersion = isDevelop ? _downloadedRelease.CommitSha : "";
							settings.Save();
						}
					}
				}
				else
				{
					SetStatus($"PlatformIO failed (exit code {exitCode}). See the log above.");
				}
			});
		}

		private void OnEnvChanged(object sender, SelectionChangedEventArgs e)
		{
			UpdateToolsPanel();
		}

		/// <summary>Shows the flash tools download, sized for what is missing, while no usable PlatformIO was found.</summary>
		private void UpdateToolsPanel()
		{
			ToolsPanel.Visibility = ToolsMissing && _pioSearched ? Visibility.Visible : Visibility.Collapsed;
			if (ToolsPanel.Visibility != Visibility.Visible)
			{
				return;
			}

			var missing = _pio == null && FlashTools.NeedsGit ? $"PlatformIO {FlashTools.MinPlatformIOVersion} or newer and Git"
				: _pio == null ? $"PlatformIO {FlashTools.MinPlatformIOVersion} or newer" : "Git";
			var env = EnvCombo.SelectedItem as PioEnvironment;
			if (_firmwareDir == null || env == null)
			{
				ToolsButton.Content = "Download Flash Tools";
				ToolsButton.IsEnabled = false;
				ToolsText.Text = $"Missing on this PC: {missing}, needed to build the firmware. Select the firmware and board first: the download includes the compiler for that board.";
				return;
			}

			var restart = FlashTools.NeedsGit ? " If you install Git yourself instead, restart OATControl." : "";
			var parts = _pio == null && FlashTools.NeedsGit ? "a private Python, PlatformIO and Git" : _pio == null ? "a private Python and PlatformIO" : "a private Git";
			var mb = FlashTools.DownloadSizeMB(_pio, _firmwareDir, env, out var compilerKnown);
			ToolsButton.Content = compilerKnown ? $"Download Flash Tools, about {mb} MB" : $"Download Flash Tools, {mb} MB + compiler";
			ToolsButton.IsEnabled = !_busy;
			ToolsText.Text = $"Missing on this PC: {missing}, needed to build the firmware. This downloads {parts} into {FlashTools.ToolsDirectory}, " +
				$"and the compiler for {env.Name}. Nothing else on this PC is changed.{restart}";
		}

		private async void OnInstallTools(object sender, RoutedEventArgs e)
		{
			var env = EnvCombo.SelectedItem as PioEnvironment;
			if (_busy || !ToolsMissing || _firmwareDir == null || env == null)
			{
				return;
			}

			var progress = new Progress<double>(p =>
			{
				ToolsProgress.IsIndeterminate = p < 0;
				ToolsProgress.Value = Math.Max(0, p);
			});
			ToolsProgress.Visibility = Visibility.Visible;
			await RunBusyAsync(async token =>
			{
				if (_downloadedRelease?.LocalPath == null)
				{
					// The compiler version can depend on them (old releases pin atmelavr@4.2.0).
					FirmwareBuilder.ApplyHotPatches(_firmwareDir, env, AppendLog);
				}

				SetStatus("Downloading flash tools... (the compiler can take several minutes)");
				_pio = await FlashTools.InstallAsync(_pio, _firmwareDir, env, AppendLog, progress, token);
				SetStatus("Flash tools installed. You can build and flash now.");
			});
			ToolsProgress.Visibility = Visibility.Collapsed;
			UpdateToolsPanel();
			await RefreshPortsAsync();
		}

		private void OnCancel(object sender, RoutedEventArgs e)
		{
			_cts?.Cancel();
		}

		private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (ConfigDirty)
			{
				var save = MessageBox.Show(this, $"Save your changes to Configuration_local.hpp in {_folderConfigDir}?", "Firmware",
					MessageBoxButton.YesNoCancel, MessageBoxImage.Question, MessageBoxResult.Yes);
				if (save == MessageBoxResult.Cancel || save == MessageBoxResult.None || (save == MessageBoxResult.Yes && !SaveFolderConfig()))
				{
					e.Cancel = true;
					return;
				}
			}

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

		private void SetBusy(bool busy)
		{
			_busy = busy;
			BuildButton.IsEnabled = !busy && !ToolsMissing;
			FlashButton.IsEnabled = !busy && !ToolsMissing;
			ReleaseCombo.IsEnabled = !busy;
			FolderButton.IsEnabled = !busy;
			AllBranchesCheck.IsEnabled = !busy && !_loadingReleases;
			EnvCombo.IsEnabled = !busy;
			PortCombo.IsEnabled = !busy;
			RefreshButton.IsEnabled = !busy;
			FlashBackButton.IsEnabled = !busy && !_loadingReleases;
			RestartButton.IsEnabled = !busy && !_loadingReleases;
			CancelButton.IsEnabled = busy;
			UpdateToolsPanel();
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
