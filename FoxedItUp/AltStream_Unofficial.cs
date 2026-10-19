using AltStreamLib.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FoxedItUp.httpRequest;
using static FoxedItUp.Functions;

namespace FoxedItUp {
    internal class AltStream_Unofficial {
        const int PollTime = 5;
        System.Timers.Timer PollTimer = new();
        string ChannelName;
        DateTime LastEvent = DateTime.UtcNow;

        internal void Connect(string ChannelName) {
            this.ChannelName = ChannelName;
            PollTimer.AutoReset = true;
            PollTimer.Interval = PollTime * 1000;
            PollTimer.Elapsed += PollTimer_Elapsed;
            PollTimer.Start();
        }

        private async void PollTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e) {
            Log("Polling for activity");
            string Result = await HttpRequest("GET", $"https://altstream.online/api/activity?channel={ChannelName}", "");
            Log(Result);
        }
    }
}
