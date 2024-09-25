using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OATControl.ViewModels
{
	internal class ChecklistItem : INotifyPropertyChanged
	{
		private string text;
		private bool isChecked;

		public string Id { get; set; } = Guid.NewGuid().ToString();

		public string Text
		{
			get { return text; }
			set
			{
				text = value;
				OnPropertyChanged(nameof(Text));
			}
		}

		public bool IsChecked
		{
			get { return isChecked; }
			set
			{
				isChecked = value;
				OnPropertyChanged(nameof(IsChecked));
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged(string name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}
	}
}
