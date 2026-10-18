// See https://aka.ms/new-console-template for more information
using System.Reflection;
using static FoxedItUp.Functions;

Console.WriteLine("Hello, World!");

String BanListFile = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\BanList.txt";
Log($"Reading banned commands from {BanListFile}");
FoxedItUp.Settings.BannedCommands = File.ReadAllLines(BanListFile);
Log($"Banned commands: {FoxedItUp.Settings.BannedCommands.Count()}");

FoxedItUp.FoxedItUp AltConnection = new FoxedItUp.FoxedItUp();
FoxedItUp.MixItUpClient.UpdateMiuCommands();
AltConnection.Connect("LumKitty");
Log("Press ENTER to exit");
Console.ReadLine();