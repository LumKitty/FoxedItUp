using AltStreamLib.EventSub;
using AltStreamLib.EventSub.Chat.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;
using static FoxedItUp.Functions;
using static FoxedItUp._Settings;

namespace FoxedItUp {
    internal static class AltStream {
        private static string ChannelName;
        internal static AltStreamLib.Api.ApiClient AltAPI = new AltStreamLib.Api.ApiClient();
        private static bool Connected = false;
        internal static EventSubClient AltEventClient;
        
        internal static void Connect(string _ChannelName) {
            SetWindowTitle("Connecting");
            ChannelName = _ChannelName;
            
            AltEventClient = new EventSubClient(ChannelName);
            AltEventClient.OnMessageReceived += AltEventClient_OnMessageReceived;
            AltEventClient.OnHighlightReceived += AltEventClient_OnHighlightReceived;
            
            AltEventClient.OnRaidReceived += AltEventClient_OnRaidReceived;
            AltEventClient.OnFollowReceived += AltEventClient_OnFollowReceived;
            AltEventClient.OnRedemptionReceived += AltEventClient_OnRedemptionReceived;
            AltEventClient.OnUnhandledReceived += AltEventClient_OnUnhandledReceived;
            
            AltEventClient.OnConnected += AltEventClient_OnConnected;
            AltEventClient.OnDisconnected += AltEventClient_OnDisconnected;

            Log("Connecting to AltStream");
            AltEventClient.ConnectAsync();
        }

        private static void AltEventClient_OnFollowReceived(EventMessage obj) {
            throw new NotImplementedException();
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
            MIUCommand PointsCommand = MixItUpClient.MIUChannelPoints.FirstOrDefault(i => (i.Name == PointsName));
            if (PointsCommand != null) {
                Log($"Attempting to run command: {PointsCommand.ID}");
                PointsCommand.RunCommand(RedeemEvent.UserInput, Settings.SpoofPlatform, RedeemEvent.From, RedeemEvent.From, obj.Context.UserColor, obj.Context.AuthorImage);
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
            if (MixItUpClient.ChannelRaided != null) {
                MixItUpClient.ChannelRaided.RunCommand("", Settings.SpoofPlatform, RaidEvent.SourceUsername, RaidEvent.SourceUsername, obj.Context.UserColor, obj.Context.AuthorImage, RaidEvent.Viewers);
            }
        }

        private static void AltMessageReceived(AltStreamLib.EventSub.Chat.Models.Message Context, bool Highlighted = false) {
            try {
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
                            MIUChatCommand ChatCommand = MixItUpClient.MIUChatCommands.FirstOrDefault(i => i.TriggerNames.Contains(Command));
                            if (ChatCommand != null) {
                                Log($"Attempting to run command: {ChatCommand.ID}");
                                ChatCommand.RunCommand(Args, Settings.SpoofPlatform, Context.AuthorUsername, Context.AuthorUsername, Context.UserColor, Context.AuthorImage);
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
            Log($"Connected to channel {ChannelName}");
            SetWindowTitle("Connected");
        }
        private static void AltEventClient_OnDisconnected() {
            Connected = false;
            Log("Disconnected.");
        }
    }
}
