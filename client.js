require('@citizenfx/client');//ignore

// Team enum
const Team = { Hunters: 0, Hunted: 1 };

// Constants to start the game
const GameSettings = {
    TimeLimit: GetConvarInt("sth_timelimit", 60000 * 24) // Time limit for each hunt (in ms)
};

var currentObj = ""; // Objective displayed to the local player, based on the team.
var team = 1; // Local player's team.
var blipId = null; // ID of the hunted player's blip.
var huntedIdx = null; // ID of the hunted player.
var huntedName = null; // Name of the hunted player.
var huntStarted = false; // Is a hunt running? This and huntOver can be true at the same time.
var huntOver = false; // Has a hunt just finished? This is false if the hunt hasn't started yet.
var currentTimeLeft = -1; // Time left in the hunt.
var timer = null; // Timer (interval) used to track time.
var deleteTimerTimeout = null; // Timeout used to delete timer (interval) at the end of a hunt.

var PlayerBlips = [];

// Car handle array to sync with the server.
var SpawnedCars = [];
var CarsToDespawn = [];
var CarsToSpawn = [];

var weaponsGiven = false;

const blipTimeLimit = GetConvarInt("sth_blipfadetime", 5000); // Amount of time it takes for the blip to fade completely (after blipLifespan runs out).
const blipLifespan = GetConvarInt("sth_bliplifespan", 40000); // Time it takes for blip to start fading

const dockSpawn = { x: 851.379, y: -3140.005, z: 5.900808 }; // Spawn coordinates

const mapBounds = { topLeft: [-3078.28, 364.85, 7.01], topRight: [-7.07, 1823.70, 207.73] };

const zLimit = 1130.0;

// Called when this script is loaded on the client.
on('onClientGameTypeStart', () => {
    exports.spawnmanager.setAutoSpawnCallback(autoSpawnCallback);
    exports.spawnmanager.setAutoSpawn(true);
    exports.spawnmanager.forceRespawn();
    on("baseevents:onPlayerKilled", () => {
        emitNet("sth:playerDied", { pid: GetPlayerServerId(PlayerId()) });
    });

    setInterval(updateWeapons, 200);
    setInterval(() => {
        PlayerBlips.forEach((playerBlip) => {
            RemoveBlip(playerBlip.blip);
        })

        PlayerBlips = [];
    }, 5000);

    on("playerSpawned", () => {
        weaponsGiven = false;

        let playerPed = GetPlayerPed(PlayerId());
        emitNet("sth:cleanClothes", { pid: PlayerId() });

        NetworkSetFriendlyFireOption(true);
        SetCanAttackFriendly(playerPed, true, true);
    });

    setTick(tickUpdate);
});

// Called when spawn is triggered.
function autoSpawnCallback() {
    exports.spawnmanager.spawnPlayer({
        ...dockSpawn,
        model: "a_m_m_skater_01"
    });
}

// Called when user issues /starthunt
function startHunt() {
    if (huntOver === true) {
        TriggerEvent("chat:addMessage", { args: ["You must wait 5 seconds before starting another hunt."] });
    }
    else {
        TriggerServerEvent("sth:startHunt");
    }
}

function checkIfPedTooFar(ped) {
    const pos = GetEntityCoords(ped);
    return pos[1] >= zLimit;
}

function setSkin(source, args) {
    if (args.length >= 1) {
        let hash = GetHashKey(args[0]);
        if (IsModelValid(hash)) {
            RequestModel(hash);
            var modelLoaded = false;
            var finishedSetting = false;
            var modelLoaderId = setTick(() => {
                if (!HasModelLoaded(hash)) {
                    Wait(500);
                }
                else {
                    modelLoaded = true;
                }

                if (modelLoaderId && modelLoaded) {
                    TriggerEvent("chat:addMessage", { args: [`setSkin: modelLoader tick ${modelLoaderId} about to be cleared`] });
                    clearTick(modelLoaderId);
                    TriggerEvent("chat:addMessage", { args: [`setSkin: modelLoader tick ${modelLoaderId} was cleared`] });
                }
                TriggerEvent("chat:addMessage", { args: [`setSkin: modelLoader tick ${modelLoaderId} is still alive`] });
            });
            var modelSetterId = setTick(() => {
                if (modelLoaded && !finishedSetting) {
                    SetPlayerModel(PlayerId(), hash);
                    for (let i = 0; i < 2; i++) { SetPedDefaultComponentVariation(GetPlayerPed(PlayerId())); }
                    TriggerServerEvent("sth:replicatePlayerModelChange", PlayerId(), hash);
                    finishedSetting = true;
                }
                if (modelSetterId && finishedSetting) {
                    clearTick(modelSetterId);
                }
            });
        }
        else {
            TriggerEvent("chat:addMessage", {
                args: ["Invalid model mesh!"]
            })
        }
    }
    else {
        TriggerEvent("chat:addMessage", {
            args: ["Need to provide a valid ped model!"]
        });
    }
}

