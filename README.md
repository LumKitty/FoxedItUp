## Partial MixItUp support for AltStream.online

This is early alpha software. You are your own, good luck.  
How it works is we run a separate app that connects to AltStream as if it were a streamer bot and then relays those commands to MixItUp via the developer API

### Instructions
1. Enable the MixItUp developer API: Services -> Developer API -> Connect
2. Run FoxedItUp.exe
3. It will create a new Settings.json and BanList.txt file
4. Edit Settings.json and add your AltStream channelname
5. Review the !command banlist file (see below)
6. Run FoxedItUp.exe again

The app should now connect to both AltStream and MixItUp. Any !commands received in chat will be relayed to MixItUp if the command exists there.

Note: Only the main name will be used, as MixItUp doesn't let me see any of the alternate names. For example if your command is called "ShoutOut"
and the options are !shoutout and !so, only !shoutout will work. If there are any spaces in your command name, these will be removed (e.g.
Content Warning becomes !contentwarning)

Any channel point redeems must have the exact same name on AltStream as they do on Twitch

Channel Raided alerts *might* work, but untested.  
Follow / Sub / Tips don't work as they are not yet officially available in the API.

### Credits
Uses [AltStreamLib](https://github.com/MachaCeleste/AltStreamLib) by [MachaCeleste](https://altstream.online/machaceleste)  
Give me a follow on [AltStream](https://altstream.online/LumKitty) or [Twitch](https://twitch.tv/LumKitty)
