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
using System.Drawing;
using static FoxedItUp._Settings;
using static FoxedItUp.Functions;

namespace FoxedItUp {
    static class FoxedItUp {
        static int Main(string[] args) {

            File.WriteAllText(Settings.LogFile, $"FoxedItUp v{Assembly.GetExecutingAssembly().GetName().Version} starting\n");
            string AppDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string BanListFile = AppDir + "\\BanList.txt";
            string SettingsFileName = AppDir + "\\Settings.json";
            string ThemeFileName = AppDir + "\\Theme.json";
            bool SettingsOK = true;

            SetWindowTitle("init");
            Console.OutputEncoding = Encoding.UTF8;

            SettingsFile.Load(SettingsFileName);
            ThemeFile.Load(ThemeFileName);
            ThemeFile.Save(ThemeFileName);

            if (Settings.DisableColor) {
                Pastel.ConsoleExtensions.Disable();
            }

            if (String.IsNullOrEmpty(Settings.ChannelName)) {
                Console.Write("Please enter your channel name: ");
                Settings.ChannelName = Console.ReadLine();
            }

            if (String.IsNullOrEmpty(Settings.EncryptedBotToken)) {
                if (Settings.BotToken == null) {
                    Output("Please create a bot token at https://https://altstream.online/YOUR_CHANNEL/settings?tab=bot");
                    Output("Do not enter your token on-screen while streaming!", Color.White, Color.Red);
                    Console.Write("Bot token: ");
                    string TempToken = Console.ReadLine();
                    Settings.EncryptedBotToken = Encrypt(TempToken);
                    TempToken = null;
                    System.GC.Collect();
                } else {
                    Output("Found unencrypted bot token in settings... Encrypting");
                    Settings.EncryptedBotToken = Encrypt(Settings.BotToken);
                    Settings.BotToken = null;
                    System.GC.Collect();
                }
            }
            Log($"Encrypted token: {Settings.EncryptedBotToken}", 1);

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
                Output("to general users.");
                Output("MixItUp commands that use user inventories (such as daily checkin commands)");
                Output("Cannot work with AltStream until we get native support in MixItUp. Therefore");
                Output("you should add these to the banlist and ensure that it worked!");
                Output("MixItUp pre-made commands may be named differently if your language is not");
                Output("English, so you may need to adjust the default banlist");
                Output("");
                Console.Write("Press enter to continue");
                Console.ReadLine();
            }

            SettingsFile.Save(SettingsFileName);

            if (!SettingsOK) {
                Output("");
                Output("Press ENTER to exit");
                Console.ReadLine();
                return 1;
            }

            if (Settings.LogLevel >= 3) { Functions.AltStreamLibLogInit(); }

            Log($"Reading banned commands from {BanListFile}",1);
            Settings.BannedCommands = File.ReadAllLines(BanListFile);
            Output($"Found {Settings.BannedCommands.Count()} banned commands");

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



