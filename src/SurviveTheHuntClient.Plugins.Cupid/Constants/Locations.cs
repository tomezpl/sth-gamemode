using CitizenFX.Core;

namespace SurviveTheHuntClient.Plugins.Cupid
{
    internal static partial class Constants
    {
        internal static class Location
        {
            internal static class RobberyJob
            {
                internal static Vector3 FleecaLegion => new Vector3(152.26f, -1034.67f, 29.15f);

                internal static Vector3 FleecaRockford => new Vector3(-1216.75f, -322.44f, 36.3f);
                internal static Vector3 FleecaRockfordObjective => new Vector3(-1210.809f, -336.4966f, 36.38103f);

                internal static Vector3 FleecaChumash => new Vector3(-2967.11f, 483f, 14.08f);
            }

            internal static class CarRobberyJob
            {
                internal static Vector3 Elysian = new Vector3(636.07f, -2661.24f, 48.73f);
                internal const float ElysianHeading = -79.67f;

                internal static Vector3 DelPerro = new Vector3(-756.39f, -503.48f, 27.32f);
                internal const float DelPerroHeading = 93.84f;

                internal const float DeliveryPosX = 1169.435302734375f, DeliveryPosY = -2970.55078125f, DeliveryPosZ = 5.15926456451416f;
                internal static Vector3 DeliveryPos = new Vector3(DeliveryPosX, DeliveryPosY, DeliveryPosZ);
                internal const float DeliveryRadius = 9f;
            }
        }
    }
}
