using System;
using System.Collections.Generic;
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

namespace OATControl
{
	/// <summary>
	/// Interaction logic for RenameSlewPointDialog.xaml
	/// </summary>

	public partial class RenameSlewPointDialog : Window, INotifyPropertyChanged
	{
		private string _pointName;
		public string PointName
		{
			get => _pointName;
			set
			{
				if (_pointName != value)
				{
					_pointName = value;
					OnPropertyChanged(nameof(PointName));
				}
			}
		}

		public RenameSlewPointDialog(string currentName)
		{
			PointName = currentName;
			DataContext = this;
			InitializeComponent();
			Loaded += (s, e) =>
			{
				nameTextBox.Focus();
				nameTextBox.SelectAll();
			};
		}

		private void OkButton_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}

		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
