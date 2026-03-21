using Avalonia.Controls;
using System;
using OATControl.ViewModels;

namespace OATControl.Avalonia
{
    public partial class DlgSharpCapPolarAlignment : Window, IPolarAlignDialog
    {
        private Action _closeAction;

        public DlgSharpCapPolarAlignment()
        {
            _closeAction = () => { };
            InitializeComponent();
        }

        public DlgSharpCapPolarAlignment(Action closeAction)
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
