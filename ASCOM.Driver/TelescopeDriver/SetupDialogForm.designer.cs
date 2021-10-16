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
			this.btnAzLeft = new System.Windows.Forms.Button();
			this.btnAzRight = new System.Windows.Forms.Button();
			this.btnAltDown = new System.Windows.Forms.Button();
			this.btnAltUp = new System.Windows.Forms.Button();
			this.btnFocusOut = new System.Windows.Forms.Button();
			this.btnFocusIn = new System.Windows.Forms.Button();
			this.rdoRateOne = new System.Windows.Forms.RadioButton();
			this.rdoRateTwo = new System.Windows.Forms.RadioButton();
			this.rdoRateThree = new System.Windows.Forms.RadioButton();
			this.rdoRateFour = new System.Windows.Forms.RadioButton();
			this.lblFocusPosition = new System.Windows.Forms.Label();
			this.lblAzAltControl = new System.Windows.Forms.Label();
			this.label29 = new System.Windows.Forms.Label();
			this.lblFocusControl = new System.Windows.Forms.Label();
			this.label31 = new System.Windows.Forms.Label();
			this.lblRateFastest = new System.Windows.Forms.Label();
			this.lblRateSlowest = new System.Windows.Forms.Label();
			this.label34 = new System.Windows.Forms.Label();
			this.lblFocusRequired = new System.Windows.Forms.Label();
			this.lblAutoPARequired = new System.Windows.Forms.Label();
			this.timerMountUpdate = new System.Windows.Forms.Timer(this.components);
			this.label26 = new System.Windows.Forms.Label();
			this.lblStatus = new System.Windows.Forms.Label();
			this.lblRAPosition = new System.Windows.Forms.Label();
			this.label28 = new System.Windows.Forms.Label();
			this.label30 = new System.Windows.Forms.Label();
			this.lblDECPosition = new System.Windows.Forms.Label();
			this.label27 = new System.Windows.Forms.Label();
			this.lblRACoordinate = new System.Windows.Forms.Label();
			this.label33 = new System.Windows.Forms.Label();
			this.label35 = new System.Windows.Forms.Label();
			this.lblDECCoordinate = new System.Windows.Forms.Label();
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
			this.Label5.Location = new System.Drawing.Point(17, 272);
			this.Label5.Name = "Label5";
			this.Label5.Size = new System.Drawing.Size(72, 13);
			this.Label5.TabIndex = 28;
			this.Label5.Text = "Site Elevation";
			// 
			// txtElevation
			// 
			this.txtElevation.Location = new System.Drawing.Point(94, 269);
			this.txtElevation.Name = "txtElevation";
			this.txtElevation.Size = new System.Drawing.Size(84, 20);
			this.txtElevation.TabIndex = 27;
			// 
			// Label4
			// 
			this.Label4.AutoSize = true;
			this.Label4.Location = new System.Drawing.Point(23, 220);
			this.Label4.Name = "Label4";
			this.Label4.Size = new System.Drawing.Size(66, 13);
			this.Label4.TabIndex = 26;
			this.Label4.Text = "Site Latitude";
			// 
			// Label3
			// 
			this.Label3.AutoSize = true;
			this.Label3.Location = new System.Drawing.Point(14, 246);
			this.Label3.Name = "Label3";
			this.Label3.Size = new System.Drawing.Size(75, 13);
			this.Label3.TabIndex = 25;
			this.Label3.Text = "Site Longitude";
			// 
			// txtLong
			// 
			this.txtLong.Location = new System.Drawing.Point(94, 243);
			this.txtLong.Name = "txtLong";
			this.txtLong.Size = new System.Drawing.Size(84, 20);
			this.txtLong.TabIndex = 24;
			// 
			// txtLat
			// 
			this.txtLat.Location = new System.Drawing.Point(94, 217);
			this.txtLat.Name = "txtLat";
			this.txtLat.Size = new System.Drawing.Size(84, 20);
			this.txtLat.TabIndex = 23;
			// 
			// Label2
			// 
			this.Label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.Label2.AutoSize = true;
			this.Label2.Location = new System.Drawing.Point(194, 419);
			this.Label2.Name = "Label2";
			this.Label2.Size = new System.Drawing.Size(153, 13);
			this.Label2.TabIndex = 22;
			this.Label2.Text = "Enable ASCOM Trace Logging";
			// 
			// ComboBoxComPort
			// 
			this.ComboBoxComPort.FormattingEnabled = true;
			this.ComboBoxComPort.Location = new System.Drawing.Point(94, 30);
			this.ComboBoxComPort.Name = "ComboBoxComPort";
			this.ComboBoxComPort.Size = new System.Drawing.Size(84, 21);
			this.ComboBoxComPort.TabIndex = 21;
			this.ComboBoxComPort.SelectedValueChanged += new System.EventHandler(this.ComboBoxComPort_SelectedValueChanged);
			// 
			// chkTrace
			// 
			this.chkTrace.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.chkTrace.AutoSize = true;
			this.chkTrace.Location = new System.Drawing.Point(173, 419);
			this.chkTrace.Name = "chkTrace";
			this.chkTrace.Size = new System.Drawing.Size(15, 14);
			this.chkTrace.TabIndex = 20;
			this.chkTrace.UseVisualStyleBackColor = true;
			// 
			// PictureBox1
			// 
			this.PictureBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.PictureBox1.Cursor = System.Windows.Forms.Cursors.Hand;
			this.PictureBox1.Image = global::ASCOM.OpenAstroTracker.Properties.Resources.ASCOM;
			this.PictureBox1.Location = new System.Drawing.Point(172, 338);
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
			this.Label1.Location = new System.Drawing.Point(36, 33);
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
			this.TableLayoutPanel1.Location = new System.Drawing.Point(626, 435);
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
			this.pictureBox2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.pictureBox2.Cursor = System.Windows.Forms.Cursors.Hand;
			this.pictureBox2.Image = global::ASCOM.OpenAstroTracker.Properties.Resources.OATAscom;
			this.pictureBox2.Location = new System.Drawing.Point(12, 330);
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
			this.comboBoxBaudRate.Location = new System.Drawing.Point(94, 54);
			this.comboBoxBaudRate.Name = "comboBoxBaudRate";
			this.comboBoxBaudRate.Size = new System.Drawing.Size(84, 21);
			this.comboBoxBaudRate.TabIndex = 31;
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(31, 57);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(58, 13);
			this.label6.TabIndex = 30;
			this.label6.Text = "Baud Rate";
			// 
			// label7
			// 
			this.label7.Location = new System.Drawing.Point(94, 80);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(127, 40);
			this.label7.TabIndex = 32;
			this.label7.Text = "Firmware V1.9.03 and earlier used 57600, while later version use 19200.";
			// 
			// label8
			// 
			this.label8.AutoSize = true;
			this.label8.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label8.Location = new System.Drawing.Point(52, 80);
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size(38, 13);
			this.label8.TabIndex = 33;
			this.label8.Text = "Note:";
			// 
			// label9
			// 
			this.label9.AutoSize = true;
			this.label9.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label9.Location = new System.Drawing.Point(10, 10);
			this.label9.Name = "label9";
			this.label9.Size = new System.Drawing.Size(92, 13);
			this.label9.TabIndex = 34;
			this.label9.Text = "Communication";
			// 
			// label10
			// 
			this.label10.AutoSize = true;
			this.label10.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label10.Location = new System.Drawing.Point(10, 197);
			this.label10.Name = "label10";
			this.label10.Size = new System.Drawing.Size(70, 13);
			this.label10.TabIndex = 35;
			this.label10.Text = "Site details";
			// 
			// label11
			// 
			this.label11.AutoSize = true;
			this.label11.Location = new System.Drawing.Point(182, 272);
			this.label11.Name = "label11";
			this.label11.Size = new System.Drawing.Size(38, 13);
			this.label11.TabIndex = 36;
			this.label11.Text = "meters";
			// 
			// label12
			// 
			this.label12.AutoSize = true;
			this.label12.Location = new System.Drawing.Point(182, 220);
			this.label12.Name = "label12";
			this.label12.Size = new System.Drawing.Size(45, 13);
			this.label12.TabIndex = 37;
			this.label12.Text = "degrees";
			// 
			// label13
			// 
			this.label13.AutoSize = true;
			this.label13.Location = new System.Drawing.Point(182, 246);
			this.label13.Name = "label13";
			this.label13.Size = new System.Drawing.Size(45, 13);
			this.label13.TabIndex = 38;
			this.label13.Text = "degrees";
			// 
			// label14
			// 
			this.label14.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.label14.AutoSize = true;
			this.label14.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label14.Location = new System.Drawing.Point(169, 398);
			this.label14.Name = "label14";
			this.label14.Size = new System.Drawing.Size(68, 13);
			this.label14.TabIndex = 39;
			this.label14.Text = "Debugging";
			// 
			// btnConnect
			// 
			this.btnConnect.Location = new System.Drawing.Point(215, 30);
			this.btnConnect.Name = "btnConnect";
			this.btnConnect.Size = new System.Drawing.Size(78, 26);
			this.btnConnect.TabIndex = 40;
			this.btnConnect.Text = "Connect";
			this.toolTip1.SetToolTip(this.btnConnect, "Connect to the mount and enable the controls to the right");
			this.btnConnect.Click += new System.EventHandler(this.btnConnect_Click);
			// 
			// lblFirmware
			// 
			this.lblFirmware.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.lblFirmware.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.lblFirmware.Location = new System.Drawing.Point(391, 53);
			this.lblFirmware.Name = "lblFirmware";
			this.lblFirmware.Size = new System.Drawing.Size(139, 17);
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
			this.numRASteps.Location = new System.Drawing.Point(392, 294);
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
			this.label15.Location = new System.Drawing.Point(288, 296);
			this.label15.Name = "label15";
			this.label15.Size = new System.Drawing.Size(98, 13);
			this.label15.TabIndex = 42;
			this.label15.Text = "RA Steps/degree";
			this.label15.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// label16
			// 
			this.label16.Location = new System.Drawing.Point(288, 321);
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
			this.numDECSteps.Location = new System.Drawing.Point(392, 319);
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
			this.label17.Location = new System.Drawing.Point(288, 346);
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
			this.numSpeedFactor.Location = new System.Drawing.Point(392, 344);
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
			this.label18.Location = new System.Drawing.Point(303, 10);
			this.label18.Name = "label18";
			this.label18.Size = new System.Drawing.Size(109, 13);
			this.label18.TabIndex = 48;
			this.label18.Text = "Mount Information";
			// 
			// label19
			// 
			this.label19.Location = new System.Drawing.Point(303, 53);
			this.label19.Name = "label19";
			this.label19.Size = new System.Drawing.Size(82, 13);
			this.label19.TabIndex = 49;
			this.label19.Text = "Firmware";
			this.label19.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// label20
			// 
			this.label20.Location = new System.Drawing.Point(287, 73);
			this.label20.Name = "label20";
			this.label20.Size = new System.Drawing.Size(98, 13);
			this.label20.TabIndex = 51;
			this.label20.Text = "Controller Board";
			this.label20.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// lblBoard
			// 
			this.lblBoard.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.lblBoard.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.lblBoard.Location = new System.Drawing.Point(391, 73);
			this.lblBoard.Name = "lblBoard";
			this.lblBoard.Size = new System.Drawing.Size(139, 17);
			this.lblBoard.TabIndex = 50;
			this.lblBoard.Text = "-";
			this.lblBoard.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// label21
			// 
			this.label21.Location = new System.Drawing.Point(287, 93);
			this.label21.Name = "label21";
			this.label21.Size = new System.Drawing.Size(98, 13);
			this.label21.TabIndex = 53;
			this.label21.Text = "Display";
			this.label21.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// lblDisplay
			// 
			this.lblDisplay.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.lblDisplay.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.lblDisplay.Location = new System.Drawing.Point(391, 93);
			this.lblDisplay.Name = "lblDisplay";
			this.lblDisplay.Size = new System.Drawing.Size(139, 17);
			this.lblDisplay.TabIndex = 52;
			this.lblDisplay.Text = "-";
			this.lblDisplay.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// label22
			// 
			this.label22.Location = new System.Drawing.Point(287, 113);
			this.label22.Name = "label22";
			this.label22.Size = new System.Drawing.Size(98, 13);
			this.label22.TabIndex = 55;
			this.label22.Text = "Add-Ons";
			this.label22.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// lblAddons
			// 
			this.lblAddons.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.lblAddons.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.lblAddons.Location = new System.Drawing.Point(391, 113);
			this.lblAddons.Name = "lblAddons";
			this.lblAddons.Size = new System.Drawing.Size(139, 39);
			this.lblAddons.TabIndex = 54;
			this.lblAddons.Text = "-";
			this.lblAddons.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// label24
			// 
			this.label24.Location = new System.Drawing.Point(282, 162);
			this.label24.Name = "label24";
			this.label24.Size = new System.Drawing.Size(103, 13);
			this.label24.TabIndex = 57;
			this.label24.Text = "Sidereal Time (LST)";
			this.label24.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// lblLST
			// 
			this.lblLST.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.lblLST.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.lblLST.Location = new System.Drawing.Point(391, 162);
			this.lblLST.Name = "lblLST";
			this.lblLST.Size = new System.Drawing.Size(139, 17);
			this.lblLST.TabIndex = 56;
			this.lblLST.Text = "-";
			this.lblLST.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// btnOPenASCOMLogs
			// 
			this.btnOPenASCOMLogs.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.btnOPenASCOMLogs.Location = new System.Drawing.Point(172, 437);
			this.btnOPenASCOMLogs.Name = "btnOPenASCOMLogs";
			this.btnOPenASCOMLogs.Size = new System.Drawing.Size(175, 26);
			this.btnOPenASCOMLogs.TabIndex = 58;
			this.btnOPenASCOMLogs.Text = "Open ASCOM Logs folder";
			this.btnOPenASCOMLogs.Click += new System.EventHandler(this.btnOpenASCOMLogs_Click);
			// 
			// label23
			// 
			this.label23.AutoSize = true;
			this.label23.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label23.Location = new System.Drawing.Point(287, 272);
			this.label23.Name = "label23";
			this.label23.Size = new System.Drawing.Size(117, 13);
			this.label23.TabIndex = 59;
			this.label23.Text = "Calibration Settings";
			// 
			// btnUpdate
			// 
			this.btnUpdate.Location = new System.Drawing.Point(375, 370);
			this.btnUpdate.Name = "btnUpdate";
			this.btnUpdate.Size = new System.Drawing.Size(78, 26);
			this.btnUpdate.TabIndex = 60;
			this.btnUpdate.Text = "Update OAT";
			this.toolTip1.SetToolTip(this.btnUpdate, "Send the changed three settings above to the mount");
			this.btnUpdate.Click += new System.EventHandler(this.btnUpdate_Click);
			// 
			// label25
			// 
			this.label25.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.label25.AutoSize = true;
			this.label25.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label25.Location = new System.Drawing.Point(597, 10);
			this.label25.Name = "label25";
			this.label25.Size = new System.Drawing.Size(99, 13);
			this.label25.TabIndex = 61;
			this.label25.Text = "RA/DEC Control";
			// 
			// btnNorth
			// 
			this.btnNorth.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnNorth.Image = global::ASCOM.OpenAstroTracker.Properties.Resources.arrow_090;
			this.btnNorth.Location = new System.Drawing.Point(628, 31);
			this.btnNorth.Name = "btnNorth";
			this.btnNorth.Size = new System.Drawing.Size(58, 50);
			this.btnNorth.TabIndex = 62;
			this.btnNorth.Tag = "N";
			this.toolTip1.SetToolTip(this.btnNorth, "Slew DEC North");
			this.btnNorth.UseVisualStyleBackColor = true;
			this.btnNorth.MouseDown += new System.Windows.Forms.MouseEventHandler(this.btnManualSlew_MouseDown);
			this.btnNorth.MouseUp += new System.Windows.Forms.MouseEventHandler(this.btnManualSlew_MouseUp);
			// 
			// btnSouth
			// 
			this.btnSouth.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnSouth.Image = global::ASCOM.OpenAstroTracker.Properties.Resources.arrow_270;
			this.btnSouth.Location = new System.Drawing.Point(628, 129);
			this.btnSouth.Name = "btnSouth";
			this.btnSouth.Size = new System.Drawing.Size(58, 50);
			this.btnSouth.TabIndex = 63;
			this.btnSouth.Tag = "S";
			this.toolTip1.SetToolTip(this.btnSouth, "Slew DEC South");
			this.btnSouth.UseVisualStyleBackColor = true;
			this.btnSouth.MouseDown += new System.Windows.Forms.MouseEventHandler(this.btnManualSlew_MouseDown);
			this.btnSouth.MouseUp += new System.Windows.Forms.MouseEventHandler(this.btnManualSlew_MouseUp);
			// 
			// btnEast
			// 
			this.btnEast.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnEast.Image = global::ASCOM.OpenAstroTracker.Properties.Resources.arrow;
			this.btnEast.Location = new System.Drawing.Point(685, 80);
			this.btnEast.Name = "btnEast";
			this.btnEast.Size = new System.Drawing.Size(58, 50);
			this.btnEast.TabIndex = 64;
			this.btnEast.Tag = "E";
			this.toolTip1.SetToolTip(this.btnEast, "Slew RA East");
			this.btnEast.UseVisualStyleBackColor = true;
			this.btnEast.MouseDown += new System.Windows.Forms.MouseEventHandler(this.btnManualSlew_MouseDown);
			this.btnEast.MouseUp += new System.Windows.Forms.MouseEventHandler(this.btnManualSlew_MouseUp);
			// 
			// btnWest
			// 
			this.btnWest.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnWest.Image = global::ASCOM.OpenAstroTracker.Properties.Resources.arrow_180;
			this.btnWest.Location = new System.Drawing.Point(571, 80);
			this.btnWest.Name = "btnWest";
			this.btnWest.Size = new System.Drawing.Size(58, 50);
			this.btnWest.TabIndex = 65;
			this.btnWest.Tag = "W";
			this.toolTip1.SetToolTip(this.btnWest, "Slew RA West");
			this.btnWest.UseVisualStyleBackColor = true;
			this.btnWest.MouseDown += new System.Windows.Forms.MouseEventHandler(this.btnManualSlew_MouseDown);
			this.btnWest.MouseUp += new System.Windows.Forms.MouseEventHandler(this.btnManualSlew_MouseUp);
			// 
			// btnUnparkDEC
			// 
			this.btnUnparkDEC.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnUnparkDEC.Location = new System.Drawing.Point(690, 33);
			this.btnUnparkDEC.Name = "btnUnparkDEC";
			this.btnUnparkDEC.Size = new System.Drawing.Size(78, 26);
			this.btnUnparkDEC.TabIndex = 66;
			this.btnUnparkDEC.Text = "Unpark DEC";
			this.toolTip1.SetToolTip(this.btnUnparkDEC, "Move DEC from Park to Home position");
			this.btnUnparkDEC.Click += new System.EventHandler(this.btnUnparkDEC_Click);
			// 
			// btnParkDEC
			// 
			this.btnParkDEC.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnParkDEC.Location = new System.Drawing.Point(691, 153);
			this.btnParkDEC.Name = "btnParkDEC";
			this.btnParkDEC.Size = new System.Drawing.Size(78, 26);
			this.btnParkDEC.TabIndex = 67;
			this.btnParkDEC.Text = "Park DEC";
			this.toolTip1.SetToolTip(this.btnParkDEC, "Move DEC from Home to Park position");
			this.btnParkDEC.Click += new System.EventHandler(this.btnParkDEC_Click);
			// 
			// btnGoHome
			// 
			this.btnGoHome.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnGoHome.Image = global::ASCOM.OpenAstroTracker.Properties.Resources.home;
			this.btnGoHome.Location = new System.Drawing.Point(641, 89);
			this.btnGoHome.Name = "btnGoHome";
			this.btnGoHome.Size = new System.Drawing.Size(32, 32);
			this.btnGoHome.TabIndex = 68;
			this.toolTip1.SetToolTip(this.btnGoHome, "Go Home");
			this.btnGoHome.UseVisualStyleBackColor = true;
			this.btnGoHome.Click += new System.EventHandler(this.btnGoHome_Click);
			// 
			// btnSetHome
			// 
			this.btnSetHome.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnSetHome.Image = global::ASCOM.OpenAstroTracker.Properties.Resources.sethome;
			this.btnSetHome.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.btnSetHome.Location = new System.Drawing.Point(571, 153);
			this.btnSetHome.Name = "btnSetHome";
			this.btnSetHome.Size = new System.Drawing.Size(53, 26);
			this.btnSetHome.TabIndex = 69;
			this.btnSetHome.Text = "Set";
			this.btnSetHome.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.toolTip1.SetToolTip(this.btnSetHome, "Set Home position (Shift to set parking offset)");
			this.btnSetHome.UseVisualStyleBackColor = true;
			this.btnSetHome.Click += new System.EventHandler(this.btnSetHome_Click);
			// 
			// btnAzLeft
			// 
			this.btnAzLeft.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnAzLeft.Image = global::ASCOM.OpenAstroTracker.Properties.Resources.arrow_180;
			this.btnAzLeft.Location = new System.Drawing.Point(546, 311);
			this.btnAzLeft.Name = "btnAzLeft";
			this.btnAzLeft.Size = new System.Drawing.Size(40, 40);
			this.btnAzLeft.TabIndex = 74;
			this.btnAzLeft.Tag = "W";
			this.toolTip1.SetToolTip(this.btnAzLeft, "Move mount the left");
			this.btnAzLeft.UseVisualStyleBackColor = true;
			this.btnAzLeft.MouseDown += new System.Windows.Forms.MouseEventHandler(this.btnAltAz_MouseDown);
			this.btnAzLeft.MouseUp += new System.Windows.Forms.MouseEventHandler(this.btnAltAz_MouseUp);
			// 
			// btnAzRight
			// 
			this.btnAzRight.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnAzRight.Image = global::ASCOM.OpenAstroTracker.Properties.Resources.arrow;
			this.btnAzRight.Location = new System.Drawing.Point(626, 311);
			this.btnAzRight.Name = "btnAzRight";
			this.btnAzRight.Size = new System.Drawing.Size(40, 40);
			this.btnAzRight.TabIndex = 73;
			this.btnAzRight.Tag = "E";
			this.toolTip1.SetToolTip(this.btnAzRight, "Move mount to the right");
			this.btnAzRight.UseVisualStyleBackColor = true;
			this.btnAzRight.MouseDown += new System.Windows.Forms.MouseEventHandler(this.btnAltAz_MouseDown);
			this.btnAzRight.MouseUp += new System.Windows.Forms.MouseEventHandler(this.btnAltAz_MouseUp);
			// 
			// btnAltDown
			// 
			this.btnAltDown.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnAltDown.Image = global::ASCOM.OpenAstroTracker.Properties.Resources.arrow_270;
			this.btnAltDown.Location = new System.Drawing.Point(586, 332);
			this.btnAltDown.Name = "btnAltDown";
			this.btnAltDown.Size = new System.Drawing.Size(40, 40);
			this.btnAltDown.TabIndex = 72;
			this.btnAltDown.Tag = "S";
			this.toolTip1.SetToolTip(this.btnAltDown, "Tilt mount down");
			this.btnAltDown.UseVisualStyleBackColor = true;
			this.btnAltDown.MouseDown += new System.Windows.Forms.MouseEventHandler(this.btnAltAz_MouseDown);
			this.btnAltDown.MouseUp += new System.Windows.Forms.MouseEventHandler(this.btnAltAz_MouseUp);
			// 
			// btnAltUp
			// 
			this.btnAltUp.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnAltUp.Image = global::ASCOM.OpenAstroTracker.Properties.Resources.arrow_090;
			this.btnAltUp.Location = new System.Drawing.Point(586, 292);
			this.btnAltUp.Name = "btnAltUp";
			this.btnAltUp.Size = new System.Drawing.Size(40, 40);
			this.btnAltUp.TabIndex = 71;
			this.btnAltUp.Tag = "N";
			this.toolTip1.SetToolTip(this.btnAltUp, "Tilt mount up");
			this.btnAltUp.UseVisualStyleBackColor = true;
			this.btnAltUp.MouseDown += new System.Windows.Forms.MouseEventHandler(this.btnAltAz_MouseDown);
			this.btnAltUp.MouseUp += new System.Windows.Forms.MouseEventHandler(this.btnAltAz_MouseUp);
			// 
			// btnFocusOut
			// 
			this.btnFocusOut.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnFocusOut.Image = global::ASCOM.OpenAstroTracker.Properties.Resources.arrow_270;
			this.btnFocusOut.Location = new System.Drawing.Point(699, 352);
			this.btnFocusOut.Name = "btnFocusOut";
			this.btnFocusOut.Size = new System.Drawing.Size(45, 40);
			this.btnFocusOut.TabIndex = 78;
			this.btnFocusOut.Tag = "S";
			this.toolTip1.SetToolTip(this.btnFocusOut, "Move focuser out at set rate");
			this.btnFocusOut.UseVisualStyleBackColor = true;
			this.btnFocusOut.MouseDown += new System.Windows.Forms.MouseEventHandler(this.btnFocus_MouseDown);
			this.btnFocusOut.MouseUp += new System.Windows.Forms.MouseEventHandler(this.btnFocus_MouseUp);
			// 
			// btnFocusIn
			// 
			this.btnFocusIn.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnFocusIn.Image = global::ASCOM.OpenAstroTracker.Properties.Resources.arrow_090;
			this.btnFocusIn.Location = new System.Drawing.Point(699, 292);
			this.btnFocusIn.Name = "btnFocusIn";
			this.btnFocusIn.Size = new System.Drawing.Size(45, 40);
			this.btnFocusIn.TabIndex = 77;
			this.btnFocusIn.Tag = "N";
			this.toolTip1.SetToolTip(this.btnFocusIn, "Move focuser in at set rate");
			this.btnFocusIn.UseVisualStyleBackColor = true;
			this.btnFocusIn.MouseDown += new System.Windows.Forms.MouseEventHandler(this.btnFocus_MouseDown);
			this.btnFocusIn.MouseUp += new System.Windows.Forms.MouseEventHandler(this.btnFocus_MouseUp);
			// 
			// rdoRateOne
			// 
			this.rdoRateOne.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.rdoRateOne.Appearance = System.Windows.Forms.Appearance.Button;
			this.rdoRateOne.Image = global::ASCOM.OpenAstroTracker.Properties.Resources.notification_counter;
			this.rdoRateOne.Location = new System.Drawing.Point(598, 215);
			this.rdoRateOne.Name = "rdoRateOne";
			this.rdoRateOne.Size = new System.Drawing.Size(24, 24);
			this.rdoRateOne.TabIndex = 86;
			this.rdoRateOne.TabStop = true;
			this.toolTip1.SetToolTip(this.rdoRateOne, "Set the mount move ment rate to the slowest");
			this.rdoRateOne.UseVisualStyleBackColor = true;
			this.rdoRateOne.CheckedChanged += new System.EventHandler(this.rdoRates_CheckedChanged);
			// 
			// rdoRateTwo
			// 
			this.rdoRateTwo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.rdoRateTwo.Appearance = System.Windows.Forms.Appearance.Button;
			this.rdoRateTwo.Image = global::ASCOM.OpenAstroTracker.Properties.Resources.notification_counter_02;
			this.rdoRateTwo.Location = new System.Drawing.Point(628, 215);
			this.rdoRateTwo.Name = "rdoRateTwo";
			this.rdoRateTwo.Size = new System.Drawing.Size(24, 24);
			this.rdoRateTwo.TabIndex = 87;
			this.rdoRateTwo.TabStop = true;
			this.toolTip1.SetToolTip(this.rdoRateTwo, "Set the mount move ment rate to the second slowest");
			this.rdoRateTwo.UseVisualStyleBackColor = true;
			this.rdoRateTwo.CheckedChanged += new System.EventHandler(this.rdoRates_CheckedChanged);
			// 
			// rdoRateThree
			// 
			this.rdoRateThree.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.rdoRateThree.Appearance = System.Windows.Forms.Appearance.Button;
			this.rdoRateThree.Image = global::ASCOM.OpenAstroTracker.Properties.Resources.notification_counter_03;
			this.rdoRateThree.Location = new System.Drawing.Point(658, 215);
			this.rdoRateThree.Name = "rdoRateThree";
			this.rdoRateThree.Size = new System.Drawing.Size(24, 24);
			this.rdoRateThree.TabIndex = 88;
			this.rdoRateThree.TabStop = true;
			this.toolTip1.SetToolTip(this.rdoRateThree, "Set the mount move ment rate to the second fastest");
			this.rdoRateThree.UseVisualStyleBackColor = true;
			this.rdoRateThree.CheckedChanged += new System.EventHandler(this.rdoRates_CheckedChanged);
			// 
			// rdoRateFour
			// 
			this.rdoRateFour.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.rdoRateFour.Appearance = System.Windows.Forms.Appearance.Button;
			this.rdoRateFour.Image = global::ASCOM.OpenAstroTracker.Properties.Resources.notification_counter_04;
			this.rdoRateFour.Location = new System.Drawing.Point(688, 215);
			this.rdoRateFour.Name = "rdoRateFour";
			this.rdoRateFour.Size = new System.Drawing.Size(24, 24);
			this.rdoRateFour.TabIndex = 89;
			this.rdoRateFour.TabStop = true;
			this.toolTip1.SetToolTip(this.rdoRateFour, "Set the mount move ment rate to the fastest");
			this.rdoRateFour.UseVisualStyleBackColor = true;
			this.rdoRateFour.CheckedChanged += new System.EventHandler(this.rdoRates_CheckedChanged);
			// 
			// lblFocusPosition
			// 
			this.lblFocusPosition.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.lblFocusPosition.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.lblFocusPosition.Location = new System.Drawing.Point(700, 333);
			this.lblFocusPosition.Name = "lblFocusPosition";
			this.lblFocusPosition.Size = new System.Drawing.Size(44, 17);
			this.lblFocusPosition.TabIndex = 92;
			this.lblFocusPosition.Text = "-";
			this.lblFocusPosition.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			this.toolTip1.SetToolTip(this.lblFocusPosition, "Focuser position (Shift click to reset)");
			this.lblFocusPosition.Click += new System.EventHandler(this.lblFocusPosition_Click);
			// 
			// lblAzAltControl
			// 
			this.lblAzAltControl.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.lblAzAltControl.AutoSize = true;
			this.lblAzAltControl.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.lblAzAltControl.Location = new System.Drawing.Point(558, 272);
			this.lblAzAltControl.Name = "lblAzAltControl";
			this.lblAzAltControl.Size = new System.Drawing.Size(96, 13);
			this.lblAzAltControl.TabIndex = 70;
			this.lblAzAltControl.Text = "ALT/AZ Control";
			// 
			// label29
			// 
			this.label29.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.label29.AutoSize = true;
			this.label29.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label29.Location = new System.Drawing.Point(749, 305);
			this.label29.Name = "label29";
			this.label29.Size = new System.Drawing.Size(16, 13);
			this.label29.TabIndex = 79;
			this.label29.Text = "In";
			// 
			// lblFocusControl
			// 
			this.lblFocusControl.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.lblFocusControl.AutoSize = true;
			this.lblFocusControl.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.lblFocusControl.Location = new System.Drawing.Point(683, 272);
			this.lblFocusControl.Name = "lblFocusControl";
			this.lblFocusControl.Size = new System.Drawing.Size(85, 13);
			this.lblFocusControl.TabIndex = 76;
			this.lblFocusControl.Text = "Focus Control";
			// 
			// label31
			// 
			this.label31.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.label31.AutoSize = true;
			this.label31.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label31.Location = new System.Drawing.Point(747, 366);
			this.label31.Name = "label31";
			this.label31.Size = new System.Drawing.Size(24, 13);
			this.label31.TabIndex = 79;
			this.label31.Text = "Out";
			// 
			// lblRateFastest
			// 
			this.lblRateFastest.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.lblRateFastest.AutoSize = true;
			this.lblRateFastest.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.lblRateFastest.Location = new System.Drawing.Point(717, 221);
			this.lblRateFastest.Name = "lblRateFastest";
			this.lblRateFastest.Size = new System.Drawing.Size(41, 13);
			this.lblRateFastest.TabIndex = 83;
			this.lblRateFastest.Text = "Fastest";
			// 
			// lblRateSlowest
			// 
			this.lblRateSlowest.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.lblRateSlowest.AutoSize = true;
			this.lblRateSlowest.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.lblRateSlowest.Location = new System.Drawing.Point(552, 221);
			this.lblRateSlowest.Name = "lblRateSlowest";
			this.lblRateSlowest.Size = new System.Drawing.Size(44, 13);
			this.lblRateSlowest.TabIndex = 84;
			this.lblRateSlowest.Text = "Slowest";
			// 
			// label34
			// 
			this.label34.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.label34.AutoSize = true;
			this.label34.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label34.Location = new System.Drawing.Point(617, 195);
			this.label34.Name = "label34";
			this.label34.Size = new System.Drawing.Size(78, 13);
			this.label34.TabIndex = 80;
			this.label34.Text = "Control Rate";
			// 
			// lblFocusRequired
			// 
			this.lblFocusRequired.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.lblFocusRequired.Enabled = false;
			this.lblFocusRequired.ForeColor = System.Drawing.SystemColors.GrayText;
			this.lblFocusRequired.Location = new System.Drawing.Point(679, 396);
			this.lblFocusRequired.Name = "lblFocusRequired";
			this.lblFocusRequired.Size = new System.Drawing.Size(87, 28);
			this.lblFocusRequired.TabIndex = 90;
			this.lblFocusRequired.Text = "Requires Focuser Add-On";
			this.lblFocusRequired.TextAlign = System.Drawing.ContentAlignment.TopCenter;
			// 
			// lblAutoPARequired
			// 
			this.lblAutoPARequired.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.lblAutoPARequired.Enabled = false;
			this.lblAutoPARequired.ForeColor = System.Drawing.SystemColors.GrayText;
			this.lblAutoPARequired.Location = new System.Drawing.Point(546, 376);
			this.lblAutoPARequired.Name = "lblAutoPARequired";
			this.lblAutoPARequired.Size = new System.Drawing.Size(120, 38);
			this.lblAutoPARequired.TabIndex = 91;
			this.lblAutoPARequired.Text = "Requires AutoPA Add-On";
			this.lblAutoPARequired.TextAlign = System.Drawing.ContentAlignment.TopCenter;
			// 
			// timerMountUpdate
			// 
			this.timerMountUpdate.Interval = 1000;
			this.timerMountUpdate.Tick += new System.EventHandler(this.timerMountUpdate_Tick);
			// 
			// label26
			// 
			this.label26.Location = new System.Drawing.Point(303, 33);
			this.label26.Name = "label26";
			this.label26.Size = new System.Drawing.Size(82, 13);
			this.label26.TabIndex = 94;
			this.label26.Text = "Status";
			this.label26.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// lblStatus
			// 
			this.lblStatus.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.lblStatus.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.lblStatus.Location = new System.Drawing.Point(391, 33);
			this.lblStatus.Name = "lblStatus";
			this.lblStatus.Size = new System.Drawing.Size(139, 17);
			this.lblStatus.TabIndex = 93;
			this.lblStatus.Text = "Disconnected";
			this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// lblRAPosition
			// 
			this.lblRAPosition.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.lblRAPosition.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.lblRAPosition.Location = new System.Drawing.Point(573, 33);
			this.lblRAPosition.Margin = new System.Windows.Forms.Padding(1, 0, 1, 0);
			this.lblRAPosition.Name = "lblRAPosition";
			this.lblRAPosition.Size = new System.Drawing.Size(51, 17);
			this.lblRAPosition.TabIndex = 95;
			this.lblRAPosition.Text = "-";
			this.lblRAPosition.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			this.toolTip1.SetToolTip(this.lblRAPosition, "Focuser position (Shift click to reset)");
			// 
			// label28
			// 
			this.label28.AutoSize = true;
			this.label28.Location = new System.Drawing.Point(547, 35);
			this.label28.Name = "label28";
			this.label28.Size = new System.Drawing.Size(22, 13);
			this.label28.TabIndex = 96;
			this.label28.Text = "RA";
			this.label28.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// label30
			// 
			this.label30.AutoSize = true;
			this.label30.Location = new System.Drawing.Point(541, 58);
			this.label30.Name = "label30";
			this.label30.Size = new System.Drawing.Size(29, 13);
			this.label30.TabIndex = 98;
			this.label30.Text = "DEC";
			this.label30.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// lblDECPosition
			// 
			this.lblDECPosition.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.lblDECPosition.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.lblDECPosition.Location = new System.Drawing.Point(573, 56);
			this.lblDECPosition.Margin = new System.Windows.Forms.Padding(1, 0, 1, 0);
			this.lblDECPosition.Name = "lblDECPosition";
			this.lblDECPosition.Size = new System.Drawing.Size(51, 17);
			this.lblDECPosition.TabIndex = 97;
			this.lblDECPosition.Text = "-";
			this.lblDECPosition.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			this.toolTip1.SetToolTip(this.lblDECPosition, "Focuser position (Shift click to reset)");
			// 
			// label27
			// 
			this.label27.Location = new System.Drawing.Point(303, 222);
			this.label27.Name = "label27";
			this.label27.Size = new System.Drawing.Size(82, 13);
			this.label27.TabIndex = 103;
			this.label27.Text = "RA Coordinate";
			this.label27.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// lblRACoordinate
			// 
			this.lblRACoordinate.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.lblRACoordinate.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.lblRACoordinate.Location = new System.Drawing.Point(391, 222);
			this.lblRACoordinate.Name = "lblRACoordinate";
			this.lblRACoordinate.Size = new System.Drawing.Size(139, 17);
			this.lblRACoordinate.TabIndex = 102;
			this.lblRACoordinate.Text = "-";
			this.lblRACoordinate.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// label33
			// 
			this.label33.Location = new System.Drawing.Point(285, 242);
			this.label33.Name = "label33";
			this.label33.Size = new System.Drawing.Size(100, 13);
			this.label33.TabIndex = 101;
			this.label33.Text = "DEC Coordinate";
			this.label33.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// label35
			// 
			this.label35.AutoSize = true;
			this.label35.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label35.Location = new System.Drawing.Point(300, 203);
			this.label35.Name = "label35";
			this.label35.Size = new System.Drawing.Size(114, 13);
			this.label35.TabIndex = 100;
			this.label35.Text = "Active Coordinates";
			// 
			// lblDECCoordinate
			// 
			this.lblDECCoordinate.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.lblDECCoordinate.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.lblDECCoordinate.Location = new System.Drawing.Point(391, 242);
			this.lblDECCoordinate.Name = "lblDECCoordinate";
			this.lblDECCoordinate.Size = new System.Drawing.Size(139, 17);
			this.lblDECCoordinate.TabIndex = 99;
			this.lblDECCoordinate.Text = "-";
			this.lblDECCoordinate.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// SetupDialogForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.SystemColors.Control;
			this.ClientSize = new System.Drawing.Size(784, 476);
			this.Controls.Add(this.label27);
			this.Controls.Add(this.lblRACoordinate);
			this.Controls.Add(this.label33);
			this.Controls.Add(this.label35);
			this.Controls.Add(this.lblDECCoordinate);
			this.Controls.Add(this.label30);
			this.Controls.Add(this.lblDECPosition);
			this.Controls.Add(this.label28);
			this.Controls.Add(this.lblRAPosition);
			this.Controls.Add(this.label26);
			this.Controls.Add(this.lblStatus);
			this.Controls.Add(this.lblFocusPosition);
			this.Controls.Add(this.lblAutoPARequired);
			this.Controls.Add(this.lblFocusRequired);
			this.Controls.Add(this.rdoRateFour);
			this.Controls.Add(this.rdoRateThree);
			this.Controls.Add(this.rdoRateTwo);
			this.Controls.Add(this.rdoRateOne);
			this.Controls.Add(this.lblRateFastest);
			this.Controls.Add(this.lblRateSlowest);
			this.Controls.Add(this.label34);
			this.Controls.Add(this.label31);
			this.Controls.Add(this.label29);
			this.Controls.Add(this.btnFocusOut);
			this.Controls.Add(this.btnFocusIn);
			this.Controls.Add(this.lblFocusControl);
			this.Controls.Add(this.btnAzLeft);
			this.Controls.Add(this.btnAzRight);
			this.Controls.Add(this.btnAltDown);
			this.Controls.Add(this.btnAltUp);
			this.Controls.Add(this.lblAzAltControl);
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
			this.MinimumSize = new System.Drawing.Size(800, 515);
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
		private System.Windows.Forms.Button btnAzLeft;
		private System.Windows.Forms.Button btnAzRight;
		private System.Windows.Forms.Button btnAltDown;
		private System.Windows.Forms.Button btnAltUp;
		internal System.Windows.Forms.Label lblAzAltControl;
		internal System.Windows.Forms.Label label29;
		private System.Windows.Forms.Button btnFocusOut;
		private System.Windows.Forms.Button btnFocusIn;
		internal System.Windows.Forms.Label lblFocusControl;
		internal System.Windows.Forms.Label label31;
		internal System.Windows.Forms.Label lblRateFastest;
		internal System.Windows.Forms.Label lblRateSlowest;
		internal System.Windows.Forms.Label label34;
		private System.Windows.Forms.RadioButton rdoRateOne;
		private System.Windows.Forms.RadioButton rdoRateTwo;
		private System.Windows.Forms.RadioButton rdoRateThree;
		private System.Windows.Forms.RadioButton rdoRateFour;
		internal System.Windows.Forms.Label lblFocusRequired;
		internal System.Windows.Forms.Label lblAutoPARequired;
		internal System.Windows.Forms.Label lblFocusPosition;
		private System.Windows.Forms.Timer timerMountUpdate;
		internal System.Windows.Forms.Label label26;
		internal System.Windows.Forms.Label lblStatus;
		internal System.Windows.Forms.Label lblRAPosition;
		internal System.Windows.Forms.Label label28;
		internal System.Windows.Forms.Label label30;
		internal System.Windows.Forms.Label lblDECPosition;
		internal System.Windows.Forms.Label label27;
		internal System.Windows.Forms.Label lblRACoordinate;
		internal System.Windows.Forms.Label label33;
		internal System.Windows.Forms.Label label35;
		internal System.Windows.Forms.Label lblDECCoordinate;
	}
}