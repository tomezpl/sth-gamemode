# Survive the Hunt
[Overview](#overview) | [Features](#features) | [Installation](#installation) | [Usage](#commands) | [Configuration](#convars) | [Recommended resources](#other-resources) | [Compiling from source](#building)

## Overview
This is a FiveM gamemode resource that attempts to recreate [FailRace's "Survive the Hunt" series of videos](https://www.youtube.com/playlist?list=PLHw7hcztgbtslirPWPBL4G_8r4XPlp_vr).

## Features
Currently, the gamemode implements the absolute basic ruleset, ie. a random player is chosen to be hunted by all other players.

The hunted player has to survive 24 minutes in Los Santos, blending in with AI traffic in order to avoid being compromised by the hunters, 
who cannot see the hunted player on the map (and vice-versa). However, every minute, the hunted player's current approximate area is revealed to the hunters so that they can converge on the search, increasing the tension.

You need to think smart while being hunted; acquiring a fast car might look suspicious to the hunters, but it may prove useful if a chase breaks out. Balance your odds and adapt to the situation!

Hunters are able to see each other so that they can coordinate a number of strategies, be it during search or pursuit.

## Usage
On a server with Survive the Hunt and [lbg-char-neo](https://github.com/tomezpl/lbg-char-neo) set up, you can press M (keyboard) or hold the GTA interaction menu control (Back/View/Touchpad) to open the gamemode menu. This will let you start a match, spawn new cars in the starting area, or respawn.

![image](/docs/img/gamemode_ui_menu.png)

> Note that in order to access the Appearance menu you will need [lbg-char-neo](https://github.com/tomezpl/lbg-char-neo).

### Commands
Below are all commands made available by this script:
* `/spawncars`
  * Entering this command will spawn a random selection of tuned land vehicles in the starting area.
    * Sometimes you may need to run the command multiple times if not enough vehicles spawn in. This is relatively normal.
    * As of now, I recommend only one player running this command.
* `/starthunt`
  * Entering this command will choose a random player and start a hunt session.
  * By default, the hunted player gets a 1 minute headstart - a "prep phase" which temporarily grants them invincibility and prevents hunters from leaving the spawn area.
* `/respawn`
  * Respawns the local player at the starting point.
* `/heal`
  * Heals the local player (if a hunt isn't currently in progress)

### Convars
[Convars](https://docs.fivem.net/docs/scripting-reference/convars/) are FiveM's way of configuring variables that can be used by the resource (in this case, the gamemode itself).

You can provide your own values for these convars in your `server.cfg` file.

Survive the Hunt exposes the following convars:

| Convar | Description | Default value |
| --- | --- | --- |
| `sth_maxHealth` | The amount of health the player spawns with | 228 |
| `sth_globalPlayerDeathBlips` | Should player death blips be visible to players from the enemy team? | false | 
| `sth_deathbliplifespan` | The number of seconds a player's death blip is visible for on the map. | 5 |
| `sth_prepPhaseDuration` | The number of seconds dedicated to a prep phase before the hunt. This is added to the total round time. | 60 |
| `sth_charCreatorIntegration` | Should the [lbg-char-neo](https://github.com/tomezpl/lbg-char-neo) character creator integration be enabled? | true |
| `sth_syncTimeOnHuntStart` | Should the hunters' in-game clocks be synced to the hunted player's in-game time when the hunt starts? | true |

These are supposed to be server-replicated, so you'll want to use the `setr` command, like so: `setr sth_globalPlayerDeathBlips false`, `setr sth_deathbliplifespan 5` etc.

### Setup
#### Building
**You don't have to build binaries from source. I provide pre-compiled binaries as a release. Skip to the "Installation" section if that's what you're here for.**

The project uses CitizenFX NuGet packages. Some C# development knowledge should be sufficient to build your own binaries from source using Visual Studio.

#### Installation
1. Download the latest precompiled gamemode .zip file from the [Releases](https://github.com/tomezpl/sth-gamemode/releases/latest) page.
2. Extract the .zip file.
3. Copy the `sth-gamemode` folder from the extracted file to your FiveM's server `server-data\resources\[gamemodes]`.
   - You should now see `server-data\resources\[gamemodes]\sth-gamemode\fxmanifest.lua` in your FiveM server files
4. Copy the `sth-ui` folder from the extracted file to your FiveM's server `server-data\resources`.
   - You should now see `server-data\resources\sth-ui\fxmanifest.lua` in your FiveM server files
5. Enable `sth-gamemode` and `sth-ui` in your `server.cfg` - you need to add the following lines:
```
ensure sth-gamemode
ensure sth-ui

setr lbg-char-neo_createKeybind false # this avoids lbg-char-neo's keybind clash
```

##### Other resources
###### Character customisation
In order to properly enjoy this gamemode, you will need some means of character customisation. Feel free to install my [lbg-char-neo resource](https://github.com/tomezpl/lbg-char-neo), which Survive the Hunt officially supports.

###### A note on vMenu
If you are using vMenu, you may find some of its features are incompatible with this gamemode. You can use [my fork of vMenu](https://github.com/tomezpl/vMenu) that cuts out most incompatible features.

If you use the fork, you will need the following convars:

```
setr vmenu_use_permissions false
setr vmenu_enable_dynamic_weather false
setr vmenu_enable_weather_sync true
setr vmenu_enable_time_sync true
```

## License
Survive the Hunt is an open-source FiveM implementation of a community-made unofficial gamemode, made popular by FailRace's videos.

This implementation has been developed by Tomasz Zajac (2020-2025).

You are free to use my work, with or without changes, for non-commercial purposes. Do not resell this work or claim it as yours. No warranty provided.
