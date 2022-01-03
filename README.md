# Survive the Hunt
## Overview
This is a FiveM gamemode resource that attempts to recreate [FailRace's "Survive the Hunt" series of videos](https://www.youtube.com/playlist?list=PLHw7hcztgbtslirPWPBL4G_8r4XPlp_vr).

## Features
Currently, the gamemode implements the absolute basic ruleset, ie. a random player is chosen to be hunted by all other players.

The hunted player has to survive 24 minutes in Los Santos, blending in with AI traffic in order to avoid being compromised by the hunters, 
who cannot see the hunted player on the map (and vice-versa). However, every minute, the hunted player's current approximate area is revealed to the hunters so that they can converge on the search, increasing the tension.

You need to think smart while being hunted; acquiring a fast car might look suspicious to the hunters, but it may prove useful if a chase breaks out. Balance your odds and adapt to the situation!

Hunters are able to see each other so that they can coordinate a number of strategies, be it during search or pursuit.

## Usage
### Commands
Below are all commands made available by this script:
* `/spawncars`
  * Entering this command will spawn a random selection of tuned land vehicles in the starting area.
    * Sometimes you may need to run the command multiple times if not enough vehicles spawn in. This is relatively normal.
    * As of now, I recommend only one player running this command.
* `/starthunt`
  * Entering this command will choose a random player and start a hunt session.
  * I recommend allowing the hunted player 1 minute from this point as a headstart.
* `/suicide`
  * Respawns the local player at the starting point.

### Setup
#### Building
**You don't have to build binaries from source. I provide pre-compiled binaries as a release. Skip to the "Installation" section if that's what you're here for.**

The project uses CitizenFX NuGet packages. Some C# development knowledge should be sufficient to build your own binaries from source using Visual Studio.

#### Installation
1. Download the latest precompiled gamemode .zip file from the [Releases](https://github.com/tomezpl/sth-gamemode/releases/latest) page.
2. Extract the .zip file.
3. In your FiveM's server `server-data\resources\[gamemodes]`, create a new folder called `sth-gamemode`.
4. Copy the extracted .zip file contents to the newly created folder, so that `fxmanifest.lua` ends up located at `server-data\resources\[gamemodes]\sth-gamemode\fxmanifest.lua`.
5. Enable `sth-gamemode` in your `server.cfg` - you need to add the following lines:
   * ```
     setr vmenu_use_permissions false
     setr vmenu_enable_dynamic_weather false
     setr vmenu_enable_weather_sync true
     setr vmenu_enable_time_sync true
     ```
   * ```
     ensure vMenu
     ensure sth-gamemode
     ```

**IMPORTANT: Before you play, you WILL NEED vMenu - it is used to provide character customisation which is very important to enjoy this gamemode properly. 
More specifically, you will need my specific fork of vMenu which cuts out some incompatible features. It's available [here](https://github.com/tomezpl/vMenu) but is created by Tom Grobbe - I only adapted it to use with my gamemode.**

## License
Survive the Hunt is an open-source FiveM implementation of a community-made unofficial gamemode, made popular by FailRace's videos.

This implementation has been developed by Tomasz Zajac (2020-2022).

You are free to use my work, with or without changes, for non-commercial purposes. Do not resell this work or claim it as yours. No warranty provided.
