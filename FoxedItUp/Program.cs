// See https://aka.ms/new-console-template for more information
using FoxedItUp;
using System.Reflection;
using static FoxedItUp.Functions;
using static FoxedItUp._Settings;

string AppDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
string BanListFile = AppDir+ "\\BanList.txt";
string JsonFile = AppDir + "\\Settings.json";

SetWindowTitle("init");

if (!SettingsFile.Load(JsonFile)) {
    SettingsFile.Save(JsonFile);
}

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
}

if (String.IsNullOrEmpty(Settings.ChannelName)) {
    Log("");
    Log($"Please set your channelname in {JsonFile}");
    Log("");
    Log("Press ENTER to exit");
    Console.ReadLine();
    return 1;
}


Log($"Reading banned commands from {BanListFile}");
Settings.BannedCommands = File.ReadAllLines(BanListFile);
Log($"Banned commands: {Settings.BannedCommands.Count()}");

FoxedItUp.MixItUpClient.UpdateMiuCommands();
AltStream.Connect(Settings.ChannelName);

AltStream_Unofficial.Connect(Settings.ChannelName);

System.Threading.Thread.Sleep(5000);
Log("");
Log("Press ENTER to exit");
Console.ReadLine();
return 0;
