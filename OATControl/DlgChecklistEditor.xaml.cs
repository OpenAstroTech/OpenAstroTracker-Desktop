using MahApps.Metro.Controls;
using System.ComponentModel;
using System.IO;
using System.Windows;

namespace OATControl
{
	public partial class DlgChecklistEditor : MetroWindow, INotifyPropertyChanged
	{
		private string checklistText;
		private string originalText;
		string _filePath;

		public string ChecklistText
		{
			get { return checklistText; }
			set
			{
				if (checklistText != value)
				{
					checklistText = value;
					OnPropertyChanged(nameof(ChecklistText));
				}
			}
		}

		public DlgChecklistEditor(string filePath)
		{
			_filePath = filePath;
			InitializeComponent();
			DataContext = this;

			ChecklistText = File.ReadAllText(_filePath);
			originalText = ChecklistText;
		}

		private void OKButton_Click(object sender, RoutedEventArgs e)
		{
			// Indicate success, save changes and close the window
			this.DialogResult = true;
			File.WriteAllText(_filePath, checklistText);
			this.Close();
		}

		private void CancelButton_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = false;
			this.Close();
		}

		// Implement INotifyPropertyChanged
		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
