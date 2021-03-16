using OATControl.ViewModels;
using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace OATControl
{
	/// <summary>
	/// Interaction logic for TargetChooser.xaml
	/// </summary>
	public partial class TargetChooser : Window
	{
		public TargetChooser(MountVM mount)
		{
			this.DataContext = mount;
			InitializeComponent();
		}

		protected void HandleDoubleClick(object sender, MouseButtonEventArgs args)
		{
			var point = (args.Source as ListViewItem).Content as PointOfInterest;
			if (point != null)
			{
				(this.DataContext as MountVM).SelectedPointOfInterest = point;
			}
		}

		private void TextBlock_MouseUp(object sender, MouseButtonEventArgs e)
		{
			TextBlock source = sender as TextBlock;
			//source.Text
		}
	}
}
