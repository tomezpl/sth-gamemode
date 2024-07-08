# 1.3.0
## Features
- Added support for joining matches in progress. ([#15](https://github.com/tomezpl/sth-gamemode/issues/15))
- Added the `sth_maxHealth` convar to allow tweaking players' max health. ([#51](https://github.com/tomezpl/sth-gamemode/issues/51))
- Added a safe zone at the Terminal spawn.  ([#52](https://github.com/tomezpl/sth-gamemode/issues/52))
- Added a prep phase during which the hunted player is invincible and hunters can't leave the spawn.  ([#52](https://github.com/tomezpl/sth-gamemode/issues/52))
  - The prep phase duration can be set with the `sth_prepPhaseDuration` convar.
- Added more varied default characters.  ([#56](https://github.com/tomezpl/sth-gamemode/issues/56))

## Fixes
- Fixed matches not ending if the hunted player has disconnected.

# 1.2.0
## Features
- Added spawnable vehicle list configuration. Modify [configs/vehicles.json](https://github.com/tomezpl/sth-gamemode/blob/develop/configs/vehicles.json) to add or remove vehicles from the list used by `/spawncars`. ([#34](https://github.com/tomezpl/sth-gamemode/issues/34))

## Fixes
- Fixed previous batch of vehicles not being deleted when using `/spawncars`. ([#44](https://github.com/tomezpl/sth-gamemode/issues/44))
- Fixed some vehicles not spawning if they took too long to load.

## Known issues
- Due to how the fix for [#44](https://github.com/tomezpl/sth-gamemode/issues/44) is implemented, there is an additional 3.5s delay when using `/spawncars`.
  - This is in order to allow the server to properly delete the existing vehicles.
  - A fix to mitigate this will be introduced in a future version.
- On rare occasions, some vehicles may fail to delete properly. The repro steps for this are currently unclear.

# 1.1.0
## Features
- Added player death markers on the radar. Use `setr sth_deathbliplifespan <seconds>` to override the number of seconds the marker stays on the map. Use `setr sth_globalPlayerDeathBlips true` to make hunter death blips visible to hunted players. ([#26](https://github.com/tomezpl/sth-gamemode/issues/26))  ([#32](https://github.com/tomezpl/sth-gamemode/issues/32)) 
- Added weapon loadout configuration. Modify [configs/team_loadouts.json](https://github.com/tomezpl/sth-gamemode/blob/develop/configs/team_loadouts.json) to customise them. ([#29](https://github.com/tomezpl/sth-gamemode/issues/29))

## Fixes
- Fixed hunted player mugshots intermittently failing to display. ([#40](https://github.com/tomezpl/sth-gamemode/issues/40))

## Tweaks
- Respawn command is now `/respawn` ([#37](https://github.com/tomezpl/sth-gamemode/issues/37))
- Rebalanced the default weapon loadout. ([#29](https://github.com/tomezpl/sth-gamemode/issues/29))

# 1.0.1
## Fixes
- Fixed an issue where one player would be consistently excluded from the random hunted pick. The game now operates a queue system to ensure every player waits roughly the same amount of time between their hunted rounds. ([#22](https://github.com/tomezpl/sth-gamemode/issues/22))

# 1.0.0
Initial release (C# port)

# 0.1
Early prototype written in JS
