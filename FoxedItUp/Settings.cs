using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using static FoxedItUp.Functions;

namespace FoxedItUp {
    public class __Theme {
        public Color Chat = Color.White;
        public Color Raid = Color.Red;
        public Color Follow = Color.Yellow;
        public Color Subscribe = Color.Green;
        public Color Redeem = Color.Magenta;
        public Color System = Color.Gray;
        public Color Pronouns = Color.Orange;
        public Color Background = Color.Black;
        public Color HighlightBG = Color.DarkViolet;
        public Color SystemMsgBG = Color.DarkRed;
    }
    
    public class __Settings {
        internal readonly string LogFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FoxedItUp.log");
        public string ChannelName = "";
        public string miuURL = "http://localhost:8911/api/v2";
        public string BotToken = null;
        public string EncryptedBotToken = null;
        public bool ShowChannelName = false;
        public bool ShowPronouns = true;
        public int HttpPort = 41757;
        public bool DisableColor = false;
        public string SpoofPlatform = "Twitch";
        public string ReadCommandPermissionsFrom = "Twitch";
        public bool IncludeAccountAgeAndGameInfo = false;
        public bool ParseChannelPointsFromTwitch = true;
        public bool ParseChannelPointsFromKick = false;
        public bool ParseChannelPointsFromVelora = false;
        public bool ParseChannelPointsFromVPZone = false;
        public string LocalNameForChat = "chat";
        public string LocalNameForPreMade = "pre-made";
        public string LocalNameForEvent = "event";
        public string LocalNameForTimer = "timer";
        public string LocalNameForTwitchChannelPoints = "twitch channel points";
        public string LocalNameForKickChannelPoints = "kick channel points";
        public string LocalNameForVeloraChannelPoints = "velora channel points";
        public string LocalNameForVPZoneChannelPoints = "vpzone channel points";

        public int LogLevel = 1;

        /*
        public bool ShowAltStreamLibMessages = false;
        public bool ShowAltStreamLibPingMessages = false;
        public bool ShowAltStreamChatMessages = true;
        public bool ShowAltStreamChatCommands = true;
        public bool ShowAltStreamRedeems = true;
        public bool ShowAltStreamEvents = true;
        public bool ShowRandomJSON = false;
        public bool ShowMIURawCommands = false;
        */
        
        [JsonIgnore] public string[] BannedCommands = new string[0];
    }
    
    internal static class _Settings {
        internal static __Settings Settings = new __Settings();
        internal static __Theme Theme = new __Theme();
    }

    internal static class SettingsFile {
        internal static bool Load(string FileName) {
            if (File.Exists(FileName)) {
                Log($"Loading {FileName}",1);
                __Settings TempSettings = JsonConvert.DeserializeObject<__Settings>(File.ReadAllText(FileName));
                if (TempSettings != null) {
                    _Settings.Settings = TempSettings;
                    return true;
                } else {
                    Log($"Invalid settings file: {FileName} - using defaults",1);
                    return false;
                }
            } else {
                Log($"Could not find settings file {FileName} - using defaults",1);
                return false;
            }
        }
        internal static void Save(string FileName) {
            Log($"Saving to {FileName}",1);
            File.WriteAllText(FileName, JsonConvert.SerializeObject(_Settings.Settings, Formatting.Indented));
        }
    }
    internal static class ThemeFile {
        internal static bool Load(string FileName) {
            if (File.Exists(FileName)) {
                Log($"Loading {FileName}",1);
                __Theme TempSettings = JsonConvert.DeserializeObject<__Theme>(File.ReadAllText(FileName));
                if (TempSettings != null) {
                    _Settings.Theme = TempSettings;
                    return true;
                } else {
                    Output($"Invalid theme file: {FileName} - using defaults");
                    return false;
                }
            } else {
                Output($"Could not find theme file {FileName} - using defaults");
                return false;
            }
        }
        internal static void Save(string FileName) {
            Log($"Saving to {FileName}",1);
            File.WriteAllText(FileName, JsonConvert.SerializeObject(_Settings.Theme, Formatting.Indented));
        }
    }

}
