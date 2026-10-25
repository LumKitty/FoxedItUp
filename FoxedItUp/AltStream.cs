using AltStreamLib.EventSub;
using AltStreamLib.EventSub.Chat.Models;
using AltStreamLib.EventSub.Chat.Models.Events;
using AltStreamLib.Api.v1.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;
using static FoxedItUp.Functions;
using static FoxedItUp._Settings;
using System.ComponentModel;

namespace FoxedItUp {
    internal static class AltStream {
        private static string ChannelName;
        internal static AltStreamLib.Api.ApiClient AltAPI = new AltStreamLib.Api.ApiClient();
        private static bool Connected = false;
        internal static EventSubClient AltEventClient;
        
        internal static void Connect(string _ChannelName) {
            try {
                SetWindowTitle("Connecting");
                ChannelName = _ChannelName;

                AltEventClient = new EventSubClient(ChannelName);
                AltEventClient.OnMessageReceived += AltEventClient_OnMessageReceived;
                AltEventClient.OnHighlightReceived += AltEventClient_OnHighlightReceived;

                AltEventClient.OnRaidReceived += AltEventClient_OnRaidReceived;
                AltEventClient.OnFollowReceived += AltEventClient_OnFollowReceived;
                AltEventClient.OnSubscriptionReceived += AltEventClient_OnSubscriptionReceived;
                AltEventClient.OnTipReceived += AltEventClient_OnTipReceived;
                AltEventClient.OnRedemptionReceived += AltEventClient_OnRedemptionReceived;
                AltEventClient.OnUnhandledReceived += AltEventClient_OnUnhandledReceived;

                AltEventClient.OnConnected += AltEventClient_OnConnected;
                AltEventClient.OnTimeout += AltEventClient_OnTimeout;
                AltEventClient.OnDisconnected += AltEventClient_OnDisconnected;

                Log("Connecting to AltStream");
                AltEventClient.ConnectAsync().Wait();
            } catch (Exception Ex) {
                ErrorHandler(Ex);
            }
        }

        private static bool GetBasicUserInfo(string UserName, out string DisplayName, out string AvatarURL, out string UserColor) {
            UserSettingsResponse UserSettingsInfo = AltAPI.GetUserSettingsInfoAsync(UserName).GetAwaiter().GetResult();

            if (UserSettingsInfo != null) {
                DisplayName = UserSettingsInfo.Name;
                AvatarURL = UserSettingsInfo.Image;
                UserColor = UserSettingsInfo.ChatColor;
                return false;
            } else {
                DisplayName = UserName;
                AvatarURL = null;
                UserColor = "#ffffff";
                return true;
            }
        }

        private static void AltEventClient_OnTipReceived(EventMessage obj) {
            try { 
                string DisplayName;
                string AvatarURL;
                string UserColor;

                Log("Subscription received");
                KofiEvent DonationEvent = (KofiEvent)obj.Event;
                GetBasicUserInfo(DonationEvent.Username, out DisplayName, out AvatarURL, out UserColor);
                Log($"* {DisplayName} ({DonationEvent.Username}) Donated {DonationEvent.Currency}{DonationEvent.Amount} - {DonationEvent.Message}");
           
                if (MixItUpClient.ChannelDonation != null) {
                    MixItUpClient.ChannelDonation.RunCommand(DonationEvent.Message, Settings.SpoofPlatform, DonationEvent.Username, DisplayName, UserColor, AvatarURL, DonationEvent.Currency, DonationEvent.Amount);
                }
            } catch (Exception Ex) {
                ErrorHandler(Ex);
            }
        }

        private static void AltEventClient_OnSubscriptionReceived(EventMessage obj) {
            try {
                string DisplayName;
                string AvatarURL;
                string UserColor;

                Log("Subscription received");
                KofiEvent SubEvent = (KofiEvent)obj.Event;
                GetBasicUserInfo(SubEvent.Username, out DisplayName, out AvatarURL, out UserColor);
                Log($"* {DisplayName} ({SubEvent.Username}) subscribed at {SubEvent.TierName}");

                if (SubEvent.IsFirstSubscriptionPayment) {
                    if (MixItUpClient.ChannelSubscribed != null) {
                        MixItUpClient.ChannelSubscribed.RunCommand("", Settings.SpoofPlatform, SubEvent.Username, DisplayName, UserColor, AvatarURL, SubEvent.TierName);
                    }
                } else {
                    if (MixItUpClient.ChannelResubscribed != null) {
                        MixItUpClient.ChannelResubscribed.RunCommand("", Settings.SpoofPlatform, SubEvent.Username, DisplayName, UserColor, AvatarURL, SubEvent.TierName);
                    }
                }
            } catch (Exception Ex) {
                ErrorHandler(Ex);
            }
        }

