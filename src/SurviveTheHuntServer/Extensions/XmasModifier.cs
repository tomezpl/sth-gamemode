using CitizenFX.Core;
using System;
using System.Collections.Generic;
using static CitizenFX.Core.Native.API;

namespace SurviveTheHuntServer
{
    public partial class MainScript
    {
        internal static Models.XmasModifierState XmasModifierState = new Models.XmasModifierState();

        [EventHandler(SurviveTheHuntShared.Events.Server.XmasBroadcastPresentsLocations)]
        public void XmasBroadcastPresentsLocations(List<object> locationIndices)
        {
            // Reduce the ping radius when using the Pole-Rider.
            SetConvarReplicated("sth_huntedPingRadius", "80");

            Debug.WriteLine($"XmasBroadcastPresentsLocations: passed {locationIndices.Count} params");
            Debug.WriteLine($"Broadcasting {locationIndices.Count} prezzie locations: {locationIndices}");
            XmasModifierState.SetPresentLocations(locationIndices);
            TriggerClientEvent(SurviveTheHuntShared.Events.Client.XmasReceivePresentsLocations, locationIndices);
        }

        [EventHandler(SurviveTheHuntShared.Events.Server.XmasNotifyDeliveredPresent)]
        public void XmasReceiveDeliveredPresentIndex(int presentIndex)
        {
            Debug.WriteLine($"Marking present {presentIndex} as delivered.");
            XmasModifierState.MarkAsDelivered(XmasModifierState.PresentLocationIndices[presentIndex]);
            int[] delivered = XmasModifierState.GetDeliveredOrRemainingPresents(remaining: false);
            int[] remaining = XmasModifierState.GetDeliveredOrRemainingPresents(remaining: true);
            TriggerClientEvent(SurviveTheHuntShared.Events.Client.XmasReceiveDeliveryUpdate, delivered, remaining);

            // Restore the original ping radius after the game goes to the regular "survive" objective
            if(remaining.Length == 0)
            {
                SetConvarReplicated("sth_huntedPingRadius", "200");
            }
        }

        [EventHandler(SurviveTheHuntShared.Events.Server.XmasBroadcastHuntedCapturableState)]
        public void XmasBroadcastHuntedCapturableState([FromSource] Player player, bool isCapturable)
        {
            TriggerClientEvent(SurviveTheHuntShared.Events.Client.XmasReceiveCapturableUpdate, player.Handle, isCapturable);
        }

        [EventHandler(SurviveTheHuntShared.Events.Server.XmasBroadcastHuntedCaptured)]
        public void XmasBroadcastHuntedCaptured(int huntedServerId)
        {
            TriggerClientEvent(SurviveTheHuntShared.Events.Client.XmasReceiveHuntedCaptured, huntedServerId);
        }

        [EventHandler(SurviveTheHuntShared.Events.Server.XmasBroadcastSleighSpawn)]
        public void XmasBroadcastSleighSpawn(Int64 sleighNetIdsPair)
        {
            Debug.WriteLine($"{nameof(XmasBroadcastSleighSpawn)}({nameof(sleighNetIdsPair)}: {sleighNetIdsPair} (hex: {sleighNetIdsPair:X}))");
            TriggerClientEvent(SurviveTheHuntShared.Events.Client.XmasReceiveSleighSpawn, sleighNetIdsPair);
        }

        [EventHandler(SurviveTheHuntShared.Events.Server.XmasBroadcastSantaSpawn)]
        public void XmasBroadcastSantaSpawn(int santaSpawnIndex)
        {
            TriggerClientEvent(SurviveTheHuntShared.Events.Client.XmasReceiveSantaSpawn, santaSpawnIndex);
        }
    }

    namespace Models
    {
        internal class XmasModifierState
        {
            internal int[] PresentLocationIndices = { };

            internal List<int> DeliveredPresentIndices = new List<int>();

            internal void SetPresentLocations(List<object> indices)
            {
                int nbIndices = indices.Count;
                PresentLocationIndices = new int[nbIndices];

                for (int i = 0; i < nbIndices; i++)
                {
                    PresentLocationIndices[i] = (int)indices[i];
                }

                DeliveredPresentIndices = new List<int>(nbIndices);
            }

            internal void MarkAsDelivered(int deliveredPresentIndex)
            {
                DeliveredPresentIndices.Add(deliveredPresentIndex);
            }

            internal int[] GetDeliveredOrRemainingPresents(bool remaining = false)
            {
                List<int> delivered = new List<int>(PresentLocationIndices.Length);

                for (int i = 0; i < PresentLocationIndices.Length; i++)
                {
                    int locationIndex = PresentLocationIndices[i];
                    if (DeliveredPresentIndices.Contains(locationIndex) == !remaining)
                    {
                        delivered.Add(i);
                    }
                }

                return delivered.ToArray();
            }
        }
    }
}
