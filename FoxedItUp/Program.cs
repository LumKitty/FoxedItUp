// See https://aka.ms/new-console-template for more information
using AltStreamLib.Logging;
using FoxedItUp;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using static FoxedItUp._Settings;
using static FoxedItUp.Functions;

namespace FoxedItUp {
    static class FoxedItUp {
        static int Main(string[] args) {
            string AppDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string BanListFile = AppDir + "\\BanList.txt";
            string JsonFile = AppDir + "\\Settings.json";
            bool SettingsOK = true;

            SetWindowTitle("init");
            Console.OutputEncoding = Encoding.UTF8;

            SettingsFile.Load(JsonFile);
            SettingsFile.Save(JsonFile);
            
            if (!File.Exists(BanListFile)) {
                Output("");
                Output($"BanList file not found. Creating {BanListFile}");
                File.WriteAllText(BanListFile, @"addcommand
addquote
commands
deletequote
disablecommand
followage
game
games
lastquote
linkaccount
magic8ball
mixitup
mixitupcommands
quote
setgame
settitle
setusertitle
startgiveaway
steamgame
streamerage
subscribeage
title
updatecommand
uptime
xboxgame");
                Output("");
                Output("Please review this file and enter any commands that you do not want available");
                Output("to general users. MixItUp does not give me information on who has permission");
                Output("to use what commands and this method bypasses all permissions. You should ban");
                Output("all !commands that you don't want available to everyone");
                SettingsOK = false;
            }

            if (String.IsNullOrEmpty(Settings.BotToken)) {
                Output("Please create a bot token at https://https://altstream.online/YOUR_CHANNEL/settings?tab=bot");
                SettingsOK = false;
            }

            if (String.IsNullOrEmpty(Settings.ChannelName)) {
                Output($"Please set your channelname in {JsonFile}");
                SettingsOK = false;
            }

            if (!SettingsOK) {
                Output("");
                Output("Press ENTER to exit");
                Console.ReadLine();
                return 1;
            }

            if (Settings.LogLevel >= 3) { Functions.AltStreamLibLogInit(); }

            Output($"Reading banned commands from {BanListFile}");
            Settings.BannedCommands = File.ReadAllLines(BanListFile);
            Output($"Banned commands: {Settings.BannedCommands.Count()}");

            MixItUpClient.UpdateMiuCommands().Wait();
            AltStream.Connect(Settings.ChannelName);
            //AltStream.UpdateModerators(Settings.ChannelName).Wait(); // Can get rid of all this when Follower info is supported!
            HTTPServer.Init(Settings.HttpPort);

            //if (Settings.EnableUnofficialEvents) {
            //    Output($"Unofficial events no-longer supported due to new stream key requirement");
                //AltStream_Unofficial.Connect(Settings.ChannelName);
            //}

            Output("");
            Output($"Call http://localhost:{Settings.HttpPort}/fiu/quit to terminate this application, or press Ctrl + C");
            //Output("");
            //Output("PRE-PRERELEASE: DO NOT DISTRIBUTE");
            Functions.QuitSignal.WaitOne();
            return 0;
        }
    }
}



