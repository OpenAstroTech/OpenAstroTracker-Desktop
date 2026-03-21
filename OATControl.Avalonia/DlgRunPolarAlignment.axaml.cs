using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using OATCommunications.Avalonia;
using OATCommunications.CommunicationHandlers;
using OATCommunications.Utilities;
using OATControl.ViewModels;

namespace OATControl.Avalonia
{
    public partial class DlgRunPolarAlignment : Window, INotifyPropertyChanged
    {
        private readonly Action<string, Action<CommandResponse>>? _sendCommand;
        private int _state;

        public new event PropertyChangedEventHandler? PropertyChanged;

        public DlgRunPolarAlignment()
        {
            DataContext = this;
            InitializeComponent();
            State = 1;
        }

        public DlgRunPolarAlignment(Action<string, Action<CommandResponse>> sendCommand)
        {
            _sendCommand = sendCommand;
            DataContext = this;

            OKCommand = new DelegateCommand(async () =>
            {
                State++;
                if (_state == 2)
                {
                    // Slew RA to Polaris
                    await SendCommandAsync(":Sr02:59:09#,n");

                    // Slew DEC to Polaris (double the declination offset)
                    if (AppSettings.Instance.SiteLatitude >= 0)
                        await SendCommandAsync(":Sd+88*42:12#,n");
                    else
                        await SendCommandAsync(":Sd-88*42:12#,n");

                    await SendCommandAsync(":MS#,n");
                }
                else if (_state == 3)
                {
                    // Sync mount to Polaris coordinates
                    if (AppSettings.Instance.SiteLatitude >= 0)
                        await SendCommandAsync(":SY+89*21:06.02:59:09#,n");
                    else
                        await SendCommandAsync(":SY-89*21:06.02:59:09#,n");
                }
            });

            CloseCommand = new DelegateCommand(() => Close());

            InitializeComponent();
            State = 1;
        }

        public ICommand OKCommand { get; } = new DelegateCommand(() => { });
        public ICommand CloseCommand { get; } = new DelegateCommand(() => { });

        public int State
        {
            get => _state;
            set
            {
                if (_state != value)
                {
                    _state = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsStep1));
                    OnPropertyChanged(nameof(IsStep2));
                    OnPropertyChanged(nameof(IsStep3));
                }
            }
        }

        public bool IsStep1 => _state == 1;
        public bool IsStep2 => _state == 2;
        public bool IsStep3 => _state == 3;

        private async Task SendCommandAsync(string command)
        {
            if (_sendCommand == null) return;
            var done = new SemaphoreSlim(0, 1);
            _sendCommand(command, _ => done.Release());
            await done.WaitAsync();
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
