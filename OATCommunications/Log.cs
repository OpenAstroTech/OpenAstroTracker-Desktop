using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OATCommunications.Utilities
{
    public class Log
    {
        private static DateTime appStartTime = DateTime.UtcNow;
        private static object oLock = new object();
        private static string sFolder;
        private static string sPath;

        private static List<string> lstBuffer = new List<string>();
        private static int maxBuffered = 0;
        private static Timer _flushTimer;
#if DEBUG
        private static bool logging = true;
#else
		private static bool logging = false;
#endif


        public static void EnableLogging(bool state = true)
        {
            logging = state;
        }

        public static bool IsLoggingEnabled
        {
            get { return logging; }
        }

        public static string Filename
        {
            get
            {
                return Log.sPath;
            }
        }
        public static void ReInit(string sTitle)
        {
            _flushTimer.Change(Timeout.Infinite, Timeout.Infinite);
            _flushTimer.Dispose();
            Log.Flush();
            Log.maxBuffered = 0;
            Init(sTitle, false);
        }

        public static void Init(string sTitle, bool deleteOlder = true)
        {
            // Create our logfile folder in AppData/Roaming
            sFolder = string.Format("{0}\\OpenAstroTracker", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
            Directory.CreateDirectory(sFolder);

            // Create this session logfile
            sPath = string.Format("{0}\\OATControl_{1}-{2}.log", sFolder, DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"), Environment.UserName);

            if (deleteOlder)
            {
                // Find old logfiles and keep the latest 5 around.
                var oldLogFiles = Directory.GetFiles(sFolder, "OATControl*.log").OrderByDescending(s => s).Skip(5).ToList();

                foreach (var logFile in oldLogFiles)
                {
                    try
                    {
                        File.Delete(logFile);
                    }
                    catch
                    {
                        // Oh well....
                    }
                }
            }

            _flushTimer = new Timer(OnFlushTimer, null, 5000, 1000);

            Log.WriteLine("SYSTEM: *********************************");
            Log.WriteLine(string.Format("SYSTEM: *  {0} *", sTitle.PadRight(28)));
            Log.WriteLine("SYSTEM: *********************************");
            Log.WriteLine("SYSTEM: * Started : " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " *");
            Log.WriteLine(string.Format("SYSTEM: * User    : {0} *", Environment.UserName.PadRight(19)));
            Log.WriteLine("SYSTEM: *********************************");
            Log.Flush();
        }

        private static void OnFlushTimer(object state)
        {
            lock (Log.oLock)
            {
                if (IsLoggingEnabled && Log.lstBuffer.Any())
                {
                    Log.Flush();
                }
            }
        }

        private static string FormatMessage(string message, object[] args)
        {
            var sb = new StringBuilder(message.Length + 64);

            TimeSpan elapsed = DateTime.UtcNow - Log.appStartTime;
            sb.AppendFormat("[{0}] [{1:00}]: ", elapsed.ToString("hh\\:mm\\:ss\\.fff"), Thread.CurrentThread.ManagedThreadId);

            if (args != null && args.Length > 0)
            {
                sb.AppendFormat(message, args);
            }
            else
            {
                sb.Append(message);
            }

            return sb.ToString();
        }

        protected static void Flush()
        {
            lock (Log.oLock)
            {
                var lines = string.Join("\r\n", Log.lstBuffer.ToArray()) + "\r\n";
                File.AppendAllText(Log.sPath, lines);
                Log.lstBuffer.Clear();
            }
        }

        public static void WriteLine(string message, params object[] args)
        {
            if (logging)
            {
                string sLine = FormatMessage(message, args);

                lock (Log.oLock)
                {
                    Log.lstBuffer.Add(sLine);
                    Debug.WriteLine(sLine);
                    if (Log.lstBuffer.Count > Log.maxBuffered)
                    {
                        Log.maxBuffered = Log.lstBuffer.Count;
                    }
                }
            }
        }

        public static void Quit()
        {
            _flushTimer.Dispose();
            logging = false;

            lock (Log.oLock)
            {
                Log.lstBuffer.Add(Log.FormatMessage("Shutdown logging. Maximum of {0} lines buffered.", new Object[] { (object)Log.maxBuffered }));
                // Flush it.
                File.AppendAllText(Log.sPath, string.Join("\r\n", Log.lstBuffer.ToArray()) + "\r\n");
                Log.lstBuffer.Clear();
            }
        }
    }
}


