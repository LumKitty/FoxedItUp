using AltStreamLib;
using AltStreamLib.Api;
using AltStreamLib.Api.v1.Models;
using AltStreamLib.EventSub;
using AltStreamLib.EventSub.Chat.Models;
using AltStreamLib.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FoxedItUp.Functions;
using static FoxedItUp._Settings;

namespace FoxedItUp {
    internal static class AltStream {
        private const int ConnectionTimeout = 30;
        private static string ChannelName;
        internal static AltStreamLib.Api.ApiClient AltAPI = new();
        private static int ConnectAttempts = 0;
        private static bool Connected = false;
        private static System.Timers.Timer ConnectTimer;
        internal static EventSubClient AltEventClient;
        
        internal static void Connect(string _ChannelName) {
            SetWindowTitle("Connecting");
            ChannelName = _ChannelName;
            if (Settings.EnableOutdatedWorkaround) {
                ConnectTimer = new();
                ConnectTimer.Interval = ConnectionTimeout * 1000;
                ConnectTimer.Elapsed += ConnectTimer_Elapsed;
                ConnectTimer.AutoReset = false;
            }
            
            AltEventClient = new EventSubClient(ChannelName);
            AltEventClient.OnMessageReceived += AltEventClient_OnMessageReceived;
            AltEventClient.OnHighlightReceived += AltEventClient_OnHighlightReceived;
            
            AltEventClient.OnRaidReceived += AltEventClient_OnRaidReceived;
            AltEventClient.OnRedemptionReceived += AltEventClient_OnRedemptionReceived;
            AltEventClient.OnUnhandledReceived += AltEventClient_OnUnhandledReceived;
            
            AltEventClient.OnConnected += AltEventClient_OnConnected;
            AltEventClient.OnDisconnected += AltEventClient_OnDisconnected;

            Log("Connecting to AltStream");
            AltEventClient.ConnectAsync();
            if (Settings.EnableOutdatedWorkaround) { ConnectTimer.Start(); }
        }

        private static void ConnectTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e) {
            if (!Connected) {
                Log("Connection timed out");
                if (Settings.EnableOutdatedWorkaround) {
                    ConnectTimer.Stop();
                    ConnectTimer.Dispose();
                }
                HandleConnectionFailure();
            }
        }

        private static void AltEventClient_OnUnhandledReceived(EventMessage obj) {
            Log("Unrecognised Event:");
            Log(Newtonsoft.Json.JsonConvert.SerializeObject(obj));
        }

        private static void AltEventClient_OnRedemptionReceived(EventMessage obj) {
            AltStreamLib.EventSub.Chat.Models.Events.Redemption RedeemEvent = (AltStreamLib.EventSub.Chat.Models.Events.Redemption)obj.Event;
            Log($"* {RedeemEvent.From} redeemed \"{RedeemEvent.RewardName}\" for {RedeemEvent.Cost} {RedeemEvent.PointsName}");
            string PointsName = RedeemEvent.RewardName.ToLower();
            Log($"Attempting to run command {PointsName}");
            MIUCommand? PointsCommand = MixItUpClient.MIUChannelPoints.FirstOrDefault(i => (i.Name == PointsName));
            if (PointsCommand != null) {
                Log($"Attempting to run command: {PointsCommand.ID}");
                JObject SpecialIdentifiers = new JObject(
                    new JProperty("UserName", RedeemEvent.From),
                    new JProperty("UserDisplayName", RedeemEvent.From),
                    new JProperty("UserColor", obj.Context.UserColor)
                );
                if (!string.IsNullOrEmpty(RedeemEvent.UserInput)) { SpecialIdentifiers.Add(new JProperty("Message", RedeemEvent.UserInput)); }
                if (!string.IsNullOrEmpty(obj.Context.AuthorImage)) { SpecialIdentifiers.Add(new JProperty("UserAvatar", HandleRelativePaths(obj.Context.AuthorImage))); }
                PointsCommand.RunCommand(RedeemEvent.UserInput, Settings.SpoofPlatform, SpecialIdentifiers, "altstream");
            } else {
                Log("Could not find command");
            }

        }

