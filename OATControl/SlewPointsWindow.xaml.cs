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

namespace OATControl
{
	public class SlewPoint : INotifyPropertyChanged
	{
		private string _name;
		private int _raStepperPosition;
		private int _decStepperPosition;

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
		public ObservableCollection<SlewPoint> SlewPoints { get; set; }

		public SlewPointsWindow(MountVM mount)
		{
			_mount = mount;
			this.DataContext = this;
			SlewPoints = new ObservableCollection<SlewPoint>();
			LoadSlewPoints();
			InitializeComponent();
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

		protected override void OnClosing(CancelEventArgs e)
		{
			AppSettings.Instance.SlewPointsWindow = new Point((int)this.Left, (int)this.Top);
			AppSettings.Instance.SlewPoints = SlewPoints.ToList();
			AppSettings.Instance.Save();
			base.OnClosing(e);
		}

		private void AddButton_Click(object sender, RoutedEventArgs e)
		{
			SlewPoint newPoint = new SlewPoint
			{
				Name = $"New Position {SlewPoints.Count + 1}",
				//RaStepperPosition = _mount != null ? _mount.RAStepperPosition : 0,
				//DecStepperPosition = _mount != null ? _mount.DECStepperPosition : 0
			};
			SlewPoints.Add(newPoint);
		}

		private void RemoveButton_Click(object sender, RoutedEventArgs e)
		{
			if (slewPointsList.SelectedItem != null)
			{
				SlewPoints.Remove(slewPointsList.SelectedItem as SlewPoint);
			}
		}

		private void RenameButton_Click(object sender, RoutedEventArgs e)
		{
			if (slewPointsList.SelectedItem != null)
			{
				SlewPoint selectedPoint = slewPointsList.SelectedItem as SlewPoint;
				RenameSlewPointDialog dialog = new RenameSlewPointDialog(selectedPoint.Name);
				if (dialog.ShowDialog() == true)
				{
					selectedPoint.Name = dialog.PointName;
				}
			}
		}

		private void GoToButton_Click(object sender, RoutedEventArgs e)
		{
			if (slewPointsList.SelectedItem != null && _mount != null)
			{
				SlewPoint selectedPoint = slewPointsList.SelectedItem as SlewPoint;
				//TODO : Implement the logic to move the mount to the selected point
				//selectedPoint.RaStepperPosition;
				//selectedPoint.DecStepperPosition;
			}
		}

		private void UpdateButton_Click(object sender, RoutedEventArgs e)
		{
			if (slewPointsList.SelectedItem != null && _mount != null)
			{
				SlewPoint selectedPoint = slewPointsList.SelectedItem as SlewPoint;
				selectedPoint.RaStepperPosition = (int)_mount.RAStepper;
				selectedPoint.DecStepperPosition = (int)_mount.DECStepper;
			}
		}
	}

	public class RenameSlewPointDialog : Window
	{
		private TextBox nameTextBox;
		public string PointName { get; private set; }

		public RenameSlewPointDialog(string currentName)
		{
			Title = "Rename Position";
			Width = 300;
			Height = 150;
			WindowStartupLocation = WindowStartupLocation.CenterOwner;
			PointName = currentName;

			Grid grid = new Grid();
			grid.Margin = new Thickness(10);
			grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

			Label label = new Label { Content = "Enter new name:", Margin = new Thickness(0, 0, 0, 5) };
			Grid.SetRow(label, 0);
			grid.Children.Add(label);

			nameTextBox = new TextBox { Text = currentName, Margin = new Thickness(0, 0, 0, 15) };
			Grid.SetRow(nameTextBox, 1);
			grid.Children.Add(nameTextBox);

			StackPanel buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
			Button okButton = new Button { Content = "OK", Width = 75, Margin = new Thickness(0, 0, 10, 0), IsDefault = true };
			okButton.Click += OkButton_Click;
			Button cancelButton = new Button { Content = "Cancel", Width = 75, IsCancel = true };
			buttonPanel.Children.Add(okButton);
			buttonPanel.Children.Add(cancelButton);
			Grid.SetRow(buttonPanel, 2);
			grid.Children.Add(buttonPanel);

			Content = grid;
		}

		private void OkButton_Click(object sender, RoutedEventArgs e)
		{
			PointName = nameTextBox.Text;
			DialogResult = true;
		}
	}
}
