using OATCommunications.WPF;
using OATControl.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml.Linq;
using DelegateCommand = OATCommunications.WPF.DelegateCommand;

namespace OATControl
{
	public class SlewPoint : INotifyPropertyChanged
	{
		private string _name;
		private string _savedName;
		private int _raStepperPosition;
		private int _decStepperPosition;
		private bool _isEditing;

		public string SavedName
		{
			get { return _savedName; }
		}

		public string Name
		{
			get { return _name; }
			set
			{
				if (_name != value)
				{
					_name = value;
					OnPropertyChanged("Name");
				}
			}
		}

		public bool IsEditing
		{
			get => _isEditing;
			set
			{
				if (_isEditing != value)
				{
					_isEditing = value;
					if (_isEditing)
					{
						_savedName = _name;
					}
					OnPropertyChanged(nameof(IsEditing));
				}
			}
		}


		public int RaStepperPosition
		{
			get { return _raStepperPosition; }
			set
			{
				if (_raStepperPosition != value)
				{
					_raStepperPosition = value;
					OnPropertyChanged("RaStepperPosition");
				}
			}
		}

		public int DecStepperPosition
		{
			get { return _decStepperPosition; }
			set
			{
				if (_decStepperPosition != value)
				{
					_decStepperPosition = value;
					OnPropertyChanged("DecStepperPosition");
				}
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public override string ToString()
		{
			return $"{Name}";
		}
	}

	/// <summary>
	/// Interaction logic for SlewPointsWindow.xaml
	/// </summary>
	public partial class SlewPointsWindow : Window
	{
		MountVM _mount;
		private Point _startCapturePos;
		private Point _startWindowPos;
		private string _lastCommand = string.Empty;
		public ICommand RemoveCommand { get; }
		public ICommand RenameCommand { get; }
		public DelegateCommand UpdateCommand { get; }
		public DelegateCommand GoToCommand { get; }
		public ObservableCollection<SlewPoint> SlewPoints { get; set; }

		public SlewPointsWindow(MountVM mount)
		{
			_mount = mount;
			this.DataContext = this;
			SlewPoints = new ObservableCollection<SlewPoint>();
			LoadSlewPoints();
			RemoveCommand = new DelegateCommand((p) => RemoveSlewPoint(p as SlewPoint), () => true);
			RenameCommand = new DelegateCommand((p) => RenameSlewPoint(p as SlewPoint), () => true);
			UpdateCommand = new DelegateCommand((p) => UpdateSlewPoint(p as SlewPoint), () => _mount.MountConnected);
			GoToCommand = new DelegateCommand((p) => GoToSlewPoint(p as SlewPoint), () => _mount.MountConnected);
			InitializeComponent();
			this.Left = AppSettings.Instance.SlewPointsWindowPos.X;
			this.Top = AppSettings.Instance.SlewPointsWindowPos.Y;
			this.Width = AppSettings.Instance.SlewPointsWindowSize.Width;
			this.Height = AppSettings.Instance.SlewPointsWindowSize.Height;
			IsVisibleChanged += Window_IsVisibleChanged;
		}

		private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if (IsVisible)
			{
				OnPropertyChanged("MountConnected");
				UpdateCommand.Requery();
				GoToCommand.Requery();

			}
		}

		private void LoadSlewPoints()
		{
			SlewPoints.Clear();
			var points = AppSettings.Instance.SlewPoints;
			if (points != null)
			{
				foreach (var point in points)
				{
					SlewPoints.Add(point);
				}
			}
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			AppSettings.Instance.SlewPointsWindowPos = new Point((int)this.Left, (int)this.Top);
			AppSettings.Instance.SlewPointsWindowSize = new Size((int)this.Width, (int)this.Height);
			AppSettings.Instance.SlewPoints = SlewPoints.ToList();
			AppSettings.Instance.Save();
			_mount.OnShowSlewPoints();
		}


		private void AddButton_Click(object sender, RoutedEventArgs e)
		{
			SlewPoint newPoint = new SlewPoint
			{
				Name = $"New Position {SlewPoints.Count + 1}",
				RaStepperPosition = (int)((_mount != null && _mount.MountConnected) ? _mount.RAStepper : 0),
				DecStepperPosition = (int)((_mount != null && _mount.MountConnected) ? _mount.DECStepper : 0),
			};
			SlewPoints.Add(newPoint);
		}

		public bool MountConnected
		{
			get { return _mount != null && _mount.MountConnected; }
		}

		private void RemoveSlewPoint(SlewPoint point)
		{
			SlewPoints.Remove(point);
		}

		private void RenameSlewPoint(SlewPoint point)
		{

			point.IsEditing = true;
			
		}

		private async void GoToSlewPoint(SlewPoint point)
		{
			if ((_mount != null) && _mount.MountConnected)
			{
				await _mount.MoveMountBy(point.RaStepperPosition - (int)_mount.RAStepper, point.DecStepperPosition - (int)_mount.DECStepper);
			}
		}
		
		private void NameTextBox_KeyDown(object sender, KeyEventArgs e)
		{
			var tb = sender as TextBox;
			SlewPoint point = (tb?.DataContext as SlewPoint);
			if (point != null)
			{
				if (e.Key == Key.Enter)
				{
					point.IsEditing = false;
				}
				else if (e.Key == Key.Escape)
				{
					point.Name = point.SavedName;
					point.IsEditing = false;
				}
			}
		}

		private void NameTextBox_LostFocus(object sender, RoutedEventArgs e)
		{
			var tb = sender as TextBox;
			if (tb?.DataContext is SlewPoint point)
			{
				point.IsEditing = false;
			}
		}

		private void UpdateSlewPoint(SlewPoint point)
		{
			if (_mount != null)
			{
				point.RaStepperPosition = (int)_mount.RAStepper;
				point.DecStepperPosition = (int)_mount.DECStepper;
			}
		}

		public void TitleTextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Pressed)
			{
				DragMove();
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		private void NameTextBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			var tb = sender as TextBox;
			if (tb != null && tb.IsVisible)
			{
				tb.Focus();
				tb.SelectAll();
			}
		}

	}
}
