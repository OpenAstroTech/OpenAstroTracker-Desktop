using OATControl.Properties;
using OATControl.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OATControl
{
    /// <summary>
    /// Interaction logic for VisualizationWindow.xaml
    /// </summary>
    public partial class VisualizationWindow : Window
    {
        public VisualizationVM visualizationVM;

        public VisualizationWindow(VisualizationVM vizVM)
        {
            InitializeComponent();
            visualizationVM = vizVM; //new VisualizationVM();
            MainLayoutGrid.DataContext = visualizationVM;
        }

        
        #region Overrides
        protected override void OnClosing(CancelEventArgs e)
        {
            Settings.Default.VisualizationPos = new System.Drawing.Point((int)this.Left, (int)this.Top);
            base.OnClosing(e);
        }

        #endregion

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            visualizationVM.viewport = helixViewport;
            visualizationVM.v1 = helixViewport.Viewport;
            visualizationVM.InitScene();
        }
    }

}
