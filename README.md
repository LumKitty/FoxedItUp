## Partial MixItUp support for AltStream.online

This is early alpha software. You are your own, good luck

### Instructions
1. Run FoxedItUp.exe
2. It will create a new Settings.json and BanList.txt file
3. Edit Settings.json and add your AltStream channelname
4. Review the !command banlist file (see below)
5. Run FoxedItUp.exe again

The app should now connect to both AltStream and MixItUp. Any !commands received in chat will be relayed to MixItUp if the command exists there.

Note: Only the main name will be used, as MixItUp doesn't let me see any of the alternate names. For example if your command is called "ShoutOut"
and the options are !shoutout and !so, only !shoutout will work. If there are any spaces in your command name, these will be removed (e.g.
Content Warning becomes !contentwarning)

Any channel point redeems must have the exact same name on AltStream as they do on Twitch

Channel Raided alerts *might* work, but untested
Follow / Sub / Tips don't work as they are not yet officially available in the API.
