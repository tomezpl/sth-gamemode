## Features
- Added spawnable vehicle list configuration. Modify [configs/vehicles.json](https://github.com/tomezpl/sth-gamemode/blob/develop/configs/team_loadouts.json) to add or remove vehicles from the list used by `/spawncars`. ([#34](https://github.com/tomezpl/sth-gamemode/issues/34))

## Fixes
- Fixed previous batch of vehicles not being deleted when using `/spawncars`. ([#44](https://github.com/tomezpl/sth-gamemode/issues/44))

## Known issues
- Due to how the fix for [#44](https://github.com/tomezpl/sth-gamemode/issues/44) is implemented, there is an additional 3.5s delay when using `/spawncars`.
  - This is in order to allow the server to properly delete the existing vehicles.
  - A fix to mitigate this will be introduced in a future version.

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
