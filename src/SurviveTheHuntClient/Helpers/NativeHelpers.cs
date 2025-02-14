using SurviveTheHuntShared.Utils;
using static CitizenFX.Core.Native.API;

namespace SurviveTheHuntClient.Helpers
{
    internal static class NativeHelpers
    {
        internal static void GivePedWeapon(int pedHandle, Weapons.WeaponAmmo weapon, bool equip = false)
        {
            GiveWeaponToPed(pedHandle, weapon.Hash, weapon.Ammo, false, equip);
            foreach(string attachment in weapon.Attachments)
            {
                WeaponAttachments.ApplyAttachment(weapon.Hash, attachment);
            }
        }
    }
}
