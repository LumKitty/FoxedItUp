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
using System.Drawing;
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

                Log($"Connecting as '{Settings.ChannelName}'", 69);
                AltEventClient = new EventSubClient(Settings.ChannelName, GetBotToken());
                System.GC.Collect();
                AltEventClient.OnMessageReceived += AltEventClient_OnMessageReceived;  //
                AltEventClient.OnHighlightReceived += AltEventClient_OnHighlightReceived;  //
                AltEventClient.OnSystemMessageReceived += AltEventClient_OnSystemMessageReceived;  //

                AltEventClient.OnRaidReceived += AltEventClient_OnRaidReceived;  //
                AltEventClient.OnRedemptionReceived += AltEventClient_OnRedemptionReceived;  //
                AltEventClient.OnFollowReceived += AltEventClient_OnFollowReceived;  //
                AltEventClient.OnSubscriptionReceived += AltEventClient_OnSubscriptionReceived;  //
                AltEventClient.OnTipReceived += AltEventClient_OnTipReceived;  //

                AltEventClient.OnUnhandledReceived += AltEventClient_OnUnhandledReceived;

                AltEventClient.OnConnected += AltEventClient_OnConnected;
                AltEventClient.OnTimeout += AltEventClient_OnTimeout;
                AltEventClient.OnDisconnected += AltEventClient_OnDisconnected;

                Output("Connecting to AltStream");
                AltEventClient.ConnectAsync().Wait();

                AltAPI = new AltStreamLib.Api.ApiClient(GetBotToken());
                System.GC.Collect();

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

            if (User.UserInfo == null) {
                Log($"Could not get userinfo for {UserName}",1);
                return null;
            }

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
                Author TempAuthor = obj.Author;
                FillIncompleteAuthor(ref TempAuthor);
                obj.Author = TempAuthor;
                if (Settings.LogLevel >= 6) {
                    Log(Newtonsoft.Json.JsonConvert.SerializeObject(obj), 6);
                }
                Kofi DonationEvent = obj.Kofi;
                Output("$", obj.Author, "Donated {DonationEvent.CurrencyCode}{DonationEvent.Amount} - {DonationEvent.Message}", Theme.Subscribe);
           
                if (MixItUpClient.ChannelDonation != null) {
                    await MixItUpClient.ChannelDonation.RunCommand(DonationEvent.Message, Settings.SpoofPlatform, obj.Author, DonationEvent.CurrencyCode, DonationEvent.Amount);
                }
            } catch (Exception Ex) {
                ErrorHandler(Ex);
            }
        }

        private static async void AltEventClient_OnSubscriptionReceived(Data obj) {
            try {
                if (Settings.LogLevel >= 6) {
                    Log(Newtonsoft.Json.JsonConvert.SerializeObject(obj), 6);
                }
                Author TempAuthor = obj.Author;
                FillIncompleteAuthor(ref TempAuthor);
                obj.Author = TempAuthor;

                Kofi SubEvent = obj.Kofi;
                Output("$", obj.Author, $"subscribed at {SubEvent.TierName}", Theme.Subscribe);

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
                Author TempAuthor = obj.FollowedBy;
                FillIncompleteAuthor(ref TempAuthor);
                obj.FollowedBy = TempAuthor;

                Output("*", obj.Author, "followed", Theme.Follow);

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
                    Log(Newtonsoft.Json.JsonConvert.SerializeObject(obj),6);
                }
                AltStreamLib.EventSub.Chat.Models.Redemption RedeemEvent = obj.Redemption;

                Author TempAuthor = RedeemEvent.From;
                FillIncompleteAuthor(ref TempAuthor);
                RedeemEvent.From = TempAuthor;

                Output("*", RedeemEvent.From, $"redeemed \"{RedeemEvent.RewardName}\" for {RedeemEvent.Cost} {RedeemEvent.PointsName}", Theme.Redeem);

                string PointsName = RedeemEvent.RewardName.ToLower();
                Log($"Attempting to run command {PointsName}",1);
                MIURedeemCommand PointsCommand = MixItUpClient.MIUChannelPoints.FirstOrDefault(i => (i.Name == PointsName));
                if (PointsCommand != null) {
                    Log($"Attempting to run command: {PointsCommand.ID}",1);
                    await PointsCommand.RunCommand(RedeemEvent.UserInput, Settings.SpoofPlatform, RedeemEvent.From, RedeemEvent.Id);
                } else {
                    Log("Could not find command",1);
                }
            } catch (Exception Ex) {
                ErrorHandler(Ex);
            }
        }
        

        private static async void AltEventClient_OnRaidReceived(Data obj) {
            try {
                if (Settings.LogLevel >= 6) {
                    Log(Newtonsoft.Json.JsonConvert.SerializeObject(obj), 6);
                }
                Raid RaidEvent = obj.Raid;
                Author TempAuthor = obj.Raid.From;
                FillIncompleteAuthor(ref TempAuthor);
                obj.Raid.From = TempAuthor;

                Output("*", obj.Author, $"raided with {RaidEvent.Viewers}", Theme.Raid);
                if (MixItUpClient.ChannelRaided != null) {
                    await MixItUpClient.ChannelRaided.RunCommand("", Settings.SpoofPlatform, RaidEvent.From, RaidEvent.Viewers);
                }
            } catch (Exception Ex) {
                ErrorHandler(Ex);
            }
        }
        
        private static async Task AltMessageReceived(Data Context, Color BGCol) {
            try {
                string Command = "";
                string Args = "";

                if (!string.IsNullOrEmpty(Context.Message.Text)) {
                    if (Context.Message.Text.Substring(0, 1) == "!") {
                        Log($"Received command message {Context.Message.Text}",1);
                        int n = Context.Message.Text.IndexOf(' ');
                        switch (n) {
                            case 1:
                            case 0:
                                Log($"Invalid message {Context.Message.Text}",1); break;
                            case -1:
                                Command = Context.Message.Text.Substring(1);
                                Output("*", Context.Author, $"used command '{Command}'", Theme.Chat, BGCol);
                                break;
                            default:
                                Command = Context.Message.Text.Substring(1, n - 1);
                                Args = Context.Message.Text.Substring(n + 1);
                                Output("*", Context.Author, $"used command '{Command}' with args: {Args}", Theme.Chat, BGCol);
                                break;
                        }

                        if (!String.IsNullOrEmpty(Command)) {
                            Log($"Attempting to run command {Command}",1);
                            MIUChatCommand ChatCommand = MixItUpClient.MIUChatCommands.FirstOrDefault(i => i.TriggerNames.Contains(Command));
                            if (ChatCommand != null) {
                                //Log($"Attempting to run command: {ChatCommand.ID}");
                                await ChatCommand.RunCommand(Args, Settings.SpoofPlatform, Context.Author);
                            } else {
                                Log("Could not find command",1);
                            }
                        }
                    } else {
                        //Output($"<{Context.Author.ChannelName}> ({Context.Author.DisplayName}) {Context.Message.Text}");
                        Output(">", Context.Author, Context.Message.Text, Theme.Chat, BGCol);
                    }
                } else if (Context.Message.Sticker != null) {
                    Output(">", Context.Author, $"sent a sticker: {Context.Message.Sticker.Name}", Theme.Chat, BGCol);
                    //Output($"<{Context.Author.ChannelName}> ({Context.Author.DisplayName}) sent a sticker: {Context.Message.Sticker.Name}");
                } else {
                    Output($"Received empty message: {Context.ToString()}");
                }
            } catch (Exception Ex) {
                ErrorHandler(Ex);
            }
        }
        
        private static async void AltEventClient_OnHighlightReceived(Data obj) {
            if (Settings.LogLevel >= 6) {
                Log(Newtonsoft.Json.JsonConvert.SerializeObject(obj), 6);
            }
            await AltMessageReceived(obj,Theme.HighlightBG);
        }
        private static async void AltEventClient_OnMessageReceived(Data obj) {
            if (Settings.LogLevel >= 6) {
                Log(Newtonsoft.Json.JsonConvert.SerializeObject(obj),6);
            }
            await AltMessageReceived(obj,Theme.Background);
        }

        private static async void AltEventClient_OnSystemMessageReceived(Data obj) {
            if (Settings.LogLevel >= 6) {
                Log(Newtonsoft.Json.JsonConvert.SerializeObject(obj), 6);
            }
            await AltMessageReceived(obj, Theme.SystemMsgBG);
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
            Log($"Sending chat message: {Message}",1);
            //await AltAPI.SendChatMessageAsync(ChannelName, Message);  // Not working in AltStreamLib currently
            
            
            await httpRequest.HttpRequest("POST", "https://altstream.online/api/v2/chat", new JObject(
                new JProperty("channel", ChannelName),
                new JProperty("message", Message)
            ).ToString(), GetBotToken());
                        
        }
        internal static async void ShoutOut(string UserName) {
            Output($"* Sending shoutout for {UserName}");
            await AltAPI.SendShoutoutAsync(ChannelName, UserName);  // Not working in AltStreamLib currently
            
            /*
            await httpRequest.HttpRequest("POST", "https://altstream.online/api/shoutout", new JObject(
                new JProperty("channel", ChannelName),
                new JProperty("target", UserName)
            ).ToString(), GetBotToken());
            */
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
