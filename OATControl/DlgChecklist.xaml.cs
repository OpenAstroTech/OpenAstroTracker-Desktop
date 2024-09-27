using Microsoft.Web.WebView2.Core;
using OATControl.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace OATControl
{
	/// <summary>
	/// Interaction logic for DlgAppSettings.xaml
	/// </summary>
	public partial class DlgChecklist : Window, INotifyPropertyChanged
	{
		string _listFilePath;
		private ObservableCollection<ChecklistItem> checklistItems;
		DateTime _lastCreationDate;
		private Point _startCapturePos;
		private Point _startWindowPos;

		public DlgChecklist(string listFilePath)
		{

			this.DataContext = this;

			InitializeComponent();
			InitializeWebView();

			_listFilePath = listFilePath;

			LoadChecklistItemsFromFile(_listFilePath);
			
		}

		private void UpdateChecklistHtml()
		{
			string htmlContent = GenerateChecklistHtml(checklistItems);
			ChecklistWebView.NavigateToString(htmlContent);
		}

		private string GenerateChecklistHtml(ObservableCollection<ChecklistItem> items)
		{
			StringBuilder htmlBuilder = new StringBuilder();
			htmlBuilder.Append("<!DOCTYPE html><html><head><meta charset='UTF-8'><style>");
			// Include CSS styles
			htmlBuilder.Append(@"
				.app {
					margin:0;
					padding:0;
					height:100%;
				}
				body {
					font-family: Arial, sans-serif;
					padding: 10px;
					font-size: 10pt;
					background: #600;
					margin: 0;
				}
				.item { margin-bottom: 10px; display: flex;  }
				.item-checkbox { margin-right: 10px; }
				.item-text {
					color: #F40;
					margin-top: 4px;
				}
				.item-text >b {
					color: #F86;
					font-size: 115%;
				}
				.item-text.checked >b {
					color: #A43;
				}
				.item-text.checked { color: #930; }
				input[type='checkbox'] {
					-webkit-appearance: none;
					-moz-appearance: none;
					appearance: none;
					width: 16px;
					min-width: 16px;
					height: 16px;
					border: 1px solid #D22;
					border-radius: 3px;
					outline: none;
					cursor: pointer;
					position: relative;
					margin-top: 4px;
				}

				/* Checked state */
				input[type = 'checkbox']:checked {
					width: 16px;
					min-width: 16px;
					background-color: #B22; /* Desired fill color */
					border-color: #D22;     /* Desired border color */
				}

				/* Checkmark */
				input[type = 'checkbox']:checked::after {
					content: '';
					position: absolute;
					top: 0px;
					left: 3px;
					width: 5px;
					height: 9px;
					border: solid #F88;
					border-width: 0 3px 3px 0;
					transform: rotate(37deg);
				}
			");
			htmlBuilder.Append("</style><script type='text/javascript'>");
			htmlBuilder.Append(@"
				function checkboxChanged(id) {
					console.log('clicked', id);
					const isChecked = document.getElementById(id).checked;
					console.log('posting', id, isChecked);
					window.chrome.webview.postMessage({ id: id, isChecked: isChecked });
				}
				</script>");
			htmlBuilder.Append("</head><body><div class='app'>");

			foreach (var item in items)
			{
				string textClass = "item-text" + (item.IsChecked ? " checked" : "");
				string htmlText = item.Text.Replace("\\n", "<br />");
				string checkbox = $"<input type='checkbox' class='item-checkbox' id='{item.Id}' {(item.IsChecked ? "checked" : "")} onclick='checkboxChanged(\"{item.Id}\")' />";

				htmlBuilder.AppendFormat("<div class='item'>{0}<div class='{1}'>{2}</div></div>", checkbox, textClass, htmlText, item.Id);
			}

			htmlBuilder.Append("</div></body></html>");
			htmlBuilder.Append("</script>");

			return htmlBuilder.ToString();
		}

		private async void InitializeWebView()
		{
			string userDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OATControl", "WebView2");

			// Ensure the directory exists
			Directory.CreateDirectory(userDataFolder);

			// Create the WebView2 environment with the custom user data folder
			var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
			
			ChecklistWebView.CoreWebView2InitializationCompleted += async (sender, args) =>
			{
				if (args.IsSuccess)
				{
					ChecklistWebView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
					UpdateChecklistHtml();
				}
			};
			await ChecklistWebView.EnsureCoreWebView2Async(env);
		}

		private void CoreWebView2_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
		{
			var message = e.WebMessageAsJson;
			if (!string.IsNullOrEmpty(message))
			{
				var json = Newtonsoft.Json.JsonConvert.DeserializeObject<CheckboxMessage>(message);
				var item = checklistItems.FirstOrDefault(i => i.Id == json.Id);
				if (item != null)
				{
					item.IsChecked = json.IsChecked;
					UpdateChecklistHtml();
				}
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
			UpdateChecklistHtml();
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
			List<ChecklistItem> items = new List<ChecklistItem>();
			FileInfo fi = new FileInfo(_listFilePath);
			_lastCreationDate = fi.LastWriteTimeUtc;

			try
			{
				var lines = File.ReadAllLines(filePath);
				items = lines.Select(line => new ChecklistItem
				{
					Text = line,
					IsChecked = false
				}).ToList();

				checklistItems = new ObservableCollection<ChecklistItem>(items);
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
			get { return Path.GetTempPath();  }
		}

		private void Window_Activated(object sender, EventArgs e)
		{
			FileInfo fi = new FileInfo(_listFilePath);
			if (_lastCreationDate != fi.LastWriteTimeUtc)
			{
				LoadChecklistItemsFromFile(_listFilePath);
				UpdateChecklistHtml();
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
	}

}

