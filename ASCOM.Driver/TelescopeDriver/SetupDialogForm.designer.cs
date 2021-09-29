namespace ASCOM.OpenAstroTracker
{
    partial class SetupDialogForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
			this.components = new System.ComponentModel.Container();
			this.Label5 = new System.Windows.Forms.Label();
			this.txtElevation = new System.Windows.Forms.TextBox();
			this.Label4 = new System.Windows.Forms.Label();
			this.Label3 = new System.Windows.Forms.Label();
			this.txtLong = new System.Windows.Forms.TextBox();
			this.txtLat = new System.Windows.Forms.TextBox();
			this.Label2 = new System.Windows.Forms.Label();
			this.ComboBoxComPort = new System.Windows.Forms.ComboBox();
			this.chkTrace = new System.Windows.Forms.CheckBox();
			this.PictureBox1 = new System.Windows.Forms.PictureBox();
			this.Label1 = new System.Windows.Forms.Label();
			this.TableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			this.OK_Button = new System.Windows.Forms.Button();
			this.Cancel_Button = new System.Windows.Forms.Button();
			this.pictureBox2 = new System.Windows.Forms.PictureBox();
			this.comboBoxBaudRate = new System.Windows.Forms.ComboBox();
			this.label6 = new System.Windows.Forms.Label();
			this.label7 = new System.Windows.Forms.Label();
			this.label8 = new System.Windows.Forms.Label();
			this.label9 = new System.Windows.Forms.Label();
			this.label10 = new System.Windows.Forms.Label();
			this.label11 = new System.Windows.Forms.Label();
			this.label12 = new System.Windows.Forms.Label();
			this.label13 = new System.Windows.Forms.Label();
			this.label14 = new System.Windows.Forms.Label();
			this.btnConnect = new System.Windows.Forms.Button();
			this.lblFirmware = new System.Windows.Forms.Label();
			this.numRASteps = new System.Windows.Forms.NumericUpDown();
			this.label15 = new System.Windows.Forms.Label();
			this.label16 = new System.Windows.Forms.Label();
			this.numDECSteps = new System.Windows.Forms.NumericUpDown();
			this.label17 = new System.Windows.Forms.Label();
			this.numSpeedFactor = new System.Windows.Forms.NumericUpDown();
			this.label18 = new System.Windows.Forms.Label();
			this.label19 = new System.Windows.Forms.Label();
			this.label20 = new System.Windows.Forms.Label();
			this.lblBoard = new System.Windows.Forms.Label();
			this.label21 = new System.Windows.Forms.Label();
			this.lblDisplay = new System.Windows.Forms.Label();
			this.label22 = new System.Windows.Forms.Label();
			this.lblAddons = new System.Windows.Forms.Label();
			this.label24 = new System.Windows.Forms.Label();
			this.lblLST = new System.Windows.Forms.Label();
			this.btnOPenASCOMLogs = new System.Windows.Forms.Button();
			this.label23 = new System.Windows.Forms.Label();
			this.btnUpdate = new System.Windows.Forms.Button();
			this.label25 = new System.Windows.Forms.Label();
			this.btnNorth = new System.Windows.Forms.Button();
			this.btnSouth = new System.Windows.Forms.Button();
			this.btnEast = new System.Windows.Forms.Button();
			this.btnWest = new System.Windows.Forms.Button();
			this.btnUnparkDEC = new System.Windows.Forms.Button();
			this.btnParkDEC = new System.Windows.Forms.Button();
			this.btnGoHome = new System.Windows.Forms.Button();
			this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
			this.btnSetHome = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.PictureBox1)).BeginInit();
			this.TableLayoutPanel1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.numRASteps)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.numDECSteps)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.numSpeedFactor)).BeginInit();
			this.SuspendLayout();
			// 
			// Label5
			// 
			this.Label5.AutoSize = true;
			this.Label5.Location = new System.Drawing.Point(17, 214);
			this.Label5.Name = "Label5";
			this.Label5.Size = new System.Drawing.Size(72, 13);
			this.Label5.TabIndex = 28;
			this.Label5.Text = "Site Elevation";
			// 
			// txtElevation
			// 
			this.txtElevation.Location = new System.Drawing.Point(94, 211);
			this.txtElevation.Name = "txtElevation";
			this.txtElevation.Size = new System.Drawing.Size(84, 20);
			this.txtElevation.TabIndex = 27;
			// 
			// Label4
			// 
			this.Label4.AutoSize = true;
			this.Label4.Location = new System.Drawing.Point(23, 162);
			this.Label4.Name = "Label4";
			this.Label4.Size = new System.Drawing.Size(66, 13);
			this.Label4.TabIndex = 26;
			this.Label4.Text = "Site Latitude";
			// 
			// Label3
			// 
			this.Label3.AutoSize = true;
			this.Label3.Location = new System.Drawing.Point(14, 188);
			this.Label3.Name = "Label3";
			this.Label3.Size = new System.Drawing.Size(75, 13);
			this.Label3.TabIndex = 25;
			this.Label3.Text = "Site Longitude";
			// 
			// txtLong
			// 
			this.txtLong.Location = new System.Drawing.Point(94, 185);
			this.txtLong.Name = "txtLong";
			this.txtLong.Size = new System.Drawing.Size(84, 20);
			this.txtLong.TabIndex = 24;
			// 
			// txtLat
			// 
			this.txtLat.Location = new System.Drawing.Point(94, 159);
			this.txtLat.Name = "txtLat";
			this.txtLat.Size = new System.Drawing.Size(84, 20);
			this.txtLat.TabIndex = 23;
			// 
			// Label2
			// 
			this.Label2.AutoSize = true;
			this.Label2.Location = new System.Drawing.Point(115, 288);
			this.Label2.Name = "Label2";
			this.Label2.Size = new System.Drawing.Size(153, 13);
			this.Label2.TabIndex = 22;
			this.Label2.Text = "Enable ASCOM Trace Logging";
			// 
			// ComboBoxComPort
			// 
			this.ComboBoxComPort.FormattingEnabled = true;
			this.ComboBoxComPort.Location = new System.Drawing.Point(94, 36);
			this.ComboBoxComPort.Name = "ComboBoxComPort";
			this.ComboBoxComPort.Size = new System.Drawing.Size(84, 21);
			this.ComboBoxComPort.TabIndex = 21;
			this.ComboBoxComPort.SelectedValueChanged += new System.EventHandler(this.ComboBoxComPort_SelectedValueChanged);
			// 
			// chkTrace
			// 
			this.chkTrace.AutoSize = true;
			this.chkTrace.Location = new System.Drawing.Point(94, 288);
			this.chkTrace.Name = "chkTrace";
			this.chkTrace.Size = new System.Drawing.Size(15, 14);
			this.chkTrace.TabIndex = 20;
			this.chkTrace.UseVisualStyleBackColor = true;
			// 
			// PictureBox1
			// 
			this.PictureBox1.Cursor = System.Windows.Forms.Cursors.Hand;
			this.PictureBox1.Image = global::ASCOM.OpenAstroTracker.Properties.Resources.ASCOM;
			this.PictureBox1.Location = new System.Drawing.Point(32, 285);
			this.PictureBox1.Name = "PictureBox1";
			this.PictureBox1.Size = new System.Drawing.Size(48, 56);
			this.PictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
			this.PictureBox1.TabIndex = 19;
			this.PictureBox1.TabStop = false;
			this.PictureBox1.Click += new System.EventHandler(this.ShowAscomWebPage);
			// 
			// Label1
			// 
			this.Label1.AutoSize = true;
			this.Label1.Location = new System.Drawing.Point(36, 39);
			this.Label1.Name = "Label1";
			this.Label1.Size = new System.Drawing.Size(53, 13);
			this.Label1.TabIndex = 18;
			this.Label1.Text = "COM Port";
			// 
			// TableLayoutPanel1
			// 
			this.TableLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.TableLayoutPanel1.ColumnCount = 2;
			this.TableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.TableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.TableLayoutPanel1.Controls.Add(this.OK_Button, 0, 0);
			this.TableLayoutPanel1.Controls.Add(this.Cancel_Button, 1, 0);
			this.TableLayoutPanel1.Location = new System.Drawing.Point(526, 320);
			this.TableLayoutPanel1.Name = "TableLayoutPanel1";
			this.TableLayoutPanel1.RowCount = 1;
			this.TableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.TableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 29F));
			this.TableLayoutPanel1.Size = new System.Drawing.Size(146, 29);
			this.TableLayoutPanel1.TabIndex = 17;
			// 
			// OK_Button
			// 
			this.OK_Button.Anchor = System.Windows.Forms.AnchorStyles.None;
			this.OK_Button.Location = new System.Drawing.Point(3, 3);
			this.OK_Button.Name = "OK_Button";
			this.OK_Button.Size = new System.Drawing.Size(67, 23);
			this.OK_Button.TabIndex = 0;
			this.OK_Button.Text = "OK";
			this.OK_Button.Click += new System.EventHandler(this.OK_Button_Click);
			// 
			// Cancel_Button
			// 
			this.Cancel_Button.Anchor = System.Windows.Forms.AnchorStyles.None;
			this.Cancel_Button.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.Cancel_Button.Location = new System.Drawing.Point(76, 3);
			this.Cancel_Button.Name = "Cancel_Button";
			this.Cancel_Button.Size = new System.Drawing.Size(67, 23);
			this.Cancel_Button.TabIndex = 1;
			this.Cancel_Button.Text = "Cancel";
			this.Cancel_Button.Click += new System.EventHandler(this.Cancel_Button_Click);
			// 
			// pictureBox2
			// 
			this.pictureBox2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.pictureBox2.Cursor = System.Windows.Forms.Cursors.Hand;
			this.pictureBox2.Image = global::ASCOM.OpenAstroTracker.Properties.Resources.OATAscom;
			this.pictureBox2.Location = new System.Drawing.Point(544, 1);
			this.pictureBox2.Name = "pictureBox2";
			this.pictureBox2.Size = new System.Drawing.Size(140, 128);
			this.pictureBox2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
			this.pictureBox2.TabIndex = 29;
			this.pictureBox2.TabStop = false;
			this.pictureBox2.Click += new System.EventHandler(this.ShowOATWebPage);
			// 
			// comboBoxBaudRate
			// 
			this.comboBoxBaudRate.FormattingEnabled = true;
			this.comboBoxBaudRate.Location = new System.Drawing.Point(94, 60);
			this.comboBoxBaudRate.Name = "comboBoxBaudRate";
			this.comboBoxBaudRate.Size = new System.Drawing.Size(84, 21);
			this.comboBoxBaudRate.TabIndex = 31;
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(31, 63);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(58, 13);
			this.label6.TabIndex = 30;
			this.label6.Text = "Baud Rate";
			// 
			// label7
			// 
			this.label7.Location = new System.Drawing.Point(94, 86);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(127, 40);
			this.label7.TabIndex = 32;
			this.label7.Text = "Firmware V1.9.03 and earlier used 57600, while later version use 19200.";
			// 
			// label8
			// 
			this.label8.AutoSize = true;
			this.label8.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label8.Location = new System.Drawing.Point(52, 86);
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size(38, 13);
			this.label8.TabIndex = 33;
			this.label8.Text = "Note:";
			// 
			// label9
			// 
			this.label9.AutoSize = true;
			this.label9.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label9.Location = new System.Drawing.Point(10, 9);
			this.label9.Name = "label9";
			this.label9.Size = new System.Drawing.Size(92, 13);
			this.label9.TabIndex = 34;
			this.label9.Text = "Communication";
			// 
			// label10
			// 
			this.label10.AutoSize = true;
			this.label10.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label10.Location = new System.Drawing.Point(10, 139);
			this.label10.Name = "label10";
			this.label10.Size = new System.Drawing.Size(70, 13);
			this.label10.TabIndex = 35;
			this.label10.Text = "Site details";
			// 
			// label11
			// 
			this.label11.AutoSize = true;
			this.label11.Location = new System.Drawing.Point(182, 214);
			this.label11.Name = "label11";
			this.label11.Size = new System.Drawing.Size(38, 13);
			this.label11.TabIndex = 36;
			this.label11.Text = "meters";
			// 
			// label12
			// 
			this.label12.AutoSize = true;
			this.label12.Location = new System.Drawing.Point(182, 162);
			this.label12.Name = "label12";
			this.label12.Size = new System.Drawing.Size(45, 13);
			this.label12.TabIndex = 37;
			this.label12.Text = "degrees";
			// 
			// label13
			// 
			this.label13.AutoSize = true;
			this.label13.Location = new System.Drawing.Point(182, 188);
			this.label13.Name = "label13";
			this.label13.Size = new System.Drawing.Size(45, 13);
			this.label13.TabIndex = 38;
			this.label13.Text = "degrees";
			// 
			// label14
			// 
			this.label14.AutoSize = true;
			this.label14.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label14.Location = new System.Drawing.Point(10, 260);
			this.label14.Name = "label14";
			this.label14.Size = new System.Drawing.Size(68, 13);
			this.label14.TabIndex = 39;
			this.label14.Text = "Debugging";
			// 
			// btnConnect
			// 
			this.btnConnect.Location = new System.Drawing.Point(215, 36);
			this.btnConnect.Name = "btnConnect";
			this.btnConnect.Size = new System.Drawing.Size(78, 26);
			this.btnConnect.TabIndex = 40;
			this.btnConnect.Text = "Connect";
			this.btnConnect.Click += new System.EventHandler(this.btnConnect_Click);
			// 
			// lblFirmware
			// 
			this.lblFirmware.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.lblFirmware.Location = new System.Drawing.Point(399, 39);
			this.lblFirmware.Name = "lblFirmware";
			this.lblFirmware.Size = new System.Drawing.Size(142, 17);
			this.lblFirmware.TabIndex = 37;
			this.lblFirmware.Text = "-";
			this.lblFirmware.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// numRASteps
			// 
			this.numRASteps.DecimalPlaces = 1;
			this.numRASteps.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
			this.numRASteps.Location = new System.Drawing.Point(408, 190);
			this.numRASteps.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
			this.numRASteps.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.numRASteps.Name = "numRASteps";
			this.numRASteps.Size = new System.Drawing.Size(60, 20);
			this.numRASteps.TabIndex = 41;
			this.numRASteps.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.numRASteps.ValueChanged += new System.EventHandler(this.numUpDown_ValueChanged);
			// 
			// label15
			// 
			this.label15.Location = new System.Drawing.Point(304, 192);
			this.label15.Name = "label15";
			this.label15.Size = new System.Drawing.Size(98, 13);
			this.label15.TabIndex = 42;
			this.label15.Text = "RA Steps/degree";
			this.label15.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// label16
			// 
			this.label16.Location = new System.Drawing.Point(304, 217);
			this.label16.Name = "label16";
			this.label16.Size = new System.Drawing.Size(98, 13);
			this.label16.TabIndex = 44;
			this.label16.Text = "DEC Steps/degree";
			this.label16.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// numDECSteps
			// 
			this.numDECSteps.DecimalPlaces = 1;
			this.numDECSteps.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
			this.numDECSteps.Location = new System.Drawing.Point(408, 215);
			this.numDECSteps.Maximum = new decimal(new int[] {
            9999,
            0,
            0,
            0});
			this.numDECSteps.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.numDECSteps.Name = "numDECSteps";
			this.numDECSteps.Size = new System.Drawing.Size(60, 20);
			this.numDECSteps.TabIndex = 43;
			this.numDECSteps.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.numDECSteps.ValueChanged += new System.EventHandler(this.numUpDown_ValueChanged);
			// 
			// label17
			// 
			this.label17.Location = new System.Drawing.Point(304, 242);
			this.label17.Name = "label17";
			this.label17.Size = new System.Drawing.Size(98, 13);
			this.label17.TabIndex = 46;
			this.label17.Text = "Speed Factor";
			this.label17.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// numSpeedFactor
			// 
			this.numSpeedFactor.DecimalPlaces = 4;
			this.numSpeedFactor.Increment = new decimal(new int[] {
            1,
            0,
            0,
            262144});
			this.numSpeedFactor.Location = new System.Drawing.Point(408, 240);
			this.numSpeedFactor.Maximum = new decimal(new int[] {
            2,
            0,
            0,
            0});
			this.numSpeedFactor.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            65536});
			this.numSpeedFactor.Name = "numSpeedFactor";
			this.numSpeedFactor.Size = new System.Drawing.Size(60, 20);
			this.numSpeedFactor.TabIndex = 45;
			this.numSpeedFactor.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.numSpeedFactor.ValueChanged += new System.EventHandler(this.numUpDown_ValueChanged);
			// 
			// label18
			// 
			this.label18.AutoSize = true;
			this.label18.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label18.Location = new System.Drawing.Point(303, 12);
			this.label18.Name = "label18";
			this.label18.Size = new System.Drawing.Size(109, 13);
			this.label18.TabIndex = 48;
			this.label18.Text = "Mount Information";
			// 
			// label19
			// 
			this.label19.Location = new System.Drawing.Point(295, 39);
			this.label19.Name = "label19";
			this.label19.Size = new System.Drawing.Size(98, 13);
			this.label19.TabIndex = 49;
			this.label19.Text = "Firmware";
			this.label19.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// label20
			// 
			this.label20.Location = new System.Drawing.Point(295, 59);
			this.label20.Name = "label20";
			this.label20.Size = new System.Drawing.Size(98, 13);
			this.label20.TabIndex = 51;
			this.label20.Text = "Controller Board";
			this.label20.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// lblBoard
			// 
			this.lblBoard.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.lblBoard.Location = new System.Drawing.Point(399, 59);
			this.lblBoard.Name = "lblBoard";
			this.lblBoard.Size = new System.Drawing.Size(142, 17);
			this.lblBoard.TabIndex = 50;
			this.lblBoard.Text = "-";
			this.lblBoard.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// label21
			// 
			this.label21.Location = new System.Drawing.Point(295, 79);
			this.label21.Name = "label21";
			this.label21.Size = new System.Drawing.Size(98, 13);
			this.label21.TabIndex = 53;
			this.label21.Text = "Display";
			this.label21.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// lblDisplay
			// 
			this.lblDisplay.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.lblDisplay.Location = new System.Drawing.Point(399, 79);
			this.lblDisplay.Name = "lblDisplay";
			this.lblDisplay.Size = new System.Drawing.Size(142, 17);
			this.lblDisplay.TabIndex = 52;
			this.lblDisplay.Text = "-";
			this.lblDisplay.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// label22
			// 
			this.label22.Location = new System.Drawing.Point(295, 99);
			this.label22.Name = "label22";
			this.label22.Size = new System.Drawing.Size(98, 13);
			this.label22.TabIndex = 55;
			this.label22.Text = "Add-Ons";
			this.label22.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// lblAddons
			// 
			this.lblAddons.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.lblAddons.Location = new System.Drawing.Point(399, 99);
			this.lblAddons.Name = "lblAddons";
			this.lblAddons.Size = new System.Drawing.Size(142, 17);
			this.lblAddons.TabIndex = 54;
			this.lblAddons.Text = "-";
			this.lblAddons.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// label24
			// 
			this.label24.Location = new System.Drawing.Point(290, 127);
			this.label24.Name = "label24";
			this.label24.Size = new System.Drawing.Size(103, 13);
			this.label24.TabIndex = 57;
			this.label24.Text = "Sidereal Time (LST)";
			this.label24.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// lblLST
			// 
			this.lblLST.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.lblLST.Location = new System.Drawing.Point(399, 127);
			this.lblLST.Name = "lblLST";
			this.lblLST.Size = new System.Drawing.Size(142, 17);
			this.lblLST.TabIndex = 56;
			this.lblLST.Text = "-";
			this.lblLST.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// btnOPenASCOMLogs
			// 
			this.btnOPenASCOMLogs.Location = new System.Drawing.Point(93, 316);
			this.btnOPenASCOMLogs.Name = "btnOPenASCOMLogs";
			this.btnOPenASCOMLogs.Size = new System.Drawing.Size(159, 26);
			this.btnOPenASCOMLogs.TabIndex = 58;
			this.btnOPenASCOMLogs.Text = "Open ASCOM Logs folder";
			this.btnOPenASCOMLogs.Click += new System.EventHandler(this.btnOpenASCOMLogs_Click);
			// 
			// label23
			// 
			this.label23.AutoSize = true;
			this.label23.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label23.Location = new System.Drawing.Point(303, 168);
			this.label23.Name = "label23";
			this.label23.Size = new System.Drawing.Size(117, 13);
			this.label23.TabIndex = 59;
			this.label23.Text = "Calibration Settings";
			// 
			// btnUpdate
			// 
			this.btnUpdate.Location = new System.Drawing.Point(391, 265);
			this.btnUpdate.Name = "btnUpdate";
			this.btnUpdate.Size = new System.Drawing.Size(78, 26);
			this.btnUpdate.TabIndex = 60;
			this.btnUpdate.Text = "Update OAT";
			this.btnUpdate.Click += new System.EventHandler(this.btnUpdate_Click);
			// 
			// label25
			// 
			this.label25.AutoSize = true;
			this.label25.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label25.Location = new System.Drawing.Point(507, 168);
			this.label25.Name = "label25";
			this.label25.Size = new System.Drawing.Size(82, 13);
			this.label25.TabIndex = 61;
			this.label25.Text = "Mount Status";
			// 
			// btnNorth
			// 
			this.btnNorth.Image = global::ASCOM.OpenAstroTracker.Properties.Resources.arrow_090;
			this.btnNorth.Location = new System.Drawing.Point(556, 189);
			this.btnNorth.Name = "btnNorth";
			this.btnNorth.Size = new System.Drawing.Size(32, 32);
			this.btnNorth.TabIndex = 62;
			this.btnNorth.Tag = "N";
			this.toolTip1.SetToolTip(this.btnNorth, "Slew DEC North");
			this.btnNorth.UseVisualStyleBackColor = true;
			this.btnNorth.MouseDown += new System.Windows.Forms.MouseEventHandler(this.btnManualSlew_MouseDown);
			this.btnNorth.MouseUp += new System.Windows.Forms.MouseEventHandler(this.btnManualSlew_MouseUp);
			// 
			// btnSouth
			// 
			this.btnSouth.Image = global::ASCOM.OpenAstroTracker.Properties.Resources.arrow_270;
			this.btnSouth.Location = new System.Drawing.Point(556, 259);
			this.btnSouth.Name = "btnSouth";
			this.btnSouth.Size = new System.Drawing.Size(32, 32);
			this.btnSouth.TabIndex = 63;
			this.btnSouth.Tag = "S";
			this.toolTip1.SetToolTip(this.btnSouth, "Slew DEC South");
			this.btnSouth.UseVisualStyleBackColor = true;
			this.btnSouth.MouseDown += new System.Windows.Forms.MouseEventHandler(this.btnManualSlew_MouseDown);
			this.btnSouth.MouseUp += new System.Windows.Forms.MouseEventHandler(this.btnManualSlew_MouseUp);
			// 
			// btnEast
			// 
			this.btnEast.Image = global::ASCOM.OpenAstroTracker.Properties.Resources.arrow;
			this.btnEast.Location = new System.Drawing.Point(591, 224);
			this.btnEast.Name = "btnEast";
			this.btnEast.Size = new System.Drawing.Size(32, 32);
			this.btnEast.TabIndex = 64;
			this.btnEast.Tag = "E";
			this.toolTip1.SetToolTip(this.btnEast, "Slew RA East");
			this.btnEast.UseVisualStyleBackColor = true;
			this.btnEast.MouseDown += new System.Windows.Forms.MouseEventHandler(this.btnManualSlew_MouseDown);
			this.btnEast.MouseUp += new System.Windows.Forms.MouseEventHandler(this.btnManualSlew_MouseUp);
			// 
			// btnWest
			// 
			this.btnWest.Image = global::ASCOM.OpenAstroTracker.Properties.Resources.arrow_180;
			this.btnWest.Location = new System.Drawing.Point(521, 224);
			this.btnWest.Name = "btnWest";
			this.btnWest.Size = new System.Drawing.Size(32, 32);
			this.btnWest.TabIndex = 65;
			this.btnWest.Tag = "W";
			this.toolTip1.SetToolTip(this.btnWest, "Slew RA West");
			this.btnWest.UseVisualStyleBackColor = true;
			this.btnWest.MouseDown += new System.Windows.Forms.MouseEventHandler(this.btnManualSlew_MouseDown);
			this.btnWest.MouseUp += new System.Windows.Forms.MouseEventHandler(this.btnManualSlew_MouseUp);
			// 
			// btnUnparkDEC
			// 
			this.btnUnparkDEC.Location = new System.Drawing.Point(591, 189);
			this.btnUnparkDEC.Name = "btnUnparkDEC";
			this.btnUnparkDEC.Size = new System.Drawing.Size(78, 26);
			this.btnUnparkDEC.TabIndex = 66;
			this.btnUnparkDEC.Text = "Unpark DEC";
			this.toolTip1.SetToolTip(this.btnUnparkDEC, "Move DEC from Park to Home position");
			this.btnUnparkDEC.Click += new System.EventHandler(this.btnUnparkDEC_Click);
			// 
			// btnParkDEC
			// 
			this.btnParkDEC.Location = new System.Drawing.Point(591, 265);
			this.btnParkDEC.Name = "btnParkDEC";
			this.btnParkDEC.Size = new System.Drawing.Size(78, 26);
			this.btnParkDEC.TabIndex = 67;
			this.btnParkDEC.Text = "Park DEC";
			this.toolTip1.SetToolTip(this.btnParkDEC, "Move DEC from Home to Park position");
			this.btnParkDEC.Click += new System.EventHandler(this.btnParkDEC_Click);
			// 
			// btnGoHome
			// 
			this.btnGoHome.Image = global::ASCOM.OpenAstroTracker.Properties.Resources.home;
			this.btnGoHome.Location = new System.Drawing.Point(556, 224);
			this.btnGoHome.Name = "btnGoHome";
			this.btnGoHome.Size = new System.Drawing.Size(32, 32);
			this.btnGoHome.TabIndex = 68;
			this.toolTip1.SetToolTip(this.btnGoHome, "Go Home");
			this.btnGoHome.UseVisualStyleBackColor = true;
			this.btnGoHome.Click += new System.EventHandler(this.btnGoHome_Click);
			// 
			// btnSetHome
			// 
			this.btnSetHome.Image = global::ASCOM.OpenAstroTracker.Properties.Resources.sethome;
			this.btnSetHome.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.btnSetHome.Location = new System.Drawing.Point(504, 265);
			this.btnSetHome.Name = "btnSetHome";
			this.btnSetHome.Size = new System.Drawing.Size(49, 26);
			this.btnSetHome.TabIndex = 69;
			this.btnSetHome.Text = "Set";
			this.btnSetHome.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.toolTip1.SetToolTip(this.btnSetHome, "Set Home position");
			this.btnSetHome.UseVisualStyleBackColor = true;
			this.btnSetHome.Click += new System.EventHandler(this.btnSetHome_Click);
			// 
			// SetupDialogForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.SystemColors.Control;
			this.ClientSize = new System.Drawing.Size(684, 361);
			this.Controls.Add(this.btnSetHome);
			this.Controls.Add(this.btnGoHome);
			this.Controls.Add(this.btnParkDEC);
			this.Controls.Add(this.btnUnparkDEC);
			this.Controls.Add(this.btnWest);
			this.Controls.Add(this.btnEast);
			this.Controls.Add(this.btnSouth);
			this.Controls.Add(this.btnNorth);
			this.Controls.Add(this.label25);
			this.Controls.Add(this.btnUpdate);
			this.Controls.Add(this.label17);
			this.Controls.Add(this.label23);
			this.Controls.Add(this.numRASteps);
			this.Controls.Add(this.btnOPenASCOMLogs);
			this.Controls.Add(this.numSpeedFactor);
			this.Controls.Add(this.label15);
			this.Controls.Add(this.label24);
			this.Controls.Add(this.label16);
			this.Controls.Add(this.lblLST);
			this.Controls.Add(this.numDECSteps);
			this.Controls.Add(this.label22);
			this.Controls.Add(this.lblAddons);
			this.Controls.Add(this.label21);
			this.Controls.Add(this.lblDisplay);
			this.Controls.Add(this.label20);
			this.Controls.Add(this.lblBoard);
			this.Controls.Add(this.label19);
			this.Controls.Add(this.label18);
			this.Controls.Add(this.lblFirmware);
			this.Controls.Add(this.btnConnect);
			this.Controls.Add(this.label14);
			this.Controls.Add(this.label13);
			this.Controls.Add(this.label12);
			this.Controls.Add(this.label11);
			this.Controls.Add(this.label10);
			this.Controls.Add(this.label9);
			this.Controls.Add(this.label8);
			this.Controls.Add(this.label7);
			this.Controls.Add(this.comboBoxBaudRate);
			this.Controls.Add(this.label6);
			this.Controls.Add(this.pictureBox2);
			this.Controls.Add(this.Label5);
			this.Controls.Add(this.txtElevation);
			this.Controls.Add(this.Label4);
			this.Controls.Add(this.Label3);
			this.Controls.Add(this.txtLong);
			this.Controls.Add(this.txtLat);
			this.Controls.Add(this.Label2);
			this.Controls.Add(this.ComboBoxComPort);
			this.Controls.Add(this.chkTrace);
			this.Controls.Add(this.PictureBox1);
			this.Controls.Add(this.Label1);
			this.Controls.Add(this.TableLayoutPanel1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size(700, 400);
			this.Name = "SetupDialogForm";
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "OpenAstroTracker Setup";
			this.Load += new System.EventHandler(this.SetupDialogForm_Load);
			((System.ComponentModel.ISupportInitialize)(this.PictureBox1)).EndInit();
			this.TableLayoutPanel1.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.numRASteps)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.numDECSteps)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.numSpeedFactor)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

        internal System.Windows.Forms.Label Label5;
        internal System.Windows.Forms.TextBox txtElevation;
        internal System.Windows.Forms.Label Label4;
        internal System.Windows.Forms.Label Label3;
        internal System.Windows.Forms.TextBox txtLong;
        internal System.Windows.Forms.TextBox txtLat;
        internal System.Windows.Forms.Label Label2;
        internal System.Windows.Forms.ComboBox ComboBoxComPort;
        internal System.Windows.Forms.CheckBox chkTrace;
        internal System.Windows.Forms.PictureBox PictureBox1;
        internal System.Windows.Forms.Label Label1;
        internal System.Windows.Forms.TableLayoutPanel TableLayoutPanel1;
        internal System.Windows.Forms.Button OK_Button;
        internal System.Windows.Forms.Button Cancel_Button;
		internal System.Windows.Forms.PictureBox pictureBox2;
		internal System.Windows.Forms.ComboBox comboBoxBaudRate;
		internal System.Windows.Forms.Label label6;
		internal System.Windows.Forms.Label label7;
		internal System.Windows.Forms.Label label8;
		internal System.Windows.Forms.Label label9;
		internal System.Windows.Forms.Label label10;
		internal System.Windows.Forms.Label label11;
		internal System.Windows.Forms.Label label12;
		internal System.Windows.Forms.Label label13;
		internal System.Windows.Forms.Label label14;
		internal System.Windows.Forms.Button btnConnect;
		internal System.Windows.Forms.Label lblFirmware;
		private System.Windows.Forms.NumericUpDown numRASteps;
		internal System.Windows.Forms.Label label15;
		internal System.Windows.Forms.Label label16;
		private System.Windows.Forms.NumericUpDown numDECSteps;
		internal System.Windows.Forms.Label label17;
		private System.Windows.Forms.NumericUpDown numSpeedFactor;
		internal System.Windows.Forms.Label label18;
		internal System.Windows.Forms.Label label19;
		internal System.Windows.Forms.Label label20;
		internal System.Windows.Forms.Label lblBoard;
		internal System.Windows.Forms.Label label21;
		internal System.Windows.Forms.Label lblDisplay;
		internal System.Windows.Forms.Label label22;
		internal System.Windows.Forms.Label lblAddons;
		internal System.Windows.Forms.Label label24;
		internal System.Windows.Forms.Label lblLST;
		internal System.Windows.Forms.Button btnOPenASCOMLogs;
		internal System.Windows.Forms.Label label23;
		internal System.Windows.Forms.Button btnUpdate;
		internal System.Windows.Forms.Label label25;
		private System.Windows.Forms.Button btnNorth;
		private System.Windows.Forms.Button btnSouth;
		private System.Windows.Forms.Button btnEast;
		private System.Windows.Forms.Button btnWest;
		internal System.Windows.Forms.Button btnUnparkDEC;
		internal System.Windows.Forms.Button btnParkDEC;
		private System.Windows.Forms.Button btnGoHome;
		private System.Windows.Forms.ToolTip toolTip1;
		private System.Windows.Forms.Button btnSetHome;
	}
}