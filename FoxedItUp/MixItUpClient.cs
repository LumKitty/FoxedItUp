using AltStreamLib.Api.v1.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Web;
using static FoxedItUp._Settings;
using static FoxedItUp.Functions;
using static FoxedItUp.httpRequest;


namespace FoxedItUp {
    internal class MIUCommandBase {
        internal string ID;
        internal string Name;
        internal bool Enabled;
        internal bool AllowUser;

        protected async Task _RunCommand(string Arguments, string SpoofPlatform, JObject SpecialIdentifiers) {
            JObject Content = new JObject( new JProperty("Platform", SpoofPlatform));
            if (!String.IsNullOrEmpty(Arguments)) { Content.Add(new JProperty("Arguments", Arguments)); }
            if (SpecialIdentifiers != null) {
                Content.Add(new JProperty("SpecialIdentifiers", SpecialIdentifiers));
            }
            await HttpRequest("POST", $"{Settings.miuURL}/commands/{ID}", Content.ToString());
        }

        internal JObject DefaultSpecialIdentifiers (string UserName, string Prefix = "", string RealPlatform = "AltStream") {
            JObject SpecialIdentifiers = null;
            if (RealPlatform == "AltStream") {
                AltStreamUserInfo User = AltStream.GetBasicUserInfo(UserName);
                //TimeSpan FollowAge;
                TimeSpan AccountAge;
                /*if (User.FollowedAt != null) {
                    FollowAge = DateTime.UtcNow - User.FollowedAt;
                } else {
                    FollowAge = new TimeSpan(0);
                }*/
                if (User.CreatedAt != null) {
                    AccountAge = DateTime.UtcNow - User.CreatedAt;
                } else {
                    AccountAge = new TimeSpan(0);
                }

                List<string> Roles = new List<string>();
                //if (User.IsFollower) { Roles.Add("Follower"); }
                if (User.IsSubscriber) { Roles.Add("Subscriber"); }
                if (User.IsModerator) { Roles.Add("Moderator"); }

                SpecialIdentifiers = new JObject(
                    new JProperty($"{Prefix}UserName", UserName),
                    new JProperty($"{Prefix}UserDisplayName", User.UserInfo.Name),
                    new JProperty($"{Prefix}UserColor", User.UserInfo.ChatColor),
                    new JProperty($"{Prefix}User{Settings.SpoofPlatform}Color", User.UserInfo.ChatColor),
                    new JProperty($"{Prefix}UserAvatar", HandleRelativePaths(User.UserInfo.Image)),
                    new JProperty($"{Prefix}UserURL", $"https://altstream.online/{UserName}"),
                    new JProperty($"{Prefix}UserIsSubscriber", User.IsSubscriber.ToString()), //Yes, really
                    //new JProperty($"{Prefix}UserIsFollower", User.IsFollower.ToString()),
                    new JProperty($"{Prefix}UserIsMod", User.IsModerator.ToString()),
                    new JProperty($"{Prefix}UserRoles", String.Join(", ", Roles)),
                    //new JProperty($"{Prefix}UserFollowAge", FollowAge.ToString()),
                    //new JProperty($"{Prefix}UserFollowDays", ((int)Math.Floor(FollowAge.TotalDays)).ToString()),
                    new JProperty($"{Prefix}UserAccountAge", AccountAge.ToString()),
                    new JProperty($"{Prefix}UserAccountAgeDays", ((int)Math.Floor(AccountAge.TotalDays)).ToString()),
                    new JProperty($"{Prefix}PlatformEx", RealPlatform),
                    new JProperty($"{Prefix}UserAlejoPronouns", User.UserInfo.Pronouns)
                );

                /* Special identifiers not supported:
                 * UserID           - MIU-specific, can't guarantee it even exists!
                 * UserDisplayRoles - Beyond my scope to translate into every language
                 * UserTitle        - MIU-specific. Could do in future
                 * UserTime         - MIU-specific. Not tracking altstream presence so can't really do
                 * UserHours
                 * UserMinutes
                 * UserLastSeenAge  - Not tracking users between sessions
                 * UserLastSeenPrecise
                 * UserFollowAge    - Broken in upstream API
                 * UserFollowDays
                 * UserFollowMonths - Possible (once age is fixed) but pain in the arse to implement. Maybe in future
                 * UserFollowYears
                 * UserSubAge       - We don't have this info in AltStream
                 * UserSubMonths
                 * UserSubTier      - I don't think this exists on AltStream?
                 * UserSubBadge     - Maybe possible need to check API
                 * UserInChat       - Not tracking users
                 * UserIsFollower   - Broken in upstream API
                 * UserIsRegular    - Not tracking users
                 * UserIsVIP        - I don't think this exists either
                 * UserIsSpecialityExcluded - MIU-specific
                 * UserBitsLifeTimeAmount - Not tracking users
                 * UserTotalStreamsWatched
                 * UserTotalAmountDonated
                 * UserTotalSubsGifted
                 * UserTotalSubsReceived
                 * UserTotalChatMessagesSent
                 * UserTotalTimesTagged
                 * UserTotalCommandsRun
                 * UserTotalMonthsSubbed
                 * UserModerationStrikes
                 * UserGameQueuePosition
                 * UserPatreonTier
                 * UserNotes
                 * 
                 * All TargetUser identifiers, until I figure out how I want to handle this
                */

                if (UserName.ToLower() == User.UserInfo.Name.ToLower()) {
                    SpecialIdentifiers.Add(new JProperty("UserFullDisplayName", User.UserInfo.Name));
                } else {
                    SpecialIdentifiers.Add(new JProperty("UserFullDisplayName", $"{User.UserInfo.Name} ({UserName})"));
                }
                if (User.IsModerator) {
                    SpecialIdentifiers.Add(new JProperty("UserPrimaryRole", "Moderator"));
                } else if (User.IsSubscriber) {
                    SpecialIdentifiers.Add(new JProperty("UserPrimaryRole", "Subscriber"));
                //} else if (User.IsFollower) {
                //    SpecialIdentifiers.Add(new JProperty("UserPrimaryRole", "Follower"));
                }

                if (User.ChannelInfo != null && User.ChannelInfo.Stream != null) {
                    SpecialIdentifiers.Add(new JProperty("UserStreamTitle", User.ChannelInfo.Stream.Title));
                    SpecialIdentifiers.Add(new JProperty("UserStreamIsLive", User.ChannelInfo.Stream.IsLive.ToString()));
                    if (User.ChannelInfo.Stream.Game != null) {
                        SpecialIdentifiers.Add(new JProperty("UserStreamGame", User.ChannelInfo.Stream.Game.Name));
                        SpecialIdentifiers.Add(new JProperty("UserStreamGameName", User.ChannelInfo.Stream.Game.Name));
                        SpecialIdentifiers.Add(new JProperty("UserStreamGameImage", User.ChannelInfo.Stream.Game.CoverUrl));
                    }
                }
            }

            return SpecialIdentifiers;
        }
    }

