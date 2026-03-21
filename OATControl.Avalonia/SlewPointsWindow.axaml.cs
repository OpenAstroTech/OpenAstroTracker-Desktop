using Avalonia.Controls;
using OATControl.ViewModels;

namespace OATControl.Avalonia
{
    public partial class SlewPointsWindow : Window
    {
        public SlewPointsWindow()
        {
            InitializeComponent();
        }

        public SlewPointsWindow(MountVM mount)
        {
            DataContext = mount;
            InitializeComponent();
        }
    }
}
