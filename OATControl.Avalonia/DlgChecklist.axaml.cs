using Avalonia.Controls;

namespace OATControl.Avalonia
{
    public partial class DlgChecklist : Window
    {
        public DlgChecklist()
        {
            InitializeComponent();
        }

        public DlgChecklist(string filePath)
        {
            InitializeComponent();
            // TODO: Load checklist from filePath
        }
    }
}
