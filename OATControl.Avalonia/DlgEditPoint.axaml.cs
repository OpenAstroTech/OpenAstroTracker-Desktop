using Avalonia.Controls;
using Avalonia.Interactivity;
using OATCommunications.Avalonia;
using OATControl.ViewModels;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OATControl.Avalonia
{
	/// <summary>
	/// Interaction logic for DlgEditPoint.axaml
	/// </summary>
	public partial class DlgEditPoint : Window, INotifyPropertyChanged
	{
		private MountVM _mount = null!;
		private PointOfInterest? _selectedPoint;
		private bool _enabled;
		DelegateCommand _okCommand;
		DelegateCommand _cancelCommand;
		private string _name = string.Empty;
		private string _catalogName = string.Empty;
		private string _decCoord = "45 0 0";
		private string _raCoord = "0 0 0";

		public DlgEditPoint()
		{
			_okCommand = new DelegateCommand(() => OnOk());
			_cancelCommand = new DelegateCommand(() => OnCancel());
			this.DataContext = this;
			InitializeComponent();
		}

		public DlgEditPoint(MountVM mount)
		{
			_mount = mount;
			_selectedPoint = null;
			_enabled = true;
			_name = "";
			_catalogName = "";
			_raCoord = "0 0 0";
			_decCoord = "45 0 0";

			_okCommand = new DelegateCommand(() => OnOk());
			_cancelCommand = new DelegateCommand(() => OnCancel());

			this.DataContext = this;

			InitializeComponent();
		}

		public DlgEditPoint(MountVM mount, PointOfInterest pt)
		{
			_mount = mount;
			_selectedPoint = pt;
			_enabled = pt.Enabled;
			_name = pt.Name;
			_catalogName = pt.CatalogName;
			_raCoord = MountVM.CoordToString(pt.RA, MountVM.CoordSeparators.RaSeparators);
			_decCoord = MountVM.CoordToString(pt.DEC, MountVM.CoordSeparators.DecSeparators);

			_okCommand = new DelegateCommand(() => OnOk());
			_cancelCommand = new DelegateCommand(() => OnCancel());

			this.DataContext = this;

			InitializeComponent();
		}

		public DelegateCommand OkCommand { get { return _okCommand; } }
		public DelegateCommand CancelCommand { get { return _cancelCommand; } }

		private void OnRaLostFocus(object? sender, RoutedEventArgs e)
		{
			if (sender is TextBox textBox && !string.IsNullOrEmpty(textBox.Text))
			{
				float newRa;
				if (MountVM.TryParseCoord(textBox.Text, out newRa))
				{
					RaCoordinate = MountVM.CoordToString(newRa, MountVM.CoordSeparators.RaSeparators);
				}
			}
		}

		private void OnDecLostFocus(object? sender, RoutedEventArgs e)
		{
			if (sender is TextBox textBox && !string.IsNullOrEmpty(textBox.Text))
			{
				float newDec;
				if (MountVM.TryParseCoord(textBox.Text, out newDec))
				{
					DecCoordinate = MountVM.CoordToString(newDec, MountVM.CoordSeparators.DecSeparators);
				}
			}
		}

		public void OnOk()
		{
			var newPt = new PointOfInterest(_name);
			newPt.Enabled = _enabled;
			newPt.CatalogName = this.CatalogName;
			float tmp;
			MountVM.TryParseCoord(this._raCoord, out tmp);
			newPt.RA = tmp;
			MountVM.TryParseCoord(this._decCoord, out tmp);
			newPt.DEC = tmp;

			if (this._selectedPoint != null)
			{
				_mount.ReplacePointOfInterest(this._selectedPoint, newPt);
			}
			else
			{
				if (!_mount.AddPointOfInterest(newPt))
				{
					// Show message box equivalent in Avalonia
					var dlg = new DlgMessageBox("Name or Catalog name already exists.");
					dlg.ShowDialog(this);
					return;
				}
			}
			this.Close();
		}

		public void OnCancel()
		{
			this.Close();
		}

		public bool Enabled
		{
			get { return _enabled; }
			set
			{
				_enabled = value;
				OnPropertyChanged();
			}
		}

		public string PointName
		{
			get { return _name; }
			set
			{
				_name = value;
				OnPropertyChanged();
			}
		}

		public string CatalogName
		{
			get { return _catalogName; }
			set
			{
				_catalogName = value;
				OnPropertyChanged();
			}
		}

		public string DecCoordinate
		{
			get { return _decCoord; }
			set
			{
				_decCoord = value;
				OnPropertyChanged();
			}
		}

		public string RaCoordinate
		{
			get { return _raCoord; }
			set
			{
				_raCoord = value;
				OnPropertyChanged();
			}
		}

		public new event PropertyChangedEventHandler? PropertyChanged;
		void OnPropertyChanged([CallerMemberName] string prop = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
		}
	}
}
