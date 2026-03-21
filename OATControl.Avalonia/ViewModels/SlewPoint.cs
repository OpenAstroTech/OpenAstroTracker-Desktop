using System.ComponentModel;

namespace OATControl.ViewModels
{
    public class SlewPoint : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private string _savedName = string.Empty;
        private int _raStepperPosition;
        private int _decStepperPosition;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string SavedName => _savedName;

        public string Name
        {
            get => _name;
            set { if (_name != value) { _name = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name))); } }
        }

        public int RAStepperPosition
        {
            get => _raStepperPosition;
            set { if (_raStepperPosition != value) { _raStepperPosition = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RAStepperPosition))); } }
        }

        public int DECStepperPosition
        {
            get => _decStepperPosition;
            set { if (_decStepperPosition != value) { _decStepperPosition = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DECStepperPosition))); } }
        }
    }
}
