using CitizenFX.Core.Native;
using SurviveTheHuntShared.Utils;
using static CitizenFX.Core.Native.API;

namespace SurviveTheHuntClient.Helpers
{
    internal static class WeaponAttachments
    {
        /// <summary>
        /// Tries to apply an attachment to a weapon on the player ped.
        /// </summary>
        /// <param name="weaponHash">The weapon to apply the attachment to.</param>
        /// <param name="attachment">The attachment to apply.</param>
        /// <returns>True if attachment was applied, false otherwise.</returns>
        internal static bool ApplyAttachment(uint weaponHash, string attachment)
        {
            uint compatibleComponent = Weapons.IsAttachmentHashKey(attachment) ? (uint)GetHashKey(attachment) : 0;

            // If an attachment generic name/short-hand was provided instead of a full component name, look it up in the hard-coded dictionary
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
