using CitizenFX.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CitizenFX.Core.Native.API;

namespace SurviveTheHuntClient
{
    public partial class MainScript
    {
        private struct PantoBlipCreationInfo
        {
            public int NetId;
            public bool Created;
            public ushort Retries;
        }

        private async void ApplyPantoBlips(string pantoNetworkIdString)
        {
            Debug.WriteLine("ApplyPantoBlips start");
            try
            {
                PantoBlipCreationInfo[] pantoBlipInfo = pantoNetworkIdString.Split(';').Select((netId) => new PantoBlipCreationInfo { NetId = int.Parse(netId), Created = false, Retries = 0 }).ToArray();
                bool canTerminate = true;
                for(int i = 0; i < pantoBlipInfo.Length; i++)
                {
                    if(i == 0)
                    {
                        canTerminate = true;
                    }

                    if (pantoBlipInfo[i].Created != true &&  pantoBlipInfo[i].Retries < ushort.MaxValue && NetworkDoesEntityExistWithNetworkId(pantoBlipInfo[i].NetId))
                    {
                        int handle = NetworkGetEntityFromNetworkId(pantoBlipInfo[i].NetId);
                        int blip = AddBlipForEntity(handle);
                        bool isHunter = PlayerState.Team == Teams.Team.Hunters;
                        SetBlipAsFriendly(blip, isHunter);
                        SetBlipSprite(blip, 535 + i);
                        SetBlipColour(blip, isHunter ? 3 : 59);
                        pantoBlipInfo[i].Created = true;
                    }
                    else if (pantoBlipInfo[i].Created != true && pantoBlipInfo[i].Retries < ushort.MaxValue)
                    {
                        await Delay(100);
                        pantoBlipInfo[i].Retries++;
                        canTerminate = false;
                    }
                    
                    if(canTerminate && i == pantoBlipInfo.Length - 1)
                    {
                        break;
                    }
                    else if(i == pantoBlipInfo.Length - 1)
                    {
                        i = -1;
                    }
                }

                Debug.WriteLine("Panto blips created");
            } catch (Exception ex)
            {
                Debug.WriteLine($"Failed to create Panto blips: {ex}");
            }
        }
    }
}
