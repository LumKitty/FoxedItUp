using AltStreamLib.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FoxedItUp.httpRequest;
using static FoxedItUp.Functions;
using static FoxedItUp._Settings;
using AltStreamLib.EventSub.Chat.Models.Events;

namespace FoxedItUp {
    internal static class AltStream_Unofficial {
        const int PollTime = 5;
        static System.Timers.Timer PollTimer = new System.Timers.Timer();
        static string ChannelName;
        static DateTime LastEvent = DateTime.UtcNow;

        internal static void Connect(string _ChannelName) {
            ChannelName = _ChannelName;
            PollTimer.AutoReset = true;
            PollTimer.Interval = PollTime * 1000;
            PollTimer.Elapsed += PollTimer_Elapsed;
            PollTimer_Elapsed(new object(), null);
            PollTimer.Start();
        }

        private static async void PollTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e) {
            try {
                //Log("Polling for activity");
                string Result = await HttpRequest("GET", $"https://altstream.online/api/activity?channel={ChannelName}", "");
                JObject Results = JObject.Parse(Result);
                if (Results.ContainsKey("events")) {
                    //Log("Got events");
                    DateTime NewEvent = LastEvent;
                    JArray Events = (JArray)Results["events"];
                    DateTime TimeStamp;
                    foreach (JObject Event in Events.Reverse()) {
                        TimeStamp = DateTime.Parse(Event["timestamp"].ToString());
                        //Log($"Found event with timestamp {TimeStamp} comparing to {LastEvent}");
                        //Log(Event.ToString());
                        if (TimeStamp > LastEvent) {
                            if (TimeStamp > NewEvent) { NewEvent = TimeStamp; }
                            string EventType = Event["type"].ToString().ToLower();
                            JObject User;
                            switch (EventType) { 
                                case "follow":
                                    User = (JObject)Event["user"];
                                   
                                    Log($"* {User["username"]} just followed!");
                                    if (MixItUpClient.ChannelFollowed != null) {
                                        await MixItUpClient.ChannelFollowed.RunCommand("", Settings.SpoofPlatform, User["username"].ToString());
                                    }
                                    break;

                                case "subscription":
                                    User = (JObject)Event["user"];

                                    Log($"* {User["username"]} just subscribed!");
                                    if (MixItUpClient.ChannelFollowed != null) {  //  vvv Need to see a real one to do this properly vvv
                                        await MixItUpClient.ChannelSubscribed.RunCommand(Event["details"].ToString(), Settings.SpoofPlatform, User["username"].ToString(), ""); 
                                    }
                                    break;

                                case "tip":
                                    User = (JObject)Event["user"];

                                    Log($"* {User["username"]} just tipped!");
                                    if (MixItUpClient.ChannelFollowed != null) {  //  vvv Need to see a real one to do this properly vvv
                                        string Details = Event["details"].ToString();
                                        Details = Details.Substring(Details.IndexOf(' ') + 1);
                                        string CurrencySymbol = Details.Substring(0, 1);
                                        string Amount = Details.Substring(1, Details.IndexOf(' ') - 1);
                                        Log(Details);
                                        Details = Details.Substring(Details.IndexOf('"') + 1);
                                        Log(Details);
                                        string Message = Details.Substring(0, Details.IndexOf('"'));

                                        await MixItUpClient.ChannelDonation.RunCommand(Message, Settings.SpoofPlatform, User["username"].ToString(), CurrencySymbol, Amount);
                                    }
                                    break;

                                case "raid": break; // Raids are handled in the official lib
                                default:
                                    Log($"Received unsupported event type: {Event["type"]}");
                                    Log(Event.ToString());
                                    break;
                            }
                        }
                    }
                    LastEvent = NewEvent;
                }
            } catch (Exception ex) {
                Log($"ERROR: {ex.ToString()}");
            }
        }
    }
}
