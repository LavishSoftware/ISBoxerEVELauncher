using System;
using System.IO;

namespace ISBoxerEVELauncher.Utils
{
    public static class Debug
    {
        private static bool DebugModeEnabled => App.Settings.DebugMode;
        private static readonly object _logLock = new object();
        private static string LogFilePath => Path.Combine(App.ISBoxerEVELauncherPath, "ISBoxerEVELauncher.log");

        public static void Log(string message, string category = "General", string level = "INFO")
        {
            if (!DebugModeEnabled)
                return;

            try
            {
                lock (_logLock)
                {
                    string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{category}] [{level}] {message}{Environment.NewLine}";
                    File.AppendAllText(LogFilePath, logEntry);
                }
            }
            catch
            {
                // ignored
            }
        }

        public static void Info(string message, string category = "General") => Log(message, category, "INFO");
        public static void Warning(string message, string category = "General") => Log(message, category, "WARNING");
        public static void Error(string message, string category = "General") => Log(message, category, "ERROR");
        public static void Error(string message, Exception ex, string category = "General") => Log($"{message}{Environment.NewLine}Exception: {ex}", category, "ERROR");
    }
}