    internal class MIUCommand : MIUCommandBase {
        internal virtual async Task RunCommand(string Arguments, string SpoofPlatform, string UserName) {
            JObject SpecialIdentifiers = DefaultSpecialIdentifiers(UserName);
            await _RunCommand(Arguments, SpoofPlatform, SpecialIdentifiers);
        }
        internal MIUCommand(MIUCommandBase Base) { ID = Base.ID; Name = Base.Name; Enabled = Base.Enabled; AllowUser = Base.AllowUser; }
    }

    internal class MIUChatCommand : MIUCommandBase {
        internal List<string> TriggerNames = new List<string>();
        internal async Task RunCommand(string Arguments, string SpoofPlatform, string UserName) {
            JObject SpecialIdentifiers = DefaultSpecialIdentifiers(UserName);
            await _RunCommand(Arguments, SpoofPlatform, SpecialIdentifiers);
        }
        internal MIUChatCommand(MIUCommandBase Base) { ID = Base.ID; Name = Base.Name; Enabled = Base.Enabled; AllowUser = Base.AllowUser; }
    }

    internal class MIURaidCommand : MIUCommandBase {
        internal async Task RunCommand(string Arguments, string SpoofPlatform, string UserName, int RaidSize) {
            JObject SpecialIdentifiers = DefaultSpecialIdentifiers(UserName);
            SpecialIdentifiers.Add(
                new JProperty("RaidViewerCount", RaidSize.ToString()) // Yes, really! It fails if this is an int)
            ); 
            await _RunCommand(Arguments, SpoofPlatform, SpecialIdentifiers);
        }
        internal MIURaidCommand(MIUCommandBase Base) { ID = Base.ID; Name = Base.Name; Enabled = Base.Enabled; AllowUser = Base.AllowUser; }
    }

