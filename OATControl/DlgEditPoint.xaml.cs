using MahApps.Metro.Controls;
using OATCommunications.WPF;
using OATControl.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace OATControl
{
	/// <summary>
	/// Interaction logic for DlgAppSettings.xaml
	/// </summary>
	public partial class DlgEditPoint : MetroWindow, INotifyPropertyChanged
	{
		private MountVM _mount;
		private PointOfInterest _selectedPoint;
		private bool _enabled;
		DelegateCommand _okCommand;
		DelegateCommand _cancelCommand;
		private string _name;
		private string _catalogName;
		private string _decCoord;
		private string _raCoord;

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

		public ICommand OkCommand { get { return _okCommand; } }
		public ICommand CancelCommand { get { return _cancelCommand; } }

		private void OnRaLostFocus(object sender, RoutedEventArgs e)
		{
			float newRa;
			if (MountVM.TryParseCoord((sender as TextBox).Text, out newRa))
			{
				RaCoordinate = MountVM.CoordToString(newRa, MountVM.CoordSeparators.RaSeparators);
			}
		}
		private void OnDecLostFocus(object sender, RoutedEventArgs e)
		{
			float newDec;
			if (MountVM.TryParseCoord((sender as TextBox).Text, out newDec))
			{
				DecCoordinate = MountVM.CoordToString(newDec, MountVM.CoordSeparators.DecSeparators);
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
					MessageBox.Show("Name or Catalog name already exists.", "Conflicts found",MessageBoxButton.OK, MessageBoxImage.Exclamation);
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


		public event PropertyChangedEventHandler PropertyChanged;
		void OnPropertyChanged([CallerMemberName] string prop = "")
		{
			var handler = this.PropertyChanged;
			if (handler != null)
			{
				handler(this, new PropertyChangedEventArgs(prop));
			}
		}

		private void OnCloseClick(object sender, RoutedEventArgs e)
		{
			// Transfer to MountVM

			Close();
		}

		private void OnCloseCancelClick(object sender, RoutedEventArgs e)
		{
			Close();
		}

	}
}
