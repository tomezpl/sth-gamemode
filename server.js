require('@citizenfx/server')

// Team enum
const Team = { Hunters: 0, Hunted: 1 };

// Constants to start the game
const GameSettings = {
    TimeLimit: 60000, // Time limit for each hunt (in ms)
    HuntedPingInterval: 10000 // Amount of time between pinging the hunted player's location on the map (in ms)
};

// Game state
var gs = {
    huntStarted: false, // is there an ongoing hunt?
    huntedPlayer: -1, // server ID of the currently hunted player. Default: -1 (nobody)
    winningTeam: -1, // team winning the match. Default: -1 (neither)
    timeLeft: GameSettings.TimeLimit, // Time left on this hunt (in ms)

    tickSet: false, // Is the Main Tick set yet?
    gameTimer: null, // Handle of our main timer interval
    pingTimer: null // Handle of our blip ping (showing the player's surrounding radius on map) timer interval
};

function beginGame(player) {
    gs.huntStarted = true;
    gs.huntedPlayer = player;
    gs.winningTeam = Team.Hunted; // Make the hunted player the winner by default, as this is only overriden if they die.
    gs.timeLeft = GameSettings.TimeLimit;
}

// Main tick (main loop?)
// Checks game state and ends the hunt if needed.
function Tick() {
    if(gs.huntStarted) {

        // Check if time ran out and end the hunt if needed, notifying the winning team.
        if(gs.timeLeft <= 0) {
            gs.huntStarted = false;
            TriggerClientEvent("sth:notifyWinner", -1, gs.winningTeam);
        }
    }
}

// Updates the game state time every second.
function TimeUpdate() {
    if(gs.huntStarted) {
        gs.timeLeft -= 1000;
        TriggerClientEvent("sth:tickTime", -1, gs.timeLeft);
    }
    else {
        clearInterval(gs.gameTimer);
        gs.gameTimer = null;
    }
}

function PingBlip() {
    if(gs.huntStarted) {
        // Radius of the blip
        const radius = 200.0;
        // Radius in which the player's location can be pinged. The smaller the multiplier, the more precise the ping is.
        const playerLocationRadius = radius * 0.875;

        // Radar offsets
        const offsetX = ((Math.random() * 2) - 1) * playerLocationRadius;
        const offsetY = ((Math.random() * 2) - 1) * playerLocationRadius;

        TriggerClientEvent("sth:showPingOnMap", -1, { pid: gs.huntedPlayer, ox: offsetX, oy: offsetY, r: radius });
    }
    else {
        clearInterval(gs.pingTimer);
        gs.pingTimer = null;
    }
}

// Server events (callable from clients)
const Events = {
    startHunt: () => {
        // Get number of players present.
        const playerCount = GetNumPlayerIndices();
        // Choose a random player.
        let randomPlayerIndex = Math.round(Math.random() * (playerCount - 1));
        
        // Notify the hunted player's game.
        TriggerClientEvent("sth:notifyHuntedPlayer", GetPlayerFromIndex(randomPlayerIndex));
        
        // Notify everyone else (the hunters).
        for(let i = 0; i < playerCount; i++) {
            if(i != randomPlayerIndex) {
                // TODO: Can't we rewrite this with issuing a request to -1 players (everyone) but just reject it on the hunted player's client?
                TriggerClientEvent("sth:notifyHunters", GetPlayerFromIndex(i), GetPlayerFromIndex(randomPlayerIndex));
            }
        }

        beginGame(GetPlayerFromIndex(randomPlayerIndex));

        if(!gs.tickSet) {
            gs.tickSet = true;
            setTick(Tick);
        }

        if(gs.gameTimer === null) {
            setInterval(TimeUpdate, 1000);
        }

        if(gs.pingTimer === null) {
            setInterval(PingBlip, GameSettings.HuntedPingInterval);
        }
    },
    replicatePlayerModelChange: (playerId, hash) => {
        TriggerClientEvent("sth:replicatePlayerModelChangeCl", -1, { playerId, hash });
    }
};

// Register all server events with names so that they can be called from the clients.
function registerEvents() {
    Object.keys(Events).forEach((evName) => {
        onNet(`sth:${evName}`, Events[evName]);
    });
}

registerEvents();