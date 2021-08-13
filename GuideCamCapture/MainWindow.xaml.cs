using Hompus.VideoInputDevices;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace GuideCamCapture
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : System.Windows.Window, INotifyPropertyChanged
	{
		private static MainWindow _instance;
		VideoCapture _capture;
		private ObservableCollection<string> _resolutions = new ObservableCollection<string>();
		string _selectedCamera;
		int _cameraIndex;
		Mat _currentMat;
		ObservableCollection<string> _cameras = new ObservableCollection<string>();
		IReadOnlyDictionary<int, string> _deviceLookup;
		private bool _captureFiles;
		DispatcherTimer _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(200), DispatcherPriority.Render, GrabFrameAndRenderStatic, Application.Current.Dispatcher);
		private string _selectedResolution;
		private BitmapImage _currentImage;
		int _frame = 0;
		string _docFolder;
		private int _exposure;
		private int _gain;
		private int _brightness;
		private int _contrast;
		private int _gamma;

		private static void GrabFrameAndRenderStatic(object sender, EventArgs e)
		{
			_instance.GrabFrameAndRender();
		}

		private void GrabFrameAndRender()
		{
			_timer.Stop();
			if (_capture != null && !string.IsNullOrEmpty(_selectedResolution))
			{
				if (_currentMat != null)
				{
					_currentMat.Dispose();
				}
				_currentMat = _capture.RetrieveMat();

				_currentImage = new BitmapImage();
				_currentImage.BeginInit();
				_currentImage.StreamSource = _currentMat.ToMemoryStream();
				_currentImage.EndInit();
				OnPropertyChanged("CurrentFrame");
				if (_captureFiles)
				{
					string path;
					do
					{
						_frame++;
						path = System.IO.Path.Combine(_docFolder, $"Exp-{-_exposure:00}-Gain-{_gain:000}_{_frame:0000}.png");
					}
					while (File.Exists(path));
					_currentImage.Save(path);
				}
			}

			_timer.Start();
		}

		public MainWindow()
		{
			_instance = this;
			_docFolder = System.IO.Path.Combine( Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "GuideCamImages");
			InitializeComponent();
			this.DataContext = this;

			using (var sde = new SystemDeviceEnumerator())
			{
				_deviceLookup = sde.ListVideoInputDevice();
				//_cameras.Add("--- Select Camera ---");
				foreach (var kv in _deviceLookup)
				{
					_cameras.Add(kv.Value);
				}
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public void OnPropertyChanged([CallerMemberName] string propName = "")
		{
			var handler = this.PropertyChanged;
			if (handler != null)
			{
				handler(this, new PropertyChangedEventArgs(propName));
			}
		}

		public ObservableCollection<string> AvailableCameras
		{
			get
			{
				return _cameras;
			}
		}

		public ObservableCollection<string> AvailableResolutions
		{
			get
			{
				return _resolutions;
			}
		}

		public string SelectedCamera
		{
			get { return _selectedCamera; }
			set
			{
				if (_selectedCamera != value)
				{
					_selectedCamera = value;
					OnPropertyChanged();
					_cameraIndex = _deviceLookup.First(d => d.Value == _selectedCamera).Key;
					_capture = new VideoCapture(_cameraIndex, VideoCaptureAPIs.DSHOW);
					foreach (var res in _capture.GetAvailableResolutions().Select(r => $"{r.Item1} x {r.Item2}"))
					{
						_resolutions.Add(res);
					}
					_capture.Set(VideoCaptureProperties.Exposure, -2);
					_capture.Set(VideoCaptureProperties.Exposure, -1);

					OnPropertyChanged("AvailableResolutions");
					//_timer.Start();
				}
			}
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			base.OnClosing(e);
			if (_capture != null)
			{
				_capture.Dispose();
				_capture = null;
			}
		}

		public string SelectedResolution
		{
			get { return _selectedResolution; }
			set
			{
				if (_selectedResolution != value)
				{
					_selectedResolution = value;
					OnPropertyChanged();
					_capture.Set(VideoCaptureProperties.FrameWidth, int.Parse(_selectedResolution.Split('x', StringSplitOptions.RemoveEmptyEntries)[0]));
					_capture.Set(VideoCaptureProperties.FrameHeight, int.Parse(_selectedResolution.Split('x', StringSplitOptions.RemoveEmptyEntries)[1]));

					_timer.Start();
				}
			}
		}

		public BitmapImage CurrentFrame
		{
			get
			{
				if (_currentImage != null)
				{
					return _currentImage;
				}
				return null;
			}
		}


		public bool CaptureImageFiles
		{
			get { return _captureFiles; }
			set
			{
				if (_captureFiles != value)
				{
					_captureFiles = value;
					OnPropertyChanged();
				}
			}
		}

		public int Exposure
		{
			get { return _exposure; }
			set
			{
				if (_exposure != value)
				{
					_exposure = value;
					_capture.Set(VideoCaptureProperties.Exposure, _exposure);
					OnPropertyChanged();
				}
			}
		}

		public int Gain
		{
			get { return _gain; }
			set
			{
				if (_gain != value)
				{
					_gain = value;
					_capture.Set(VideoCaptureProperties.Gain, _gain);
					OnPropertyChanged();
				}
			}
		}

		public int Brightness
		{
			get { return _brightness; }
			set
			{
				if (_brightness != value)
				{
					_brightness = value;
					_capture.Set(VideoCaptureProperties.Brightness, _brightness);
					OnPropertyChanged();
				}
			}
		}

		public int Contrast
		{
			get { return _contrast; }
			set
			{
				if (_contrast != value)
				{
					_contrast = value;
					_capture.Set(VideoCaptureProperties.Contrast, _contrast);
					OnPropertyChanged();
				}
			}
		}

		public int Gamma
		{
			get { return _gamma; }
			set
			{
				if (_gamma != value)
				{
					_gamma = value;
					_capture.Set(VideoCaptureProperties.Gamma, _gamma);
					OnPropertyChanged();
				}
			}
		}



	}

	public static class VideoExtensions
	{
		public static List<Tuple<int, int>> GetAvailableResolutions(this VideoCapture camera)
		{
			List<Tuple<int, int>> supportedVideoResolutions = new List<Tuple<int, int>>();

			int[] commonWidths = { 320, 640, 800, 1024, 1280, 1280, 1920 };
			int[] commonHeights = { 240, 480, 600, 768, 720, 960, 1080 };

			//camera.Set(VideoCaptureProperties.Fps, 5);

			for (int i = 0; i < 7; i++)
			{
				//camera.Set(VideoCaptureProperties.FrameWidth, commonWidths[i]);
				//camera.Set(VideoCaptureProperties.FrameHeight, commonHeights[i]);
				//OpenCvSharp.Size cameraResolution = new OpenCvSharp.Size(camera.Get(VideoCaptureProperties.FrameWidth), camera.Get(VideoCaptureProperties.FrameHeight));

				//if ((cameraResolution.Width == commonWidths[i]) && (cameraResolution.Height == commonHeights[i]))
				//{
				//	supportedVideoResolutions.Add(Tuple.Create(cameraResolution.Width, cameraResolution.Height));
				//}
				supportedVideoResolutions.Add(Tuple.Create(commonWidths[i], commonHeights[i]));
			}

			/*
			int step = 256;
			double minimumWidth = 128; // Microvision
			double maximumWidth = 2000 + step; // 4K

			OpenCvSharp.Size currentSize = new OpenCvSharp.Size(minimumWidth, 1);
			OpenCvSharp.Size previousSize = currentSize;

			while (true)
			{
				camera.Set(VideoCaptureProperties.FrameWidth, currentSize.Width);
				camera.Set(VideoCaptureProperties.FrameHeight, currentSize.Height);

				OpenCvSharp.Size cameraResolution = new OpenCvSharp.Size(
					camera.Get(VideoCaptureProperties.FrameWidth),
					camera.Get(VideoCaptureProperties.FrameHeight));

				if (cameraResolution.Width != previousSize.Width && cameraResolution.Height != previousSize.Height)
				{
					supportedVideoResolutions.Add(Tuple.Create(cameraResolution.Width,cameraResolution.Height));
					currentSize = previousSize = cameraResolution;
				}

				currentSize.Width += step;
				if (currentSize.Width > maximumWidth)
				{
					break;
				}
			}
			*/
			return supportedVideoResolutions;
		}

		public static void Save(this BitmapImage image, string filePath)
		{
			if (!Directory.Exists(System.IO.Path.GetDirectoryName(filePath)))
			{
				Directory.CreateDirectory(System.IO.Path.GetDirectoryName(filePath));
			}
			BitmapEncoder encoder = new PngBitmapEncoder();
			encoder.Frames.Add(BitmapFrame.Create(image));

			using (var fileStream = new System.IO.FileStream(filePath, System.IO.FileMode.Create))
			{
				encoder.Save(fileStream);
			}
		}
	}
}
