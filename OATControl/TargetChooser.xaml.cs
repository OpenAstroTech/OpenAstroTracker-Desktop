using MahApps.Metro.Controls;
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
using System.Windows.Shapes;

namespace OATControl
{
	/// <summary>
	/// Interaction logic for TargetChooser.xaml
	/// </summary>
	public partial class TargetChooser : MetroWindow
	{
		public TargetChooser(MountVM mount)
		{
			this.DataContext = mount;
			InitializeComponent();
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			Settings.Default.TargetChooserPos = new System.Drawing.Point((int)this.Left, (int)this.Top);
			Settings.Default.TargetChooserSize = new System.Drawing.Size((int)this.Width, (int)this.Height);
			base.OnClosing(e);
		}
		
		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);
			(this.DataContext as MountVM).TargetChooserClosed();
		}
		
		protected void HandleDoubleClick(object sender, MouseButtonEventArgs args)
		{
			var point = (args.Source as ListViewItem).Content as PointOfInterest;
			if (point != null)
			{
				(this.DataContext as MountVM).SelectedPointOfInterest = point;
			}
		}
	}
}
