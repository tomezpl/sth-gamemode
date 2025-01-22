using CitizenFX.Core.Native;
using SurviveTheHuntShared.Utils;
using static CitizenFX.Core.Native.API;

namespace SurviveTheHuntClient.Helpers
{
    internal static class WeaponAttachments
    {
        internal static bool ApplyAttachment(uint weaponHash, string attachment)
        {
            uint compatibleComponent = Weapons.IsAttachmentHashKey(attachment) ? (uint)GetHashKey(attachment) : 0;

            if(compatibleComponent == 0)
            {
                if(Weapons.AttachmentNames.TryGetValue(attachment, out string[] potentialComponents))
                {
                    foreach(string componentName in potentialComponents)
                    {
                        uint compHash = (uint)GetHashKey(componentName);
                        if(DoesWeaponTakeWeaponComponent(weaponHash, compHash))
                        {
                            compatibleComponent = compHash;
                            break;
                        }
                    }
                }
            }

            if(compatibleComponent != 0 && DoesWeaponTakeWeaponComponent(weaponHash, compatibleComponent))
            {
                GiveWeaponComponentToPed(PlayerPedId(), weaponHash, compatibleComponent);
                return true;
            }

            return false;
        }
    }
}
