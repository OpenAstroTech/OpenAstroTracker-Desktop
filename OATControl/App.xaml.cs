using MahApps.Metro;
using System;
using System.Windows;
using System.Threading;
using System.Windows.Threading;
using OATCommunications.Utilities;
using OATControl.Properties;

namespace OATControl
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>

	public partial class App : Application
	{
		public App()
		{
			// Add the event handler for handling non-UI thread exceptions to the event. 
			AppDomain.CurrentDomain.UnhandledException += App.UnhandledException;

			this.DispatcherUnhandledException += new DispatcherUnhandledExceptionEventHandler(App.AppDispatcherUnhandledException);
			//AppSettings.Instance.Upgrade();
		}

		protected override void OnStartup(StartupEventArgs e)
		{
			bool enableLogging = true;

			if (e.Args.Length > 0)
			{
				if (e.Args[0].ToLower() == "-nolog")
				{
					enableLogging = false;
				}
			}

			if (enableLogging)
			{
				Log.Init("OatControl");
				Log.EnableLogging();
			}

			ThemeManager.AddAccent("RedAccent", new Uri("pack://application:,,,/OATControl;component/Resources/RedAccent.xaml"));
			ThemeManager.AddAppTheme("RedTheme", new Uri("pack://application:,,,/OATControl;component/Resources/RedTheme.xaml"));
			ThemeManager.AddAccent("RedControls", new Uri("pack://application:,,,/OATControl;component/Resources/RedControls.xaml"));

			// get the current app style (theme and accent) from the application
			// you can then use the current theme and custom accent instead set a new theme
			Tuple<AppTheme, Accent> appStyle = ThemeManager.DetectAppStyle(Application.Current);

			// now set the Green accent and dark theme
			ThemeManager.ChangeAppStyle(Application.Current,
										ThemeManager.GetAccent("RedAccent"),
											ThemeManager.GetAppTheme("RedTheme"));


			base.OnStartup(e);
		}

		protected override void OnExit(ExitEventArgs e)
		{
			Log.Quit();

			base.OnExit(e);
		}

		private static int exceptionReentrancy = 0;

		/// <summary>
		/// Delegate's instance to react to unhandled exception event happened on not UI thread.
		/// </summary>
		/// <param name="sender">the object where event happened.</param>
		/// <param name="e">The exception related information.</param>
		private static void UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			if (Interlocked.CompareExchange(ref App.exceptionReentrancy, 1, 0) == 0)
			{
				Log.WriteLine("EXCPTN: Entered UnhandledException handler.\nException:\n{0}", (e.ExceptionObject != null) ? e.ExceptionObject.ToString() : "No exception!");
			}

			Log.Quit();
			Environment.Exit(-1);
		}


		//-------------------------------------------------------------------------------------------------------------
		/// <summary>
		/// The application's unhandled exception handler.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="DispatcherUnhandledExceptionEventArgs"/> instance containing the event data.</param>
		static void AppDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			if (Interlocked.CompareExchange(ref App.exceptionReentrancy, 1, 0) == 0)
			{
				Log.WriteLine("EXCPTN: Entered AppDispatcherUnhandledException handler.\nException:\n{0}\nStacktrace:\n{1}", (e.Exception != null) ? e.Exception.ToString() : "No Exception!", (e.Exception != null) ? e.Exception.StackTrace : "No Stacktrace!");

				// Prevent default unhandled exception processing
				e.Handled = true;
			}

			Log.Quit();
		}

	}
}
