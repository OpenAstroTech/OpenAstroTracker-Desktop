using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace OATControl.Controls
{
	public class MotorIndicator : FrameworkElement
	{
		private Pen _pen;
		private Pen _activePen;

		public static readonly DependencyProperty MotorNameProperty = DependencyProperty.Register(
			"MotorName",
			typeof(string),
			typeof(MotorIndicator),
			new PropertyMetadata("", MotorIndicator.SomePropertyChanged));

		public static readonly DependencyProperty IsRunningProperty = DependencyProperty.Register(
			"IsRunning",
			typeof(bool),
			typeof(MotorIndicator),
			new PropertyMetadata(false, MotorIndicator.SomePropertyChanged));

		public static readonly DependencyProperty BackgroundProperty = DependencyProperty.Register(
			"Background",
			typeof(Brush),
			typeof(MotorIndicator),
			new PropertyMetadata(Brushes.White, MotorIndicator.SomePropertyChanged));

		public static readonly DependencyProperty ActiveBackgroundProperty = DependencyProperty.Register(
			"ActiveBackground",
			typeof(Brush),
			typeof(MotorIndicator),
			new PropertyMetadata(Brushes.White, MotorIndicator.SomePropertyChanged));

		public static readonly DependencyProperty ForegroundProperty = DependencyProperty.Register(
			"Foreground",
			typeof(Brush),
			typeof(MotorIndicator),
			new PropertyMetadata(Brushes.Black, MotorIndicator.SomePropertyChanged));

		public static readonly DependencyProperty ActiveForegroundProperty = DependencyProperty.Register(
			"ActiveForeground",
			typeof(Brush),
			typeof(MotorIndicator),
			new PropertyMetadata(Brushes.Black, MotorIndicator.SomePropertyChanged));

		private static void SomePropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
		{
			var pointer = obj as MotorIndicator;
			pointer._pen = new Pen(pointer.Background, 1.0);
			pointer._activePen = new Pen(pointer.ActiveForeground, 1.0);
			pointer.InvalidateVisual();
		}


		public string MotorName
		{
			get
			{
				return (string)this.GetValue(MotorIndicator.MotorNameProperty);
			}
			set
			{
				this.SetValue(MotorIndicator.MotorNameProperty, value);
			}
		}

		public bool IsRunning
		{
			get
			{
				return (bool)this.GetValue(MotorIndicator.IsRunningProperty);
			}
			set
			{
				this.SetValue(MotorIndicator.IsRunningProperty, value);
			}
		}

		public Brush Foreground
		{
			get
			{
				return (Brush)this.GetValue(MotorIndicator.ForegroundProperty);
			}
			set
			{
				this.SetValue(MotorIndicator.ForegroundProperty, value);
			}
		}

		public Brush ActiveForeground
		{
			get
			{
				return (Brush)this.GetValue(MotorIndicator.ActiveForegroundProperty);
			}
			set
			{
				this.SetValue(MotorIndicator.ActiveForegroundProperty, value);
			}
		}

		public Brush Background
		{
			get
			{
				return (Brush)this.GetValue(MotorIndicator.BackgroundProperty);
			}
			set
			{
				this.SetValue(MotorIndicator.BackgroundProperty, value);
			}
		}

		public Brush ActiveBackground
		{
			get
			{
				return (Brush)this.GetValue(MotorIndicator.ActiveBackgroundProperty);
			}
			set
			{
				this.SetValue(MotorIndicator.ActiveBackgroundProperty, value);
			}
		}

		//protected override Size MeasureOverride(Size availableSize)
		//{
		//	return new Size(10, 10);
		//}

		protected override void OnRender(DrawingContext dc)
		{
			//double Scale = 1;
			double rectSize = RenderSize.Height;

			Point p1 = new Point(0.0 * rectSize, 0.00 * rectSize);
			Point p2 = new Point(1.0 * rectSize, 0.00 * rectSize);
			Point p3 = new Point(1.0 * rectSize, 1.00 * rectSize);
			Point p4 = new Point(0.0 * rectSize, 1.00 * rectSize);
			dc.DrawRectangle(IsRunning ? ActiveBackground : Background, null, new Rect(p1, new Size(rectSize, rectSize)));
			dc.DrawRectangle(null, IsRunning ? _activePen : _pen, new Rect(new Point(0.5, 0.5), new Size(rectSize - 1.0, rectSize - 1.0)));
			//dc.DrawLine(_pen, p1, p2);
			//dc.DrawLine(_pen, p2, p3);
			//dc.DrawLine(_pen, p3, p4);
			//dc.DrawLine(_pen, p4, p1);
			var formatText = new FormattedText(
				MotorName,
				CultureInfo.InvariantCulture,
				FlowDirection.LeftToRight,
				new Typeface("Verdana"),
				10,
				IsRunning ? ActiveForeground : Foreground,
				1.0);

			dc.DrawText(formatText, new Point(rectSize + 2, -2.5));

			//Geometry
			//dc.DrawRectangle
			////Point center = new Point(RenderSize.Width / 2, RenderSize.Height / 2);
			//Point cursorPos = new Point(center.X,  center.Y );

			//dc.DrawEllipse(null, _pen, cursorPos, 10, 10);
			//dc.DrawEllipse(null, _pen, cursorPos, 5, 5);

			//Point p1 = new Point(cursorPos.X - 12, cursorPos.Y);
			//Point p2 = new Point(cursorPos.X + 12, cursorPos.Y);
			//dc.DrawLine(_pen, p1, p2);

			//p1 = new Point(cursorPos.X, cursorPos.Y - 12);
			//p2 = new Point(cursorPos.X, cursorPos.Y + 12);
			//dc.DrawLine(_pen, p1, p2);
		}
	}
}
