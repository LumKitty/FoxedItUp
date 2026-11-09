using AltStreamLib.Api.Chat.Models;
using AltStreamLib.Api.v1.Models;
using AltStreamLib.EventSub;
using AltStreamLib.EventSub.Chat.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Contexts;
using System.Threading.Tasks;
using static FoxedItUp._Settings;
using static FoxedItUp.Functions;

namespace FoxedItUp {
    internal class AltStreamUserInfo {
        internal UserSettingsResponse UserInfo = null;
        internal ChannelResponse ChannelInfo = null;
        internal bool IsStreamer = false;
        internal bool IsModerator = false;
        internal bool IsSubscriber = false;
        //internal bool IsFollower = false;          /* borked at API level
        internal DateTime FetchedAt = new DateTime(0);
        //internal DateTime FollowedAt = new DateTime(0);
        internal DateTime CreatedAt = new DateTime(0);
    }
    
    internal static class AltStream {
        private static string ChannelName;
        internal static AltStreamLib.Api.ApiClient AltAPI = null;
        private static bool Connected = false;
        internal static EventSubClient AltEventClient;
        internal static Dictionary<string, AltStreamUserInfo> UserInfoCache = new Dictionary<string, AltStreamUserInfo>();
        internal static List<String> Moderators = new List<string>();
         
        internal static async Task UpdateModerators(string ChannelName) {
            List<string> TempModerators = new List<string>();
            Log($"Looking up moderators for: {ChannelName}");
            ModResponse Response = await AltAPI.GetModInfoAsync(ChannelName);
            foreach (Moderator moderator in Response.Moderators) {
                Log($"Found moderator: {moderator.Username}");
                TempModerators.Add(moderator.Username.ToLower());
            }
            if (TempModerators.Count > 0) { Moderators = TempModerators; }
        }
        
        
        internal static void Connect(string _ChannelName) {
            try {
                SetWindowTitle("Connecting");
                ChannelName = _ChannelName;

                AltEventClient = new EventSubClient(ChannelName, GetBotToken());
                AltEventClient.OnMessageReceived += AltEventClient_OnMessageReceived;
                AltEventClient.OnHighlightReceived += AltEventClient_OnHighlightReceived;

                AltEventClient.OnRaidReceived += AltEventClient_OnRaidReceived;
                AltEventClient.OnRedemptionReceived += AltEventClient_OnRedemptionReceived;
                // AltEventClient.OnFollowReceived += AltEventClient_OnFollowReceived;
                AltEventClient.OnSubscriptionReceived += AltEventClient_OnSubscriptionReceived;
                AltEventClient.OnTipReceived += AltEventClient_OnTipReceived;

                AltEventClient.OnUnhandledReceived += AltEventClient_OnUnhandledReceived;

                AltEventClient.OnConnected += AltEventClient_OnConnected;
                AltEventClient.OnTimeout += AltEventClient_OnTimeout;
                AltEventClient.OnDisconnected += AltEventClient_OnDisconnected;

                Log("Connecting to AltStream");
                AltEventClient.ConnectAsync().Wait();

                AltAPI = new AltStreamLib.Api.ApiClient(Settings.BotToken);

            } catch (Exception Ex) {
                ErrorHandler(Ex);
            }
        }

        internal static AltStreamUserInfo GetBasicUserInfo(string UserName) {

            AltStreamUserInfo User = new AltStreamUserInfo();
            if (UserInfoCache.TryGetValue(UserName, out User)) { 
                if (DateTime.UtcNow.AddMinutes(-15) <= User.FetchedAt) {
                    return User;
                }
            }
            User = new AltStreamUserInfo();

            User.UserInfo = AltAPI.GetUserSettingsInfoAsync(UserName).GetAwaiter().GetResult();
            if (Settings.IncludeAccountAgeAndGameInfo) {
                User.ChannelInfo = AltAPI.GetChannelInfoAsync(UserName).GetAwaiter().GetResult();
            } else {
                User.ChannelInfo = null;
            }
            if (Settings.ShowRandomJSON) {
                Log(Newtonsoft.Json.JsonConvert.SerializeObject(User.ChannelInfo));
            }

            //Log(JsonConvert.SerializeObject(User.UserInfo, Formatting.Indented));

            if (Moderators.Contains(UserName.ToLower())) { User.IsModerator = true; }
            if (Settings.APISubFlipWorkaround) {
                User.IsSubscriber = false;
            } else {
                User.IsSubscriber = User.UserInfo.IsSubscriber;
            }
            //User.IsFollower = !String.IsNullOrEmpty(User.UserInfo.FollowedAt);
            User.IsStreamer = (UserName.ToLower() == Settings.ChannelName.ToLower());
            User.FetchedAt = DateTime.UtcNow;

            if (User.UserInfo != null) {
                if (User.UserInfo.FollowedAt != null) {
                    //User.FollowedAt = Newtonsoft.Json.JsonConvert.DeserializeObject<DateTime>(User.UserInfo.FollowedAt);
                }
                if (User.UserInfo.CreatedAt != null) {
                    //Log($"User created at raw: {User.UserInfo.CreatedAt}");
                    User.CreatedAt = DateTime.Parse(User.UserInfo.CreatedAt);
                    //Log($"User created at parsed: {User.CreatedAt.ToString()}");
                } 
                return User;
            }
            
            return null;
            
        }
        
