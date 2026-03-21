using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using OATControl.ViewModels;
using System.ComponentModel;
using System.IO;

namespace OATControl
{
	public partial class DlgChecklistEditor : Window, INotifyPropertyChanged
	{
		private string _checklistText = string.Empty;
		string _filePath;
		string _listTitle;

		public DlgChecklistEditor()
		{
			_listTitle = string.Empty;
			_filePath = string.Empty;
			InitializeComponent();
			DataContext = this;
		}

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
				// Handle error - could show a message or log
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

		private void OKButton_Click(object sender, global::Avalonia.Interactivity.RoutedEventArgs e)
		{
			// Indicate success, save changes and close the window
			File.WriteAllText(_filePath, _checklistText);
			if (AppSettings.Instance.ChecklistTitle != _listTitle)
			{
				AppSettings.Instance.ChecklistTitle = _listTitle;
				AppSettings.Instance.Save();
			}
			this.Close();
		}

		private void CancelButton_Click(object sender, global::Avalonia.Interactivity.RoutedEventArgs e)
		{
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
		public new event PropertyChangedEventHandler? PropertyChanged;

		protected void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
