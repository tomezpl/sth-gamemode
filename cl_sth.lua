-- Globals
currentObj = "" -- Current objective text displayed at the bottom of the screen.
team = 1 -- 1 if hunter, 0 if hunted
blipId = nil
huntedIdx = 0
huntStarted = false
currentTimeLeft = -1

-- Spawns
dockSpawn = vector3(851.379, -3140.005, 5.900808)

-- FiveM events
autoSpawnCallback = function()
    exports.spawnmanager:spawnPlayer({x=dockSpawn.x, y=dockSpawn.y, z=dockSpawn.z, model="a_m_m_skater_01"})
end

onClientGameTypeStart = function()
    exports.spawnmanager:setAutoSpawnCallback(autoSpawnCallback)
    exports.spawnmanager:setAutoSpawn(true)
    exports.spawnmanager:forceRespawn()

    Citizen.CreateThread(tickUpdate)
end

-- Survive the Hunt events
startHunt = function(source, args)
    TriggerServerEvent("sth:startHunt")
end

setSkin = function(source, args)
    local count = 0
    for _,v in ipairs(args) do
        count = count + 1
    end
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

-- Converts milliseconds to minutes:seconds string
msToMMSS = function(ms)
    if(ms >= 1000) then
        local sTotal = math.floor(ms / 1000)
        local s = sTotal % 60
        local m = math.floor((sTotal - s) / 60)
        local sStr = ""
        if(s < 10) then
            sStr = "0"
        end
        sStr = sStr .. tostring(s)
        mStr = tostring(m)
        return mStr .. ":" .. sStr
    end
    return "0:00"
end

replicatePlayerModelChangeCl = function(playerId, hash)
    if playerId ~= PlayerId() then
        if IsModelValid(hash) then
            RequestModel(hash)
            while HasModelLoaded(hash) == false do
                Wait(500)
            end
            SetPlayerModel(playerId, hash)
            SetPedDefaultComponentVariation(GetPlayerPed(playerId))
            SetPedDefaultComponentVariation(GetPlayerPed(playerId))
        else
            TriggerEvent('chat:addMessage', {
                args = { "Invalid model hash!" }
            })
        end
    end
end

onClientResourceStart = function()
    RegisterCommand("starthunt", startHunt)
    RegisterCommand("setskin", setSkin)
end

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
    local huntedPos = GetEntityCoords(huntedPed)

    blipId = AddBlipForRadius(huntedPos.x, huntedPos.y, huntedPos.z, 200.0)
    -- Maybe try 66 instead of 16 like in .NET?
    SetBlipColour(blipId, 66)
    SetBlipAlpha(blipId, 128)
    SetBlipNameToPlayerName(blipId, GetPlayerName(huntedIdx))

    currentObj = " is the hunted! Track them down."
    team = 1
end

notifyWinner = function()
    -- TODO: this might appear after a yellow huntedPlayerName for the hunters
    currentObj = "You've won the hunt!"
end

-- Receives current time from server
tickTime = function(time)
    currentTimeLeft = time
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

                --local timeStr = msToMMSS(currentTimeLeft)
                local timeStr = msToMMSS(currentTimeLeft)
                local rectWidth = 0.001
                BeginTextCommandWidth("STRING")
                AddTextComponentString("00:00")
                rectWidth = EndTextCommandGetWidth(true)
                local textWidth = 0.001
                BeginTextCommandWidth("STRING")
                AddTextComponentString(timeStr)
                textWidth = EndTextCommandGetWidth(true)

                DrawRect(0.94, 0.875, rectWidth + 0.001, 0.06, 0, 0, 0, 128)

                BeginTextCommandDisplayText("STRING")
                AddTextComponentString(timeStr)
                EndTextCommandDisplayText(0.89 + 0.0005 + (rectWidth/6), 0.835)
            end
        end
    end
end

-- Register & Add Events
RegisterNetEvent("sth:replicatePlayerModelChangeCl")
AddEventHandler("sth:replicatePlayerModelChangeCl", replicatePlayerModelChangeCl)
AddEventHandler("onClientResourceStart", onClientResourceStart)
AddEventHandler("onClientGameTypeStart", onClientGameTypeStart)
RegisterNetEvent("sth:notifyHuntedPlayer")
AddEventHandler("sth:notifyHuntedPlayer", notifyHuntedPlayer)
RegisterNetEvent("sth:notifyHunters")
AddEventHandler("sth:notifyHunters", notifyHunters)
RegisterNetEvent("sth:notifyWinner")
AddEventHandler("sth:notifyWinner", notifyWinner)
RegisterNetEvent("sth:tickTime")
AddEventHandler("sth:tickTime", tickTime)