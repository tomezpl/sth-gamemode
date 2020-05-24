

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
end

replicatePlayerModelChange = function(playerId, hash)
    print("Replicating model change")
    TriggerClientEvent("sth:replicatePlayerModelChangeCl", -1, playerId, hash)
end

RegisterNetEvent("sth:startHunt")
AddEventHandler("sth:startHunt", startHunt)
RegisterNetEvent("sth:replicatePlayerModelChange")
AddEventHandler("sth:replicatePlayerModelChange", replicatePlayerModelChange)