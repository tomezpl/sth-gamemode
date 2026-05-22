using SurviveTheHuntClient.Interfaces;
using SurviveTheHuntClient.Plugins.Cupid.Models;
using System;
using System.Collections.Generic;
using static CitizenFX.Core.Native.API;

namespace SurviveTheHuntClient.Plugins.Cupid.Helpers
{
    internal static class ShipPedSpawnHelper
    {
        internal const byte MaxPeds = 40;

        /// <summary>
        /// A Tickable that spawns peds in intervals so as to not cause lag spikes
        /// </summary>
        internal sealed class Spawner : ITickable
        {
            internal readonly PedNode[] Spawns;

            internal readonly byte MaxPeds;
            internal const byte MaxSpawnPerTick = 2;
            internal readonly byte RequiredCount;

            private byte _index = 0;

            internal bool IsDone => _index >= Math.Min(MaxPeds, Spawns.Length);

            internal delegate void SpawningCompletedEvent(int[] entityHandles, Dictionary<int, PedNode> optionalPedInitStates);

            internal event SpawningCompletedEvent SpawningCompleted;

            internal readonly List<int> EntityHandles;

            private Dictionary<int, PedNode> _optionalPedInitStates = new Dictionary<int, PedNode>();

            private bool _hasStarted = false;

            internal Spawner(PedNode[] spawns, byte maxPeds, byte requiredCount)
            {
                Spawns = spawns;
                MaxPeds = maxPeds;
                EntityHandles = new List<int>(maxPeds);
                RequiredCount = requiredCount;
            }

            private static void StartLoadingNextModels(PedNode[] spawns, byte index, byte toLoad)
            {
                for(byte i = index; i < Math.Min(spawns.Length, index + toLoad); i++)
                {
                    RequestModel(spawns[i].PedModel);
                }
            }

            public void Tick(float deltaTime)
            {
                if(IsDone)
                {
                    return;
                }

                if(!_hasStarted)
                {
                    _hasStarted = true;
                    StartLoadingNextModels(Spawns, _index, MaxSpawnPerTick);
                }

                byte spawned = 0;
                for(byte i = _index; i < Math.Min(_index + MaxSpawnPerTick, Spawns.Length); i++)
                {
                    if (HasModelLoaded(Spawns[i].PedModel))
                    {
                        int pedHandle = CreatePed(0, Spawns[i].PedModel, Spawns[i].Position.X, Spawns[i].Position.Y, Spawns[i].Position.Z, Spawns[i].Position.Heading, true, false);
                        spawned++;
                        EntityHandles.Add(pedHandle);
                        
                        if (i >= RequiredCount)
                        {
                            _optionalPedInitStates.Add(pedHandle, Spawns[i]);
                        }
                    }
                    else
                    {
                        // model hasn't loaded so wait till next tick
                        break;
                    }
                }

                _index += spawned;

                StartLoadingNextModels(Spawns, _index, MaxSpawnPerTick);

                if(IsDone)
                {
                    SpawningCompleted.Invoke(EntityHandles.ToArray(), _optionalPedInitStates);
                }
            }
        }

        internal static Spawner CreateSpawner(CruisegoerSpawnBase[] required, CruisegoerSpawnBase[] optional)
        {
            List<PedNode> toSpawn = new List<PedNode>(MaxPeds);

            byte requiredPedCount = 0;

            CruisegoerSpawnBase[][] spawnSets = { required, optional };
            foreach (CruisegoerSpawnBase[] spawnSet in spawnSets)
            {
                bool isRequired = spawnSet == required;

                foreach(CruisegoerSpawnBase spawn in spawnSet)
                {
                    if(!isRequired && toSpawn.Count >= MaxPeds)
                    {
                        break;
                    }

                    PedNode[] toAdd = spawn.Build();
                    if (isRequired || toSpawn.Count + toAdd.Length <= MaxPeds)
                    {
                        toSpawn.AddRange(toAdd);

                        if(isRequired)
                        {
                            requiredPedCount += (byte)toAdd.Length;
                        }
                    }
                    else
                    {
                        sbyte maxToSpawn = Math.Min((sbyte)((int)MaxPeds - toSpawn.Count), (sbyte)toAdd.Length);
                        for (sbyte i = 0; i < maxToSpawn; i++)
                        {
                            sbyte index = spawn.ShrinkStrategy != CruisegoerSpawnBase.SpawnShrinkStrategy.Sampled || maxToSpawn >= toAdd.Length
                                ? i
                                : Math.Min((sbyte)toAdd.Length, Math.Max((sbyte)0, (sbyte)(((float)toAdd.Length / (float)maxToSpawn) * (float)i)));

                            toSpawn.Add(toAdd[index]);
                        }
                    }
                }
            }

            return new Spawner(toSpawn.ToArray(), Math.Max(requiredPedCount, MaxPeds), requiredPedCount);
        }
    }
}
