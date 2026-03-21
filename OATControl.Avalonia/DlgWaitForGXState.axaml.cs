using System;
using Avalonia.Controls;
using OATCommunications.CommunicationHandlers;

namespace OATControl.Avalonia
{
    public partial class DlgWaitForGXState : Window
    {
        public bool? DialogResult { get; set; }

        public DlgWaitForGXState()
        {
            InitializeComponent();
            DialogResult = true;
        }

        public DlgWaitForGXState(string title, object mountVM, Action<string, Action<CommandResponse>> sendCommand, Func<string[]?, bool> statusCheck)
        {
            InitializeComponent();
            Title = title;
            // TODO: Implement the actual logic
            DialogResult = true;
        }
    }
}
