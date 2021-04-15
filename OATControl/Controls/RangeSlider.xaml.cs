namespace OATControl.Controls
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Input;
	using System.Windows.Media;
	using System.Windows.Shapes;

	/// <summary>
	/// Interaction logic for RangeSlider.xaml
	/// This is a range slider that has a current value indicater. Range can be edited.
	/// 
	/// The typical usage in a XAML file is:
	///           &lt;RangeSlider Padding="3" 
	///                 AxisLabel="Y Axis" 
	///                 Minimum="0" 
	///                 Maximum="112" 
	///                 Value="{Binding Degrees, Mode=TwoWay}" 
	///                 TickStart="0" 
	///                 TickEnd="120" 
	///                 TickMinorSpacing="10" 
	///                 TickMajorSpacing="50" 
	///                 TickLabels="0|50|100|120" 
	///                /&gt;
	/// 
	/// - TickLabels are optional; if they are left off, no labels will be displayed next to the slider.
	/// 
	/// </summary>
	public partial class RangeSlider : UserControl
	{
		const double ValueIndicatorSize = 12;
		const double LabelFontSize = 9;
		const int MajorTickLength = 5;
		const int MinorTickLength = 3;

		/// <summary>
		/// The dependency property for the Minimum property
		/// </summary>
		public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
			"Minimum",
			typeof(double),
			typeof(RangeSlider),
			new PropertyMetadata(0.0, RangeSlider.SliderPropertyChanged));

		/// <summary>
		/// The dependency property for the Maximum property
		/// </summary>
		public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
			"Maximum",
			typeof(double),
			typeof(RangeSlider),
			new PropertyMetadata(1.0, RangeSlider.SliderPropertyChanged));

		/// <summary>
		/// The dependency property for the Value property
		/// </summary>
		public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
			"Value",
			typeof(double),
			typeof(RangeSlider),
			new FrameworkPropertyMetadata(
				0.5,
				FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
				RangeSlider.ValueChanged,
				RangeSlider.CoerceValueCallback));

		/// <summary>
		/// The dependency property for the Value property
		/// </summary>
		public static readonly DependencyProperty MarkerValueProperty = DependencyProperty.Register(
			"MarkerValue",
			typeof(double),
			typeof(RangeSlider),
			new FrameworkPropertyMetadata(0.0, RangeSlider.MarkerValueChanged));

		/// <summary>
		/// The dependency property for the Value property
		/// </summary>
		public static readonly DependencyProperty LowerLimitProperty = DependencyProperty.Register(
			"LowerLimit",
			typeof(double),
			typeof(RangeSlider),
			new FrameworkPropertyMetadata(
				0.25,
				FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
				RangeSlider.LimitsChanged,
				RangeSlider.CoerceValueCallback));

		/// <summary>
		/// The dependency property for the UpperLimit property
		/// </summary>
		public static readonly DependencyProperty UpperLimitProperty = DependencyProperty.Register(
			"UpperLimit",
			typeof(double),
			typeof(RangeSlider),
			new FrameworkPropertyMetadata(
				0.75,
				FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
				RangeSlider.LimitsChanged,
				RangeSlider.CoerceValueCallback));

		/// <summary>
		/// The dependency property for the ShowLimits property
		/// </summary>
		public static readonly DependencyProperty ShowLimitsProperty = DependencyProperty.Register(
			"ShowLimits",
			typeof(bool),
			typeof(RangeSlider),
			new PropertyMetadata(false));

		/// <summary>
		/// The dependency property for the ShowLimits property
		/// </summary>
		public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
			"Orientation",
			typeof(Orientation),
			typeof(RangeSlider),
			new PropertyMetadata(Orientation.Vertical));

		/// <summary>
		/// The dependency property for the TickStart property
		/// </summary>
		public static readonly DependencyProperty TickStartProperty = DependencyProperty.Register(
			"TickStart",
			typeof(double),
			typeof(RangeSlider),
			new PropertyMetadata(0.0, RangeSlider.SliderPropertyChanged));

		/// <summary>
		/// The dependency property for the TickEnd property
		/// </summary>
		public static readonly DependencyProperty TickEndProperty = DependencyProperty.Register(
			"TickEnd",
			typeof(double),
			typeof(RangeSlider),
			new PropertyMetadata(1.0, RangeSlider.SliderPropertyChanged));

		/// <summary>
		/// The dependency property for the TickMinorSpacing property
		/// </summary>
		public static readonly DependencyProperty TickMinorSpacingProperty = DependencyProperty.Register(
			"TickMinorSpacing",
			typeof(double),
			typeof(RangeSlider),
			new PropertyMetadata(0.05, RangeSlider.SliderPropertyChanged));

		/// <summary>
		/// The dependency property for the TickMajorSpacing property
		/// </summary>
		public static readonly DependencyProperty TickMajorSpacingProperty = DependencyProperty.Register(
			"TickMajorSpacing",
			typeof(double),
			typeof(RangeSlider),
			new PropertyMetadata(0.2, RangeSlider.SliderPropertyChanged));

		/// <summary>
		/// The dependency property for the TickLabels property
		/// </summary>
		public static readonly DependencyProperty TickLabelsProperty = DependencyProperty.Register(
			"TickLabels",
			typeof(string),
			typeof(RangeSlider),
			new PropertyMetadata(string.Empty, RangeSlider.SliderPropertyChanged));

		/// <summary>
		/// The dependency property for the AxisLabel property
		/// </summary>
		public static readonly DependencyProperty AxisLabelProperty = DependencyProperty.Register(
			"AxisLabel",
			typeof(string),
			typeof(RangeSlider),
			new PropertyMetadata(string.Empty));

		/// <summary>
		/// The set part Height property key (read-only dependancy property)
		/// </summary>
		private static readonly DependencyPropertyKey SliderPartHeightPropertyKey = DependencyProperty.RegisterReadOnly(
			"SliderPartHeight",
			typeof(GridLength),
			typeof(RangeSlider),
			new PropertyMetadata(new GridLength(1, GridUnitType.Star)));

		/// <summary>
		/// The set part Height property (read-only dependancy property)
		/// </summary>
		private static readonly DependencyProperty SliderPartHeightProperty = RangeSlider.SliderPartHeightPropertyKey.DependencyProperty;

		/// <summary>
		/// The set part Height property key (read-only dependancy property)
		/// </summary>
		private static readonly DependencyPropertyKey UpperPartHeightPropertyKey = DependencyProperty.RegisterReadOnly(
			"UpperPartHeight",
			typeof(GridLength),
			typeof(RangeSlider),
			new PropertyMetadata(new GridLength(1, GridUnitType.Star)));

		/// <summary>
		/// The set part Height property (read-only dependancy property)
		/// </summary>
		static readonly DependencyProperty UpperPartHeightProperty = RangeSlider.UpperPartHeightPropertyKey.DependencyProperty;

		/// <summary>
		/// The set part Height property key (read-only dependancy property)
		/// </summary>
		private static readonly DependencyPropertyKey LowerPartHeightPropertyKey = DependencyProperty.RegisterReadOnly(
			"LowerPartHeight",
			typeof(GridLength),
			typeof(RangeSlider),
			new PropertyMetadata(new GridLength(1, GridUnitType.Star)));


		/// <summary>
		/// The set part Height property (read-only dependancy property)
		/// </summary>
		private static readonly DependencyProperty LowerPartHeightProperty = RangeSlider.LowerPartHeightPropertyKey.DependencyProperty;

		/// <summary>
		/// The set part Y property key (read-only dependancy property)
		/// </summary>
		private static readonly DependencyPropertyKey ValueMarginPropertyKey = DependencyProperty.RegisterReadOnly(
			"ValueMargin",
			typeof(Thickness),
			typeof(RangeSlider),
			new PropertyMetadata(new Thickness()));

		/// <summary>
		/// The set part Height property (read-only dependancy property)
		/// </summary>
		private static readonly DependencyProperty ValueMarginProperty = RangeSlider.ValueMarginPropertyKey.DependencyProperty;


		/// <summary>
		/// The set part Y property key (read-only dependancy property)
		/// </summary>
		private static readonly DependencyPropertyKey MarkerValueMarginPropertyKey = DependencyProperty.RegisterReadOnly(
			"MarkerValueMargin",
			typeof(Thickness),
			typeof(RangeSlider),
			new PropertyMetadata(new Thickness()));

		/// <summary>
		/// The set part Height property (read-only dependancy property)
		/// </summary>
		private static readonly DependencyProperty MarkerValueMarginProperty = RangeSlider.MarkerValueMarginPropertyKey.DependencyProperty;


		/// <summary>
		/// The set part Height property key (read-only dependancy property)
		/// </summary>
		private static readonly DependencyPropertyKey SliderPartWidthPropertyKey = DependencyProperty.RegisterReadOnly(
			"SliderPartWidth",
			typeof(double),
			typeof(RangeSlider),
			new PropertyMetadata(20.0));

		/// <summary>
		/// The set part Height property (read-only dependancy property)
		/// </summary>
		private static readonly DependencyProperty SliderPartWidthProperty = RangeSlider.SliderPartWidthPropertyKey.DependencyProperty;

		/// <summary>
		/// The label row height property key (read-only dependancy property)
		/// </summary>
		private static readonly DependencyProperty LabelColumnSizeProperty = DependencyProperty.Register(
			"LabelColumnSize",
			typeof(double),
			typeof(RangeSlider),
			new PropertyMetadata(0.0, LabelSizeChanged));

		private static void LabelSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var slider = d as RangeSlider;
			slider.CalculatePartHeights();
		}

		private readonly List<Tuple<double, string>> labelList = new List<Tuple<double, string>>();
		private double rounder = 0.0;
		private string stringFormat = "0.00";
		private Color _majorTickColor = Colors.White;
		private SolidColorBrush _majorTickBrush = Brushes.White;
		private Color _minorTickColor = Colors.Gray;
		private SolidColorBrush _minorTickBrush = Brushes.Gray;
		private Color _tickLabelColor = Colors.White;
		private SolidColorBrush _tickLabelBrush = Brushes.White;


		private bool _draggingUpper = false;
		private bool _draggingLower = false;

		/// <summary>
		/// Initializes a new instance of the <see cref="RangeSlider"/> class.
		/// </summary>
		public RangeSlider()
		{
			this.InitializeComponent();
		}

		/// <summary>
		/// Gets or sets the tick start location. 
		/// Minor ticks are drawn starting at this value, increasing by TickMinorSpacing until we are past TickEnd. 
		/// Major ticks are drawn starting at this value, increasing by TickMajorSpacing until we are past TickEnd. 
		/// Major ticks take precedence over minor ticks.
		/// Only ticks that are within Minimum and Maximum (inclusive) are drawn.
		/// </summary>
		public double TickStart
		{
			get
			{
				return (double)this.GetValue(RangeSlider.TickStartProperty);
			}
			set
			{
				this.SetValue(RangeSlider.TickStartProperty, value);
			}
		}

		/// <summary>
		/// Gets or sets the tick end location. 
		/// Minor ticks are drawn starting at TickStart, increasing by TickMinorSpacing until we are past this value. 
		/// Major ticks are drawn starting at TickStart, increasing by TickMajorSpacing until we are past this value. 
		/// Major ticks take precedence over minor ticks.
		/// Only ticks that are within Minimum and Maximum (inclusive) are drawn.
		/// </summary>
		public double TickEnd
		{
			get
			{
				return (double)this.GetValue(RangeSlider.TickEndProperty);
			}
			set
			{
				this.SetValue(RangeSlider.TickEndProperty, value);
			}
		}

		/// <summary>
		/// Gets or sets the minor tick spacing. 
		/// Minor ticks are drawn starting at TickStart, increasing by this value until we are past TickEnd. 
		/// Only ticks that are within Minimum and Maximum (inclusive) are drawn.
		/// </summary>
		public double TickMinorSpacing
		{
			get
			{
				return (double)this.GetValue(RangeSlider.TickMinorSpacingProperty);
			}
			set
			{
				this.SetValue(RangeSlider.TickMinorSpacingProperty, value);
			}
		}

		/// <summary>
		/// Gets or sets the major tick spacing. 
		/// Major ticks are drawn starting at TickStart, increasing by this value until we are past TickEnd. 
		/// Only ticks that are within Minimum and Maximum (inclusive) are drawn.
		/// </summary>
		public double TickMajorSpacing
		{
			get
			{
				return (double)this.GetValue(RangeSlider.TickMajorSpacingProperty);
			}
			set
			{
				this.SetValue(RangeSlider.TickMajorSpacingProperty, value);
			}
		}

		/// <summary>
		/// Gets or sets the tick labels. This is a string with pipe-delimited labels. 
		/// Each label should be either a number, or a number followed by a comma and a label.
		/// If only a number is found the number is rendered at the location indicated by it.
		/// If a label is also present, the label is rendered at the location indicated by the number.
		/// </summary>
		public string TickLabels
		{
			get
			{
				return (string)this.GetValue(RangeSlider.TickLabelsProperty);
			}
			set
			{
				this.SetValue(RangeSlider.TickLabelsProperty, value);
			}
		}

		/// <summary>
		/// Gets or sets the current value of the slider.
		/// </summary>
		public double Value
		{
			get
			{
				return (double)this.GetValue(RangeSlider.ValueProperty);
			}
			set
			{
				this.SetValue(RangeSlider.ValueProperty, value);
			}
		}

		/// <summary>
		/// Gets or sets the current value of the slider.
		/// </summary>
		public double MarkerValue
		{
			get
			{
				return (double)this.GetValue(RangeSlider.MarkerValueProperty);
			}
			set
			{
				this.SetValue(RangeSlider.MarkerValueProperty, value);
			}
		}

		/// <summary>
		/// Gets or sets the minimum value of the slider range.
		/// </summary>
		public double Minimum
		{
			get
			{
				return (double)this.GetValue(RangeSlider.MinimumProperty);
			}
			set
			{
				this.SetValue(RangeSlider.MinimumProperty, value);
			}
		}

		/// <summary>
		/// Gets or sets the maximum value of the slider range.
		/// </summary>
		public double Maximum
		{
			get
			{
				return (double)this.GetValue(RangeSlider.MaximumProperty);
			}
			set
			{
				this.SetValue(RangeSlider.MaximumProperty, value);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public bool ShowLimits
		{
			get
			{
				return (bool)this.GetValue(RangeSlider.ShowLimitsProperty);
			}
			set
			{
				this.SetValue(RangeSlider.ShowLimitsProperty, value);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public Orientation Orientation
		{
			get
			{
				return (Orientation)this.GetValue(RangeSlider.OrientationProperty);
			}
			set
			{
				this.SetValue(RangeSlider.OrientationProperty, value);
			}
		}

		/// <summary>
		/// Gets or sets the lower limit value of the slider range.
		/// </summary>
		public double LowerLimit
		{
			get
			{
				return (double)this.GetValue(RangeSlider.LowerLimitProperty);
			}
			set
			{
				this.SetValue(RangeSlider.LowerLimitProperty, value);
			}
		}

		/// <summary>
		/// Gets or sets the lower limit value of the slider range.
		/// </summary>
		public double UpperLimit
		{
			get
			{
				return (double)this.GetValue(RangeSlider.UpperLimitProperty);
			}
			set
			{
				this.SetValue(RangeSlider.UpperLimitProperty, value);
			}
		}

		/// <summary>
		/// Gets or sets the text of the AxisLabel displayed inside slider on the left.
		/// </summary>
		public string AxisLabel
		{
			get
			{
				return (string)this.GetValue(RangeSlider.AxisLabelProperty);
			}
			set
			{
				this.SetValue(RangeSlider.AxisLabelProperty, value);
			}
		}

		/// <summary>
		/// Gets or sets the minimum value of the slider range.
		/// </summary>
		private GridLength UpperPartHeight
		{
			get
			{
				return (GridLength)this.GetValue(RangeSlider.UpperPartHeightProperty);
			}
			set
			{
				this.SetValue(RangeSlider.UpperPartHeightProperty, value);
			}
		}

		/// <summary>
		/// Gets or sets the minimum value of the slider range.
		/// </summary>
		private GridLength LowerPartHeight
		{
			get
			{
				return (GridLength)this.GetValue(RangeSlider.LowerPartHeightProperty);
			}
			set
			{
				this.SetValue(RangeSlider.LowerPartHeightProperty, value);
			}
		}

		/// <summary>
		/// Gets or sets the minimum value of the slider range.
		/// </summary>
		private GridLength SliderPartHeight
		{
			get
			{
				return (GridLength)this.GetValue(RangeSlider.SliderPartHeightProperty);
			}
			set
			{
				this.SetValue(RangeSlider.SliderPartHeightProperty, value);
			}
		}

		/// <summary>
		/// Gets or sets the minimum value of the slider range.
		/// </summary>
		private Thickness ValueMargin
		{
			get
			{
				return (Thickness)this.GetValue(RangeSlider.ValueMarginProperty);
			}
			set
			{
				this.SetValue(RangeSlider.ValueMarginPropertyKey, value);
			}
		}

		/// <summary>
		/// Gets or sets the minimum value of the slider range.
		/// </summary>
		private Thickness MarkerValueMargin
		{
			get
			{
				return (Thickness)this.GetValue(RangeSlider.MarkerValueMarginProperty);
			}
			set
			{
				this.SetValue(RangeSlider.MarkerValueMarginPropertyKey, value);
			}
		}

		bool IsHorizontal { get { return Orientation == Orientation.Horizontal; } }

		bool IsVertical { get { return Orientation == Orientation.Vertical; } }

		/// <summary>
		/// Gets or sets the minimum value of the slider range.
		/// </summary>
		private double SliderPartWidth
		{
			get
			{
				return (double)this.GetValue(RangeSlider.SliderPartWidthProperty);
			}
			set
			{
				this.SetValue(RangeSlider.SliderPartWidthProperty, value);
			}
		}


		/// <summary>
		/// Gets or sets the minimum value of the slider range.
		/// </summary>
		public double LabelColumnSize
		{
			get
			{
				return (double)this.GetValue(RangeSlider.LabelColumnSizeProperty);
			}
			set
			{
				this.SetValue(RangeSlider.LabelColumnSizeProperty, value);
			}
		}

		/// <summary>
		/// Called when the <see cref="E:System.Windows.FrameworkElement.SizeChanged" /> event is raised. Used to regenerate the ticks and labels.
		/// </summary>
		/// <param name="sizeInfo">Details of the old and new size involved in the change.</param>
		protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
		{
			base.OnRenderSizeChanged(sizeInfo);
			this.RecalculateLabelsAndTicks();
		}

		/// <summary>
		/// Parses the labels property for the list of labels to render.
		/// </summary>
		private void ParseLabels()
		{
			var labelPairs = this.TickLabels.Split("|".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
			this.labelList.Clear();
			foreach (var pair in labelPairs)
			{
				var label = pair.Split(",".ToCharArray());
				var stringIndex = (label.Length == 1) ? 0 : 1;
				double location;
				if (double.TryParse(label[0], out location))
				{
					this.labelList.Add(Tuple.Create(location, label[stringIndex]));
				}
			}

			LabelColumnSize = this.labelList.Any() ? 30 : 0;
		}

		private double UsableSize
		{
			get
			{
				return Orientation == Orientation.Vertical ? this.ActualHeight - ValueIndicatorSize : this.ActualWidth - ValueIndicatorSize;
			}
		}

		/// <summary>
		/// Calculates the widths of the set and unset parts of the slider.
		/// </summary>
		private void CalculatePartHeights()
		{
			double lowerPartHeight, sliderPartHeight, upperPartHeight, sliderPartWidth, valueYPos, markerValueYPos;
			if (this.ActualHeight == 0)
			{
				sliderPartHeight = 1.0;
				sliderPartWidth = 0;
				lowerPartHeight = 0;
				upperPartHeight = 0;
				valueYPos = 0;
				markerValueYPos = 0;
			}
			else
			{
				double lowerPos = GetLocation(this.LowerLimit);
				double upperPos = GetLocation(this.UpperLimit);

				if (IsVertical)
				{
					lowerPartHeight = this.UsableSize * (1.0 - lowerPos);
					sliderPartHeight = this.UsableSize * (lowerPos - upperPos);
					upperPartHeight = this.UsableSize * (upperPos);
				}
				else
				{
					lowerPartHeight = this.UsableSize * (lowerPos);
					sliderPartHeight = this.UsableSize * (upperPos - lowerPos);
					upperPartHeight = this.UsableSize * (1.0 - upperPos);
				}

				valueYPos = this.UsableSize * GetLocation(this.Value);
				markerValueYPos = this.UsableSize * GetLocation(this.MarkerValue);

				sliderPartWidth = (IsVertical ? this.ActualWidth : this.ActualHeight) - LabelColumnSize;
			}

			this.SetValue(RangeSlider.SliderPartHeightPropertyKey, new GridLength(sliderPartHeight, GridUnitType.Star));
			this.SetValue(RangeSlider.UpperPartHeightPropertyKey, new GridLength(upperPartHeight, GridUnitType.Star));
			this.SetValue(RangeSlider.LowerPartHeightPropertyKey, new GridLength(lowerPartHeight, GridUnitType.Star));
			this.SetValue(RangeSlider.SliderPartWidthPropertyKey, sliderPartWidth);
			this.SetValue(RangeSlider.ValueMarginPropertyKey, new Thickness(IsVertical ? 0 : valueYPos, IsVertical ? valueYPos : 0, 0, 0));
			this.SetValue(RangeSlider.MarkerValueMarginPropertyKey, new Thickness(IsVertical ? 0 : markerValueYPos, IsVertical ? markerValueYPos : 0, 0, 0));
		}

		/// <summary>
		/// Recalculates the label and tick lists.
		/// </summary>
		private void RecalculateLabelsAndTicks()
		{
			//this.TickCanvas.Children.Clear();
			this.TickLabelCanvas.Children.Clear();

			if ((this.TickStart != this.TickEnd) && (this.Minimum != this.Maximum))
			{
				var lines = new Dictionary<string, Line>();

				this.InsertMinorTicks(lines);
				this.InsertMajorTicks(lines);

				// Now add the resulting list to the canvas.
				foreach (Line line in lines.Values)
				{
					this.TickLabelCanvas.Children.Add(line);
				}
			}

			// Check whether there are any labels to add to this control
			if (this.labelList.Any() && (this.Minimum != this.Maximum))
			{
				this.InsertTickLabels();
			}

			// Update the slider position.
			this.CalculatePartHeights();
		}

		internal double GetLocation(double pos)
		{
			double location = (pos - this.Minimum) / (this.Maximum - this.Minimum);
			if (Orientation == Orientation.Vertical)
			{
				location = 1.0 - location;
			}
			return location;
		}

		/// <summary>
		/// Adds the tick labels to the canvas of the control.
		/// </summary>
		private void InsertTickLabels()
		{
			foreach (var label in this.labelList)
			{
				var textBlock = new TextBlock
				{
					Text = label.Item2,
					FontSize = LabelFontSize,
					Foreground = _tickLabelBrush,
					Background = Brushes.Transparent,
					HorizontalAlignment = IsVertical ? HorizontalAlignment.Right : HorizontalAlignment.Center,
					VerticalAlignment = IsVertical ? VerticalAlignment.Center : VerticalAlignment.Bottom,
					Margin = new Thickness(0.0),
					Height = ValueIndicatorSize
				};

				// Calculate the normalized (0..1) horizontal location of the label (which is centered below this)
				double location = GetLocation(label.Item1);

				// Scale to the actual width of the grid, but place it 1.5pixels inside the range..
				location = location * (this.UsableSize - 1);

				this.TickLabelCanvas.Children.Add(textBlock);

				// Now position the label accordingly
				if (IsVertical)
				{
					textBlock.SetValue(Canvas.TopProperty, location);
					textBlock.SetValue(Canvas.RightProperty, 2.0 + MajorTickLength);
				}
				else
				{
					textBlock.SetValue(Canvas.LeftProperty, location + (LabelFontSize/3) - ((label.Item2.Length-1) * 0.75 * (LabelFontSize/3)));
					textBlock.SetValue(Canvas.BottomProperty, 2.0 + MajorTickLength);
				}
			}

			var rotate90CCW = new RotateTransform(IsVertical ? -90 : 0);

			var axisLabelBlock = new TextBlock()
			{
				Text = AxisLabel,
				FontSize = LabelFontSize * 1.5,
				Foreground = _tickLabelBrush,
				Background = Brushes.Transparent,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				Margin = new Thickness(0.0),
				RenderTransformOrigin = new Point(1.0, 0.5),
				RenderTransform = rotate90CCW,
			};

			this.TickLabelCanvas.Children.Add(axisLabelBlock);
			if (IsVertical)
			{
				axisLabelBlock.SetValue(Canvas.TopProperty, (this.ActualHeight / 2) - (AxisLabel.Length * 0.4 * LabelFontSize));
				axisLabelBlock.SetValue(Canvas.RightProperty, axisLabelBlock.FontSize + 9.0 + MajorTickLength);
			}
			else
			{
				axisLabelBlock.SetValue(Canvas.LeftProperty, (this.ActualWidth / 2) - (AxisLabel.Length * 0.4 * LabelFontSize));
				axisLabelBlock.SetValue(Canvas.BottomProperty, axisLabelBlock.FontSize + 5.0 + MajorTickLength);
			}

		}

		/// <summary>
		/// Inserts the minor ticks into the given dictionary. The key for this dictionary is the string representation
		/// of the location on the axis. This is used to make sure major ticks overwrite minor ticks and don't
		/// introduce floating point errors when comparing.
		/// </summary>
		/// <param name="lines">The dictionary containing the minor and major tick lines.</param>
		private void InsertMinorTicks(Dictionary<string, Line> lines)
		{
			// Minor tick lines
			if (this.TickMinorSpacing > 0)
			{
				for (double start = this.TickStart; start <= this.TickEnd; start += this.TickMinorSpacing)
				{
					var key = start.ToString(this.stringFormat);
					var location = GetLocation(start);
					if ((location >= 0.0) && (location <= 1.0))
					{
						// We scale the axis so that the minimum and maximum are just inside the control.
						location = location * (this.UsableSize - 1) + ValueIndicatorSize / 2;
						Line line = new Line
						{
							Y1 = IsVertical ? location : LabelColumnSize,
							Y2 = IsVertical ? location : LabelColumnSize - MinorTickLength,
							X1 = IsVertical ? LabelColumnSize : location,
							X2 = IsVertical ? LabelColumnSize - MinorTickLength : location,
							Stroke = _minorTickBrush,
							SnapsToDevicePixels = true
						};
						lines[key] = line;

					}
				}
			}
		}


		/// <summary>
		/// Inserts the major ticks into the given dictionary. The key for this dictionary is the string representation
		/// of the location on the axis. This is used to make sure major ticks overwrite minor ticks and don't
		/// introduce floating point errors when comparing.
		/// </summary>
		/// <param name="lines">The dictionary containing the minor and major tick lines.</param>
		private void InsertMajorTicks(Dictionary<string, Line> lines)
		{
			if (this.TickMajorSpacing > 0)
			{
				for (double start = this.TickStart; start <= this.TickEnd; start += this.TickMajorSpacing)
				{
					var key = start.ToString(this.stringFormat);
					double location = GetLocation(start);
					if ((location >= 0.0) && (location <= 1.0))
					{
						// We scale the axis so that the minimum and maximum are just inside the control.
						location = location * (this.UsableSize - 1) + ValueIndicatorSize / 2;
						Line line = new Line
						{
							Y1 = IsVertical ? location : LabelColumnSize,
							Y2 = IsVertical ? location : LabelColumnSize - MajorTickLength,
							X1 = IsVertical ? LabelColumnSize : location,
							X2 = IsVertical ? LabelColumnSize - MajorTickLength : location,
							Stroke = _majorTickBrush,
							SnapsToDevicePixels = true
						};

						lines[key] = line;
					}
				}
			}
		}

		public Color MajorTickColor
		{
			get { return _majorTickColor; }
			set
			{
				if (value != _majorTickColor)
				{
					_majorTickColor = value;
					_majorTickBrush = new SolidColorBrush(_majorTickColor);
				}
			}
		}

		public Color MinorTickColor
		{
			get { return _minorTickColor; }
			set
			{
				if (value != _minorTickColor)
				{
					_minorTickColor = value;
					_minorTickBrush = new SolidColorBrush(_minorTickColor);
				}
			}
		}

		public Color TickLabelColor
		{
			get { return _tickLabelColor; }
			set
			{
				if (value != _tickLabelColor)
				{
					_tickLabelColor = value;
					_tickLabelBrush = new SolidColorBrush(_tickLabelColor);
				}
			}
		}

		/// <summary>
		/// Called by the dependency property when any of the slider properties have changed.
		/// </summary>
		/// <param name="obj">The slider object that caused the change.</param>
		/// <param name="e">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
		private static void SliderPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
		{
			bool callLabelRecalc = true;
			RangeSlider slider = obj as RangeSlider;

			switch (e.Property.Name)
			{
				case "Maximum":
				case "Minimum":
				case "TickStart":
				case "TickEnd":
				case "TickMinorSpacing":
				case "TickMajorSpacing":
					// No extra processing neccessary
					break;

				case "TickLabels":
					slider.ParseLabels();
					break;

				default:
					callLabelRecalc = false;
					break;
			}

			if (callLabelRecalc)
			{
				slider.RecalculateLabelsAndTicks();
			}

		}

		/// <summary>
		/// Called by the dependency property when the Value has changed.
		/// </summary>
		/// <param name="obj">The object.</param>
		/// <param name="e">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
		private static void ValueChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
		{
			RangeSlider slider = obj as RangeSlider;
			if (slider == null)
			{
				return;
			}
			if (slider.ActualHeight > 0)
			{
				double valueYPos = slider.UsableSize * slider.GetLocation(slider.Value);
				slider.SetValue(RangeSlider.ValueMarginPropertyKey, new Thickness(slider.IsVertical ? 0 : valueYPos, slider.IsVertical ? valueYPos : 0, 0, 0));
			}
		}

		/// <summary>
		/// Called by the dependency property when the Value has changed.
		/// </summary>
		/// <param name="obj">The object.</param>
		/// <param name="e">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
		private static void MarkerValueChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
		{
			RangeSlider slider = obj as RangeSlider;
			if (slider == null)
			{
				return;
			}
			if (slider.ActualHeight > 0)
			{
				double markerValueYPos = slider.UsableSize * slider.GetLocation(slider.MarkerValue);
				slider.SetValue(RangeSlider.MarkerValueMarginPropertyKey, new Thickness(slider.IsVertical ? 0 : markerValueYPos, slider.IsVertical ? markerValueYPos : 0, 0, 0));
			}
		}

		/// <summary>
		/// Called by the dependency property when the Value has changed.
		/// </summary>
		/// <param name="obj">The object.</param>
		/// <param name="e">The <see cref="DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
		private static void LimitsChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
		{
			RangeSlider slider = obj as RangeSlider;
			if (slider == null)
			{
				return;
			}

			slider.CalculatePartHeights();
		}

		/// <summary>
		/// Coerces the given value to a valid range.
		/// </summary>
		/// <param name="dependencyObject">The dependency object.</param>
		/// <param name="baseValue">The base value.</param>
		/// <returns></returns>
		private static object CoerceValueCallback(DependencyObject dependencyObject, object baseValue)
		{
			RangeSlider slider = dependencyObject as RangeSlider;
			if (slider == null)
			{
				return null;
			}

			var newVal = (double)baseValue;

			// Check if we need to round it
			if (slider.rounder != 0.0)
			{
				newVal = Math.Round(slider.rounder * newVal) / slider.rounder;
			}

			// Make sure it's within our defined range
			newVal = Math.Min(Math.Max(slider.Minimum, newVal), slider.Maximum);

			return newVal;
		}

		Grid MainGrid { get { return Orientation == Orientation.Vertical ? this.MainVGrid : this.MainHGrid; } }
		Canvas TickLabelCanvas { get { return Orientation == Orientation.Vertical ? this.TickLabelVCanvas : this.TickLabelHCanvas; } }

		/// <summary>
		/// Handles the OnMouseButtonDown event of the MainGrid control. Captures the mouse and sets the current 
		/// value to the click point. If the textbox currently is active, it is made inactive.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="MouseButtonEventArgs"/> instance containing the event data.</param>
		private void MainGrid_OnMouseButtonDown(object sender, MouseButtonEventArgs e)
		{
			if ((e.LeftButton == MouseButtonState.Pressed) && ShowLimits)
			{
				Point pt = e.GetPosition(this.MainGrid);

				double cursorPos = IsVertical ? pt.Y - (ValueIndicatorSize / 2) : pt.X - (ValueIndicatorSize / 2);
				double lowerPos = this.UsableSize * GetLocation(this.LowerLimit);
				double upperPos = this.UsableSize * GetLocation(this.UpperLimit);

				if (Math.Abs(cursorPos - upperPos) < 6)
				{
					_draggingUpper = true;
					_draggingLower = false;
					this.MainGrid.CaptureMouse();
				}
				else if (Math.Abs(cursorPos - lowerPos) < 6)
				{
					_draggingUpper = false;
					_draggingLower = true;
					this.MainGrid.CaptureMouse();
				}
				e.Handled = true;
			}
		}

		/// <summary>
		/// Sets the value from mouse location.
		/// </summary>
		/// <param name="e">The <see cref="MouseEventArgs"/> instance containing the event data.</param>
		private void SetValueFromMouse(MouseEventArgs e)
		{
			Point pt = e.GetPosition(this.MainGrid);
			double normalized = 1.0;
			double clickValue = 0;

			if (IsVertical)
			{
				normalized = 1.0 * (pt.Y - ValueIndicatorSize / 2) / (this.UsableSize - 1);
				clickValue = this.Maximum - (float)Math.Min(1.0, Math.Max(0, normalized)) * (this.Maximum - this.Minimum);
			}
			else
			{
				normalized = 1.0 * (pt.X - ValueIndicatorSize / 2) / (this.UsableSize - 1);
				clickValue = this.Minimum + (float)Math.Min(1.0, Math.Max(0, normalized)) * (this.Maximum - this.Minimum);
			}

			if (_draggingUpper)
			{
				this.UpperLimit = Math.Max(clickValue, this.LowerLimit);
			}
			else if (_draggingLower)
			{
				this.LowerLimit = Math.Min(clickValue, this.UpperLimit);
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
		}

		/// <summary>
		/// Handles the OnMouseMove event of the MainGrid control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="MouseEventArgs"/> instance containing the event data.</param>
		private void MainGrid_OnMouseMove(object sender, MouseEventArgs e)
		{
			if (this.MainGrid.IsMouseCaptured)
			{
				this.SetValueFromMouse(e);
			}
		}

		/// <summary>
		/// Handles the OnSizeChanged event of the control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="SizeChangedEventArgs"/> instance containing the event data.</param>
		private void TrackPart_OnSizeChanged(object sender, SizeChangedEventArgs e)
		{
			this.RecalculateLabelsAndTicks();
		}
	}
}