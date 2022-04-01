using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace OATCommunications.WPF
{
	/// <summary>
	/// Base class for INotifyPropertyChanged.
	/// </summary>
	public class NotifyPropertyChanged : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		internal protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
		{
			var handler = PropertyChanged;
			if (handler != null)
			{
				handler(this, new PropertyChangedEventArgs(propertyName));
			}
		}


		public void SetProperty<T>(ref T currentVal, T newVal, string propertyName)
		{
			if (!currentVal.Equals(newVal))
			{
				currentVal= newVal;
				OnPropertyChanged(propertyName);
			}
		}
	}
}
