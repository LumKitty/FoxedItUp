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
        static System.Timers.Timer PollTimer = new();
        static string ChannelName;
        static DateTime LastEvent = DateTime.UtcNow;

        internal static void Connect(string _ChannelName) {
            ChannelName = _ChannelName;
            PollTimer.AutoReset = true;
            PollTimer.Interval = PollTime * 1000;
            PollTimer.Elapsed += PollTimer_Elapsed;
            PollTimer_Elapsed(new object(), new System.Timers.ElapsedEventArgs(LastEvent));
            PollTimer.Start();
        }

        private static async void PollTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e) {
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
                            switch (EventType) { 
                                case "follow":
                                //case "raid":
                                    //Log($"Found valid follow event with timestamp {TimeStamp}");
                                    
                                    JObject User = (JObject)Event["user"];
                                    AltStreamLib.Api.v1.Models.UserSettingsResponse UserInfo = await AltStream.AltAPI.GetUserSettingsInfoAsync(User["username"].ToString());
                                    JObject SpecialIdentifiers = new JObject(
                                        new JProperty("UserName", User["username"].ToString()),
                                        new JProperty("UserDisplayName", User["name"].ToString()),
                                        new JProperty("UserColor", "#ffffff"),
                                        new JProperty("UserAvatar", HandleRelativePaths(User["image"].ToString()))
                                    );
                                    Log($"* {User["username"]} just {EventType}ed!");
                                    /*if (EventType == "raid") {
                                        string RaidCount = Event["details"].ToString();
                                        RaidCount = RaidCount.Substring(RaidCount.IndexOf('"') + 1);
                                        RaidCount = RaidCount.Substring(0, RaidCount.IndexOf('"'));
                                        SpecialIdentifiers.Add(new JProperty("RaidViewerCount", RaidCount));
                                        if (MixItUpClient.ChannelRaided != null) {
                                            Log("Running raid command");
                                            Log(SpecialIdentifiers.ToString());
                                            MixItUpClient.ChannelRaided.RunCommand("", Settings.SpoofPlatform, SpecialIdentifiers, "altstream");
                                        }
                                    } else { */
                                        if (MixItUpClient.ChannelFollowed != null) {
                                            MixItUpClient.ChannelFollowed.RunCommand("", Settings.SpoofPlatform, SpecialIdentifiers, "altstream");
                                        }
                                    //}
                                    break;
                                default:
                                    Log($"Received unsupported event type: {Event["type"]}");
                                    Log(Event.ToString());
                                    break;
                            }
                        }
                    }
                    //Log($"Updating LastEvent to {NewEvent}");
                    LastEvent = NewEvent;
                }
            } catch (Exception ex) {
                Log($"ERROR: {ex.ToString()}");
            }
        }
    }
}
