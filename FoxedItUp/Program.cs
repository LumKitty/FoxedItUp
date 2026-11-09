// See https://aka.ms/new-console-template for more information
using FoxedItUp;
using System;
using System.IO;
using System.Reflection;
using static FoxedItUp.Functions;
using static FoxedItUp._Settings;
using System.Linq;
using AltStreamLib.Logging;

namespace FoxedItUp {
    static class FoxedItUp {
        static int Main(string[] args) {
            string AppDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string BanListFile = AppDir + "\\BanList.txt";
            string JsonFile = AppDir + "\\Settings.json";
            bool SettingsOK = true;

            SetWindowTitle("init");

            SettingsFile.Load(JsonFile);
            SettingsFile.Save(JsonFile);
            
            if (!File.Exists(BanListFile)) {
                Log("");
                Log($"BanList file not found. Creating {BanListFile}");
                File.WriteAllText(BanListFile, @"addcommand
deletequote
disablecommand
setgame
settitle
setusertitle
startgiveaway
updatecommand");
                Log("");
                Log("Please review this file and enter any commands that you do not want available");
                Log("to general users. MixItUp does not give me information on who has permission");
                Log("to use what commands and this method bypasses all permissions. You should ban");
                Log("all !commands that you don't want available to everyone");
                SettingsOK = false;
            }

            if (String.IsNullOrEmpty(Settings.BotToken)) {
                Log("Please create a bot token at https://https://altstream.online/YOUR_CHANNEL/settings?tab=bot");
                SettingsOK = false;
            }

            if (String.IsNullOrEmpty(Settings.ChannelName)) {
                Log($"Please set your channelname in {JsonFile}");
                SettingsOK = false;
            }

            if (!SettingsOK) {
                Log("");
                Log("Press ENTER to exit");
                Console.ReadLine();
                return 1;
            }

            if (Settings.ShowAltStreamLibMessages) { Functions.AltStreamLibLogInit(); }

            Log($"Reading banned commands from {BanListFile}");
            Settings.BannedCommands = File.ReadAllLines(BanListFile);
            Log($"Banned commands: {Settings.BannedCommands.Count()}");

            MixItUpClient.UpdateMiuCommands().Wait();
            AltStream.Connect(Settings.ChannelName);
            AltStream.UpdateModerators(Settings.ChannelName).Wait(); // Can get rid of all this when Follower info is supported!
            HTTPServer.Init(Settings.HttpPort);

            if (Settings.EnableUnofficialEvents) {
                Log($"Unofficial events enabled. Polling for Follows");
                AltStream_Unofficial.Connect(Settings.ChannelName);
            }

            Log("");
            Log("Press ENTER to exit");
            Console.ReadLine();
            return 0;
        }
    }
}



