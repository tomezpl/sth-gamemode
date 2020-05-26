-- Globals
currentObj = "" -- Current objective text displayed at the bottom of the screen.
team = 1 -- 1 if hunter, 0 if hunted
blipId = nil
huntedIdx = nil
huntStarted = false
currentTimeLeft = -1

-- Time it takes for the blip to fade
blipTimeLimit = 5000
blipTimer = 5000

-- Time it takes for the blip to start fading
blipLifespan = 3000

-- Hunted player ping interval
huntedPingInterval = 10000

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

keepPingingPlayer = function()
    while huntStarted == true do
        pingBlipOnMap(blipId, blipLifespan)
        Citizen.Wait(huntedPingInterval)
        if huntStarted == true then
            blipId = createBlipForPlayer(huntedIdx)
        end
    end
end

notifyHuntedPlayer = function()
    huntStarted = true
    -- Get current (hunted) player's ped
    huntedIdx = PlayerId()
    local huntedPed = GetPlayerPed(PlayerId())
    local huntedPos = GetEntityCoords(huntedPed)

    blipId = createBlipForPlayer(huntedIdx)
    
    -- Regularly ping the hunted player on the map
    Citizen.CreateThread(keepPingingPlayer)

    currentObj = "Survive"
    team = 0
end

notifyHunters = function(serverId)
    huntStarted = true
    -- Get index of hunted player from server ID (provided from sth:startHunt)
    huntedIdx = GetPlayerFromServerId(serverId)
    local huntedPed = GetPlayerPed(huntedIdx)
    local huntedPos = GetEntityCoords(huntedPed)

    blipId = createBlipForPlayer(huntedIdx)
    
    -- Regularly ping the hunted player on the map
    Citizen.CreateThread(keepPingingPlayer)

    currentObj = " is the hunted! Track them down."
    team = 1
end

endHunt = function()
    currentObj = ""
    currentTimeLeft = -1
    huntStarted = false
    huntedIdx = nil
end

fadeBlip = function(blip, initialOpacity, duration)
    Citizen.CreateThread(function() 
        local timeWaited = 0
        TriggerEvent("chat:addMessage", { args = { blip } })
        while timeWaited < duration do
            local alpha = math.floor(128.0 * (1.0 - (timeWaited / duration)))
            SetBlipAlpha(blip, alpha)
            if alpha <= 0 then
                DeleteEntity(blip)
            end
            Citizen.Wait(100)
            timeWaited = timeWaited + 100
        end
    end)
end

-- Shows a 200m radius blip of a player on the map
createBlipForPlayer = function(playerId)
    blipTimer = blipTimeLimit
    local playerPos = GetEntityCoords(GetPlayerPed(playerId))
    local radius = 200.0
    -- "Error" of the radius
    local randomRadiusLimit = radius * 0.875
    local offsetX = math.random(-1.0 * randomRadiusLimit, randomRadiusLimit)
    local offsetY = math.random(-1.0 * randomRadiusLimit, randomRadiusLimit)
    local newBlip = AddBlipForRadius(playerPos.x + offsetX, playerPos.y, playerPos.z + offsetY, radius)
    SetBlipColour(newBlip, 66)
    SetBlipAlpha(newBlip, 128)
    SetBlipNameToPlayerName(newBlip, GetPlayerName(playerId))

    return newBlip
end

pingBlipOnMap = function(blip, duration)
    SetBlipAlpha(blip, 128)
    Citizen.SetTimeout(duration, 
    function() 
        fadeBlip(blip, 128, blipTimeLimit) 
    end)
end

notifyWinner = function(winningTeam)
    -- TODO: this might appear after a yellow huntedPlayerName for the hunters
    TriggerEvent("chat:addMessage", {args={"Yuo win"}})
    if team == winningTeam then
        currentObj = "You've won the hunt!"
    else
        currentObj = "You've lost the hunt!"
    end
    Citizen.SetTimeout(5000, endHunt)
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

            AddTextEntry("CURRENT_OBJECTIVE", "~a~~a~")
            BeginTextCommandPrint("CURRENT_OBJECTIVE")
            if (huntStarted == true and huntedIdx ~= nil and GetPlayerPed(huntedIdx) ~= 0) then
                if(team == 1) then
                    SetColourOfNextTextComponent(12)
                    AddTextComponentString(GetPlayerName(huntedIdx))
                end
            end
            SetColourOfNextTextComponent(0)
            AddTextComponentString(currentObj)
            EndTextCommandPrint(1, true)

            if(currentTimeLeft >= 0) then
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