using CitizenFX.Core;
using SurviveTheHuntServer.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.XPath;
using static CitizenFX.Core.Native.API;

namespace SurviveTheHuntServer
{
    public class PantoHunt : Hunt
    {
        private readonly string[] PantoSpawnNames =
        {
            "Grove Street",
            "Simeon",
            "Little Seoul car park",
            "Vespucci Beach",
            "Mirror Park",
            "Michael's",
            "Hotel (trivago)",
            "Vinewood Hills",
            "Arcadius",
            "Lester's"
        };

        private readonly Coord[] PantoSpawns =
        {
            new Coord(new Vector3(113.58f, -1942.39f, 20.35f)), // Grove Street
            new Coord(new Vector3(-51.22f, -1116.22f, 26.06f)), // Simeon
            new Coord(new Vector3(-478.12f, -757.52f, 44.64f)), // Little Seoul car park
            new Coord(new Vector3(-1374.03f, -1119.92f, 4.13f)), // Vespucci Beach
            new Coord(new Vector3(1085.65f, -698.56f, 58.03f)), // Mirror Park
            new Coord(new Vector3(-815.16f, 159.24f, 71.15f), 90f), // Michael's
            new Coord(new Vector3(-1667.97f, -541.91f, 34.27f)), // Hotel (trivago)
            new Coord(new Vector3(-1797.87f, 399.3f, 112.05f)), // Vinewood Hills
            new Coord(new Vector3(-107.19f, -608.57f, 34.42f)), // Arcadius
            new Coord(new Vector3(1272.11f, -1728.78f, 55.03f)) // Lester's
        };

        private const float MinPantoDistance = 1000f;

        private bool CalledStart = false;

        private List<int> PantoHandles = new List<int>();

        private bool PantosNeedCullingRadiusUpdate = false;
        private bool PantosNeedMods = false;
        private bool PantoBlipsNeedUpdate = false;

        private const int TargetABlipSprite = 535;
        private const int TargetHBlipSprite = 542;

        /// <summary>
        /// Maximum number of objectives that can be spawned - GTA has sprites for target blips A-H, so we can't have more than 8.
        /// </summary>
        private const int MaxObjectives = (TargetHBlipSprite - TargetABlipSprite) + 1;

        private List<int> PantoHealth = new List<int>();

        public PantoHunt(IEnumerable<Player> playerHandles) : base(playerHandles)
        {
            CalledStart = false;
        }

        private Coord[] PickRandomPantoSpawns(int count, Random rng)
        {
            if(count < 1)
            {
                return new Coord[] { };
            }

            // Initialise the list of potential spawns with the pre-defined array of spawns.
            List<Coord> potentialSpawns = new List<Coord>(PantoSpawns);

            // Cap the returned array to the number of pre-defined Panto spawns.
            Coord[] spawns = new Coord[Math.Min(MaxObjectives, Math.Min(count, PantoSpawns.Length))];

            // Choose the first spawn completely random.
            int randomIndex = rng.Next(0, potentialSpawns.Count);
            spawns[0] = potentialSpawns[randomIndex];
            potentialSpawns.RemoveAt(randomIndex);

            // Denominator to use to reduce the minimum distance between Pantos.
            // This will be incremented whenever we reach a situation where we've run out of Pantos that are far enough away,
            // and will use this to divide the minimum distance, in effect progresssively reducing the radius of each spawn.
            int minDistanceDenominator = 1;

            for(int i = 1; i < spawns.Length; i++)
            {
                // Narrow down the potential spawns to ones that are far enough away from each spawn we've chosen already.
                potentialSpawns = potentialSpawns.Where((spawnPoint) => !spawns.Any((selectedSpawn) => selectedSpawn != null && selectedSpawn.Position.DistanceToSquared(spawnPoint.Position) < MinPantoDistance / minDistanceDenominator)).ToList();

                // If there aren't any potential spawns far enough away, reinitialise the list but reduce the minimum distance.
                while (!potentialSpawns.Any())
                {
                    minDistanceDenominator++;
                    potentialSpawns = PantoSpawns.Where((spawnPoint) => !spawns.Any((selectedSpawn) => selectedSpawn != null && selectedSpawn.Position.DistanceToSquared(spawnPoint.Position) < (MinPantoDistance / minDistanceDenominator))).ToList();
                }

                // Choose a random potential spawn that is far enough away from each spawn point we've chosen so far.
                randomIndex = rng.Next(0, potentialSpawns.Count);
                spawns[i] = potentialSpawns[randomIndex];
            }

            return spawns;
        }

