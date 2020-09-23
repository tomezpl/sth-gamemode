// <reference path="./node_modules/@citizenfx/client/natives_universal.d.ts" />
// <reference path="./node_modules/@citizenfx/client/index.d.ts" />

// require('@citizenfx/client');

var currentObj = "";
var team = 1;
var blipId = null;
var huntedIdx = null;
var huntStarted = false;
var currentTimeLeft = -1;

const blipTimeLimit = 5000;
const blipLifespan = 3000; // Time it takes for blip to start fading
var blipTimer = blipTimeLimit;

const huntedPingInterval = 10000;

const dockSpawn = {x: 851.379, y: -3140.005, z: 5.900808};

on('onClientGameTypeStart', () => {
    exports.spawnmanager.setAutoSpawnCallback(autoSpawnCallback);
    exports.spawnmanager.setAutoSpawn(true);
    exports.spawnmanager.forceRespawn();

    setTick(tickUpdate);
});

function autoSpawnCallback() {
    exports.spawnmanager.spawnPlayer({
        ...dockSpawn, 
        model: "a_m_m_skater_01"
    });
}

function startHunt() { TriggerServerEvent("sth:startHunt"); }

function setSkin(source, args) {
    if(args.length >= 1)
    {
        let hash = GetHashKey(args[0]);
        if(IsModelValid(hash))
        {
            RequestModel(hash);
            var modelLoaded = false;
            var finishedSetting = false;
            var modelLoaderId = setTick(() => {
                if(!HasModelLoaded(hash)) {
                    Wait(500);
                }
                else {
                    modelLoaded = true;
                }

                if(modelLoaderId && modelLoaded) {
                    TriggerEvent("chat:addMessage", {args: [`setSkin: modelLoader tick ${modelLoaderId} about to be cleared`]});
                    clearTick(modelLoaderId);
                    TriggerEvent("chat:addMessage", {args: [`setSkin: modelLoader tick ${modelLoaderId} was cleared`]});
                }
                TriggerEvent("chat:addMessage", {args: [`setSkin: modelLoader tick ${modelLoaderId} is still alive`]});
            });
            var modelSetterId = setTick(() => {
                if(modelLoaded && !finishedSetting)
                {
                    SetPlayerModel(PlayerId(), hash);
                    for(let i = 0; i < 2; i++) { SetPedDefaultComponentVariation(GetPlayerPed(PlayerId())); }
                    TriggerServerEvent("sth:replicatePlayerModelChange", PlayerId(), hash);
                    finishedSetting = true;
                }
                if(modelSetterId && finishedSetting) {
                    clearTick(modelSetterId);
                }
            });
        }
        else
        {
            TriggerEvent("chat:addMessage", {
                args: ["Invalid model mesh!"]
            })
        }
    }
    else
    {
        TriggerEvent("chat:addMessage", {
            args: ["Need to provide a valid ped model!"]
        });
    }
}

