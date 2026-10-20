using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoxedItUp {
    internal static class Functions {
        internal const string WindowTitle = "FoxedItUp v0.2.6";
        
        internal static void Log (string Message) {
            Console.WriteLine(Message);
        }
        internal static string HandleRelativePaths(string URL) {
            if (URL.StartsWith("https://") || URL.StartsWith("http://")) {
                return URL;
            } else {
                return $"https://altstream.online{URL}";
            }
        }
        internal static void SetWindowTitle(string msg) {
            Console.Title = $"{WindowTitle} - {msg}";
        }
    }
}
