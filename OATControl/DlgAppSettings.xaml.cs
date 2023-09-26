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

namespace OATControl
{
	/// <summary>
	/// Interaction logic for DlgAppSettings.xaml
	/// </summary>
	public partial class DlgAppSettings : Window, INotifyPropertyChanged
	{
		private List<String> _baudRates = new List<string>() {
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
				};

		private MountVM _mount;
		private string _serialBaudRate;
		private bool _raStartEast;
		private bool _decStartSouth;
		private float _raDistance;
		private float _decDistance;

		public DlgAppSettings(MountVM mount)
		{
			_mount = mount;
			this.DataContext = this;

			InitializeComponent();
			SelectedBaudRate = AppSettings.Instance.BaudRate;
			RaStartEast = AppSettings.Instance.AutoHomeRaDirection == "East";
			RaDistance = AppSettings.Instance.AutoHomeRaDistance;
			DecStartSouth = AppSettings.Instance.AutoHomeDecDirection == "South";
			DecDistance = AppSettings.Instance.AutoHomeDecDistance;

			ContentTabs.SelectedIndex = 0;
			CategorySelector.SelectedIndex = 0;
		}

		public String SelectedBaudRate
		{
			get { return _serialBaudRate; }
			set
			{
				_serialBaudRate = value;
				OnPropertyChanged();
			}
		}

		public bool DecStartSouth
		{
			get { return _decStartSouth; }
			set
			{
				_decStartSouth = value;
				OnPropertyChanged();
			}
		}
		public float DecDistance
		{
			get { return _decDistance; }
			set
			{
				_decDistance = value;
				OnPropertyChanged();
			}
		}

		public bool RaStartEast
		{
			get { return _raStartEast; }
			set
			{
				_raStartEast = value;
				OnPropertyChanged();
			}
		}
		public float RaDistance
		{
			get { return _raDistance; }
			set
			{
				_raDistance = value;
				OnPropertyChanged();
			}
		}

		public IEnumerable<String> AvailableBaudRates
		{
			get { return _baudRates; }
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

		private void OnCategorySelected(object sender, SelectionChangedEventArgs e)
		{
			var selectedCategory = (e.AddedItems[0] as ListViewItem).Content as string;

			switch (selectedCategory)
			{
				case "General":
					ContentTabs.SelectedIndex = 0;
					break;
				case "Autohoming":
					ContentTabs.SelectedIndex = 1;
					break;
			}
		}

		private void OnCloseClick(object sender, RoutedEventArgs e)
		{
			// Transfer to MountVM
			_mount.SelectedBaudRate = _serialBaudRate;
			AppSettings.Instance.AutoHomeRaDirection = RaStartEast ? "East" : "West";
			AppSettings.Instance.AutoHomeRaDistance = RaDistance;
			AppSettings.Instance.AutoHomeDecDirection = DecStartSouth ? "South" : "North";
			AppSettings.Instance.AutoHomeDecDistance = DecDistance;

			Close();
		}
		private void OnCloseCancelClick(object sender, RoutedEventArgs e)
		{
			Close();
		}

	}
}