// This is an event handler for changing the player model. The event is fired over the network from the server to all players.
// Seems like calling the change model native twice fixes ped appearance sync issues across clients.
function replicatePlayerModelChangeCl(args) {
    // Unpack arguments
    const playerId = args.playerId;
    const hash = args.hash;

    if (playerId != PlayerId()) {
        if (IsModelValid(hash)) {
            RequestModel(hash);
            var modelLoaded = false;
            var finishedSetting = false;
            var modelLoaderId = setTick(() => {
                if (!HasModelLoaded(hash)) {
                    Wait(500);
                }
                else {
                    modelLoaded = true;
                }

                if (modelLoaderId && modelLoaded) {
                    TriggerEvent("chat:addMessage", { args: [`modelLoader tick ${modelLoaderId} about to be cleared`] });
                    clearTick(modelLoaderId);
                    TriggerEvent("chat:addMessage", { args: [`modelLoader tick ${modelLoaderId} was cleared`] });
                }
                TriggerEvent("chat:addMessage", { args: [`modelLoader tick ${modelLoaderId} is still alive`] });
            });
            var modelSetterId = setTick(() => {
                if (modelLoaded && !finishedSetting) {
                    SetPlayerModel(playerId, hash);
                    for (let i = 0; i < 2; i++) { SetPedDefaultComponentVariation(GetPlayerPed(playerId)); }
                    finishedSetting = true;
                }
                if (modelSetterId && finishedSetting) {
                    clearTick(modelSetterId);
                }
            });
        }
        else {
            TriggerEvent("chat:addMessage", {
                args: ["Invalid model hash!"]
            });
        }
    }
}

// Fired when resource is loaded. Registers commands etc.
on('onClientResourceStart', () => {
    RegisterCommand("starthunt", startHunt);
    RegisterCommand("setskin", setSkin);
    RegisterCommand("spawncars", () => { emitNet("sth:spawnCars", { pid: GetPlayerServerId(PlayerId()) }) });
    RegisterCommand("suicide", () => {
        SetEntityHealth(PlayerPedId(), 0);
        TriggerEvent("baseevents:onPlayerKilled");
    });
    RegisterCommand("xyz", () => {
        const pos = GetEntityCoords(PlayerPedId());
        const rot = GetEntityHeading(PlayerPedId());
        TriggerEvent("chat:addMessage", { args: [`${pos[0]}, ${pos[1]}, ${pos[2]}`] });
        TriggerEvent("chat:addMessage", { args: [`Yaw: ${rot}`] });
    });
});

// Pings a blip on the map, then sets it to start fading after a specified duration.
function pingBlipOnMap(blip, duration) {
    SetBlipDisplay(blipId, 6);
    SetBlipAlpha(blip, 128);
    SetBlipHiddenOnLegend(blip, false);
    if (team === Team.Hunters) {
        SetBlipRoute(blip, true);
    }
    setTimeout(() => { fadeBlip(blip, 128, blipTimeLimit); }, duration);
}

// Resets the necessary globals after a hunt.
function endHunt() {
    currentObj = "";
    currentTimeLeft = -1;
    if (timer !== null) {
        clearInterval(timer);
        timer = null;
    }
    huntStarted = false;
    huntOver = false;
    huntedIdx = null;
}

// Fades blip over time.
function fadeBlip(blip, initialOpacity, duration) {
    var timeWaited = 0;
    var fadeInterval = setInterval(() => {
        let alpha = Math.floor(128 * (1.0 - (timeWaited / duration)));
        SetBlipAlpha(blip, alpha);
        timeWaited += 100;
    }, 100);
    setTimeout(() => { clearInterval(fadeInterval); }, duration);
    setTimeout(() => {
        if (blip) {
            SetBlipDisplay(blip, 0);
            SetBlipRoute(blip, false);
            SetBlipHiddenOnLegend(blip, true);
        }
    }, duration);
}