    internal class MIURedeemCommand : MIUCommandBase {
        internal async Task RunCommand(string RedeemMessage, string SpoofPlatform, string UserName) {
            JObject SpecialIdentifiers = DefaultSpecialIdentifiers(UserName);
            SpecialIdentifiers.Add(new JProperty("Message", RedeemMessage));
            await _RunCommand("", SpoofPlatform, SpecialIdentifiers);
        }
        internal MIURedeemCommand(MIUCommandBase Base) { ID = Base.ID; Name = Base.Name; Enabled = Base.Enabled; AllowUser = Base.AllowUser; }
    }

    internal class MIUSubscriptionCommand : MIUCommandBase {
        internal async Task RunCommand(string SubscriptionMessage, string SpoofPlatform, string UserName, string Tier) {
            JObject SpecialIdentifiers = DefaultSpecialIdentifiers(UserName);
            SpecialIdentifiers.Add(new JProperty("Message", SubscriptionMessage));
            SpecialIdentifiers.Add(new JProperty("UserSubPlan", Tier));
            await _RunCommand("", SpoofPlatform, SpecialIdentifiers);
        }
        internal MIUSubscriptionCommand(MIUCommandBase Base) { ID = Base.ID; Name = Base.Name; Enabled = Base.Enabled; AllowUser = Base.AllowUser; }
    }

    internal class MIUDonationCommand : MIUCommandBase {
        internal async Task RunCommand(string DonationMessage, string SpoofPlatform, string UserName, string Currency, string Amount) {
            JObject SpecialIdentifiers = DefaultSpecialIdentifiers(UserName);
            SpecialIdentifiers.Add(new JProperty("DonationMessage", DonationMessage));
            SpecialIdentifiers.Add(new JProperty("DonationCurrency", Currency));
            SpecialIdentifiers.Add(new JProperty("DonationAmount", Currency + Amount));
            SpecialIdentifiers.Add(new JProperty("DonationAmountNumber", Amount));
            SpecialIdentifiers.Add(new JProperty("DonationAmountNumberDigits", new string(Amount.Where(c => char.IsDigit(c)).ToArray())));
            await _RunCommand("", SpoofPlatform, SpecialIdentifiers);
        }
        internal MIUDonationCommand(MIUCommandBase Base) { ID = Base.ID; Name = Base.Name; Enabled = Base.Enabled; AllowUser = Base.AllowUser; }
    }
    
    internal static class MixItUpClient {
        
        private static HttpClient client = new HttpClient();
        private static JsonSerializerSettings SerialSettings = new JsonSerializerSettings();

        internal static List<MIUChatCommand> MIUChatCommands = new List<MIUChatCommand>();
        internal static List<MIURedeemCommand> MIUChannelPoints = new List<MIURedeemCommand>();
        internal static MIURaidCommand ChannelRaided = null;
        internal static MIUCommand ChannelFollowed = null;
        internal static MIUSubscriptionCommand ChannelSubscribed = null;
        internal static MIUSubscriptionCommand ChannelResubscribed = null;
        internal static MIUDonationCommand ChannelDonation = null;

