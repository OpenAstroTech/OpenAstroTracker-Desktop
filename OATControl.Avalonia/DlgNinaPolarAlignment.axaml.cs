using Avalonia.Controls;
using System;
using OATControl.ViewModels;

namespace OATControl.Avalonia
{
    public partial class DlgNinaPolarAlignment : Window, IPolarAlignDialog
    {
        private Action _closeAction;

        public DlgNinaPolarAlignment()
        {
            _closeAction = () => { };
            InitializeComponent();
        }

        public DlgNinaPolarAlignment(Action closeAction)
        {
            _closeAction = closeAction;
            InitializeComponent();
        }

        public void SetStatus(string statusType, string message)
        {
            // TODO: Implement status display
        }
    }
}
