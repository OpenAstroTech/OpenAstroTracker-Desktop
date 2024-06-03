using MahApps.Metro.Controls;
using OATCommunications.WPF;
using OATControl.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
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
using System.Windows.Threading;

namespace OATControl
{
	/// <summary>
	/// Interaction logic for DlgAppSettings.xaml
	/// </summary>
	public partial class DlgAppSettings : MetroWindow, INotifyPropertyChanged
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
		private List<PointOfInterest> _pointsOfInterest;
		private PointOfInterest _selectedPoint;
		private string sortField = "Name";
		private int sortDirection = 1;
		DelegateCommand _editPointCommand;
		DelegateCommand _addPointCommand;
		private GridViewColumnHeader _lastHeaderClicked;

		public DlgAppSettings(MountVM mount)
		{
			_mount = mount;
			this.AllPointsOfInterest = _mount.AllPointsOfInterest.ToList();
			_editPointCommand = new DelegateCommand(() => OnEditPoint());
			_addPointCommand = new DelegateCommand(() => OnAddPoint());

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

		
		public ICommand EditPointCommand { get { return _editPointCommand; } }
		public ICommand AddPointCommand { get { return _addPointCommand; } }

		public void OnEditPoint()
		{
			var dlg = new DlgEditPoint(_mount, _selectedPoint) { Owner = Application.Current.MainWindow, WindowStartupLocation = WindowStartupLocation.CenterOwner };
			dlg.ShowDialog();
			this.OnPropertyChanged("AllPointsOfInterest");
		}

		public void OnAddPoint()
		{
			var dlg = new DlgEditPoint(_mount) { Owner = Application.Current.MainWindow, WindowStartupLocation = WindowStartupLocation.CenterOwner };
			dlg.ShowDialog();
			this.OnPropertyChanged("AllPointsOfInterest");
		}

		public bool IsPointSelected
		{
			get { return _selectedPoint != null; }
		}

		public PointOfInterest SelectedPoint
		{
			get { return _selectedPoint; }
			set
			{
				_selectedPoint = value;
				OnPropertyChanged();
				OnPropertyChanged("IsPointSelected");
				_editPointCommand.Requery();

			}
		}

		public List<PointOfInterest> AllPointsOfInterest
		{
			get { return _pointsOfInterest; }
			set
			{
				_pointsOfInterest = value;
				OnPropertyChanged();
			}
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
				case "Target List":
					ContentTabs.SelectedIndex = 2;
					SetInitialSortIndicator(this.sortField);
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
			AppSettings.Instance.Save();
			_mount.AutoHomeDecDirection = AppSettings.Instance.AutoHomeDecDirection;
			_mount.AutoHomeRaDirection = AppSettings.Instance.AutoHomeRaDirection;
			_mount.AutoHomeDecDistance = AppSettings.Instance.AutoHomeDecDistance;
			_mount.AutoHomeRaDistance = AppSettings.Instance.AutoHomeRaDistance;
			_mount.SavePointsOfInterest();
			Close();
		}
		private void OnCloseCancelClick(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			OnEditPoint();
		}

		private void OnHeaderClick(object sender, RoutedEventArgs e)
		{
			if (e.OriginalSource is GridViewColumnHeader headerClicked)
			{
				string header = headerClicked.Column.Header as string;
				if (header == this.sortField)
				{
					this.sortDirection *= -1;
				}
				else
				{
					this.sortField = header;
				}

				// Determine how to sort
				this.AllPointsOfInterest.Sort((a, b) =>
				{
					switch (header)
					{
						case "Name": return this.sortDirection * (!string.IsNullOrEmpty(a.Name + b.Name) ? a.Name.CompareTo(b.Name) : a.CatalogName.CompareTo(b.CatalogName));
						case "Catalog": return this.sortDirection * (!string.IsNullOrEmpty(a.CatalogName + b.CatalogName) ? a.CatalogName.CompareTo(b.CatalogName) : a.Name.CompareTo(b.Name));
						case "Show": return this.sortDirection * (a.Enabled.CompareTo(b.Enabled));
						case "RA": return this.sortDirection * (a.RA.CompareTo(b.RA));
						case "DEC": return this.sortDirection * (a.DEC.CompareTo(b.DEC));
						default: return this.sortDirection * (a.Name.CompareTo(b.Name));
					}
				});

				SetSortIndicator(headerClicked);

				this.AllPointsOfInterest = this.AllPointsOfInterest.ToList();
			}
		}

		private void SetSortIndicator(GridViewColumnHeader column)
		{
			// Clear the existing sort indicator
			if (_lastHeaderClicked != null)
			{
				var lastSortArrow = _lastHeaderClicked.Template.FindName("SortArrow", _lastHeaderClicked) as Path;
				if (lastSortArrow != null)
				{
					lastSortArrow.Visibility = Visibility.Hidden;
				}
			}

			// Set the new sort indicator
			var newSortArrow = column.Template.FindName("SortArrow", column) as Path;
			if (newSortArrow != null)
			{
				newSortArrow.Visibility = Visibility.Visible;
				// Rotate arrow if necessary
				newSortArrow.RenderTransform = new RotateTransform(this.sortDirection == 1 ? 180 : 0);
			}
			_lastHeaderClicked = column;
		}

		private void SetInitialSortIndicator(string column)
		{
			// Wait for the visual elements to be fully loaded
			Dispatcher.BeginInvoke(new Action(() =>
			{
				foreach (var columnHeader in FindVisualChildren<GridViewColumnHeader>(TargetsListView))
				{
					if (columnHeader.Column != null && columnHeader.Column.Header as string == column)
					{
						SetSortIndicator(columnHeader);
						break;
					}
				}
			}), DispatcherPriority.Loaded);
		}

		private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
		{
			if (depObj != null)
			{
				for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
				{
					DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
					if (child != null && child is T)
					{
						yield return (T)child;
					}

					foreach (T childOfChild in FindVisualChildren<T>(child))
					{
						yield return childOfChild;
					}
				}
			}
		}

		private void OnWindowLoaded(object sender, RoutedEventArgs e)
		{
			SetInitialSortIndicator(this.sortField);
		}
	}

}
