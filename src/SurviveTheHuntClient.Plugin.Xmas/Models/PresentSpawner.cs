using System;
using System.Collections.Generic;
using static CitizenFX.Core.Native.API;

namespace SurviveTheHuntClient.Plugins.Xmas.Models
{
    internal struct PresentSpawner
    {
        internal readonly uint[] RequiredModels;
        internal readonly Func<PrezzieLocation, int> Spawn;

        internal PresentSpawner(Func<PrezzieLocation, int> spawnerFunc, uint[] requiredModels = null)
        {
            RequiredModels = requiredModels ?? new uint[0];
            Spawn = spawnerFunc;
        }
    }

    internal class PresentSpawnerCollection : Dictionary<Constants.PrezzieLocationTag, PresentSpawner>
    {
        /*private PresentSpawnerCollection(Dictionary<Helpers.XmasModifier.Constants.PrezzieLocationTag, PresentSpawner> data)
        {
            foreach(KeyValuePair<Helpers.XmasModifier.Constants.PrezzieLocationTag, PresentSpawner> kvp in data)
            {
                this.Add(kvp.Key, kvp.Value);
            }
        }*/

        internal bool LoadModels()
        {
            bool loaded = true;

            foreach(PresentSpawner spawner in Values)
            {
                foreach(uint modelHash in spawner.RequiredModels)
                {
                    if(!HasModelLoaded(modelHash))
                    {
                        RequestModel(modelHash);
                        loaded = false;
                    }
                }
            }

            return loaded;
        }

        /*public static implicit operator PresentSpawnerCollection(Dictionary<Helpers.XmasModifier.Constants.PrezzieLocationTag, PresentSpawner> v)
        {
            return v;
        }*/
    }
}
