using AltStreamLib.Api.Chat.Models;
using AltStreamLib.Api.v1.Models;
using AltStreamLib.Api.v2.Models;
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
        internal User UserInfo = null;
        internal ChannelResponse ChannelInfo = null;
        internal bool IsStreamer = false;
        internal bool IsModerator = false;
        internal bool IsSubscriber = false;
        internal bool IsFollower = false;          /* borked at API level */
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
        //internal static List<String> Moderators = new List<string>();
         
        /*
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
        */
        
        internal static void Connect(string _ChannelName) {
            try {
                SetWindowTitle("Connecting");
                ChannelName = _ChannelName;

                Log($"Connecting as '{Settings.ChannelName}' using token '{Settings.BotToken}'", 69);
                AltEventClient = new EventSubClient(Settings.ChannelName, Settings.BotToken);
                AltEventClient.OnMessageReceived += AltEventClient_OnMessageReceived;
                AltEventClient.OnHighlightReceived += AltEventClient_OnHighlightReceived;

                AltEventClient.OnRaidReceived += AltEventClient_OnRaidReceived;
                AltEventClient.OnRedemptionReceived += AltEventClient_OnRedemptionReceived;
                AltEventClient.OnFollowReceived += AltEventClient_OnFollowReceived;
                AltEventClient.OnSubscriptionReceived += AltEventClient_OnSubscriptionReceived;
                AltEventClient.OnTipReceived += AltEventClient_OnTipReceived;

                AltEventClient.OnUnhandledReceived += AltEventClient_OnUnhandledReceived;

                AltEventClient.OnConnected += AltEventClient_OnConnected;
                AltEventClient.OnTimeout += AltEventClient_OnTimeout;
                AltEventClient.OnDisconnected += AltEventClient_OnDisconnected;

                Output("Connecting to AltStream");
                AltEventClient.ConnectAsync().Wait();

                AltAPI = new AltStreamLib.Api.ApiClient(Settings.BotToken);

            } catch (Exception Ex) {
                ErrorHandler(Ex);
            }
        }

        internal static void Disconnect() {
            AltEventClient.DisconnectAsync().GetAwaiter().GetResult();
            AltAPI.Dispose();
        }

        internal static AltStreamUserInfo GetBasicUserInfo(string UserName) {

            AltStreamUserInfo User = new AltStreamUserInfo();
            if (UserInfoCache.TryGetValue(UserName, out User)) { 
                if (DateTime.UtcNow.AddMinutes(-15) <= User.FetchedAt) {
                    return User;
                }
            }
            User = new AltStreamUserInfo();

            User.UserInfo = AltAPI.GetUserInfoAsync(UserName).GetAwaiter().GetResult();
            if (Settings.IncludeAccountAgeAndGameInfo) {
                User.ChannelInfo = AltAPI.GetChannelInfoAsync(UserName).GetAwaiter().GetResult();
            } else {
                User.ChannelInfo = null;
            }
            if (Settings.LogLevel >= 6) {
                Log(Newtonsoft.Json.JsonConvert.SerializeObject(User.ChannelInfo), 6);
            }

            //Log(JsonConvert.SerializeObject(User.UserInfo, Formatting.Indented));

            //if (Moderators.Contains(UserName.ToLower())) { User.IsModerator = true; }
            User.IsSubscriber = User.UserInfo.Roles.ChannelSubscriber;
            User.IsModerator = User.UserInfo.Roles.Moderator;
            User.IsFollower = User.UserInfo.Roles.Follower;
            User.IsStreamer = User.UserInfo.Roles.Broadcaster;
            User.FetchedAt = DateTime.UtcNow;

            if (User.UserInfo != null) {
                //if (User.UserInfo. != null) {
                    //User.FollowedAt = Newtonsoft.Json.JsonConvert.DeserializeObject<DateTime>(User.UserInfo.FollowedAt);
                //}
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
                Output("Subscription received");
                if (Settings.LogLevel >= 6) {
                    Log(Newtonsoft.Json.JsonConvert.SerializeObject(obj), 6);
                }
                Kofi DonationEvent = obj.Kofi;
                Output($"* {obj.Author.ChannelName} ({obj.Author.DisplayName}) Donated {DonationEvent.CurrencyCode}{DonationEvent.Amount} - {DonationEvent.Message}");
           
                if (MixItUpClient.ChannelDonation != null) {
                    await MixItUpClient.ChannelDonation.RunCommand(DonationEvent.Message, Settings.SpoofPlatform, obj.Author, DonationEvent.CurrencyCode, DonationEvent.Amount);
                }
            } catch (Exception Ex) {
                ErrorHandler(Ex);
            }
        }

        private static async void AltEventClient_OnSubscriptionReceived(Data obj) {
            try {
                Output("Subscription received");
                if (Settings.LogLevel >= 6) {
                    Log(Newtonsoft.Json.JsonConvert.SerializeObject(obj), 6);
                }
                Kofi SubEvent = obj.Kofi;
                Output($"* {obj.Author.ChannelName} ({obj.Author.DisplayName}) subscribed at {SubEvent.TierName}");

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
        private static async void AltEventClient_OnFollowReceived(Data obj) {
            try {

                Output($"* {obj.FollowedBy.DisplayName} ({obj.Author.ChannelName}) followed");

                if (MixItUpClient.ChannelFollowed != null) {
                    await MixItUpClient.ChannelFollowed.RunCommand("", Settings.SpoofPlatform, obj.FollowedBy);
                }
            } catch (Exception Ex) {
                ErrorHandler(Ex);
            }
        }

        private static void AltEventClient_OnUnhandledReceived(Data obj) {
            Output("Unrecognised Event:");
            if (Settings.LogLevel >= 1) {
                Output(Newtonsoft.Json.JsonConvert.SerializeObject(obj));
            }
        }
        
        private static async void AltEventClient_OnRedemptionReceived(Data obj) {
            try {
                if (Settings.LogLevel >= 6) {
                    Output(Newtonsoft.Json.JsonConvert.SerializeObject(obj));
                }
                AltStreamLib.EventSub.Chat.Models.Redemption RedeemEvent = obj.Redemption;

                Output($"* {RedeemEvent.From.ChannelName} ({RedeemEvent.From.DisplayName}) redeemed \"{RedeemEvent.RewardName}\" for {RedeemEvent.Cost} {RedeemEvent.PointsName}");

                string PointsName = RedeemEvent.RewardName.ToLower();
                Output($"Attempting to run command {PointsName}");
                MIURedeemCommand PointsCommand = MixItUpClient.MIUChannelPoints.FirstOrDefault(i => (i.Name == PointsName));
                if (PointsCommand != null) {
                    Output($"Attempting to run command: {PointsCommand.ID}");
                    await PointsCommand.RunCommand(RedeemEvent.UserInput, Settings.SpoofPlatform, RedeemEvent.From, RedeemEvent.Id);
                } else {
                    Output("Could not find command");
                }
            } catch (Exception Ex) {
                ErrorHandler(Ex);
            }
        }
        

        private static async void AltEventClient_OnRaidReceived(Data obj) {
            try {
                if (Settings.LogLevel >= 6) {
                    Output(Newtonsoft.Json.JsonConvert.SerializeObject(obj));
                }
                Raid RaidEvent = obj.Raid;
                Output($"* {RaidEvent.From.ChannelName} ({RaidEvent.From.DisplayName}) raided with {RaidEvent.Viewers}");
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
                        Output($"Received command message {Context.Message.Text}");
                        int n = Context.Message.Text.IndexOf(' ');
                        Output($"Index {n}");
                        switch (n) {
                            case 1:
                            case 0:
                                Output($"Invalid message {Context.Message.Text}"); break;
                            case -1:
                                Command = Context.Message.Text.Substring(1);
                                Output($"* {Context.Author.ChannelName} ({Context.Author.DisplayName}) used command '{Command}'");
                                break;
                            default:
                                Command = Context.Message.Text.Substring(1, n - 1);
                                Args = Context.Message.Text.Substring(n + 1);
                                Output($"* {Context.Author.ChannelName} ({Context.Author.DisplayName}) used command '{Command}' with args: {Args}");
                                break;
                        }

                        if (!String.IsNullOrEmpty(Command)) {
                            Output($"Attempting to run command {Command}");
                            MIUChatCommand ChatCommand = MixItUpClient.MIUChatCommands.FirstOrDefault(i => i.TriggerNames.Contains(Command));
                            if (ChatCommand != null) {
                                //Log($"Attempting to run command: {ChatCommand.ID}");
                                await ChatCommand.RunCommand(Args, Settings.SpoofPlatform, Context.Author);
                            } else {
                                Output("Could not find command");
                            }
                        }
                    } else {
                        Output($"<{Context.Author.ChannelName}> ({Context.Author.DisplayName}) {Context.Message.Text}");
                    }
                } else if (Context.Message.Sticker != null) {
                    Output($"<{Context.Author.ChannelName}> ({Context.Author.DisplayName}) sent a sticker: {Context.Message.Sticker.Name}");
                } else {
                    Output($"Received empty message: {Context.ToString()}");
                }
            } catch (Exception Ex) {
                ErrorHandler(Ex);
            }
        }
        
        private static async void AltEventClient_OnHighlightReceived(Data obj) {
            if (Settings.LogLevel >= 6) {
                Output(Newtonsoft.Json.JsonConvert.SerializeObject(obj));
            }
            await AltMessageReceived(obj,true);
        }
        private static async void AltEventClient_OnMessageReceived(Data obj) {
            if (Settings.LogLevel >= 6) {
                Output(Newtonsoft.Json.JsonConvert.SerializeObject(obj));
            }
            await AltMessageReceived(obj);
        }

        private static void AltEventClient_OnConnected() {
            try {
                Connected = true;
                Output($"Connected to channel {ChannelName}");
                SetWindowTitle("Connected");
            } catch (Exception Ex) {
                ErrorHandler(Ex);
            }
        }
        private static void AltEventClient_OnDisconnected() {
            try {
                Connected = false;
                Output("Disconnected and giving up");
            } catch (Exception Ex) {
                ErrorHandler(Ex);
            }
        }
        private static void AltEventClient_OnTimeout() {
            try {
                Connected = false;
                Output("Connection timed out, reconnecting....");
            } catch (Exception Ex) {
                ErrorHandler(Ex);
            }
        }

        internal static async void SendChat(string Message) {
            Output($"Sending chat message {Message}");
            //await AltAPI.SendChatMessageAsync(ChannelName, Message);  // Not working in AltStreamLib currently
            
            await httpRequest.HttpRequest("POST", "https://altstream.online/api/v2/chat", new JObject(
                new JProperty("channel", ChannelName),
                new JProperty("message", Message)
            ).ToString(), Settings.BotToken);
            
        }
        internal static async void ShoutOut(string UserName) {
            Output($"Sending shoutout for {UserName}");
            //await AltAPI.SendShoutoutAsync(ChannelName, UserName);  // Not working in AltStreamLib currently
            
            await httpRequest.HttpRequest("POST", "https://altstream.online/api/shoutout", new JObject(
                new JProperty("channel", ChannelName),
                new JProperty("target", UserName)
            ).ToString(), Settings.BotToken);
            
        }
        internal static async void CompleteRedeem(string ID, bool Fulfilled=true) {
            int intID;
            if (int.TryParse(ID, out intID)) {
                /*
                JObject Message = new JObject(
                    new JProperty("channel", ChannelName),
                    new JProperty("id", intID)
                 );
                */
                if (Fulfilled) {
                    Output($"Fulfilling redeem {intID}");
                    await AltAPI.SetRewardFulfillmentAsync(ChannelName, intID, Fulfillment.Fulfill);
                    //Message.Add(new JProperty("status", "fulfilled"));
                } else {
                    Output($"Refunding redeem {intID}");
                    await AltAPI.SetRewardFulfillmentAsync(ChannelName, intID, Fulfillment.Refund);
                    //Message.Add(new JProperty("status", "refunded"));
                }
                //Log(Message.ToString());
                //await httpRequest.HttpRequest("PATCH", "https://altstream.online/api/v2/rewards/redemptions", Message.ToString(), Settings.BotToken);
            }
        }
    }
}
