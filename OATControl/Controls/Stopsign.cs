using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace OATControl.Controls
{
	public class StopSign : FrameworkElement
	{
		private Pen _pen;

		public static readonly DependencyProperty FrameWidthProperty = DependencyProperty.Register(
			"FrameWidth",
			typeof(int),
			typeof(StopSign),
			new PropertyMetadata(2, StopSign.SomePropertyChanged));

		public static readonly DependencyProperty BackgroundProperty = DependencyProperty.Register(
			"Background",
			typeof(Brush),
			typeof(StopSign),
			new PropertyMetadata(Brushes.White, StopSign.SomePropertyChanged));

		public static readonly DependencyProperty ForegroundProperty = DependencyProperty.Register(
			"Foreground",
			typeof(Brush),
			typeof(StopSign),
			new PropertyMetadata(Brushes.Black, StopSign.SomePropertyChanged));

		public static readonly DependencyProperty TextColorProperty = DependencyProperty.Register(
			"TextColor",
			typeof(Brush),
			typeof(StopSign),
			new PropertyMetadata(Brushes.White, StopSign.SomePropertyChanged));


		//public static readonly DependencyProperty ScaleProperty = DependencyProperty.Register(
		//	"Scale",
		//	typeof(double),
		//	typeof(StopSign),
		//	new PropertyMetadata(1.0, StopSign.SomePropertyChanged));

		private static void SomePropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
		{
			var pointer = obj as StopSign;
			pointer.InvalidateVisual();
		}

		public Brush Foreground
		{
			get
			{
				return (Brush)this.GetValue(StopSign.ForegroundProperty);
			}
			set
			{
				this.SetValue(StopSign.ForegroundProperty, value);
			}
		}

		public Brush Background
		{
			get
			{
				return (Brush)this.GetValue(StopSign.BackgroundProperty);
			}
			set
			{
				this.SetValue(StopSign.BackgroundProperty, value);
			}
		}

		public Brush TextColor
		{
			get
			{
				return (Brush)this.GetValue(StopSign.TextColorProperty);
			}
			set
			{
				this.SetValue(StopSign.TextColorProperty, value);
			}
		}

		public int FrameWidth
		{
			get
			{
				return (int)this.GetValue(StopSign.FrameWidthProperty);
			}
			set
			{
				this.SetValue(StopSign.FrameWidthProperty, value);
			}
		}

		//protected override Size MeasureOverride(Size availableSize)
		//{
		//	return availableSize;
		//}

		protected override void OnRender(DrawingContext dc)
		{
			//double Scale = 1;

			Point[] stopPts = new Point[]  {
				new Point(0.05, 0.31),
				new Point(0.05, 0.69),
				new Point(0.31, 0.95),
				new Point(0.69, 0.95),
				new Point(0.95, 0.69),
				new Point(0.95, 0.31),
				new Point(0.69, 0.05),
				new Point(0.31, 0.05)
			};

			StreamGeometry streamGeometry = new StreamGeometry();
			using (StreamGeometryContext geometryContext = streamGeometry.Open())
			{
				geometryContext.BeginFigure(new Point(stopPts[0].X * RenderSize.Width, stopPts[0].Y * RenderSize.Height), true, true);
				PointCollection points = new PointCollection();
				foreach (Point p in stopPts.Skip(1))
				{
					points.Add(new Point(p.X * RenderSize.Width, p.Y * RenderSize.Height));
				}
				geometryContext.PolyLineTo(points, true, true);
			}

			streamGeometry.Freeze();
			Pen pen = new Pen(Foreground, FrameWidth);
			dc.DrawGeometry(Background, pen, streamGeometry);
			pen = new Pen(TextColor, 1);
			dc.DrawGeometry(Background, pen, streamGeometry);
			//dc.DrawLine(_pen, p1, p2);
			//dc.DrawLine(_pen, p2, p3);
			//dc.DrawLine(_pen, p3, p1);

			var text = new FormattedText("STOP", new System.Globalization.CultureInfo("en-US"), FlowDirection.LeftToRight, new Typeface("Segoe UI"), RenderSize.Height*0.31, TextColor, 1.0);
			text.TextAlignment = TextAlignment.Center;
			text.SetFontWeight(FontWeight.FromOpenTypeWeight(800));
			dc.DrawText(text, new Point(RenderSize.Width / 2, 0.25 * RenderSize.Height));
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