        private static void AltEventClient_OnRaidReceived(EventMessage obj) {
            Log("Raid received");
            AltStreamLib.EventSub.Chat.Models.Events.RaidIncoming RaidEvent = (AltStreamLib.EventSub.Chat.Models.Events.RaidIncoming)obj.Event;
            Log($"* {RaidEvent.SourceUsername} raided with {RaidEvent.Viewers}");

            JObject SpecialIdentifiers = new JObject(
                new JProperty("RaidViewerCount", RaidEvent.Viewers.ToString()), // Yes, really! It fails if this is an int
                new JProperty("UserName", RaidEvent.SourceUsername),
                new JProperty("UserDisplayName", RaidEvent.SourceUsername),
                new JProperty("UserAvatar", HandleRelativePaths(obj.Context.AuthorImage)),
                new JProperty("UserColor", obj.Context.UserColor)
            );
            if (!string.IsNullOrEmpty(obj.Context.AuthorImage)) { SpecialIdentifiers.Add(new JProperty("UserAvatar", HandleRelativePaths(obj.Context.AuthorImage))); }
            if (MixItUpClient.ChannelRaided != null) { MixItUpClient.ChannelRaided.RunCommand("", Settings.SpoofPlatform, SpecialIdentifiers, "altstream"); }
        }

        private static void AltMessageReceived(AltStreamLib.EventSub.Chat.Models.Message Context, bool Highlighted = false) {

            try {

                //Log($"<{Context.AuthorImage}");.
                string Command = "";
                string Args = "";

                if (!string.IsNullOrEmpty(Context.Text)) {
                    if (Context.Text.Substring(0, 1) == "!") {
                        Log($"Received command message {Context.Text}");
                        int n = Context.Text.IndexOf(' ');
                        Log($"Index {n}");
                        switch (n) {
                            case 1:
                            case 0:
                                Log($"Invalid message {Context.Text}"); break;
                            case -1:
                                Command = Context.Text.Substring(1);
                                Log($"* {Context.AuthorUsername} used command '{Command}'");
                                break;
                            default:
                                Command = Context.Text.Substring(1, n - 1);
                                Args = Context.Text.Substring(n + 1);
                                Log($"* {Context.AuthorUsername} used command '{Command}' with args: {Args}");
                                break;
                        }

                        if (!String.IsNullOrEmpty(Command)) {
                            Log($"Attempting to run command {Command}");
                            MIUChatCommand? ChatCommand = MixItUpClient.MIUChatCommands.FirstOrDefault(i => i.TriggerNames.Contains(Command));
                            if (ChatCommand != null) {
                                Log($"Attempting to run command: {ChatCommand.ID}");

                                JObject SpecialIdentifiers = new JObject(
                                new JProperty("UserName", Context.AuthorUsername),
                                new JProperty("UserDisplayName", Context.AuthorUsername),
                                new JProperty("UserColor", Context.UserColor)
    );
                                if (!string.IsNullOrEmpty(Context.AuthorImage)) { SpecialIdentifiers.Add(new JProperty("UserAvatar", HandleRelativePaths(Context.AuthorImage))); }

                                ChatCommand.RunCommand(Args, Settings.SpoofPlatform, new JObject(
                                    new JProperty("UserName", Context.AuthorUsername),
                                    new JProperty("UserDisplayName", Context.AuthorUsername),
                                    new JProperty("UserAvatar", Context.AuthorImage),
                                    new JProperty("UserColor", Context.UserColor)
                                ), "altstream");
                            } else {
                                Log("Could not find command");
                            }
                        }
                    } else {
                        Log($"<{Context.AuthorUsername}> {Context.Text}");
                    }
                } else {
                    Log($"Received empty message: {Context.ToString()}");
                }
            } catch (Exception ex) {
                Log(ex.ToString());
            }
        }
        private static void AltEventClient_OnHighlightReceived(EventMessage obj) {
            AltMessageReceived(obj.Context,true);
        }
        private static void AltEventClient_OnMessageReceived(Message obj) {
            AltMessageReceived(obj);

        }
        private static void AltEventClient_OnConnected() {
            Connected = true;
            if (Settings.EnableOutdatedWorkaround) {
                ConnectTimer.Stop();
                ConnectTimer.Dispose();
            }
            Log($"Connected to channel {ChannelName}");
            SetWindowTitle("Connected");
            ConnectAttempts = 0;
        }
        private static void AltEventClient_OnDisconnected() {
            Connected = false;
            Log("Disconnected.");
            if (Settings.EnableOutdatedWorkaround) {
                HandleConnectionFailure();
            }
        }
        private static async Task HandleConnectionFailure() {
            SetWindowTitle("Disconnected");
            ConnectAttempts++;
            int Delay;
            switch (ConnectAttempts) {
                case 1: Delay = 1; break;
                case 2: Delay = 5; break;
                case 3: Delay = 10; break;
                case 4: Delay = 20; break;
                default: Delay = 30; break;
            }
            Log($"Waiting {Delay} seconds to reconnect (Attempts: {ConnectAttempts})");
            await Task.Delay(Delay * 1000);
            if (Connected) {
                Log("Never mind! We got connected while waiting!");
            } else { 
                Connect(ChannelName);
            }
        }
    }
}