function createBlipForPlayer(args) {
    const radius = parseInt(args.r);
    const offsetX = Number(args.ox);
    const offsetY = Number(args.oy);
    const playerId = Number(args.pid);

    const playerPos = GetEntityCoords(GetPlayerPed(GetPlayerFromServerId(playerId)));

    /*
    TriggerEvent("chat:addMessage", {args: [`local id: ${PlayerId()}, server id: ${playerId}`]});
    TriggerEvent("chat:addMessage", {args: [`radius: ${radius === 200}`]});
    TriggerEvent("chat:addMessage", {args: [`offsetX: ${offsetX}`]});
    */

    //TriggerEvent("chat:addMessage", {args: ["Test"]});
    if (blipId === null) {
        //TriggerEvent("chat:addMessage", {args: ["Creating blip"]});
        blipId = AddBlipForRadius(playerPos[0] + offsetX, playerPos[1], playerPos[2] + offsetY, radius);
    }
    else {
        //TriggerEvent("chat:addMessage", {args: ["Updating blip"]});
        SetBlipCoords(blipId, playerPos[0] + offsetX, playerPos[1], playerPos[2] + offsetY);
    }
    //TriggerEvent("chat:addMessage", {args: ["Test2"]});
    SetBlipColour(blipId, 66);
    SetBlipAlpha(blipId, 128);
    SetBlipDisplay(blipId, 6);
    SetBlipNameToPlayerName(blipId, GetPlayerFromServerId(playerId));

    pingBlipOnMap(blipId, blipLifespan);
}

function tickUpdate() {
    Wait(1);
    if (GetPlayerPed(PlayerId()) != 0) {
        // Infinite stamina.
        ResetPlayerStamina(PlayerId());

        // Show current objective if hunt still going.
        if (currentObj !== null && currentObj.trim() !== "") {
            AddTextEntry("CURRENT_OBJECTIVE", "~a~~a~");
            BeginTextCommandPrint("CURRENT_OBJECTIVE");
            if (huntStarted === true && huntOver === false && huntedName !== null && GetPlayerPed(huntedIdx) != 0) {
                // If current player is a hunter, set initial text colour to yellow
                // as it's meant to say the hunted player's name.
                if (team == Team.Hunters) {
                    SetColourOfNextTextComponent(12);
                    AddTextComponentString(huntedName);
                }
            }
            // Ensure the main objective string is displayed in default (white) text colour.
            SetColourOfNextTextComponent(0);
            AddTextComponentString(currentObj);
            EndTextCommandPrint(1, true);
        }

        ClearPlayerWantedLevel(PlayerId());

        updateCars();
        updatePlayerBlips();

        // Show remaining time if hunt still going.
        if (currentTimeLeft >= 0) {
            const timeStr = formatIntoMMSS(currentTimeLeft);
            drawRemainingTime(timeStr);
        }
    }
}

function updatePlayerBlips() {
    for (let i = 0; i < 32; i++) {
        if (NetworkIsPlayerActive(i) && !PlayerBlips.some((playerBlip) => playerBlip.id === i)) {
            const index = PlayerBlips.push({ id: i, blip: AddBlipForEntity(GetPlayerPed(i)) }) - 1;
            SetBlipNameToPlayerName(PlayerBlips[index].blip, i);
            SetBlipColour(PlayerBlips[index].blip, i + 10);
            SetBlipCategory(PlayerBlips[index].blip, 2);
            SetBlipShrink(PlayerBlips[index].blip, true);
            SetBlipScale(PlayerBlips[index].blip, 0.9);
        }
    }

    PlayerBlips.forEach((playerBlip) => {
        if ((GetPlayerPed(playerBlip.id) == PlayerPedId()) || (GetPlayerName(playerBlip.id) === huntedName && !checkIfPedTooFar(GetPlayerPed(playerBlip.id))) || team === Team.Hunted) {
            // Hide the blip
            SetBlipDisplay(playerBlip.blip, 0);
        }
        else if (checkIfPedTooFar(GetPlayerPed(playerBlip.id))) {
            // Show the blip
            SetBlipDisplay(playerBlip.blip, 2);
        }
    });
}

