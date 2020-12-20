require('@citizenfx/server')//ignore

// Team enum
const Team = { Hunters: 0, Hunted: 1 };

// List of cars that can be spawned for the players.
const AllowedCars = [
    'adder',
    'banshee',
    'bfinjection',
    'blista',
    'buffalo',
    'buffalo2',
    'bullet',
    'carbonizzare',
    'coquette',
    'dubsta2',
    'exemplar',
    'gauntlet',
    'dominator',
    'infernus',
    'sultan',
    'khamelion',
    'mesa3',
    'oracle2',
    'phoenix',
    'schafter2',
    'schwarzer',
    'serrano',
    'comet3',
    'phantom2'
];

const CarSpawnPoints = [
    { xyz: [818.21, -3128.38, 5.9], rot: 180 }, // 1
    { xyz: [822.22, -3129.37, 5.9], rot: 180 }, // 2
    { xyz: [826.22, -3129.37, 5.9], rot: 180 }, // 3
    { xyz: [830.22, -3129.37, 5.9], rot: 180 }, // 4
    { xyz: [834.22, -3129.37, 5.9], rot: 180 }, // 5
    { xyz: [838.22, -3129.37, 5.9], rot: 180 }, // 6
    { xyz: [842.22, -3129.37, 5.9], rot: 180 }, // 7
    { xyz: [846.22, -3129.37, 5.9], rot: 180 }, // 8
    { xyz: [850.22, -3129.37, 5.9], rot: 180 }, // 9
    { xyz: [854.22, -3129.37, 5.9], rot: 180 }, // 10
    { xyz: [858.22, -3129.37, 5.9], rot: 180 }, // 11
    { xyz: [862.22, -3129.37, 5.9], rot: 180 }, // 12
    { xyz: [866.22, -3129.37, 5.9], rot: 180 }, // 13
    { xyz: [866.22, -3143.73, 5.9], rot: 0 }, // 14
    { xyz: [862.22, -3143.73, 5.9], rot: 0 }, // 15
    { xyz: [858.22, -3143.73, 5.9], rot: 0 }, // 16
    { xyz: [854.22, -3143.73, 5.9], rot: 0 }, // 17
    { xyz: [850.22, -3143.73, 5.9], rot: 0 }, // 18
    { xyz: [846.22, -3143.73, 5.9], rot: 0 }, // 19
    { xyz: [842.22, -3143.73, 5.9], rot: 0 }, // 20
    { xyz: [838.22, -3143.73, 5.9], rot: 0 }, // 21
    { xyz: [834.22, -3143.73, 5.9], rot: 0 }, // 22
    { xyz: [830.22, -3143.73, 5.9], rot: 0 }, // 23
    { xyz: [826.22, -3143.73, 5.9], rot: 0 }, // 24
    { xyz: [822.22, -3143.73, 5.9], rot: 0 }, // 25
    { xyz: [818.22, -3143.73, 5.9], rot: 0 }  // 26
];

// List of spawned cars for the players. Can be refreshed.
var SpawnedCars = [];

// Constants to start the game
const GameSettings = {
    TimeLimit: GetConvarInt("sth_timelimit", 60000 * 24), // Time limit for each hunt (in ms)
    HuntedPingInterval: GetConvarInt("sth_pinginterval", 120000) // Amount of time between pinging the hunted player's location on the map (in ms)
};

function defaultGameState() {
    return {
        huntStarted: false, // is there an ongoing hunt?
        huntedPlayer: -1, // server ID of the currently hunted player. Default: -1 (nobody)
        winningTeam: -1, // team winning the match. Default: -1 (neither)
        timeLeft: GameSettings.TimeLimit, // Time left on this hunt (in ms)

        tickSet: false, // Is the Main Tick set yet?
        gameTimer: null, // Handle of our main timer interval
        pingTimer: null, // Handle of our blip ping (showing the player's surrounding radius on map) timer interval

        gameTimeout: null, // Handle of our main timeout (needs to be cleared if a hunt ends prematurely),
        pingTimeout: null // Handle of our pinging interval timeout (same as above)
    }
}

// Game state
var gs = defaultGameState();

function clearTimers() {
    if (gs.gameTimer !== null) {
        clearInterval(gs.gameTimer);
        gs.gameTimer = null;
    }
    if (gs.pingTimer !== null) {
        clearInterval(gs.pingTimer);
        gs.pingTimer = null;
    }
}

function endHunt() {
    clearTimers();
    gs.huntStarted = false;
    TriggerClientEvent("sth:notifyWinner", -1, gs.winningTeam);
    gs = defaultGameState();
}

function beginGame(player) {
    gs.huntStarted = true;
    gs.huntedPlayer = player;
    gs.winningTeam = Team.Hunted; // Make the hunted player the winner by default, as this is only overriden if they die.
    gs.timeLeft = GameSettings.TimeLimit;
}

