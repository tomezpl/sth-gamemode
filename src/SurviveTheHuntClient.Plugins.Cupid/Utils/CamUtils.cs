using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

namespace SurviveTheHuntClient.Plugins.Cupid.Utils
{
    internal static class CamUtils
    {
        internal static void Lerp(int camera, in float posAX, in float posAY, in float posAZ, in float rotAX, in float rotAY, in float rotAZ, in float fovA, in float posBX, in float posBY, in float posBZ, in float rotBX, in float rotBY, in float rotBZ, in float fovB, float amount)
        {
            SetCamCoord(camera, MathUtil.Lerp(posAX, posBX, amount), MathUtil.Lerp(posAY, posBY, amount), MathUtil.Lerp(posAZ, posBZ, amount));
            SetCamRot(camera, MathUtil.Lerp(rotAX, rotBX, amount), MathUtil.Lerp(rotAY, rotBY, amount), MathUtil.Lerp(rotAZ, rotBZ, amount), 2);
            SetCamFov(camera, MathUtil.Lerp(fovA, fovB, amount));
        }

        internal static void Lerp(int camera, in float posAX, in float posAY, in float posAZ, in float rotAX, in float rotAY, in float rotAZ, in float fovA, in float posBX, in float posBY, in float posBZ, in float rotBX, in float rotBY, in float rotBZ, in float fovB, float duration, float elapsed)
        {
            Lerp(camera, posAX, posAY, posAZ, rotAX, rotAY, rotAZ, fovA, posBX, posBY, posBZ, rotBX, rotBY, rotBZ, fovB, elapsed / duration);
        }
    }
}