        private async Task Start(MainScript main)
        {
            CalledStart = true;
            Coord[] pantoSpawns = PickRandomPantoSpawns(5, main.RNG);

            foreach (Coord spawnPoint in pantoSpawns)
            {
                PantoHandles.Add(await main.SpawnVehicle("panto", spawnPoint.Position, spawnPoint.Heading));
                string spawnName = PantoSpawnNames[PantoSpawns.ToList().IndexOf(spawnPoint)];
                Debug.WriteLine($"Created a Panto at {spawnName}");
                PantoHealth.Add(1000);
            }

            PantosNeedCullingRadiusUpdate = true;
            PantosNeedMods = true;
            PantoBlipsNeedUpdate = true;
        }

        private void OnEnded(MainScript main)
        {
            CalledStart = false;

            Cleanup(main);
        }

        public override void Shutdown(MainScript main)
        {
            base.Shutdown(main);

            Cleanup(main);
        }

        private void Cleanup(MainScript main)
        {
            Debug.WriteLine("Removing Pantos");
            foreach (int pantoHandle in PantoHandles)
            {
                if (DoesEntityExist(pantoHandle))
                {
                    DeleteEntity(pantoHandle);
                }

                main.EntityHandles.Remove(pantoHandle);
            }
            Debug.WriteLine("Removed Pantos");
            PantoHandles.Clear();

            PantoHealth.Clear();
        }

        public override async Task Tick(MainScript main)
        {
            await base.Tick(main);

            if(!main.GameState.Hunt.IsStarted)
            {
                if(CalledStart)
                {
                    OnEnded(main);
                }

                return;
            }

            if(PantoHealth.Count > 0 && !PantoBlipsNeedUpdate)
            {
                int index = 0;
                foreach(int health in PantoHealth)
                {
                    if (DoesEntityExist(PantoHandles[index]))
                    {
                        int currentHealth = GetEntityHealth(PantoHandles[index]);
                        if (currentHealth <= 0 && health != 0)
                        {
                            main.TriggerClientEventProxy("sth:markPantoAsDead", NetworkGetNetworkIdFromEntity(PantoHandles[index]), index);
                            PantoHealth[index] = 0;
                        }
                    }

                    index++;
                }
            }

            if(PantoHandles.Count > 0 && PantoBlipsNeedUpdate)
            {
                bool pantoDoesNotExistYet = false;
                foreach(int handle in PantoHandles)
                {
                    pantoDoesNotExistYet = !DoesEntityExist(handle);
                }

                if (!pantoDoesNotExistYet)
                {
                    Debug.WriteLine("Triggering client-side blip natives by broadcasting sth:applyPantoBlips...");
                    main.TriggerClientEventProxy("sth:applyPantoBlips", string.Join(";", PantoHandles.Select(h => $"{NetworkGetNetworkIdFromEntity(h)}").ToArray()));
                    Debug.WriteLine("sth:applyPantoBlips event sent");
                    PantoBlipsNeedUpdate = false;
                }
            }

            if(!PantosNeedCullingRadiusUpdate && PantosNeedMods)
            {
                foreach(int handle in PantoHandles)
                {
                    // hot pink panto baby
                    SetVehicleColours(handle, 135, 135);
                }

                PantosNeedMods = false;
            }

            // Pantos must not be culled because we need to apply mods to them.
            if(PantosNeedCullingRadiusUpdate)
            {
                bool pantoDoesNotExistYet = false;
                foreach(int pantoHandle in PantoHandles)
                {
                    pantoDoesNotExistYet = !DoesEntityExist(pantoHandle);

                    if (!pantoDoesNotExistYet)
                    {
                        SetEntityDistanceCullingRadius(pantoHandle, float.MaxValue);
                    }
                }

                if(!pantoDoesNotExistYet)
                {
                    PantosNeedCullingRadiusUpdate = false;
                }
            }

            if(!CalledStart)
            {
                await Start(main);
            }
        }
    }
}
