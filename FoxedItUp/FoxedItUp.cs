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

namespace FoxedItUp {
    internal class FoxedItUp {
        private int ConnectAttempts = 0;
        internal EventSubClient AltEventClient;
        internal void Connect(string ChannelName) {
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
        }

        private void AltEventClient_OnUnhandledReceived(EventMessage obj) {
            Log("Unrecognised Event:");
            Log(Newtonsoft.Json.JsonConvert.SerializeObject(obj));
        }

        private void AltEventClient_OnRedemptionReceived(EventMessage obj) {
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
                if (!string.IsNullOrEmpty(obj.Context.AuthorImage)) { SpecialIdentifiers.Add(new JProperty("UserAvatar", $"https://altstream.online{obj.Context.AuthorImage}")); }
                PointsCommand.RunCommand(RedeemEvent.UserInput, Settings.SpoofPlatform, SpecialIdentifiers);
            } else {
                Log("Could not find command");
            }

        }

        private void AltEventClient_OnRaidReceived(EventMessage obj) {
            AltStreamLib.EventSub.Chat.Models.Events.RaidIncoming RaidEvent = (AltStreamLib.EventSub.Chat.Models.Events.RaidIncoming)obj.Event;
            Log($"* {RaidEvent.SourceUsername} raided with {RaidEvent.Viewers}");

            JObject SpecialIdentifiers = new JObject(
                new JProperty("RaidViewerCount", RaidEvent.Viewers),
                new JProperty("UserName", RaidEvent.SourceUsername),
                new JProperty("UserDisplayName", RaidEvent.SourceUsername),
                new JProperty("UserAvatar", obj.Context.AuthorImage),
                new JProperty("UserColor", obj.Context.UserColor)
            );
            if (!string.IsNullOrEmpty(obj.Context.AuthorImage)) { SpecialIdentifiers.Add(new JProperty("UserAvatar", $"https://altstream.online{obj.Context.AuthorImage}")); }
            MixItUpClient.ChannelRaided.RunCommand("", Settings.SpoofPlatform, SpecialIdentifiers);
        }

        private void AltMessageReceived(AltStreamLib.EventSub.Chat.Models.Message Context, bool Highlighted = false) {

            try {

                //Log($"<{Context.AuthorImage}");.
                string Command = "";
                string Args = "";

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
                            if (!string.IsNullOrEmpty(Context.AuthorImage)) { SpecialIdentifiers.Add(new JProperty("UserAvatar", $"https://altstream.online{Context.AuthorImage}")); }

                            ChatCommand.RunCommand(Args, Settings.SpoofPlatform, new JObject(
                                new JProperty("UserName", Context.AuthorUsername),
                                new JProperty("UserDisplayName", Context.AuthorUsername),
                                new JProperty("UserAvatar", Context.AuthorImage),
                                new JProperty("UserColor", Context.UserColor)
                            ));
                        } else {
                            Log("Could not find command");
                        }
                    }
                } else {
                    Log($"<{Context.AuthorUsername}> {Context.Text}");
                }
            } catch (Exception e) {
                Log(e.ToString());
            }
        }
        private void AltEventClient_OnHighlightReceived(EventMessage obj) {
            AltMessageReceived(obj.Context,true);
        }
        private void AltEventClient_OnMessageReceived(Message obj) {
            AltMessageReceived(obj);

        }
        private void AltEventClient_OnConnected() {
            Log($"Connected");
            ConnectAttempts = 0;
        }
        private void AltEventClient_OnDisconnected() {
            ConnectAttempts++;
            int Delay;
            switch(ConnectAttempts) {
                case 1: Delay = 1; break;
                case 2: Delay = 5; break;
                case 3: Delay = 10; break;
                case 4: Delay = 20; break;
                default:Delay = 30; break;
            }
            Log($"Disconnected. Waiting {Delay} seconds to reconnect (Attempts: {ConnectAttempts})");
            System.Threading.Thread.Sleep(Delay);
            Log("Reconnecting to AltStream");
            AltEventClient.ConnectAsync();
        }
    }
}
