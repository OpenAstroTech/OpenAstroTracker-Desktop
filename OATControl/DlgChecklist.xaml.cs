using OATControl.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using DelegateCommand = OATCommunications.WPF.DelegateCommand;

namespace OATControl
{
	/// <summary>
	/// Interaction logic for DlgAppSettings.xaml
	/// </summary>
	public partial class DlgChecklist : Window, INotifyPropertyChanged
	{
		string _listFilePath;
		string _listTitle;
		private ObservableCollection<ChecklistItem> checklistItems;
		DateTime _lastCreationDate;
		private Point _startCapturePos;
		private Point _startWindowPos;
		private DelegateCommand _editChecklistCommand;

		public DlgChecklist(string listFilePath)
		{

			this.DataContext = this;
			_editChecklistCommand = new DelegateCommand(() => OnShowChecklist(), () => true);
			_listTitle = AppSettings.Instance.ChecklistTitle;
			InitializeComponent();
			
			_listFilePath = listFilePath;

			LoadChecklistItemsFromFile(_listFilePath);
		}

		private void OnShowChecklist()
		{
			DlgChecklistEditor dlg = new DlgChecklistEditor(_listFilePath);
			dlg.ShowDialog();
			_listTitle = AppSettings.Instance.ChecklistTitle;
			OnPropertyChanged("ListTitle");
			LoadChecklistItemsFromFile(_listFilePath);
		}

		public ICommand EditChecklistCommand { get { return _editChecklistCommand; } }

		public ObservableCollection<ChecklistItem> ChecklistItems
		{
			get => checklistItems;
			set
			{
				checklistItems = value;
				OnPropertyChanged(nameof(ChecklistItems));
			}
		}

		public class CheckboxMessage
		{
			public string Id { get; set; }
			public bool IsChecked { get; set; }
		}


		private void OnResetClick(object sender, RoutedEventArgs e)
		{
			foreach (var item in checklistItems)
			{
				item.IsChecked = false;
			}
		}

		private void OnCloseClick(object sender, RoutedEventArgs e)
		{
			AppSettings.Instance.ChecklistSize = new Size(this.Width, this.Height);
			AppSettings.Instance.ChecklistPos = new Point(this.Left, this.Top);
			AppSettings.Instance.Save();
			Hide();
		}


		private void LoadChecklistItemsFromFile(string filePath)
		 {
			var items = new List<ChecklistItem>();
			ChecklistItem currentHeading = null;
			FileInfo fi = new FileInfo(_listFilePath);
			_lastCreationDate = fi.LastWriteTimeUtc;

			try
			{
				var lines = File.ReadAllLines(filePath);

				foreach (var line in lines)
				{
					if (line.StartsWith("*"))
					{
						// Create a new heading
						currentHeading = new ChecklistItem
						{
							Text = line.Substring(1).Trim(),
							IsChecked = false,
							SubItems = new ObservableCollection<ChecklistItem>()
						};
						items.Add(currentHeading);
					}
					else if (line.StartsWith("-") && currentHeading != null)
					{
						// Add subitem to the current heading
						currentHeading.SubItems.Add(new ChecklistItem
						{
							Text = line.Substring(1).Trim(),
							IsChecked = false
						});
					}
				}

				checklistItems = new ObservableCollection<ChecklistItem>(items);
				OnPropertyChanged(nameof(ChecklistItems));
			}
			catch (Exception ex)
			{
				MessageBox.Show("Error loading checklist: " + ex.Message);
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

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			// Restore size and position
			this.Width = AppSettings.Instance.ChecklistSize.Width;
			this.Height = AppSettings.Instance.ChecklistSize.Height;
			this.Left = AppSettings.Instance.ChecklistPos.X;
			this.Top = AppSettings.Instance.ChecklistPos.Y;
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			// Save size and position
			AppSettings.Instance.ChecklistSize = new Size(this.Width, this.Height);
			AppSettings.Instance.ChecklistPos = new Point(this.Left, this.Top);
			AppSettings.Instance.Save();
		}

		public string TempFolder
		{
			get { return Path.GetTempPath(); }
		}

		private void Window_Activated(object sender, EventArgs e)
		{
			FileInfo fi = new FileInfo(_listFilePath);
			if (_lastCreationDate != fi.LastWriteTimeUtc)
			{
				LoadChecklistItemsFromFile(_listFilePath);
			}
		}

		private void OnTitleMouseDown(object sender, MouseButtonEventArgs e)
		{
			UIElement el = (UIElement)sender;
			if (el.IsEnabled)
			{
				el.CaptureMouse();
				_startCapturePos = PointToScreen(e.GetPosition(el));
				_startWindowPos = new Point(this.Left, this.Top);
				e.Handled = true;
			}
		}

		private void OnTitleMouseUp(object sender, MouseButtonEventArgs e)
		{
			UIElement el = (UIElement)sender;
			if (el.IsMouseCaptured)
			{
				el.ReleaseMouseCapture();
				e.Handled = true;
			}
		}

		private void OnTitleMouseMove(object sender, MouseEventArgs e)
		{
			UIElement el = (UIElement)sender;
			if (el.IsMouseCaptured)
			{
				var mousePos = PointToScreen(e.GetPosition(el));
				var delta = new Point(_startCapturePos.X - mousePos.X, _startCapturePos.Y - mousePos.Y);
				this.Left = _startWindowPos.X - delta.X;
				this.Top = _startWindowPos.Y - delta.Y;
				e.Handled = true;
			}
		}

		public string ListTitle
		{
			get { return _listTitle; }
		}
	}

}

