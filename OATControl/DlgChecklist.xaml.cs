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

namespace OATControl
{
	/// <summary>
	/// Interaction logic for DlgAppSettings.xaml
	/// </summary>
	public partial class DlgChecklist : Window, INotifyPropertyChanged
	{
		string _listFilePath;
		private ObservableCollection<ChecklistItem> checklistItems;

		public DlgChecklist(string listFilePath)
		{

			this.DataContext = this;


			InitializeComponent();
			_listFilePath = listFilePath;

			InitializeWebView();
			var items = LoadChecklistItemsFromFile(_listFilePath);
			checklistItems = new ObservableCollection<ChecklistItem>(items);
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
					margin-top: -1px;
					position: absolute;
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
			ChecklistWebView.CoreWebView2InitializationCompleted += async (sender, args) =>
			{
				if (args.IsSuccess)
				{
					ChecklistWebView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
					UpdateChecklistHtml();
				}
			};
			await ChecklistWebView.EnsureCoreWebView2Async(null);
			UpdateChecklistHtml();
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


		private List<ChecklistItem> LoadChecklistItemsFromFile(string filePath)
		{
			List<ChecklistItem> items = new List<ChecklistItem>();

			try
			{
				var lines = File.ReadAllLines(filePath);
				items = lines.Select(line => new ChecklistItem
				{
					Text = line,
					IsChecked = false
				}).ToList();
			}
			catch (Exception ex)
			{
				MessageBox.Show("Error loading checklist: " + ex.Message);
			}

			return items;
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

	}

}

