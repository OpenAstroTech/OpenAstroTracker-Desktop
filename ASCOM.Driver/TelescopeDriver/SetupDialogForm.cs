using ASCOM.DeviceInterface;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ASCOM.OpenAstroTracker
{
	[ComVisible(false)] // Form not registered for COM!
	public partial class SetupDialogForm : Form
	{
		private ProfileData _profile;
		ITelescopeV3 _oat;
		CultureInfo _oatCulture = new CultureInfo("en-US");
		HashSet<string> scopeFeatures;
		Action<string> _logger;

		public SetupDialogForm(ProfileData profile, ITelescopeV3 telescope, Action<string> logger)
		{
			_logger = logger;
			_profile = profile;
			_oat = telescope;
			InitializeComponent();
			btnConnect.Visible = (_oat != null);

			btnUnparkDEC.Visible = false;
			btnParkDEC.Visible = false;

			SlewRate = 4;
			lblStatus.Text = "Disconnected";

			Text = $"OpenAstroTracker Setup - {Version}";
			scopeFeatures = new HashSet<string>();

			EnableAccordingToConnectState();
		}

		private void EnableAccordingToConnectState()
		{
			btnUnparkDEC.Visible = FirmwareVersion > 10933;
			btnParkDEC.Visible = FirmwareVersion > 10933;

			if ((_oat != null) && (_oat.Connected))
			{
				numDECSteps.Enabled = true;
				numRASteps.Enabled = true;
				numSpeedFactor.Enabled = true;
				btnUpdate.Enabled = true;
				btnNorth.Enabled = true;
				btnSouth.Enabled = true;
				btnEast.Enabled = true;
				btnWest.Enabled = true;
				btnGoHome.Enabled = true;
				btnSetHome.Enabled = true;

				if (scopeFeatures.Contains("Focuser"))
				{
					btnFocusIn.Enabled = true;
					btnFocusOut.Enabled = true;
					lblFocusPosition.ForeColor = System.Drawing.SystemColors.ControlText;
					lblFocusPosition.Enabled = true;
					lblFocusRequired.Visible = false;
				}
				else
				{
					btnFocusIn.Enabled = false;
					btnFocusOut.Enabled = false;
					lblFocusPosition.Enabled = false;
					lblFocusPosition.ForeColor = System.Drawing.SystemColors.GrayText;
					lblFocusRequired.Visible = true;
				}

				if (scopeFeatures.Contains("AutoPA") || scopeFeatures.Contains("MotorALT"))
				{
					btnAltDown.Enabled = true;
					btnAltUp.Enabled = true;
				}
				else
				{
					btnAltDown.Enabled = false;
					btnAltUp.Enabled = false;
				}

				if (scopeFeatures.Contains("AutoPA") || scopeFeatures.Contains("MotorAZ"))
				{
					btnAzLeft.Enabled = true;
					btnAzRight.Enabled = true;
				}
				else
				{
					btnAzLeft.Enabled = false;
					btnAzRight.Enabled = false;
				}

				if (scopeFeatures.Contains("AutoPA") || scopeFeatures.Contains("MotorAZ") || scopeFeatures.Contains("MotorALT"))
				{
					lblAutoPARequired.Visible = false;
				}
				else
				{
					lblAutoPARequired.Visible = true;
				}

				btnUnparkDEC.Enabled = true;
				btnParkDEC.Enabled = true;
				rdoRateOne.Enabled = true;
				rdoRateTwo.Enabled = true;
				rdoRateThree.Enabled = true;
				rdoRateFour.Enabled = true;
			}
			else
			{
				lblLST.Text = "-";
				lblAddons.Text = "-";
				lblBoard.Text = "-";
				lblDisplay.Text = "-";
				lblFirmware.Text = "-";
				lblRACoordinate.Text = "-";
				lblDECCoordinate.Text = "-";
				lblRAPosition.Text = "-";
				lblDECPosition.Text = "-";
				lblFocusPosition.Text = "-";
				numDECSteps.Enabled = false;
				numRASteps.Enabled = false;
				numSpeedFactor.Enabled = false;
				btnUpdate.Enabled = false;
				btnNorth.Enabled = false;
				btnSouth.Enabled = false;
				btnEast.Enabled = false;
				btnWest.Enabled = false;
				btnGoHome.Enabled = false;
				btnUnparkDEC.Enabled = false;
				btnParkDEC.Enabled = false;
				btnSetHome.Enabled = false;
				btnAzLeft.Enabled = false;
				lblAutoPARequired.Visible = true;
				lblAutoPARequired.ForeColor = System.Drawing.SystemColors.GrayText;
				btnAzRight.Enabled = false;
				btnAltDown.Enabled = false;
				btnAltUp.Enabled = false;
				btnFocusIn.Enabled = false;
				btnFocusOut.Enabled = false;
				lblFocusPosition.Enabled = false;
				lblFocusPosition.ForeColor= System.Drawing.SystemColors.GrayText;
				lblFocusRequired.Visible = true;
				rdoRateOne.Enabled = false;
				rdoRateTwo.Enabled = false;
				rdoRateThree.Enabled = false;
				rdoRateFour.Enabled = false;
			}
		}

		private string Version => GetType().Assembly.GetName().Version.ToString();

		public int SlewRate { get; private set; }
		public long FirmwareVersion { get; private set; }

		private void OK_Button_Click(System.Object sender, System.EventArgs e) // OK button event handler
		{
			if ((this._oat != null) && (this._oat.Connected))
			{
				this._oat.Connected = false;
				this._oat = null;
			}

			// Persist new values of user settings to the ASCOM profile
			// Update the state variables with results from the dialogue
			_profile.ComPort = (string)ComboBoxComPort.SelectedItem;
			_profile.BaudRate = Convert.ToInt64(comboBoxBaudRate.SelectedItem);
			_profile.TraceState = chkTrace.Checked;
			_profile.Latitude = System.Convert.ToDouble(txtLat.Text);
			_profile.Longitude = System.Convert.ToDouble(txtLong.Text);
			_profile.Elevation = System.Convert.ToInt32(txtElevation.Text);
			this.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.Close();
		}

		public ProfileData GetProfileData() => _profile;

		private void Cancel_Button_Click(System.Object sender, System.EventArgs e) // Cancel button event handler
		{
			if ((this._oat != null) && (this._oat.Connected))
			{
				this._oat.Connected = false;
				this._oat = null;
			}

			this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.Close();
		}

		private void ShowAscomWebPage(System.Object sender, System.EventArgs e)
		{
			// Click on ASCOM logo event handler
			try
			{
				System.Diagnostics.Process.Start("http://ascom-standards.org/");
			}
			catch (Win32Exception noBrowser)
			{
				if (noBrowser.ErrorCode == -2147467259)
					MessageBox.Show(noBrowser.Message);
			}
			catch (Exception other)
			{
				MessageBox.Show(other.Message);
			}
		}

		private void ShowOATWebPage(System.Object sender, System.EventArgs e)
		{
			// Click on OAT logo event handler
			try
			{
				System.Diagnostics.Process.Start("https://wiki.openastrotech.com/");
			}
			catch (Win32Exception noBrowser)
			{
				if (noBrowser.ErrorCode == -2147467259)
					MessageBox.Show(noBrowser.Message);
			}
			catch (Exception other)
			{
				MessageBox.Show(other.Message);
			}
		}

		private void SetupDialogForm_Load(System.Object sender, System.EventArgs e) // Form load event handler
		{
			// Retrieve current values of user settings from the ASCOM Profile
			InitUI();
		}

		private void InitUI()
		{
			chkTrace.Checked = _profile.TraceState;

			// Set the list of com ports to those that are currently available using System.IO because it's static
			ComboBoxComPort.Items.Clear();
			ComboBoxComPort.Items.AddRange(System.IO.Ports.SerialPort.GetPortNames());
			comboBoxBaudRate.Items.AddRange(new string[]
				{
					"230400",
					"115200",
					"57600",
					"38400",
					"28800",
					"19200",
					"14400",
					"9600",
					"4800",
					"2400",
					"1200",
					"300",
				});

			// Select the current port and baudrate if possible...
			if (ComboBoxComPort.Items.Contains(_profile.ComPort))
			{
				ComboBoxComPort.SelectedItem = _profile.ComPort;
				btnConnect.Enabled = true;
			}
			else
			{
				btnConnect.Enabled = false;
			}

			if (comboBoxBaudRate.Items.Contains(_profile.BaudRate.ToString()))
			{
				comboBoxBaudRate.SelectedItem = _profile.BaudRate.ToString();
			}
			else
			{
				btnConnect.Enabled = false;
			}

			txtLat.Text = _profile.Latitude.ToString();
			txtLong.Text = _profile.Longitude.ToString();
			txtElevation.Text = _profile.Elevation.ToString();
		}

		private void txtLat_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar) && e.KeyChar != '.' && e.KeyChar != ',' &&
				e.KeyChar != '-')
				e.Handled = true;
		}

		private void txtLong_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar) && e.KeyChar != '.' && e.KeyChar != ',' &&
				e.KeyChar != '-')
				e.Handled = true;
		}

		private void txtElevation_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar))
				e.Handled = true;
		}

		private bool TryParseDec(string dec, out double dDec)
		{
			try
			{
				var parts = dec.Split('*', '\'');
				dDec = int.Parse(parts[0]) + int.Parse(parts[1]) / 60.0;
				if (parts.Length > 2)
				{
					dDec += int.Parse(parts[2]) / 3600.0;
				}

				return true;
			}
			catch (Exception)
			{
			}

			dDec = 0;
			return false;
		}

		private void btnConnect_Click(object sender, EventArgs e)
		{
			Cursor.Current = Cursors.WaitCursor;

			if (btnConnect.Text == "Connect")
			{
				lblStatus.Text = "Connecting...";
				lblStatus.Update();

				_profile.ComPort = (string)ComboBoxComPort.SelectedItem;
				_profile.BaudRate = Convert.ToInt64(comboBoxBaudRate.SelectedItem);
				SharedResources.WriteProfile(GetProfileData());

				try
				{
					this._oat.Connected = true;
					string fwVersion = this._oat.Action("Serial:PassThroughCommand", ":GVN#,#");
					if (string.IsNullOrEmpty(fwVersion))
					{
						MessageBox.Show("Unable to communicate with OAT, even though it says we're connected. Exit the app and use Task Manager to kill the ASCOM.OpenAstroTracker process. Then try again.");
						return;
					}

					var versionNumbers = fwVersion.Substring(1).Split(".".ToCharArray());
					FirmwareVersion = long.Parse(versionNumbers[0]) * 10000L + long.Parse(versionNumbers[1]) * 100L + long.Parse(versionNumbers[2]);

					lblFirmware.Text = fwVersion;
					string hardware = this._oat.Action("Serial:PassThroughCommand", ":XGM#,#");
					var hwParts = hardware.Split(',');
					lblBoard.Text = hwParts[0];
					string scopeDisplay = "";
					for (int i = 3; i < hwParts.Length; i++)
					{
						if (hwParts[i] == "GPS")
						{
							scopeFeatures.Add("GPS");
						}
						else if (hwParts[i] == "AUTO_AZ_ALT")
						{
							scopeFeatures.Add("AutoPA");
						}
						else if (hwParts[i] == "AUTO_ALT")
						{
							scopeFeatures.Add("MotorALT");
						}
						else if (hwParts[i] == "AUTO_AZ")
						{
							scopeFeatures.Add("MotorAZ");
						}
						else if (hwParts[i] == "GYRO")
						{
							scopeFeatures.Add("Digital Level");
						}
						else if (hwParts[i] == "LCD_KEYPAD")
						{
							scopeDisplay = "16x2 LCD (w/ buttons) ";
						}
						else if (hwParts[i] == "LCD_I2C_MCP23008")
						{
							scopeDisplay = "LCD (I2C MCP23008) ";
						}
						else if (hwParts[i] == "LCD_I2C_MCP23017")
						{
							scopeDisplay = "LCD (I2C MCP23017)";
						}
						else if (hwParts[i] == "LCD_JOY_I2C_SSD1306")
						{
							scopeDisplay = "Pixel OLED (SSD1306)";
						}
						else if (hwParts[i] == "FOC")
						{
							scopeFeatures.Add("Focuser");
						}
					}

					if (!scopeFeatures.Any())
					{
						lblAddons.Text = "No addons";
					}
					else
					{
						lblAddons.Text = string.Join(", ", scopeFeatures);
					}

					if (string.IsNullOrEmpty(scopeDisplay))
					{
						scopeDisplay = "No display";
					}

					lblDisplay.Text = scopeDisplay;

					string steps = _oat.Action("Serial:PassThroughCommand", ":XGR#,#");
					numRASteps.Value = (Decimal)Math.Max(1, float.Parse(steps, _oatCulture));

					steps = _oat.Action("Serial:PassThroughCommand", ":XGD#,#");
					numDECSteps.Value = (Decimal)Math.Max(1, float.Parse(steps, _oatCulture));

					string speedFactor = _oat.Action("Serial:PassThroughCommand", ":XGS#,#");
					numSpeedFactor.Value = (Decimal)float.Parse(speedFactor, _oatCulture);

					string lst = _oat.Action("Serial:PassThroughCommand", ":XGL#,#");
					if (lst.Length == 6)
					{
						lst = $"{lst.Substring(0, 2)}:{lst.Substring(2, 2)}:{lst.Substring(4)}";
					}
					lblLST.Text = lst;

					string lon = _oat.Action("Serial:PassThroughCommand", ":Gg#,#");
					double longitude;
					if (TryParseDec(lon, out longitude))
					{
						longitude = 180 - longitude;
					}
					txtLong.Text = longitude.ToString("0.00");
					_profile.Longitude = longitude;

					string lat = _oat.Action("Serial:PassThroughCommand", ":Gt#,#");
					double latitude;
					TryParseDec(lat, out latitude);
					txtLat.Text = latitude.ToString("0.00");
					_profile.Latitude = latitude;

					btnConnect.Text = "Disconnect";
					btnUpdate.Enabled = false;
					rdoRateFour.Checked = true;
					SlewRate = 4;
					lblStatus.Text = "Connected";
					lblStatus.Update();
					timerMountUpdate.Start();
				}
				catch (Exception ex)
				{
					lblStatus.Text = "Connection failed";
					lblStatus.Update();
					MessageBox.Show("Exception:" + ex.Message);
				}
			}
			else
			{
				timerMountUpdate.Stop();
				this._oat.Connected = false;
				lblFirmware.Text = "-";
				btnConnect.Text = "Connect";
				lblStatus.Text = "Disconnected";
			}

			EnableAccordingToConnectState();

			Cursor.Current = Cursors.Arrow;
		}

		private void numUpDown_ValueChanged(object sender, EventArgs e)
		{
			btnUpdate.Enabled = true;
		}

		private void btnUpdate_Click(object sender, EventArgs e)
		{
			Cursor.Current = Cursors.WaitCursor;
			this._oat.Action("Serial:PassThroughCommand", string.Format(_oatCulture, ":XSR{0:0.0}#", numRASteps.Value));
			this._oat.Action("Serial:PassThroughCommand", string.Format(_oatCulture, ":XSD{0:0.0}#", numDECSteps.Value));
			this._oat.Action("Serial:PassThroughCommand", string.Format(_oatCulture, ":XSS{0:0.0000}#", numSpeedFactor.Value));
			Cursor.Current = Cursors.Arrow;
		}

		private void btnOpenASCOMLogs_Click(object sender, EventArgs e)
		{
			string logsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ASCOM");
			string todaysLogs = $"Logs {DateTime.Now.Year}-{DateTime.Now.Month}-{DateTime.Now.Day}";
			if (File.Exists(Path.Combine(logsFolder, todaysLogs)))
			{
				logsFolder = Path.Combine(logsFolder, todaysLogs);
			}

			Process.Start(logsFolder);
		}

		private void ComboBoxComPort_SelectedValueChanged(object sender, EventArgs e)
		{
			_profile.ComPort = (string)ComboBoxComPort.SelectedItem;
			btnConnect.Enabled = (_oat != null) && !string.IsNullOrEmpty(_profile.ComPort);
		}

		private void btnManualSlew_MouseDown(object sender, MouseEventArgs e)
		{
			char direction = ' ';
			if (sender == btnNorth)
			{
				direction = 'n';
			}
			else if (sender == btnSouth)
			{
				direction = 's';
			}
			else if (sender == btnWest)
			{
				direction = 'w';
			}
			else if (sender == btnEast)
			{
				direction = 'e';
			}
			else
			{
				return;
			}
			this._oat.Action("Serial:PassThroughCommand", $":M{direction}#");
		}

		private void btnManualSlew_MouseUp(object sender, MouseEventArgs e)
		{
			char direction = ' ';
			if (sender == btnNorth)
			{
				direction = 'n';
			}
			else if (sender == btnSouth)
			{
				direction = 's';
			}
			else if (sender == btnWest)
			{
				direction = 'w';
			}
			else if (sender == btnEast)
			{
				direction = 'e';
			}
			else
			{
				return;
			}

			this._oat.Action("Serial:PassThroughCommand", $":Q{direction}#");
		}

		private void btnUnparkDEC_Click(object sender, EventArgs e)
		{
			_logger($"Unpark clicked");
			string status = this._oat.Action("Serial:PassThroughCommand", ":GX#,#");
			_logger($"Unpark - GX returned {status}");
			var statusParts = status.Split(',');
			_logger($"Unpark - DEC is {statusParts[3]}");
			long decSteps = long.Parse(statusParts[3]);
			if (decSteps == 0)
			{
				_logger($"Unpark - DEC parsed {decSteps}, getting offset");
				status = this._oat.Action("Serial:PassThroughCommand", ":XGDP#,#");
				_logger($"Unpark - XGDP returned {status}");
				decSteps = long.Parse(status);
				_logger($"Unpark - XGDP dec offset is {decSteps}");
				if (decSteps != 0)
				{
					this._oat.Action("Serial:PassThroughCommand", $":MXd{decSteps}#,n");
					MessageBox.Show("When slew has stopped, click Set Home.", "Unparking", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
				else
				{
					MessageBox.Show("Mount has not been configured for DEC parking. Manually slew to home position, then Shift-click Set Home to store parking offset.", "Parking", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				}
			}
			else
			{
				MessageBox.Show("Mount has moved since power on, unable to unpark.", "Unparking", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}

		private void btnParkDEC_Click(object sender, EventArgs e)
		{
			_logger($"Park clicked");
			string status = this._oat.Action("Serial:PassThroughCommand", ":GX#,#");
			_logger($"Park - GX returned {status}");
			var statusParts = status.Split(',');
			_logger($"Park - DEC is {statusParts[3]}");
			long decSteps = long.Parse(statusParts[3]);
			if (decSteps == 0)
			{
				_logger($"Park - DEC parsed {decSteps}, getting offset");
				status = this._oat.Action("Serial:PassThroughCommand", ":XGDP#,#");
				_logger($"Park - XGDP returned {status}");
				decSteps = long.Parse(status);
				_logger($"Park - XGDP dec offset is {decSteps}");
				if (decSteps != 0)
				{
					this._oat.Action("Serial:PassThroughCommand", $":MXd{-decSteps}#,n");
				}
				else
				{
					MessageBox.Show("Mount has not been configured for DEC parking.", "Parking", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				}
			}
			else
			{
				MessageBox.Show("Mount is not in home position, unable to park.", "Parking", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}

		private void btnGoHome_Click(object sender, EventArgs e)
		{
			this._oat.Action("Serial:PassThroughCommand", ":hF#");
		}

		private void btnSetHome_Click(object sender, EventArgs e)
		{
			_logger($"SetHome clicked");

			if ((Control.ModifierKeys & Keys.Shift) == Keys.Shift)
			{
				string status = this._oat.Action("Serial:PassThroughCommand", ":GX#,#");
				_logger($"GX returned {status}");
				var statusParts = status.Split(',');
				_logger($"DEC is {statusParts[3]}");
				long decSteps = long.Parse(statusParts[3]);
				if (decSteps != 0)
				{
					var result = MessageBox.Show($"Mount has moved {decSteps} in DEC. Is this the distance from parked position to home?", "Confirm Parking DEC offset", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
					if (result == DialogResult.Yes)
					{
						this._oat.Action("Serial:PassThroughCommand", string.Format(_oatCulture, ":XSDP{0}#", -decSteps));
					}
				}
				else
				{
					MessageBox.Show("Mount reports no DEC movement. Nothing to do.", "Set Parking DEC offset", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				}
			}
			else
			{
				this._oat.Action("Serial:PassThroughCommand", ":SHP#,n");
			}
		}

		private void btnFocus_MouseDown(object sender, MouseEventArgs e)
		{
			string sign = (sender == btnFocusIn) ? "+" : "-";
			this._oat.Action("Serial:PassThroughCommand", string.Format(_oatCulture, ":F{0}#", SlewRate));
			this._oat.Action("Serial:PassThroughCommand", string.Format(_oatCulture, ":F{0}#", sign));
		}

		private void btnFocus_MouseUp(object sender, MouseEventArgs e)
		{
			this._oat.Action("Serial:PassThroughCommand", string.Format(_oatCulture, ":FQ#", SlewRate));
		}

		private void rdoRates_CheckedChanged(object sender, EventArgs e)
		{
			if (sender == rdoRateFour)
			{
				SlewRate = 4;
			}
			else if (sender == rdoRateThree)
			{
				SlewRate = 3;
			}
			else if (sender == rdoRateTwo)
			{
				SlewRate = 2;
			}
			else if (sender == rdoRateOne)
			{
				SlewRate = 1;
			}

		}

		private void btnAltAz_MouseDown(object sender, MouseEventArgs e)
		{
			float[] rateDistance = { 0, 0.25f, 2.0f, 7.5f, 30.0f };
			float distance = rateDistance[SlewRate];
			string sign = (sender == btnAltUp) || (sender == btnAzLeft) ? "+" : "-";
			if ((sender == btnAltUp) || (sender == btnAltDown))
			{
				this._oat.Action("Serial:PassThroughCommand", string.Format(_oatCulture, ":MAL{0}{1:0.0}#", sign, distance));
			}
			else
			{
				this._oat.Action("Serial:PassThroughCommand", string.Format(_oatCulture, ":MAZ{0}{1:0.0}#", sign, distance));
			}

		}

		private void btnAltAz_MouseUp(object sender, MouseEventArgs e)
		{
			// NOP, because ALT and AZ movement is set distance
		}

		private void lblFocusPosition_Click(object sender, EventArgs e)
		{
			if ((_oat != null) && (_oat.Connected))
			{
				if ((Control.ModifierKeys & Keys.Shift) == Keys.Shift)
				{
					if (FirmwareVersion > 10918)
					{
						this._oat.Action("Serial:PassThroughCommand", ":FP50000#,n");
					}
				}
			}
		}

		private void timerMountUpdate_Tick(object sender, EventArgs e)
		{
			timerMountUpdate.Stop();
			string status = this._oat.Action("Serial:PassThroughCommand", ":GX#,#");
			var statusParts = status.Split(',');
			lblStatus.Text = statusParts[0];

			string lst = _oat.Action("Serial:PassThroughCommand", ":XGL#,#");
			if (lst.Length == 6)
			{
				lst = $"{lst.Substring(0, 2)}:{lst.Substring(2, 2)}:{lst.Substring(4)}";
			}
			lblLST.Text = lst;
			lblRACoordinate.Text = $"{statusParts[5].Substring(0, 2)}h {statusParts[5].Substring(2, 2)}m {statusParts[5].Substring(4, 2)}s";
			lblDECCoordinate.Text = $"{statusParts[6].Substring(1, 2)}° {statusParts[6].Substring(3, 2)}\" {statusParts[6].Substring(5, 2)}'";
			lblRAPosition.Text = statusParts[2];
			lblDECPosition.Text = statusParts[3];
			if (statusParts.Length > 8)
			{
				lblFocusPosition.Text = statusParts[7];
			}

			timerMountUpdate.Start();
		}
	}
}