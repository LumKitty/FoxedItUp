using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using static FoxedItUp.Functions;

namespace FoxedItUp {
    public class __Settings {
        public string ChannelName = "";
        public string miuURL = "http://localhost:8911/api/v2";
        public bool ParseChannelPointsFromTwitch = true;
        public bool ParseChannelPointsFromKick = false;
        public bool ParseChannelPointsFromVelora = false;
        public bool ParseChannelPointsFromVPZone = false;
        public string SpoofPlatform = "Twitch";
        public bool ShowAltStreamLibMessages = false;
        public bool ShowAltStreamLibPingMessages = false;
        public bool ShowAltStreamChatMessages = true;
        public bool ShowAltStreamChatCommands = true;
        public bool ShowAltStreamRedeems = true;
        public bool ShowAltStreamEvents = true;
        [JsonIgnore] public string[] BannedCommands = new string[0];
    }
    
    internal static class _Settings {
        internal static __Settings Settings = new __Settings();
    }

    internal static class SettingsFile {
        internal static bool Load(string FileName) {
            if (File.Exists(FileName)) {
                Log($"Loading {FileName}");
                __Settings TempSettings = JsonConvert.DeserializeObject<__Settings>(File.ReadAllText(FileName));
                if (TempSettings != null) {
                    _Settings.Settings = TempSettings;
                    return true;
                } else {
                    Log($"Invalid settings file: {FileName}");
                    return false;
                }
            } else {
                Log($"Could not find settings file {FileName}");
                return false;
            }
        }
        internal static void Save(string FileName) {
            Log($"Saving to {FileName}");
            File.WriteAllText(FileName, JsonConvert.SerializeObject(_Settings.Settings, Formatting.Indented));
        }
    }
}
