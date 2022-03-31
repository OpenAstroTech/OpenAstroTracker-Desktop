using MahApps.Metro.Converters;
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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OATControl.Controls
{
	/// <summary>
	/// Interaction logic for StopButton.xaml
	/// </summary>
	public partial class StopButton : UserControl
	{
		/// <summary>
		/// The dependency property for the Minimum property
		/// </summary>
		public static readonly DependencyProperty FrameWidthProperty = DependencyProperty.Register(
			"FrameWidth",
			typeof(int),
			typeof(StopButton),
			new PropertyMetadata(2, StopButton.AnyPropertyChanged));


		public static readonly DependencyProperty IsPressedProperty = DependencyProperty.Register(
			"IsPressed",
			typeof(bool),
			typeof(StopButton),
			new PropertyMetadata(false, StopButton.AnyPropertyChanged));

		public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
			"Command",
			typeof(ICommand),
			typeof(StopButton),
			new PropertyMetadata(null, StopButton.AnyPropertyChanged));

		//
		// Summary:
		//     Gets or sets the command to invoke when this button is pressed.
		//
		// Returns:
		//     A command to invoke when this button is pressed. The default value is null.
		[Bindable(true)]
		[Category("Action")]
		[Localizability(LocalizationCategory.NeverLocalize)]
		public ICommand Command 
		{
			get
			{
				return (ICommand)this.GetValue(StopButton.CommandProperty);
			}
			set
			{
				this.SetValue(StopButton.CommandProperty, value);
			}
		}

	
		private static void AnyPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
		{
			var StopButton = obj as StopButton;
		}

		public StopButton()
		{
			InitializeComponent();
		}

		[Bindable(true)]
		public bool IsPressed
		{
			get
			{
				return (bool)this.GetValue(StopButton.IsPressedProperty);
			}
			set
			{
				this.SetValue(StopButton.IsPressedProperty, value);
			}
		}

		public int FrameWidth
		{
			get
			{
				return (int)this.GetValue(StopButton.FrameWidthProperty);
			}
			set
			{
				this.SetValue(StopButton.FrameWidthProperty, value);
			}
		}



		/// <summary>
		/// Handles the OnMouseButtonDown event of the MainGrid control. Captures the mouse and sets the current 
		/// value to the click point. If the textbox currently is active, it is made inactive.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="MouseButtonEventArgs"/> instance containing the event data.</param>
		private void MainGrid_OnMouseButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Pressed)
			{
				IsPressed = true;
				this.MainGrid.CaptureMouse();
			}
		}

		/// <summary>
		/// Handles the OnMouseButtonUp event of the MainGrid control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="MouseButtonEventArgs"/> instance containing the event data.</param>
		private void MainGrid_OnMouseButtonUp(object sender, MouseButtonEventArgs e)
		{
			this.MainGrid.ReleaseMouseCapture();
			IsPressed = false;
			this.Command?.Execute(null);
		}
	}
}
