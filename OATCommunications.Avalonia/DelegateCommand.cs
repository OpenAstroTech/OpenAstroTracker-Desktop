using System;
using System.Windows.Input;

namespace OATCommunications.Avalonia
{
    public class DelegateCommand : ICommand
    {
        Action<object?> _commandAction;
        Func<object?, bool> _commandEnabledAction;

        public DelegateCommand(Action<object?> command)
        {
            _commandAction = command;
            _commandEnabledAction = _ => true;
        }

        public DelegateCommand(Action<object?> command, Func<bool> commandEnabled)
        {
            _commandAction = command;
            _commandEnabledAction = _ => commandEnabled();
        }

        public DelegateCommand(Action command) : this(_ => command()) { }

        public DelegateCommand(Action command, Func<bool> commandEnabled)
            : this(_ => command(), _ => commandEnabled()) { }

        public DelegateCommand(Action<object?> command, Func<object?, bool> commandEnabled)
        {
            _commandAction = command;
            _commandEnabledAction = commandEnabled;
        }

        public bool CanExecute(object? parameter) => _commandEnabledAction(parameter);

        public event EventHandler? CanExecuteChanged;

        public void Execute(object? parameter) => _commandAction(parameter);

        public void Requery() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
