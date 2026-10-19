// See https://aka.ms/new-console-template for more information
using FoxedItUp;
using System.Reflection;
using static FoxedItUp.Functions;
using static FoxedItUp._Settings;

string AppDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
string BanListFile = AppDir+ "\\BanList.txt";
string JsonFile = AppDir + "\\Settings.json";
if (!SettingsFile.Load(JsonFile)) {
    SettingsFile.Save(JsonFile);
}

if (String.IsNullOrEmpty(Settings.ChannelName)) {
    Log($"Please set your channelname in {JsonFile}");
    return 1;
}

Log($"Reading banned commands from {BanListFile}");
Settings.BannedCommands = File.ReadAllLines(BanListFile);
Log($"Banned commands: {Settings.BannedCommands.Count()}");

FoxedItUp.FoxedItUp AltConnection = new FoxedItUp.FoxedItUp();
FoxedItUp.MixItUpClient.UpdateMiuCommands();
AltConnection.Connect("LumKitty");

System.Threading.Thread.Sleep(5000);
Log("Press ENTER to exit");
Console.ReadLine();
return 0;
