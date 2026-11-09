using Newtonsoft.Json;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using static FoxedItUp._Settings;

namespace FoxedItUp {
    internal static class Functions {
        internal static System.Version version = Assembly.GetExecutingAssembly().GetName().Version;
        private static readonly string WindowTitle = $"FoxedItUp {version}"; 
        private static AltStreamLib.Logging.Logger logger = new AltStreamLib.Logging.Logger();
        internal static AutoResetEvent QuitSignal = new AutoResetEvent(false);


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

        internal static LocalDateTime FromDateTimeUtc(DateTime datetime) {
            return new LocalDateTime(
                datetime.Year,
                datetime.Month,
                datetime.Day,
                datetime.Hour,
                datetime.Minute,
                datetime.Second,
                datetime.Millisecond
            );
        }

        internal static string DateTimeToAge(DateTime datetime) {
            Period Between = Period.Between(FromDateTimeUtc(datetime), FromDateTimeUtc(DateTime.UtcNow));
            string Result = Between.Years.ToString();
            if (Between.Years != 1) { Result += " Years "; } else { Result += " Year "; }
            Result += Between.Months.ToString();
            if (Between.Months != 1) { Result += " Months "; } else { Result += " Month "; }
            Result += Between.Days.ToString();
            if (Between.Days != 1) { Result += " Days"; } else { Result += " Day"; }
            return Result;
        }
        internal static int DateTimeToDays(DateTime datetime) {
            return (int)Math.Floor((DateTime.UtcNow - datetime).TotalDays);
        }
        internal static string GetBotToken() {
            return Settings.BotToken;
        }
        internal static void Quit() {
            if (Settings.EnableUnofficialEvents) {
                Log("Stopping unofficial event polling");
                AltStream_Unofficial.Disconnect();
            }
            Log("Stopping HTTP server");
            HTTPServer.Stop();
            Log("Disconnecting AltEventClient");
            AltStream.Disconnect();

            QuitSignal.Set();
        }
    }
}
