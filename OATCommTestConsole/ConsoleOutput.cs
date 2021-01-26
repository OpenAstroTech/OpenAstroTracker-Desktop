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
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("    ____   ____  _____ ");
            Console.WriteLine("   / () \\ / () \\|_   _|");
            Console.WriteLine("   \\____//__/\\__\\ |_|  ");
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
    }
}