        private static void AltEventClient_OnFollowReceived(EventMessage obj) {
            try {
                string DisplayName;
                string AvatarURL;
                string UserColor;

                Log("Follow received");
                GetBasicUserInfo(obj.Context.Author, out DisplayName, out AvatarURL, out UserColor);
                Log($"* {DisplayName} ({obj.Context.AuthorUsername}) followed");

                if (MixItUpClient.ChannelFollowed != null) {
                    MixItUpClient.ChannelFollowed.RunCommand("", Settings.SpoofPlatform, obj.Context.AuthorUsername, DisplayName, UserColor, AvatarURL);
                }
            } catch (Exception Ex) {
                ErrorHandler(Ex);
            }
        }

        private static void AltEventClient_OnUnhandledReceived(EventMessage obj) {
            Log("Unrecognised Event:");
            Log(Newtonsoft.Json.JsonConvert.SerializeObject(obj));
        }

        private static void AltEventClient_OnRedemptionReceived(EventMessage obj) {
            try {
                AltStreamLib.EventSub.Chat.Models.Events.Redemption RedeemEvent = (AltStreamLib.EventSub.Chat.Models.Events.Redemption)obj.Event;
                Log($"* {RedeemEvent.From} redeemed \"{RedeemEvent.RewardName}\" for {RedeemEvent.Cost} {RedeemEvent.PointsName}");
                string PointsName = RedeemEvent.RewardName.ToLower();
                Log($"Attempting to run command {PointsName}");
                MIURedeemCommand PointsCommand = MixItUpClient.MIUChannelPoints.FirstOrDefault(i => (i.Name == PointsName));
                if (PointsCommand != null) {
                    Log($"Attempting to run command: {PointsCommand.ID}");
                    PointsCommand.RunCommand(RedeemEvent.UserInput, Settings.SpoofPlatform, RedeemEvent.From, obj.Context.Author, obj.Context.UserColor, obj.Context.AuthorImage);
                } else {
                    Log("Could not find command");
                }
            } catch (Exception Ex) {
                ErrorHandler(Ex);
            }
        }

        private static void AltEventClient_OnRaidReceived(EventMessage obj) {
            try {
                string DisplayName;
                string AvatarURL;
                string UserColor;

                Log("Raid received");
                AltStreamLib.EventSub.Chat.Models.Events.RaidIncoming RaidEvent = (AltStreamLib.EventSub.Chat.Models.Events.RaidIncoming)obj.Event;

                GetBasicUserInfo(RaidEvent.SourceUsername, out DisplayName, out AvatarURL, out UserColor);

                Log($"* {DisplayName} ({RaidEvent.SourceUsername}) raided with {RaidEvent.Viewers}");
                if (MixItUpClient.ChannelRaided != null) {
                    MixItUpClient.ChannelRaided.RunCommand("", Settings.SpoofPlatform, RaidEvent.SourceUsername, DisplayName, UserColor, AvatarURL, RaidEvent.Viewers);
                }
            } catch (Exception Ex) {
                ErrorHandler(Ex);
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
                                Log($"* {Context.Author} ({Context.AuthorUsername}) used command '{Command}'");
                                break;
                            default:
                                Command = Context.Text.Substring(1, n - 1);
                                Args = Context.Text.Substring(n + 1);
                                Log($"* {Context.Author} ({Context.AuthorUsername}) used command '{Command}' with args: {Args}");
                                break;
                        }

                        if (!String.IsNullOrEmpty(Command)) {
                            Log($"Attempting to run command {Command}");
                            MIUChatCommand ChatCommand = MixItUpClient.MIUChatCommands.FirstOrDefault(i => i.TriggerNames.Contains(Command));
                            if (ChatCommand != null) {
                                //Log($"Attempting to run command: {ChatCommand.ID}");
                                ChatCommand.RunCommand(Args, Settings.SpoofPlatform, Context.AuthorUsername, Context.Author, Context.UserColor, Context.AuthorImage);
                            } else {
                                Log("Could not find command");
                            }
                        }
                    } else {
                        Log($"<{Context.Author}> ({Context.AuthorUsername}) {Context.Text}");
                    }
                } else {
                    Log($"Received empty message: {Context.ToString()}");
                }
            } catch (Exception Ex) {
                ErrorHandler(Ex);
            }
        }
        private static void AltEventClient_OnHighlightReceived(EventMessage obj) {
            AltMessageReceived(obj.Context,true);
        }
        private static void AltEventClient_OnMessageReceived(Message obj) {
            AltMessageReceived(obj);

        }
        private static void AltEventClient_OnConnected() {
            try {
                Connected = true;
                Log($"Connected to channel {ChannelName}");
                SetWindowTitle("Connected");
            } catch (Exception Ex) {
                ErrorHandler(Ex);
            }
        }
        private static void AltEventClient_OnDisconnected() {
            try {
                Connected = false;
                Log("Connection timed out, reconnecting....");
            } catch (Exception Ex) {
                ErrorHandler(Ex);
            }
        }
        private static void AltEventClient_OnTimeout() {
            try {
                Connected = false;
                Log("Disconnected and giving up");
            } catch (Exception Ex) {
                ErrorHandler(Ex);
            }
        }
    }
}
