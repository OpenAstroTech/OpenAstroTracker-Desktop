using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OATCommTestConsole
{
	public class ConsoleOutput
	{
		public static System.ConsoleColor menuColor = ConsoleColor.DarkYellow;

		public static void Info(string message)
		{
			Console.ForegroundColor = ConsoleColor.Gray;
			Console.WriteLine(message);
			Console.ResetColor();
		}

		public static void Success(string message)
		{
			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine(message);
			Console.ResetColor();
		}

		public static void Warning(string message)
		{
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine(message);
			Console.ResetColor();
		}

		public static void Error(string message)
		{
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine(message);
			Console.ResetColor();
		}

		public static void Logo()
		{
			Console.ForegroundColor = ConsoleColor.Cyan;
			Console.CursorTop = 1; Console.CursorLeft = 35; Console.Write("Settings ");
			Console.CursorTop = 2; Console.CursorLeft = 35;
			if (OATCommTestConsole.Program._commHandler != null)
			{
				Console.Write(" Device   : " + (OATCommTestConsole.Program._commHandler.Connected ? "Connected (" + OATCommTestConsole.Program._oatFwVersion + " on " + OATCommTestConsole.Program._comPort + ")" : "Disconnected"));
			}
			else
			{
				Console.Write(" Device   : " + (string.IsNullOrEmpty(OATCommTestConsole.Program._comPort) ? "<unset>" : OATCommTestConsole.Program._comPort));
			}
			Console.CursorTop = 3; Console.CursorLeft = 35; Console.Write(" Baudrate : " + OATCommTestConsole.Program._baudRate);
			Console.CursorTop = 4; Console.CursorLeft = 35; Console.Write(" Timeouts : R:" + OATCommTestConsole.Program._readTimeout + "ms  W:" + OATCommTestConsole.Program._writeTimeout + "ms");

			Console.ResetColor();
			Console.CursorTop = 0;
			Console.CursorLeft = 0;

			Console.ForegroundColor = ConsoleColor.White;
			Console.WriteLine(@"    ______    ____  _______ ");
			Console.WriteLine(@"   /      \  /    \|__   __|");
			Console.WriteLine(@"   |  ()  | /  ()  \  | |");
			Console.WriteLine(@"   \______//___/\___\ |_|  ");
			Console.WriteLine();
			Console.ResetColor();
		}

		public static void PreTestInfo()
		{
			Console.ForegroundColor = menuColor;
			Console.WriteLine("****************************** IMPORTANT *****************************\r");
			Console.WriteLine("* Make sure the device shows up in the Device Manager.               *\r");
			Console.WriteLine("* Do not change any of the USB device settings in Windows.           *\r");
			Console.WriteLine("**********************************************************************\r");

			Console.ResetColor();
		}

		public static void ClearRestOfScreen()
		{
			int left = Console.CursorLeft;
			int top = Console.CursorTop;
			var clearLine = new String(' ', Console.WindowWidth - 1);
			for (int i = top; i < Console.WindowHeight - 1; i++)
			{
				Console.CursorLeft = 0;
				Console.CursorTop = i;
				Console.WriteLine(clearLine);
			}
			Console.CursorLeft = left;
			Console.CursorTop = top;
		}
	}
}
