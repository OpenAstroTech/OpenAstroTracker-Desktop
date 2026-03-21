using Avalonia.Controls;
using OATControl.ViewModels;

namespace OATControl.Avalonia
{
    public partial class DlgAxisCalibration : Window
    {
        public DlgAxisCalibration()
        {
            InitializeComponent();
        }

        public DlgAxisCalibration(MountVM mountVM)
        {
            InitializeComponent();
            // TODO: Initialize with mount VM
        }
    }
}