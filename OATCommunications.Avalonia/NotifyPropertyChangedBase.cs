using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OATCommunications.Avalonia
{
    public class NotifyPropertyChanged : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected internal void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void SetProperty<T>(ref T currentVal, T newVal, string propertyName)
        {
            if (!currentVal!.Equals(newVal))
            {
                currentVal = newVal;
                OnPropertyChanged(propertyName);
            }
        }
    }
}
