using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia;
using OATCommunications.Utilities;
using OATControl.ViewModels;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace OATControl.Avalonia
{
    public partial class MiniController : Window
    {
        private MountVM? Mount => DataContext as MountVM;
        private string _lastCommand = string.Empty;
        private bool _parkedWarningShown = false;
        private DateTime _lastPointerEventAt = DateTime.MinValue;

        private const int ClickFallbackWindowMs = 350;
        private const int ClickJogDurationMs = 200;

        public MiniController()
        {
            InitializeComponent();
            RegisterSlewButtonHandlers();
            Log.WriteLine("MiniCtrl: ctor() created");
        }

        public MiniController(MountVM mount)
        {
            DataContext = mount;
            InitializeComponent();
            RegisterSlewButtonHandlers();
            Log.WriteLine("MiniCtrl: ctor(mount) created");
        }

        private void RegisterSlewButtonHandlers()
        {
            AddHandler(InputElement.PointerPressedEvent, OnAnyPointerPressed, RoutingStrategies.Tunnel, true);
            AddHandler(InputElement.PointerReleasedEvent, OnAnyPointerReleased, RoutingStrategies.Tunnel, true);
            AddHandler(Button.ClickEvent, OnAnyButtonClick, RoutingStrategies.Bubble, true);
            AddHandler(InputElement.KeyDownEvent, OnWindowKeyDown, RoutingStrategies.Tunnel, true);
            AddHandler(InputElement.KeyUpEvent, OnWindowKeyUp, RoutingStrategies.Tunnel, true);
        }

        private void HookNamedSlewButtons()
        {
            string[] buttonNames =
            {
                "SlewW", "SlewN", "SlewS", "SlewE",
                "SlewA", "SlewZ", "SlewL", "SlewR", "SlewF", "SlewG"
            };

            int hooked = 0;
            foreach (var name in buttonNames)
            {
                var button = this.FindControl<Button>(name);
                if (button == null)
                {
                    Log.WriteLine("MiniCtrl: Named button not found: {0}", name);
                    continue;
                }

                button.PointerPressed += OnSlewPressed;
                button.PointerReleased += OnSlewReleased;
                button.Click += OnSlewClick;
                hooked++;
            }

            Log.WriteLine("MiniCtrl: Hooked {0} named slew buttons", hooked);
        }

        public double Left
        {
            get => Position.X;
            set => Position = new global::Avalonia.PixelPoint((int)value, Position.Y);
        }

        public double Top
        {
            get => Position.Y;
            set => Position = new global::Avalonia.PixelPoint(Position.X, (int)value);
        }

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);
            HookNamedSlewButtons();
            Log.WriteLine("MiniCtrl: OnOpened MountConnected={0}, Status={1}, ParkButton={2}", Mount?.MountConnected, Mount?.MountStatus, Mount?.ParkCommandString);
            Activate();
            Focus();
        }

        protected override void OnClosing(WindowClosingEventArgs e)
        {
            Log.WriteLine("MiniCtrl: OnClosing");
            AppSettings.Instance.MiniControllerPos = (Left, Top);
            base.OnClosing(e);
        }

        private void OnAnyPointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (!IsActive)
            {
                Activate();
            }
            Focus();

            var button = FindTaggedButton(e.Source, e.GetPosition(this));
            if (button != null)
            {
                _lastPointerEventAt = DateTime.UtcNow;
                Log.WriteLine("MiniCtrl: PointerPressed captured for {0}", button.Tag);
                OnSlewPressed(button, e);
            }
        }

        private void OnAnyPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            var button = FindTaggedButton(e.Source, e.GetPosition(this));
            if (button != null)
            {
                _lastPointerEventAt = DateTime.UtcNow;
                Log.WriteLine("MiniCtrl: PointerReleased captured for {0}", button.Tag);
                OnSlewReleased(button, e);
            }
        }

        private void OnAnyButtonClick(object? sender, RoutedEventArgs e)
        {
            var button = FindTaggedButtonFromSource(e.Source);
            if (button?.Tag is not string direction || direction.Length == 0)
            {
                return;
            }

            // If pointer handlers are working, Click follows immediately after release.
            // Ignore click in that case to avoid duplicate movement.
            if ((DateTime.UtcNow - _lastPointerEventAt).TotalMilliseconds < ClickFallbackWindowMs)
            {
                return;
            }

            if (Mount?.MountConnected != true)
            {
                return;
            }

            Log.WriteLine("MiniCtrl: Click fallback jog for {0}", direction);
            _ = RunClickJogAsync(direction);
        }

        private async Task RunClickJogAsync(string direction)
        {
            SendSlewCommand($"+{direction}");
            await Task.Delay(ClickJogDurationMs);
            SendSlewCommand($"-{direction}");
        }

        private static Button? FindTaggedButtonFromSource(object? source)
        {
            if (source is Button directButton && directButton.Tag is string directTag && directTag.Length > 0)
            {
                return directButton;
            }

            if (source is global::Avalonia.StyledElement element)
            {
                var current = element;
                while (current != null)
                {
                    if (current is Button button && button.Tag is string tag && tag.Length > 0)
                    {
                        return button;
                    }

                    current = current.Parent as global::Avalonia.StyledElement;
                }
            }

            return null;
        }

        private Button? FindTaggedButton(object? source, Point pointerPosition)
        {
            var fromSource = FindTaggedButtonFromSource(source);
            if (fromSource != null)
            {
                return fromSource;
            }

            var hitElement = this.InputHitTest(pointerPosition) as global::Avalonia.StyledElement;
            while (hitElement != null)
            {
                if (hitElement is Button hitButton && hitButton.Tag is string hitTag && hitTag.Length > 0)
                {
                    return hitButton;
                }

                hitElement = hitElement.Parent as global::Avalonia.StyledElement;
            }

            return null;
        }

        private void OnSlewPressed(object? sender, PointerPressedEventArgs e)
        {
            if (sender is Button button && button.Tag is string direction && direction.Length > 0)
            {
                if (Mount?.MountConnected != true)
                {
                    return;
                }
                SendSlewCommand($"+{direction}");
                e.Handled = true;
            }
        }

        private void OnSlewReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (sender is Button button && button.Tag is string direction && direction.Length > 0)
            {
                if (Mount?.MountConnected == true)
                {
                    SendSlewCommand($"-{direction}");
                }
                e.Handled = true;
            }
        }

        private void OnWindowPointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (Mount?.MountConnected != true)
            {
                return;
            }

            if (!string.IsNullOrEmpty(_lastCommand) && _lastCommand.StartsWith("+"))
            {
                SendSlewCommand($"-{_lastCommand.Substring(1)}");
            }
        }

        private void OnRateClicked(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string rateText && int.TryParse(rateText, out int rate) && rate >= 1 && rate <= 5)
            {
                if (Mount != null)
                {
                    Mount.SlewRate = rate;
                }
            }
        }

        private void OnSlewClick(object? sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not string direction || direction.Length == 0)
            {
                return;
            }

            Log.WriteLine("MiniCtrl: OnSlewClick tag={0}", direction);

            if ((DateTime.UtcNow - _lastPointerEventAt).TotalMilliseconds < ClickFallbackWindowMs)
            {
                Log.WriteLine("MiniCtrl: OnSlewClick ignored due recent pointer event");
                return;
            }

            if (Mount?.MountConnected != true)
            {
                Log.WriteLine("MiniCtrl: OnSlewClick ignored, mount not connected");
                return;
            }

            Log.WriteLine("MiniCtrl: Direct click jog for {0}", direction);
            _ = RunClickJogAsync(direction);
            e.Handled = true;
        }

        private void OnWindowKeyDown(object? sender, KeyEventArgs e)
        {
            string cmdParam = e.Key switch
            {
                Key.Up => "+N",
                Key.Down => "+S",
                Key.Left => "+W",
                Key.Right => "+E",
                Key.W when Mount?.ScopeHasALT == true => "+A",
                Key.S when Mount?.ScopeHasALT == true => "+Z",
                Key.A when Mount?.ScopeHasAZ == true => "+L",
                Key.D when Mount?.ScopeHasAZ == true => "+R",
                Key.X when Mount?.ScopeHasFOC == true => "+F",
                Key.C when Mount?.ScopeHasFOC == true => "+G",
                _ => string.Empty
            };

            if (!string.IsNullOrEmpty(cmdParam))
            {
                SendSlewCommand(cmdParam);
                e.Handled = true;
            }
        }

        private void OnWindowKeyUp(object? sender, KeyEventArgs e)
        {
            if (Mount == null)
            {
                return;
            }

            switch (e.Key)
            {
                case Key.H:
                    if (Mount.HomeCommand.CanExecute(null)) Mount.HomeCommand.Execute(null);
                    e.Handled = true;
                    return;
                case Key.P:
                    if (Mount.ParkCommand.CanExecute(null)) Mount.ParkCommand.Execute(null);
                    e.Handled = true;
                    return;
                case Key.D1:
                case Key.NumPad1:
                    Mount.SlewRate = 1;
                    e.Handled = true;
                    return;
                case Key.D2:
                case Key.NumPad2:
                    Mount.SlewRate = 2;
                    e.Handled = true;
                    return;
                case Key.D3:
                case Key.NumPad3:
                    Mount.SlewRate = 3;
                    e.Handled = true;
                    return;
                case Key.D4:
                case Key.NumPad4:
                    Mount.SlewRate = 4;
                    e.Handled = true;
                    return;
                case Key.D5:
                case Key.NumPad5:
                    Mount.SlewRate = 5;
                    e.Handled = true;
                    return;
                case Key.Escape:
                    Hide();
                    e.Handled = true;
                    return;
            }

            string cmdParam = e.Key switch
            {
                Key.Up => "-N",
                Key.Down => "-S",
                Key.Left => "-W",
                Key.Right => "-E",
                Key.W when Mount.ScopeHasALT => "-A",
                Key.S when Mount.ScopeHasALT => "-Z",
                Key.A when Mount.ScopeHasAZ => "-L",
                Key.D when Mount.ScopeHasAZ => "-R",
                Key.X when Mount.ScopeHasFOC => "-F",
                Key.C when Mount.ScopeHasFOC => "-G",
                _ => string.Empty
            };

            if (!string.IsNullOrEmpty(cmdParam))
            {
                SendSlewCommand(cmdParam);
                e.Handled = true;
            }
        }

        private void OnDragAreaPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                BeginMoveDrag(e);
                e.Handled = true;
            }
        }

        private void SendSlewCommand(string command)
        {
            if (Mount?.MountConnected != true || string.IsNullOrEmpty(command))
            {
                return;
            }

            bool isParked = string.Equals(Mount.MountStatus, "Parked", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(Mount.ParkCommandString, "Unpark", StringComparison.OrdinalIgnoreCase);

            if (command.StartsWith("+") && isParked)
            {
                if (!_parkedWarningShown)
                {
                    _parkedWarningShown = true;
                    var dlg = new DlgMessageBox("Mount is parked. Please unpark before slewing.");
                    dlg.Show();
                }
                return;
            }

            if (!isParked)
            {
                _parkedWarningShown = false;
            }

            if (_lastCommand == command)
            {
                return;
            }

            Log.WriteLine($"MiniCtrl: Send command {command}");
            if (Mount.ChangeSlewingStateCommand != null)
            {
                Mount.ChangeSlewingStateCommand.Execute(command);
            }
            else
            {
                Log.WriteLine("MiniCtrl: ChangeSlewingStateCommand is null");
            }

            _lastCommand = command.StartsWith("-") ? string.Empty : command;
        }
    }
}
