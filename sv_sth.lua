-- Game State
gs = {
    huntStarted = false,
    huntedPlayer = -1,
    winningTeam = -1,
    timeLimit = 60000, -- 1 minute for testing
    timeLeft = 60000
}

-- Update function that will run on each server tick of the hunt
huntUpdate = function()
    while gs.huntStarted == true do
        Citizen.Wait(0)

        if gs.timeLeft <= 0 then
            gs.huntStarted = false
            TriggerClientEvent("sth:notifyWinner", gs.huntedPlayer)
        end
    end
end

timeUpdate = function()
    while gs.huntStarted == true do
        TriggerClientEvent("sth:tickTime", -1, gs.timeLeft)
        Citizen.Wait(1000)
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

    Citizen.CreateThread(huntUpdate)
    Citizen.CreateThread(timeUpdate)
end

replicatePlayerModelChange = function(playerId, hash)
    print("Replicating model change")
    TriggerClientEvent("sth:replicatePlayerModelChangeCl", -1, playerId, hash)
end

RegisterNetEvent("sth:startHunt")
AddEventHandler("sth:startHunt", startHunt)
RegisterNetEvent("sth:replicatePlayerModelChange")
AddEventHandler("sth:replicatePlayerModelChange", replicatePlayerModelChange)