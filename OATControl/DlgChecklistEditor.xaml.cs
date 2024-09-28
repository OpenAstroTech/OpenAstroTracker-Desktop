using MahApps.Metro.Controls;
using OATControl.ViewModels;
using System.ComponentModel;
using System.IO;
using System.Windows;

namespace OATControl
{
	public partial class DlgChecklistEditor : MetroWindow, INotifyPropertyChanged
	{
		private string _checklistText;
		string _filePath;
		string _listTitle;

		public DlgChecklistEditor(string filePath)
		{
			_listTitle = AppSettings.Instance.ChecklistTitle;
			_filePath = filePath;
			InitializeComponent();
			DataContext = this;

			try
			{
				var content = File.ReadAllText(_filePath);
				ChecklistText = content;
			}
			catch
			{
				MessageBox.Show("No checklist found");
			}
		}

		public string ChecklistText
		{
			get { return _checklistText; }
			set
			{
				if (_checklistText != value)
				{
					_checklistText = value;
					OnPropertyChanged("ChecklistText");
				}
			}
		}


		private void OKButton_Click(object sender, RoutedEventArgs e)
		{
			// Indicate success, save changes and close the window
			this.DialogResult = true;
			File.WriteAllText(_filePath, _checklistText);
			if (AppSettings.Instance.ChecklistTitle != _listTitle)
			{
				AppSettings.Instance.ChecklistTitle = _listTitle;
				AppSettings.Instance.Save();
			}
			this.Close();
		}

		private void CancelButton_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = false;
			this.Close();
		}

		public string ListTitle
		{
			get { return _listTitle; }
			set
			{
				if (_listTitle != value)
				{
					_listTitle = value;
					OnPropertyChanged("ListTitle");
				}
			}
		}

		// Implement INotifyPropertyChanged
		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
