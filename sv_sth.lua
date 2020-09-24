-- Game State
gs = {
    huntStarted = false,
    huntedPlayer = -1,
    winningTeam = -1,
    timeLimit = 60000, -- 1 minute for testing
    timeLeft = 60000,
    huntedPingInterval = 10000
}

-- Update function that will run on each server tick of the hunt
huntUpdate = function()
    while gs.huntStarted == true do
        Citizen.Wait(0)

        if gs.timeLeft <= 0 then
            gs.huntStarted = false
            TriggerClientEvent("sth:notifyWinner", -1, gs.winningTeam)
        end
    end
end

-- Update the timer for all clients during the hunt
-- TODO: Move to client-side script to avoid spamming ClientEvents every second?
timeUpdate = function()
    if gs.huntStarted == true then
        TriggerClientEvent("sth:tickTime", -1, gs.timeLeft)
        Citizen.SetTimeout(1000, function()
            timeUpdate()
        end)
        gs.timeLeft = gs.timeLeft - 1000
    end
end

startHunt = function()
    n = 0
    players = GetPlayers()
    for _, v in ipairs(players) do
        n = n + 1
    end
    print(n)
    randomPlayer = math.random(1, n)
    TriggerClientEvent("sth:notifyHuntedPlayer", players[randomPlayer])
    for i=1,n,1 do
        if i ~= randomPlayer then
            TriggerClientEvent("sth:notifyHunters", players[i], players[randomPlayer])
        end
    end

    gs.huntStarted = true
    gs.huntedPlayer = randomPlayer
    gs.winningTeam = 0 -- Make the hunted player a winner by default; this will be only overwritten if the hunted player dies.
    gs.timeLeft = gs.timeLimit;

    Citizen.CreateThread(huntUpdate)
    timeUpdate()
    keepPinging()
end

replicatePlayerModelChange = function(playerId, hash)
    print("Replicating model change")
    TriggerClientEvent("sth:replicatePlayerModelChangeCl", -1, playerId, hash)
end

keepPinging = function()
    if gs.huntStarted == true then
        createPing()
        Citizen.SetTimeout(gs.huntedPingInterval, function()
            keepPinging()
        end)
    end
end

createPing = function()
    playerPos = GetEntityCoords(GetPlayerPed(gs.huntedPlayer))
    radius = 200.0
    randomRadiusLimit = radius * 0.875
    offsetX = math.random(-1, 1) * randomRadiusLimit
    offsetY = math.random(-1, 1) * randomRadiusLimit

    -- Subtracting 1 as we'll be using this in JS (0-indexed)
    TriggerClientEvent("sth:showPingOnMap", -1, { pid = gs.huntedPlayer-1, ox = offsetX, oy = offsetY, r = radius })
    print("Test!")
end

RegisterNetEvent("sth:startHunt")
AddEventHandler("sth:startHunt", startHunt)
RegisterNetEvent("sth:replicatePlayerModelChange")
AddEventHandler("sth:replicatePlayerModelChange", replicatePlayerModelChange)