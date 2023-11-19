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
