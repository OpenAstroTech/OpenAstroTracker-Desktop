using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OATControl.ViewModels
{
	public class ChecklistItem : INotifyPropertyChanged
	{
		private string text;
		private bool isChecked;

		public string Id { get; set; } = Guid.NewGuid().ToString();

		public string Text
		{
			get => text; 
			set
			{
				text = value;
				OnPropertyChanged(nameof(Text));
			}
		}

		public bool IsChecked
		{
			get => isChecked; 
			set
			{
				isChecked = value;
				SubItems.ToList().ForEach(item => item.IsChecked = value); // Propagate the check state to sub-items	
				OnPropertyChanged(nameof(IsChecked));
			}
		}

		public ObservableCollection<ChecklistItem> SubItems { get; set; } = new ObservableCollection<ChecklistItem>();

		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged(string name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}
	}
}
