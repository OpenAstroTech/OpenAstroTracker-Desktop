using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using OATCommunications.Utilities;

namespace OATTest
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		protected override void OnStartup(StartupEventArgs e)
		{
			Log.Init("OatTest");
			Log.EnableLogging();
			base.OnStartup(e);
		}

		protected override void OnExit(ExitEventArgs e)
		{
			Log.Quit();
			base.OnExit(e);
		}
	}
}