function replicatePlayerModelChangeCl(playerId, hash) {
    if(playerId != PlayerId()) {
        if(IsModelValid(hash)) {
            RequestModel(hash);
            var modelLoaded = false;
            var finishedSetting = false;
            var modelLoaderId = setTick(() => {
                if(!HasModelLoaded(hash)) {
                    Wait(500);
                }
                else {
                    modelLoaded = true;
                }

                if(modelLoaderId && modelLoaded) {
                    TriggerEvent("chat:addMessage", {args: [`modelLoader tick ${modelLoaderId} about to be cleared`]});
                    clearTick(modelLoaderId);
                    TriggerEvent("chat:addMessage", {args: [`modelLoader tick ${modelLoaderId} was cleared`]});
                }
                TriggerEvent("chat:addMessage", {args: [`modelLoader tick ${modelLoaderId} is still alive`]});
            });
            var modelSetterId = setTick(() => {
                if(modelLoaded && !finishedSetting)
                {
                    SetPlayerModel(playerId, hash);
                    for(let i = 0; i < 2; i++) { SetPedDefaultComponentVariation(GetPlayerPed(playerId)); }
                    finishedSetting = true;
                }
                if(modelSetterId && finishedSetting) {
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

on('onClientResourceStart', () => {
    RegisterCommand("starthunt", startHunt);
    RegisterCommand("setskin", setSkin);
});

function Repeat(callback, ms) {
    setTimeout(() => {callback(); Repeat(callback, ms); }, ms);
}

function pingBlipOnMap(blip, duration) {
    SetBlipDisplay(blipId, 6);
    SetBlipAlpha(blip, 128);
    setTimeout(() => { fadeBlip(blip, 128, blipTimeLimit); }, duration);
}

function keepPingingPlayer() {
    if(huntStarted) {
        pingBlipOnMap(blipId, blipLifespan);
    }
}

function notifyHuntedPlayer() {
    huntStarted = true;
    huntedIdx = PlayerId();
    let huntedPed = GetPlayerPed(huntedIdx);
    let huntedPos = GetEntityCoords(huntedPed);

    //if(DoesEntityExist(blipId)) {
    //}
    //createBlipForPlayer(huntedIdx);

    //keepPingingPlayer();
    //var ping = setInterval(() => { if(ping && !huntStarted) { clearInterval(ping); } else { keepPingingPlayer(); } }, huntedPingInterval);

    currentObj = "Survive";
    team = 0;
}

function notifyHunters(serverId) {
    huntStarted = true;
    huntedIdx = GetPlayerFromServerId(serverId);
    let huntedPed = GetPlayerPed(huntedIdx);
    let huntedPos = GetEntityCoords(huntedPed);

    //if(DoesEntityExist(blipId)) {
    //}
    //createBlipForPlayer(huntedIdx);

    //keepPingingPlayer();
    //var ping = setInterval(() => { if(ping && !huntStarted) { clearInterval(ping); } else { keepPingingPlayer(); } }, huntedPingInterval);

    currentObj = " is the hunted! Track them down."
    team = 1;
}

function endHunt() {
    currentObj = "";
    currentTimeLeft = -1;
    huntStarted = false;
    huntedIdx = null;
}

function fadeBlip(blip, initialOpacity, duration) {
    //setTimeout(() => {
    var timeWaited = 0;
    var fadeInterval = setInterval(() => {
        let alpha = Math.floor(128 * (1.0 - (timeWaited / duration)));
        SetBlipAlpha(blip, alpha);
        if(alpha <= 0 || timeWaited >= duration) {
            if(fadeInterval) {
                clearInterval(fadeInterval);
            }
        }
        timeWaited += 100;
    }, 100);
    setTimeout(() => {
        if(blip) {
            SetBlipDisplay(blip, 0);
        }
    }, duration);
    //}, duration);
}

function createBlipForPlayer(args) {
    blipTimer = blipTimeLimit;
    const radius = parseInt(args.r);
    const offsetX = Number(args.ox);
    const offsetY = Number(args.oy);
    const playerId = Number(args.pid);
    TriggerEvent("chat:addMessage", {args: [`local id: ${PlayerId()}, server id: ${playerId}`]});
    TriggerEvent("chat:addMessage", {args: [`radius: ${radius === 200}`]});
    TriggerEvent("chat:addMessage", {args: [`offsetX: ${offsetX}`]});
    let playerPos = GetEntityCoords(GetPlayerPed(GetPlayerFromServerId(playerId)));
    TriggerEvent("chat:addMessage", {args: ["Test"]});
    if(blipId === null) {
        TriggerEvent("chat:addMessage", {args: ["Creating blip"]});
        blipId = AddBlipForRadius(playerPos[0] + offsetX, playerPos[1], playerPos[2] + offsetY, radius);
    }
    else {
        TriggerEvent("chat:addMessage", {args: ["Updating blip"]});
        SetBlipCoords(blipId, playerPos[0] + offsetX, playerPos[1], playerPos[2] + offsetY);
    }
    TriggerEvent("chat:addMessage", {args: ["Test2"]});
    SetBlipColour(blipId, 66);
    SetBlipAlpha(blipId, 128);
    SetBlipDisplay(blipId, 6);
    TriggerEvent("chat:addMessage", {args: ["Test3"]});
    SetBlipNameToPlayerName(blipId, GetPlayerName(GetPlayerFromServerId(playerId)));
    TriggerEvent("chat:addMessage", {args: ["Test4"]});
}

function notifyWinner(winningTeam) {
    TriggerEvent("chat:addMessage", {args: ["You win"]});
    if(team == winningTeam) {
        currentObj = "You've won the hunt!";
    }
    else {
        currentObj = "You've lost the hunt!";
    }
    setTimeout(endHunt, 5000);
}

function tickTime(time) {
    currentTimeLeft = time;
}

function tickUpdate() {
        Wait(1);
        if(GetPlayerPed(PlayerId()) != 0) {
            ResetPlayerStamina(PlayerId());

            AddTextEntry("CURRENT_OBJECTIVE", "~a~~a~");
            BeginTextCommandPrint("CURRENT_OBJECTIVE");
            if(huntStarted && huntedIdx && GetPlayerPed(huntedIdx) != 0) {
                if(team == 1) {
                    SetColourOfNextTextComponent(12);
                    AddTextComponentString(GetPlayerName(huntedIdx));
                }
            }
            SetColourOfNextTextComponent(0);
            AddTextComponentString(currentObj);
            EndTextCommandPrint(1, true);

            if(currentTimeLeft >= 0) {
                let dateTime = new Date(currentTimeLeft);
                let seconds = dateTime.getSeconds();
                seconds = (seconds < 10 ? `0${seconds}` : seconds);
                let minutes = dateTime.getMinutes();
                minutes = (minutes < 10 ? `0${minutes}` : minutes);
                let timeStr = `${dateTime.getMinutes()}:${dateTime.getSeconds()}`;

                let rectWidth = 0.001;
                BeginTextCommandWidth("STRING");
                AddTextComponentString("00:00")
                rectWidth = EndTextCommandGetWidth(true);
                let textWidth = 0.001;
                BeginTextCommandWidth("STRING");
                AddTextComponentString(timeStr);
                textWidth = EndTextCommandGetWidth(true);

                DrawRect(0.94, 0.875, rectWidth + 0.001, 0.06, 0,0,0, 128);

                BeginTextCommandDisplayText("STRING");
                AddTextComponentString(timeStr);
                EndTextCommandDisplayText(0.89 + 0.0005 + (rectWidth/6), 0.835);
            }
        }
}

onNet("sth:replicatePlayerModelChangeCl", replicatePlayerModelChangeCl);
onNet("sth:notifyHuntedPlayer", notifyHuntedPlayer);
onNet("sth:notifyHunters", notifyHunters);
onNet("sth:notifyWinner", notifyWinner);
onNet("sth:tickTime", tickTime);
onNet("sth:showPingOnMap", createBlipForPlayer);