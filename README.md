## Partial MixItUp support for AltStream.online

This is early alpha software. You are your own, good luck.  
How it works is we run a separate app that connects to AltStream as if it were a streamer bot and then relays those commands to MixItUp via the developer API.  
The purpose of this app is to bring basic functionality for Twitch streamers moving to AltStream, while we wait for the MixItUp devs to notice the site and implement full support!  

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

Any channel point redeems must have the exact same name on AltStream as they do on Twitch.

Raid, Follow, Tip & Sub alerts must use the Generic -> "Channel Raided" event in MIU, not the "Twitch Channel Raided" event.  

Sub, Tip & Follow alerts use unofficial APIs. Only enable the unofficial API feature if you need these 3 specific events as they cause polling to happen and could easily be broken by a site update.  

Events fired from FIU should set $PlatformEx = altstream. I have no idea if this actually gets passed through. If it does you can check for that before responding on Twitch, for example

### Security Warning (BanList.txt)
MixItUp does not give me visibility of any permissions you have set on commands, and the dev API bypasses permissions anyway. This means that
by default commands you have restricted to Moderators only, or Streamer only, are made available to everyone on AltStream.  

Therefore the only way to remain secure is to ban all !commands that are not generally available to everyone. By default I fill this list with
the MixItUp pre-made commands that are mod-only, but if you have made any custom commands, you will need to add them here and actually test that
they get banned. I will not be held responsible if a rando runs your !endstream command or whatever. You have been warned!

### Issues with MIU currency/inventory and actions that refer to the calling user
Because we're spoofing twitch. If an altstream user doesn't have a twitch account with an identical name, then as far as MIU is concerned they
don't exist. Where users do exist, I would need to look them up by name and provide the MIU ID for them in order for certain commands to work.
This is risky because some may exist, some may not and some may actually be different people. If I did this it would appear to work most of
the time, sometimes have the wrong person and sometimes have no-one. When it's no-one it defaults to the streamer

Therefore I have chosen to not implement this. This means that any MIU action that refers to the calling user, such as checking or adding to inventory
will not work properly. You can work around some of these by explicitly referencing $UserName, but that still has risks.

I recommend simply not using these types of commands. The most common example is daily checkin or First redeems

### Missing MIU special identifiers

```UserID```          - MIU-specific, can't guarantee it even exists!  
```UserDisplayRoles``` - Beyond my scope to translate into every language  
```UserTitle```        - MIU-specific. Could do in future  
```UserTime```         - MIU-specific. Not tracking altstream presence so can't really do  
```UserHours```  
```UserMinutes```  
```UserLastSeenAge```  - Not tracking users between sessions  
```UserLastSeenPrecise```  
```UserFollowAge```    - Unavailable in upstream API  
```UserFollowDays```  
```UserFollowMonths```
```UserFollowYears```  
```UserSubAge```       - We don't have this info in AltStream  
```UserSubMonths```  
```UserSubTier```      - I don't think this exists on AltStream?  
```UserSubBadge```     - Maybe possible need to check API  
```UserInChat```       - Not tracking users  
```UserIsFollower```   - Unavailable in upstream API  
```UserIsRegular```    - Not tracking users  
```UserIsVIP```        - I don't think this exists either  
```UserIsSpecialityExcluded``` - MIU-specific  
```UserBitsLifeTimeAmount``` - Not tracking users  
```UserTotalStreamsWatched```  
```UserTotalAmountDonated```  
```UserTotalSubsGifted```  
```UserTotalSubsReceived```  
```UserTotalChatMessagesSent```  
```UserTotalTimesTagged```  
```UserTotalCommandsRun```  
```UserTotalMonthsSubbed```  
```UserModerationStrikes```  
```UserGameQueuePosition```  
```UserPatreonTier```  
```UserNotes```

All TargetUser special identifiers are missing as well, until I figure out how best to handle that

### Credits
Uses [AltStreamLib](https://github.com/MachaCeleste/AltStreamLib) by [MachaCeleste](https://altstream.online/machaceleste)  
Testing: [BeoWynn](https://altstream.online/beowynn)  
Give me a follow on [AltStream](https://altstream.online/LumKitty) or [Twitch](https://twitch.tv/LumKitty)