        internal static async Task UpdateMiuCommands() {
            try {
                List<MIUChatCommand> TempMIUChatCommands = new List<MIUChatCommand>();
                List<MIURedeemCommand> TempMIUChannelPoints = new List<MIURedeemCommand>();
                int skip = 0;
                int count = 0;
                do {
                    var GetResult = await client.GetAsync($"{Settings.miuURL}/commands?pagesize=10&skip=" + skip.ToString());
                    string Response = GetResult.Content.ReadAsStringAsync().Result;

                    JObject Results = JsonConvert.DeserializeObject<JObject>(Response);
                    //Log(Results.ToString());
                    count = ((JArray)Results["Commands"]).Count;
                    // Console.WriteLine("Count: " + count.ToString());
                    foreach (JObject Result in (JArray)Results["Commands"]) {
                        //Log(Result.ToString());
                        if (!Settings.BannedCommands.Contains(((string)Result["Name"]).ToLower(), StringComparer.OrdinalIgnoreCase) &&
                          !Settings.BannedCommands.Contains(((string)Result["Name"]).ToLower().Replace(" ",""), StringComparer.OrdinalIgnoreCase)) {
                            MIUCommandBase TempCommand = new MIUCommandBase();
                            TempCommand.Enabled = (bool)Result["IsEnabled"];
                            TempCommand.ID = (string)Result["ID"];
                            TempCommand.Name = ((string)Result["Name"]).ToLower();
                            if (TempCommand.Enabled) {
                                switch (((string)Result["Type"]).ToLower()) {
                                    case "chat":
                                    case "pre-made":
                                        MIUChatCommand TempChatCommand = new MIUChatCommand(TempCommand);
                                        TempChatCommand.TriggerNames.Add(TempCommand.Name.Replace(" ", ""));
                                        TempMIUChatCommands.Add(TempChatCommand);
                                        Log($"Added chat command: {TempChatCommand.Name} (as !{string.Join(" !", TempChatCommand.TriggerNames)})");

                                        break;
                                    case "twitch channel points":
                                        if (Settings.ParseChannelPointsFromTwitch) {
                                            TempMIUChannelPoints.Add(new MIURedeemCommand(TempCommand));
                                            Log($"Added twitch redeem: {TempCommand.Name}");
                                        }
                                        break;
                                    case "kick channel points":
                                        if (Settings.ParseChannelPointsFromKick) {
                                            TempMIUChannelPoints.Add(new MIURedeemCommand(TempCommand));
                                            Log($"Added kick redeem: {TempCommand.Name}");
                                        }
                                        break;
                                    case "velora channel points":
                                        if (Settings.ParseChannelPointsFromVelora) {
                                            TempMIUChannelPoints.Add(new MIURedeemCommand(TempCommand));
                                            Log($"Added velora redeem: {TempCommand.Name}");
                                        }
                                        break;
                                    case "vpzone channel points":
                                        if (Settings.ParseChannelPointsFromVPZone) {
                                            TempMIUChannelPoints.Add(new MIURedeemCommand(TempCommand));
                                            Log($"Added vpzone redeem: {TempCommand.Name}");
                                        }
                                        break;
                                    case "event":
                                        switch (((string)Result["Name"]).ToLower()) {
                                            case "channel raided":       ChannelRaided       = new MIURaidCommand(TempCommand); Log("Added channel raided event"); break;
                                            case "channel followed":     ChannelFollowed     = new MIUCommand(TempCommand); Log("Added channel followed event"); break;
                                            case "channel subscribed":   ChannelSubscribed   = new MIUSubscriptionCommand(TempCommand); Log("Added channel subscribed event"); break;
                                            case "channel resubscribed": ChannelResubscribed = new MIUSubscriptionCommand(TempCommand); Log("Added channel subscribed event"); break;
                                            case "generic donation":     ChannelDonation     = new MIUDonationCommand(TempCommand); Log("Added channel donated event"); break;
                                        }
                                        break;
                                }
                            } else {
                                Log($"Skipping disabled command: {TempCommand.Name}");
                            }
                        } else {
                            Log($"Skipping banned command: {Result["Name"]}");
                        }
                    }
                    GetResult.Dispose();
                    skip += 10;
                } while (count >= 10);
                if (TempMIUChatCommands.Count > 0) { MIUChatCommands = TempMIUChatCommands; }
                if (TempMIUChannelPoints.Count > 0) { MIUChannelPoints = TempMIUChannelPoints; }
                if (ChannelRaided == null) { Log("Warning: We did not find a \"Generic Channel Raided\" event. \r\nIf you used \"Twitch Channel Raided\" please copy it as a generic"); }
                if (ChannelFollowed == null) { Log("Warning: We did not find a \"Generic Channel Followed\" event. \r\nIf you used \"Twitch Channel Followed\" please copy it as a generic"); }
                if (ChannelSubscribed == null) { Log("Warning: We did not find a \"Generic Channel Subscribed\" event. \r\nIf you used \"Twitch Channel Subscribed\" please copy it as a generic"); }
                if (ChannelResubscribed == null) { Log("Warning: We did not find a \"Generic Channel Resubscribed\" event. \r\nIf you used \"Twitch Channel Resubscribed\" please copy it as a generic"); }
            } catch (Exception e) {
                Log(e.ToString());
            }
        }

    }
}