function updateWeapons() {
    const takeWeaponsAway = () => {
        weaponsGiven = false;
        RemoveAllPedWeapons(PlayerPedId(), false);
    };

    const giveWeaponsBack = () => {
        if (team === Team.Hunted && huntStarted === true) {
            weaponsGiven = true;
            GiveWeaponToPed(PlayerPedId(), GetHashKey("WEAPON_APPISTOL"), 9999, false, false);
            GiveWeaponToPed(PlayerPedId(), GetHashKey("WEAPON_CARBINERIFLE"), 9999, false, false);
            GiveWeaponToPed(PlayerPedId(), GetHashKey("WEAPON_STICKYBOMB"), 25, false, false);
            GiveWeaponToPed(PlayerPedId(), GetHashKey("WEAPON_RPG"), 25, false, false);
            GiveWeaponToPed(PlayerPedId(), GetHashKey("WEAPON_ASSAULTSHOTGUN"), 25, false, false);
        }
        else {
            weaponsGiven = true;
            GiveWeaponToPed(PlayerPedId(), GetHashKey("WEAPON_COMBATPISTOL"), 9999, false, false);
            GiveWeaponToPed(PlayerPedId(), GetHashKey("WEAPON_PUMPSHOTGUN"), 9999, false, false);
        }
    };

    const isInCar = IsPedInAnyVehicle(PlayerPedId(), true);

    if (weaponsGiven === true && isInCar !== false) {
        takeWeaponsAway();
    }
    else if (weaponsGiven === false && isInCar === false) {
        giveWeaponsBack();
    }
}

function updateCars() {
    CarsToDespawn.forEach((car) => {
        DeleteVehicle(car);
    });

    CarsToDespawn = [];

    let updateServer = false;
    if (CarsToSpawn.length > 0) {
        updateServer = true;
    }

    if (updateServer === true) {
        SpawnedCars = [];
    }

    CarsToSpawn.forEach(({ car, spawnPoint }) => {
        const hash = GetHashKey(car);
        if (IsModelInCdimage(hash) && IsModelAVehicle(hash)) {
            RequestModel(hash);
            Wait(50);
            if (HasModelLoaded(hash)) {
                const index = SpawnedCars.push(CreateVehicle(hash, spawnPoint.xyz[0], spawnPoint.xyz[1], spawnPoint.xyz[2], spawnPoint.rot, true, false)) - 1;
                const spawnedCar = SpawnedCars[index];
                // Set all vehicle mods to maximum
                for (let i = 0; i < 50; i++) {
                    const nbMods = GetNumVehicleMods(spawnedCar, i);
                    if (nbMods > 0) {
                        SetVehicleModKit(spawnedCar, 0);
                        SetVehicleMod(spawnedCar, i, nbMods - 1, false);
                    }
                }
                // Add neons
                for (let i = 0; i < 4; i++) {
                    SetVehicleNeonLightEnabled(spawnedCar, i, Math.random() >= 0.5);
                }
                SetVehicleNeonLightsColour(spawnedCar, Math.round(Math.random() * 255), Math.round(Math.random() * 255), Math.round(Math.random() * 255));

                SetVehicleXenonLightsColour(spawnedCar, Math.round(Math.random() * 12));

                SetVehicleCustomPrimaryColour(spawnedCar, Math.round(Math.random() * 255), Math.round(Math.random() * 255), Math.round(Math.random() * 255));
                SetVehicleCustomSecondaryColour(spawnedCar, Math.round(Math.random() * 255), Math.round(Math.random() * 255), Math.round(Math.random() * 255));
            }
        }
    });

    CarsToSpawn = [];

    if (updateServer === true) {
        emitNet("sth:saveSpawnedCars", SpawnedCars);
    }
}

// Formats milliseconds into a mm:ss string.
function formatIntoMMSS(milliseconds) {
    const dateTime = new Date(milliseconds);
    let seconds = dateTime.getSeconds();
    seconds = (seconds < 10 ? `0${seconds}` : seconds);
    let minutes = dateTime.getMinutes();
    minutes = (minutes < 10 ? `0${minutes}` : minutes);
    return `${minutes}:${seconds}`;
}

