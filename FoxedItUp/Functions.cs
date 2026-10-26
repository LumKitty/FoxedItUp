using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static FoxedItUp._Settings;

namespace FoxedItUp {
    internal static class Functions {
        internal static System.Version version = Assembly.GetExecutingAssembly().GetName().Version;
        private static readonly string WindowTitle = $"FoxedItUp {version}"; 
        private static AltStreamLib.Logging.Logger logger = new AltStreamLib.Logging.Logger();

        internal static void AltStreamLibLogInit() {
            logger.OnLog += Logger_OnLog;
        }

        private static void Logger_OnLog(LogMessage obj) {
            if (!Settings.ShowAltStreamLibPingMessages && obj.Message == "PING") { return; }
            Log($"AltStreamLib: {obj.Message}");
        }

        internal static void Log (string Message) {
            Console.WriteLine(Message);
        }

        internal static void ErrorHandler(Exception Ex) {
            Log($"ERROR: {Ex.ToString()}");
        }

        internal static string HandleRelativePaths(string URL) {
            if (!String.IsNullOrEmpty(URL)) {
                if (URL.StartsWith("https://") || URL.StartsWith("http://")) {
                    return URL;
                } else {
                    return $"https://altstream.online{URL}";
                }
            } else {
                return "https://altstream.online/logo_ashe_big.gif";
            }
        }
        internal static void SetWindowTitle(string msg) {
            Console.Title = $"{WindowTitle} - {msg}";
        }
    }
}