// Main tick (main loop?)
// Checks game state and ends the hunt if needed.
function Tick() {

}

// Updates the game state time every 10 seconds.
function TimeUpdate(firstTick = false) {
    if(gs.huntStarted) {
        gs.timeLeft -= firstTick ? 0 : 10000;
        TriggerClientEvent("sth:tickTime", -1, gs.timeLeft);
    }
}

function PingBlip() {
    if(gs.huntStarted === true) {
        // Radius of the blip
        const radius = 200.0;
        // Radius in which the player's location can be pinged. The smaller the multiplier, the more precise the ping is.
        const playerLocationRadius = radius * 0.875;

        // Radar offsets
        const offsetX = ((Math.random() * 2) - 1) * playerLocationRadius;
        const offsetY = ((Math.random() * 2) - 1) * playerLocationRadius;

        TriggerClientEvent("sth:showPingOnMap", -1, { pid: gs.huntedPlayer, ox: offsetX, oy: offsetY, r: radius });
    }
}

function spawnCars({ pid }) {
    console.log("Spawning cars for players...");

    console.log("Getting rid of already spawned cars.");
    // Delete any cars already spawned.
    SpawnedCars.forEach((car) => {
        DeleteEntity(car);
    });
    console.log("Existing cars deleted.");

    SpawnedCars = [];

    console.log("Spawning new cars...");
    CarSpawnPoints.forEach((spawnPoint, index) => {
        const randomCar = AllowedCars[Math.floor((AllowedCars.length - 1) * Math.random())];
        console.log(`Chosen random car '${randomCar}'`);
        const hash = GetHashKey(randomCar);
        console.log("Obtained hash key for random car");
        console.log(`Spawning ${randomCar} at spawn point ${index + 1}.`);
        emitNet("sth:spawnCar", pid, { hash, spawnPoint });
        console.log(`Dispatched 'spawn ${randomCar} at spawn point ${index + 1}' event to client ${pid}.`);
    });
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
                TriggerClientEvent("sth:notifyHunters", GetPlayerFromIndex(i), {
                    serverId: GetPlayerFromIndex(randomPlayerIndex), huntedPlayerName: GetPlayerName(GetPlayerFromIndex(randomPlayerIndex))
                });
            }
        }

        beginGame(GetPlayerFromIndex(randomPlayerIndex));

        if(!gs.tickSet) {
            gs.tickSet = true;
            setTick(Tick);
        }

        if (gs.pingTimer === null) {
            gs.pingTimer = setInterval(PingBlip, GameSettings.HuntedPingInterval);
            gs.pingTimeout = setTimeout(() => { clearInterval(gs.pingTimer); gs.pingTimer = null; }, GameSettings.TimeLimit);
        }

        if(gs.gameTimer === null) {
            TimeUpdate(true);
            gs.gameTimer = setInterval(TimeUpdate, 10000);
            gs.gameTimeout = setTimeout(() => { clearInterval(gs.gameTimer); gs.gameTimer = null; endHunt(); }, GameSettings.TimeLimit);
        }

        TriggerClientEvent("sth:huntStartedByServer", -1);
    },
    replicatePlayerModelChange: (playerId, hash) => {
        TriggerClientEvent("sth:replicatePlayerModelChangeCl", -1, { playerId, hash });
    },
    playerDied: ({ pid }) => {
        console.log(`Player died: ${GetPlayerName(pid)}`);
        if (pid == gs.huntedPlayer) {
            gs.winningTeam = Team.Hunters;
            if (gs.gameTimeout !== null) {
                clearTimeout(gs.gameTimeout);
            }
            if (gs.pingTimeout !== null) {
                clearTimeout(gs.pingTimeout);
            }
            endHunt();
        }
    },
    spawnCars: ({ pid }) => {
        console.log("Spawning cars for player " + pid + "...");

        TriggerClientEvent("sth:despawnCars", pid, SpawnedCars);

        setTimeout(() => {
            let carArr = [];

            console.log("Spawning new cars...");
            CarSpawnPoints.forEach((spawnPoint, index) => {
                const randomCar = AllowedCars[Math.floor((AllowedCars.length - 1) * Math.random())];
                console.log(`Chosen random car '${randomCar}'`);
                console.log(`Spawning ${randomCar} at spawn point ${index + 1}.`);
                carArr.push({ car: randomCar, spawnPoint });
            });

            TriggerClientEvent("sth:createCars", pid, carArr);
        }, 1000);
    },
    saveSpawnedCars: (carHandles) => {
        console.log("Saving " + carHandles.length + " cars.");
        SpawnedCars = carHandles;
    }
};

// Register all server events with names so that they can be called from the clients.
function registerEvents() {
    Object.keys(Events).forEach((evName) => {
        onNet(`sth:${evName}`, Events[evName]);
    });
}

registerEvents();