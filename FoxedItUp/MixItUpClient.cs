using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using static FoxedItUp.Functions;
using static FoxedItUp.httpRequest;
using static FoxedItUp._Settings;

namespace FoxedItUp {
    internal class MIUCommand {
        internal string ID;
        internal string Name;
        internal bool Enabled;
        internal bool AllowUser;

        internal void RunCommand(string Arguments, string SpoofPlatform, JObject? SpecialIdentifiers, string RealPlatform) {
            JObject Content = new JObject( new JProperty("Platform", SpoofPlatform));
            if (!String.IsNullOrEmpty(Arguments)) { Content.Add(new JProperty("Arguments", Arguments)); }
            if (SpecialIdentifiers == null) { SpecialIdentifiers = new JObject(); }
            SpecialIdentifiers.Add(new JObject("PlatformEx", RealPlatform));
            Content.Add(new JProperty("SpecialIdentifiers", SpecialIdentifiers));
            HttpRequest("POST", $"{Settings.miuURL}/commands/{ID}", Content.ToString());
        }

    }

    internal class MIUChatCommand : MIUCommand {
        internal List<string> TriggerNames = new();
    }
    
    internal static class MixItUpClient {
        
        private static HttpClient client = new HttpClient();
        private static JsonSerializerSettings SerialSettings = new JsonSerializerSettings();

        internal static List<MIUChatCommand> MIUChatCommands = new();
        internal static List<MIUCommand> MIUChannelPoints = new();
        internal static MIUCommand ChannelRaided;
        internal static MIUCommand ChannelFollowed;
        internal static MIUCommand ChannelSubscribed;
        internal static MIUCommand ChannelDonation;


        internal static async Task UpdateMiuCommands() {
            try {
                List<MIUChatCommand> TempMIUChatCommands = new ();
                List<MIUCommand> TempMIUChannelPoints = new ();
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
                            MIUCommand TempCommand = new();
                            TempCommand.Enabled = (bool)Result["IsEnabled"];
                            TempCommand.ID = (string)Result["ID"];
                            TempCommand.Name = ((string)Result["Name"]).ToLower();
                            switch (((string)Result["Type"]).ToLower()) {
                                case "chat":
                                case "pre-made":
                                    MIUChatCommand TempChatCommand = new();
                                    TempChatCommand.Enabled = TempCommand.Enabled;
                                    TempChatCommand.ID = TempCommand.ID;
                                    TempChatCommand.Name = TempCommand.Name;
                                    TempChatCommand.TriggerNames.Add(TempCommand.Name.Replace(" ", ""));
                                    TempMIUChatCommands.Add(TempChatCommand);
                                    Log($"Added chat command: {TempChatCommand.Name}");

                                    break;
                                case "twitch channel points":
                                    if (Settings.ParseChannelPointsFromTwitch) {
                                        TempMIUChannelPoints.Add(TempCommand);
                                        Log($"Added twitch redeem: {TempCommand.Name}");
                                    }
                                    break;
                                case "kick channel points":
                                    if (Settings.ParseChannelPointsFromTwitch) {
                                        TempMIUChannelPoints.Add(TempCommand);
                                        Log($"Added kick redeem: {TempCommand.Name}");
                                    }
                                    break;
                                case "velora channel points":
                                    if (Settings.ParseChannelPointsFromTwitch) {
                                        TempMIUChannelPoints.Add(TempCommand);
                                        Log($"Added velora redeem: {TempCommand.Name}");
                                    }
                                    break;
                                case "vpzone channel points":
                                    if (Settings.ParseChannelPointsFromTwitch) {
                                        TempMIUChannelPoints.Add(TempCommand);
                                        Log($"Added vpzone redeem: {TempCommand.Name}");
                                    }
                                    break;
                                case "event":
                                    switch (((string)Result["Name"]).ToLower()) {
                                        case "channel raided": ChannelRaided = TempCommand; Log("Added channel raided event"); break;
                                        case "channel followed": ChannelFollowed = TempCommand; Log("Added channel followed event"); break;
                                        case "channel subscribed": ChannelSubscribed = TempCommand; Log("Added channel subscribed event"); break;
                                        case "generic donation": ChannelDonation = TempCommand; Log("Added channel donated event"); break;
                                    }
                                    break;
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
            } catch (Exception e) {
                Log(e.ToString());
            }
        }

    }
}
