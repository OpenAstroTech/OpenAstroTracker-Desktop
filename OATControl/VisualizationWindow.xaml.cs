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
        //VisualizationVM _visualizationVM; 

        public VisualizationWindow(VisualizationVM vizVM)
        {
            InitializeComponent();

            MainLayoutGrid.DataContext = vizVM;
        }
        #region Overrides
        protected override void OnClosing(CancelEventArgs e)
        {
            Settings.Default.VisualizationPos = new System.Drawing.Point((int)this.Left, (int)this.Top);
            base.OnClosing(e);
        }
        #endregion

        /*
        public static readonly DependencyProperty OATModelProperty =
        DependencyProperty.Register("OATModel", typeof(Model3D),
                        typeof(Model3D));
        public Model3D OATModel
        {
            get { return (Model3D)GetValue(OATModelProperty); }
            set { SetValue(OATModelProperty, value); }
        }
        */
    }

}