        private static async void AltEventClient_OnTipReceived(Data obj) {
            try { 
                Log("Subscription received");
                if (Settings.ShowRandomJSON) {
                    Log(Newtonsoft.Json.JsonConvert.SerializeObject(obj));
                }
                Kofi DonationEvent = obj.Kofi;
                Log($"* {obj.Author.ChannelName} Donated {DonationEvent.CurrencyCode}{DonationEvent.Amount} - {DonationEvent.Message}");
           
                if (MixItUpClient.ChannelDonation != null) {
                    await MixItUpClient.ChannelDonation.RunCommand(DonationEvent.Message, Settings.SpoofPlatform, obj.Author, DonationEvent.CurrencyCode, DonationEvent.Amount);
                }
            } catch (Exception Ex) {
                ErrorHandler(Ex);
            }
        }

        private static async void AltEventClient_OnSubscriptionReceived(Data obj) {
            try {
                Log("Subscription received");
                if (Settings.ShowRandomJSON) {
                    Log(Newtonsoft.Json.JsonConvert.SerializeObject(obj));
                }
                Kofi SubEvent = obj.Kofi;
                Log($"* {obj.Author.ChannelName} subscribed at {SubEvent.TierName}");

                if (SubEvent.IsFirstSubscriptionPayment) {
                    if (MixItUpClient.ChannelSubscribed != null) {
                        await MixItUpClient.ChannelSubscribed.RunCommand("", Settings.SpoofPlatform, obj.Author, SubEvent.TierName);
                    }
                } else {
                    if (MixItUpClient.ChannelResubscribed != null) {
                        await MixItUpClient.ChannelResubscribed.RunCommand("", Settings.SpoofPlatform, obj.Author, SubEvent.TierName);
                    }
                }
            } catch (Exception Ex) {
                ErrorHandler(Ex);
            }
        }
        /* Will be available in a future version of AltStreamLib
        private static void AltEventClient_OnFollowReceived(EventMessage obj) {
            try {

                Log("Follow received");
                Log($"* {obj.Context.AuthorUsername} followed");

                if (MixItUpClient.ChannelFollowed != null) {
                    MixItUpClient.ChannelFollowed.RunCommand("", Settings.SpoofPlatform, obj.Context.AuthorUsername);
                }
            } catch (Exception Ex) {
                ErrorHandler(Ex);
            }
        }
        */

        private static void AltEventClient_OnUnhandledReceived(Data obj) {
            Log("Unrecognised Event:");
            if (Settings.ShowRandomJSON) {
                Log(Newtonsoft.Json.JsonConvert.SerializeObject(obj));
            }
            Log(Newtonsoft.Json.JsonConvert.SerializeObject(obj));
        }
        
        private static async void AltEventClient_OnRedemptionReceived(Data obj) {
            try {
                if (Settings.ShowRandomJSON) {
                    Log(Newtonsoft.Json.JsonConvert.SerializeObject(obj));
                }
                Redemption RedeemEvent = obj.Redemption;
                Log($"* {RedeemEvent.From.ChannelName} ({RedeemEvent.From.DisplayName}) redeemed \"{RedeemEvent.RewardName}\" for {RedeemEvent.Cost} {RedeemEvent.PointsName}");

                string PointsName = RedeemEvent.RewardName.ToLower();
                Log($"Attempting to run command {PointsName}");
                MIURedeemCommand PointsCommand = MixItUpClient.MIUChannelPoints.FirstOrDefault(i => (i.Name == PointsName));
                if (PointsCommand != null) {
                    Log($"Attempting to run command: {PointsCommand.ID}");
                    await PointsCommand.RunCommand(RedeemEvent.UserInput, Settings.SpoofPlatform, RedeemEvent.From, RedeemEvent.Id);
                } else {
                    Log("Could not find command");
                }
            } catch (Exception Ex) {
                ErrorHandler(Ex);
            }
        }
        

