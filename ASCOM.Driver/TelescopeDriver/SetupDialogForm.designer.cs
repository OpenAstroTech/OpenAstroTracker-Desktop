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
			((System.ComponentModel.ISupportInitialize)(this.PictureBox1)).BeginInit();
			this.TableLayoutPanel1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
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
			this.Label2.Location = new System.Drawing.Point(115, 285);
			this.Label2.Name = "Label2";
			this.Label2.Size = new System.Drawing.Size(117, 13);
			this.Label2.TabIndex = 22;
			this.Label2.Text = "ASCOM Trace Logging";
			// 
			// ComboBoxComPort
			// 
			this.ComboBoxComPort.FormattingEnabled = true;
			this.ComboBoxComPort.Location = new System.Drawing.Point(94, 36);
			this.ComboBoxComPort.Name = "ComboBoxComPort";
			this.ComboBoxComPort.Size = new System.Drawing.Size(84, 21);
			this.ComboBoxComPort.TabIndex = 21;
			// 
			// chkTrace
			// 
			this.chkTrace.AutoSize = true;
			this.chkTrace.Location = new System.Drawing.Point(94, 285);
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
			this.TableLayoutPanel1.Location = new System.Drawing.Point(226, 320);
			this.TableLayoutPanel1.Name = "TableLayoutPanel1";
			this.TableLayoutPanel1.RowCount = 1;
			this.TableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
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
			this.pictureBox2.Location = new System.Drawing.Point(229, 12);
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
			// SetupDialogForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.SystemColors.Control;
			this.ClientSize = new System.Drawing.Size(384, 361);
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
			this.MinimumSize = new System.Drawing.Size(400, 400);
			this.Name = "SetupDialogForm";
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "OpenAstroTracker Setup";
			this.Load += new System.EventHandler(this.SetupDialogForm_Load);
			((System.ComponentModel.ISupportInitialize)(this.PictureBox1)).EndInit();
			this.TableLayoutPanel1.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
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
	}
}