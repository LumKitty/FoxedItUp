using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoxedItUp {
    internal static class Settings {
        internal static string ChannelName = "LumKitty";
        internal static string miuURL = "http://localhost:8911/api/v2";
        internal static bool ParseChannelPointsFromTwitch = true;
        internal static bool ParseChannelPointsFromKick = false;
        internal static bool ParseChannelPointsFromVelora = false;
        internal static bool ParseChannelPointsFromVPZone = false;
        internal static string SpoofPlatform = "Twitch";
        internal static string[] BannedCommands = [];
    }
}
