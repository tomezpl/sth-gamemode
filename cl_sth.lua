-- FiveM events

autoSpawnCallback = function()
    --[[spawnData = json.decode(exports["spawnmanager"].loadSpawns())
    spawn = spawnData.spawns[1]--]]
    --exports["spawnmanager"].spawnPlayer(x=spawn.x, y=spawn.y, z=spawn.z, model=spawn.model)
    exports.spawnmanager:spawnPlayer({x=851.379, y=-3140.005, z=5.900808, model="a_m_m_skater_01"})
end

onClientGameTypeStart = function()
    --if GetCurrentResourceName() == resName then
    exports.spawnmanager:setAutoSpawnCallback(autoSpawnCallback)
    exports.spawnmanager:setAutoSpawn(true)
    exports.spawnmanager:forceRespawn()

    Citizen.CreateThread(tickUpdate)
    --end
end

startHunt = function(source, args)
    TriggerServerEvent("sth:startHunt")
end

setSkin = function(source, args)
    TriggerEvent("chat:addMessage", {
        args = { tostring("args") }
    })
    TriggerEvent("chat:addMessage", {
        args = { tostring(args) }
    })
    local count = 0
    for _,v in ipairs(args) do
        count = count + 1
    end
    TriggerEvent("chat:addMessage", {
        args = { tostring(count) }
    })
    if count >= 1 then
        local hash = GetHashKey(args[1])
        if IsModelValid(hash) then
            RequestModel(hash)
            while HasModelLoaded(hash) == false do
                Wait(500)
            end
            SetPlayerModel(PlayerId(), hash)
            SetPedDefaultComponentVariation(GetPlayerPed(PlayerId()))
            SetPedDefaultComponentVariation(GetPlayerPed(PlayerId()))
            TriggerServerEvent("sth:replicatePlayerModelChange", PlayerId(), hash)
            --[[msg = "You're now Lester, " . GetPlayerName(PlayerId()) . "!"
            TriggerEvent('chat:addMessage', {
                args = { msg }
            })--]]
        else
            TriggerEvent('chat:addMessage', {
                args = { "Invalid model hash!" }
            })
        end
    else
        TriggerEvent("chat:addMessage", {
            args = {"Need to provide a valid ped model!"}
        })
    end
end

replicatePlayerModelChangeCl = function(playerId, hash)
    TriggerEvent('chat:addMessage', {
        args = { "cunt" }
    })
    if playerId ~= PlayerId() then
        TriggerEvent('chat:addMessage', {
            args = { "cunt1" }
        })
        if IsModelValid(hash) then
            RequestModel(hash)
            while HasModelLoaded(hash) == false do
                Wait(500)
            end
            TriggerEvent('chat:addMessage', {
                args = { "cunt2" }
            })
            SetPlayerModel(playerId, hash)
            TriggerEvent('chat:addMessage', {
                args = { "cunt3" }
            })
            SetPedDefaultComponentVariation(GetPlayerPed(playerId))
            SetPedDefaultComponentVariation(GetPlayerPed(playerId))
            TriggerEvent('chat:addMessage', {
                args = { "cunt4" }
            })
        else
            TriggerEvent('chat:addMessage', {
                args = { "Invalid model hash!" }
            })
        end
    end
end

onClientResourceStart = function()
    --if GetCurrentResourceName() == resName then
        RegisterCommand("starthunt", startHunt)
        RegisterCommand("setskin", setSkin)
    --end
end

-- Survive the Hunt events

-- Current objective text displayed at the bottom of the screen.
currentObj = ""
team = 1 -- 1 if hunter, 0 if hunted
blipId = nil
huntedIdx = 0
huntStarted = false

notifyHuntedPlayer = function()
    huntStarted = true
    -- Get current (hunted) player's ped
    huntedIdx = PlayerId()
    local huntedPed = GetPlayerPed(PlayerId())
    local huntedPos = GetEntityCoords(huntedPed)

    blipId = AddBlipForRadius(huntedPos.x, huntedPos.y, huntedPos.z, 200.0)
    -- Maybe try 66 instead of 16 like in .NET?
    SetBlipColour(blipId, 66)
    SetBlipAlpha(blipId, 128)
    SetBlipNameToPlayerName(blipId, GetPlayerName(PlayerId()))

    currentObj = "Survive"
    team = 0
end

notifyHunters = function(serverId)
    huntStarted = true
    -- Get index of hunted player from server ID (provided from sth:startHunt)
    huntedIdx = GetPlayerFromServerId(serverId)
    local huntedPed = GetPlayerPed(huntedIdx)
    local huntedPos = GetEntityCoords(huntedPed)--]]

    blipId = AddBlipForRadius(huntedPos.x, huntedPos.y, huntedPos.z, 200.0)
    -- Maybe try 66 instead of 16 like in .NET?
    SetBlipColour(blipId, 66)
    SetBlipAlpha(blipId, 128)
    SetBlipNameToPlayerName(blipId, GetPlayerName(huntedIdx))

    currentObj = " is the hunted! Track them down."
    team = 1
end

-- Tick thread
tickUpdate = function()
    while true do
        Citizen.Wait(1)
        if GetPlayerPed(PlayerId()) ~= 0 then
          ResetPlayerStamina(PlayerId())

            if (huntStarted == true and blipId ~= nil and GetPlayerPed(huntedIdx) ~= 0) then
                SetBlipCoords(blipId, GetEntityCoords(GetPlayerPed(huntedIdx)))
                AddTextEntry("CURRENT_OBJECTIVE", "~a~~a~")
                BeginTextCommandPrint("CURRENT_OBJECTIVE")
                if(team == 1) then
                    SetColourOfNextTextComponent(12)
                    AddTextComponentString(GetPlayerName(huntedIdx))
                end
                SetColourOfNextTextComponent(0)
                AddTextComponentString(currentObj)
                EndTextCommandPrint(1, true)
            end
        end
    end
end

RegisterNetEvent("sth:replicatePlayerModelChangeCl")
AddEventHandler("sth:replicatePlayerModelChangeCl", replicatePlayerModelChangeCl)
AddEventHandler("onClientResourceStart", onClientResourceStart)
AddEventHandler("onClientGameTypeStart", onClientGameTypeStart)
RegisterNetEvent("sth:notifyHuntedPlayer")
AddEventHandler("sth:notifyHuntedPlayer", notifyHuntedPlayer)
RegisterNetEvent("sth:notifyHunters")
AddEventHandler("sth:notifyHunters", notifyHunters)