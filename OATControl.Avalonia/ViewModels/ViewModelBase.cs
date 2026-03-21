using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace OATControl.ViewModels
{
	public class ViewModelBase : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler? PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		protected void SetPropertyValue<T>(ref T variable, T newValue, [CallerMemberName] string memberName = "")
		{
			if (!Equals(variable, newValue))
			{
				variable = newValue;
				OnPropertyChanged(memberName);
			}
		}

		protected void SetPropertyValue<T>(ref T variable, T newValue, Action<T, T> onChangedcallBack, [CallerMemberName] string memberName = "")
		{
			if (!Equals(variable, newValue))
			{
				var oldValue = variable;
				variable = newValue;
				onChangedcallBack(oldValue, variable);
				OnPropertyChanged(memberName);
			}
		}

		protected void SetPropertyValue<T>(T newValue, Func<T> getValue, Func<T, bool> updater, Action<T, T> onChanged, [CallerMemberName] string memberName = "")
		{
			T oldValue = getValue();
			if (!Equals(newValue, oldValue))
			{
				if (updater(newValue))
				{
					onChanged(oldValue, getValue());
					OnPropertyChanged(memberName);
				}
			}
		}

		protected void SetPropertyValue<T>(ref T field, T value, Action<T, T> onChanged)
		{
			if (!Equals(field, value))
			{
				T oldValue = field;
				field = value;
				onChanged(oldValue, value);
				OnPropertyChanged();
			}
		}

		protected void SetPropertyValue<T>(T value, Func<T> getter, Action<T> setter, Action<T, T>? onChanged = null)
		{
			if (!Equals(value, getter()))
			{
				T oldValue = getter();
				setter(value);
				onChanged?.Invoke(oldValue, value);
				OnPropertyChanged();
			}
		}
	}
}