        private static async void AltEventClient_OnRaidReceived(Data obj) {
            try {
                if (Settings.ShowRandomJSON) {
                    Log(Newtonsoft.Json.JsonConvert.SerializeObject(obj));
                }
                Raid RaidEvent = obj.Raid;
                Log($"* {RaidEvent.From} raided with {RaidEvent.Viewers}");
                if (MixItUpClient.ChannelRaided != null) {
                    await MixItUpClient.ChannelRaided.RunCommand("", Settings.SpoofPlatform, RaidEvent.From, RaidEvent.Viewers);
                }
            } catch (Exception Ex) {
                ErrorHandler(Ex);
            }
        }
        
        private static async Task AltMessageReceived(Data Context, bool Highlighted = false) {
            try {
                string Command = "";
                string Args = "";

                if (!string.IsNullOrEmpty(Context.Message.Text)) {
                    if (Context.Message.Text.Substring(0, 1) == "!") {
                        Log($"Received command message {Context.Message.Text}");
                        int n = Context.Message.Text.IndexOf(' ');
                        Log($"Index {n}");
                        switch (n) {
                            case 1:
                            case 0:
                                Log($"Invalid message {Context.Message.Text}"); break;
                            case -1:
                                Command = Context.Message.Text.Substring(1);
                                Log($"* {Context.Author.ChannelName} ({Context.Author.DisplayName}) used command '{Command}'");
                                break;
                            default:
                                Command = Context.Message.Text.Substring(1, n - 1);
                                Args = Context.Message.Text.Substring(n + 1);
                                Log($"* {Context.Author.ChannelName} ({Context.Author.DisplayName}) used command '{Command}' with args: {Args}");
                                break;
                        }

                        if (!String.IsNullOrEmpty(Command)) {
                            Log($"Attempting to run command {Command}");
                            MIUChatCommand ChatCommand = MixItUpClient.MIUChatCommands.FirstOrDefault(i => i.TriggerNames.Contains(Command));
                            if (ChatCommand != null) {
                                //Log($"Attempting to run command: {ChatCommand.ID}");
                                await ChatCommand.RunCommand(Args, Settings.SpoofPlatform, Context.Author);
                            } else {
                                Log("Could not find command");
                            }
                        }
                    } else {
                        Log($"<{Context.Author.ChannelName}> ({Context.Author.DisplayName}) {Context.Message.Text}");
                    }
                } else {
                    Log($"Received empty message: {Context.ToString()}");
                }
            } catch (Exception Ex) {
                ErrorHandler(Ex);
            }
        }
        
        private static async void AltEventClient_OnHighlightReceived(Data obj) {
            if (Settings.ShowRandomJSON) {
                Log(Newtonsoft.Json.JsonConvert.SerializeObject(obj));
            }
            await AltMessageReceived(obj,true);
        }
        private static async void AltEventClient_OnMessageReceived(Data obj) {
            if (Settings.ShowRandomJSON) {
                Log(Newtonsoft.Json.JsonConvert.SerializeObject(obj));
            }
            await AltMessageReceived(obj);
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
                Log("Disconnected and giving up");
            } catch (Exception Ex) {
                ErrorHandler(Ex);
            }
        }
        private static void AltEventClient_OnTimeout() {
            try {
                Connected = false;
                Log("Connection timed out, reconnecting....");
            } catch (Exception Ex) {
                ErrorHandler(Ex);
            }
        }

        internal static async void SendChat(string Message) {
            Log($"Sending chat message {Message}");
            //await AltAPI.SendChatMessageAsync(Message);  // Not working in AltStreamLib currently
            await httpRequest.HttpRequest("POST", "https://altstream.online/api/v2/chat", new JObject(
                new JProperty("channel", ChannelName),
                new JProperty("message", Message)
            ).ToString(), Settings.BotToken);
        }
        internal static async void ShoutOut(string UserName) {
            Log($"Sending shoutout for {UserName}");
            //await AltAPI.SendShoutoutAsync(ChannelName, UserName);  // Not working in AltStreamLib currently
            await httpRequest.HttpRequest("POST", "https://altstream.online/api/shoutout", new JObject(
                new JProperty("channel", ChannelName),
                new JProperty("target", UserName)
            ).ToString(), Settings.BotToken);
        }
        internal static async void CompleteRedeem(string ID, bool Fulfilled=true) {
            int intID;
            if (int.TryParse(ID, out intID)) {
                JObject Message = new JObject(
                    new JProperty("channel", ChannelName),
                    new JProperty("id", intID)
                 );
                if (Fulfilled) {
                    Message.Add(new JProperty("status", "fulfilled"));
                } else {
                    Message.Add(new JProperty("status", "refunded"));
                }
                //Log(Message.ToString());
                await httpRequest.HttpRequest("PATCH", "https://altstream.online/api/v2/rewards/redemptions", Message.ToString(), Settings.BotToken);
            }
        }
    }
}