// Draws a timerbar with the remaining time in the bottom right corner of the screen.
function drawRemainingTime(timeStr) {
    SetTextScale(0, 0.55);
    BeginTextCommandWidth("STRING");
    AddTextComponentString("TIME LEFT  00:00")
    const rectWidth = EndTextCommandGetWidth(true);

    RequestStreamedTextureDict("timerbars");
    if (HasStreamedTextureDictLoaded("timerbars")) {
        DrawSprite("timerbars", "all_black_bg", 0.92, 0.875, rectWidth, 0.06 * 0.5 * 1.4, 0.0, 255, 255, 255, 128);
    }

    BeginTextCommandDisplayText("STRING");
    AddTextComponentString(`${timeStr}`);
    EndTextCommandDisplayText(0.94, 0.855);
    SetTextScale(0, 0.35);
    BeginTextCommandDisplayText("STRING");
    AddTextComponentString("TIME LEFT");
    EndTextCommandDisplayText(0.94 - rectWidth / 2.35, 0.865);
    SetTextScale(0, 1.0);
}

function resetTimer() {
    if (timer !== null) {
        clearInterval(timer);
        timer = null;
    }

    timer = setInterval(() => { currentTimeLeft -= 1000; }, 1000);
    if (deleteTimerTimeout !== null) {
        clearTimeout(deleteTimerTimeout);
        deleteTimerTimeout = null;
    }
    deleteTimerTimeout = setTimeout(() => { if (timer !== null) { clearInterval(timer); timer = null; deleteTimerTimeout = null; } }, GameSettings.TimeLimit);
}

// Network-aware events.
const Events = {
    // Reset the timer when the hunt starts on the server.
    huntStartedByServer: () => { resetTimer(); },

    // Ping the hunted player on the map.
    showPingOnMap: (args) => {
        createBlipForPlayer(args);
        if (huntedIdx === PlayerId()) {
            const pos = GetEntityCoords(GetPlayerPed(huntedIdx));
            emitNet("sth:broadcastHuntedZone", { pos });
        }
    },

    // Show a notification about the hunted player's zone.
    notifyAboutHuntedZone: ({ pos }) => {
        const zoneName = GetLabelText(GetNameOfZone(pos[0], pos[1], pos[2]));
        BeginTextCommandThefeedPost("STRING");
        AddTextComponentString(`${huntedName} is somewhere in ${zoneName} right now.`);
        EndTextCommandThefeedPostTicker(true, true);
    },

    // Update the remaining time to sync with the server.
    tickTime: (time) => { currentTimeLeft = time; },

    // Notify the winning team at the end of a hunt.
    notifyWinner: (winningTeam) => {
        huntOver = true;
        TriggerEvent("chat:addMessage", {
            args: [`You ${team === winningTeam ? 'win' : 'lose'}`]
        });
        if (team == winningTeam) {
            currentObj = "You've won the hunt!";
        }
        else {
            currentObj = "You've lost the hunt!";
        }
        setTimeout(endHunt, 5000);
    },

    // Notify the hunters about their objective when the hunt starts.
    notifyHunters: ({ serverId, huntedPlayerName }) => {
        huntStarted = true;
        huntedIdx = GetPlayerFromServerId(serverId);
        TriggerEvent("chat:addMessage", {
            args: [`huntedPlayerName: ${huntedPlayerName}`]
        });
        huntedName = huntedPlayerName;

        currentObj = " is the hunted! Track them down."
        team = Team.Hunters;
    },

    // Notify the hunted player(s) about their objective when the hunt starts.
    notifyHuntedPlayer: () => {
        huntStarted = true;
        huntedIdx = PlayerId();
        huntedName = GetPlayerName(PlayerId());

        currentObj = "Survive";
        team = Team.Hunted;
    },

    createCars: (cars) => {
        CarsToSpawn = cars;
    },

    despawnCars: (spawnedCarHandles) => {
        CarsToDespawn = spawnedCarHandles;
    },

    cleanClothesForPlayer: ({ pid }) => {
        ClearPedBloodDamage(GetPlayerPed(pid));
    }
};

// Register all client events with names so that they can be called from the server.
function registerEvents() {
    Object.keys(Events).forEach((evName) => {
        onNet(`sth:${evName}`, Events[evName]);
    });
}

registerEvents();

// skin change bugfix
onNet("sth:replicatePlayerModelChangeCl", replicatePlayerModelChangeCl);