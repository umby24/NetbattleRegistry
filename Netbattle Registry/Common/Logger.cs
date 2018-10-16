using System;
using System.IO;

namespace Netbattle_Registry.Common {
    public class Logger : TaskItem {
        private static string _filename;
        private static LogType _minimumLevel;
        public static bool ConsoleOutEnabled = true;
        private bool _setup;
        private static readonly object LogLock = new object();

        public override void Setup() {
            if (_setup)
                return;

            LastRun = new DateTime();
            Interval = new TimeSpan(0, 0, 2);

            DateTime nowTime = DateTime.UtcNow;
            _filename = "log." + nowTime.Year + nowTime.Month + nowTime.Day + nowTime.Hour + nowTime.Minute + ".txt";
            File.AppendAllText(_filename, "# Log Start at " + nowTime.ToLongDateString() + " - " + nowTime.ToLongTimeString() + Environment.NewLine);
            _setup = true;
        }

        public override void Main() {
            _minimumLevel = LogType.Verbose;
        }

        public override void Teardown() {
        }

        public static void Log(LogType type, string message) {
            var item = new LogItem { Type = type, Time = DateTime.UtcNow, Message = message };

            lock (LogLock) {
        //        File.AppendAllText(_filename, $"{item.Time.ToLongTimeString()} > [{item.Type}] {item.Message}" + Environment.NewLine);
                if (ConsoleOutEnabled)
                    ConsoleOutput(item);
            }
        }

        public static void Log(Exception ex) {
            Log(LogType.Error, $"Error occured: {ex.Message}");
            Log(LogType.Debug, ex.StackTrace);

            if (ex.InnerException == null)
                return;

            Log(LogType.Debug, "INNER EXCEPTION:");
            Log(LogType.Debug, ex.InnerException.Message);
            Log(LogType.Debug, ex.InnerException.StackTrace);
        }

        private static void ConsoleOutput(LogItem item) {
            if ((int)item.Type < (int)_minimumLevel)
                return;

            string line = $"{item.Time.ToLongTimeString()} > ";

            switch (item.Type) {
                case LogType.Verbose:
                    line += "[Verbose]";
                    break;
                case LogType.Debug:
                    line += "[Debug]";
                    break;
                case LogType.Warning:
                    line += "[Warning]";
                    break;
                case LogType.Error:
                    line += "[Error]";
                    break;
                case LogType.Info:
                    line += "[Info]";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            Console.WriteLine($"{line} {item.Message}");
        }
    }
}
