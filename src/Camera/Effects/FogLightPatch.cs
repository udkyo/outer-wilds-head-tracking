extern alias UnityCoreModule;
using System;
using HarmonyLib;
using HeadTracking.Camera.Utilities;
using HeadTracking.Camera.Core;
using Quaternion = UnityCoreModule::UnityEngine.Quaternion;

namespace HeadTracking.Camera.Effects
{
    /// <summary>
    /// Patches FogLight.UpdateFogLight to remove head tracking during WorldToScreenPoint calculations.
    /// </summary>
    public static class FogLightPatch
    {
        private static CameraRotationHelper? _rotationHelper;

        public static void ApplyPatches(Harmony harmony)
        {
            var fogLightType = AccessTools.TypeByName("FogLight");
            if (fogLightType == null)
            {
                throw new InvalidOperationException("Could not find FogLight type!");
            }

            var updateFogLightMethod = AccessTools.Method(fogLightType, "UpdateFogLight");
            if (updateFogLightMethod == null)
            {
                throw new InvalidOperationException("Could not find FogLight.UpdateFogLight method!");
            }

            var prefixMethod = AccessTools.Method(typeof(FogLightPatch), nameof(UpdateFogLight_Prefix));
            var postfixMethod = AccessTools.Method(typeof(FogLightPatch), nameof(UpdateFogLight_Postfix));

            if (prefixMethod == null || postfixMethod == null)
            {
                throw new InvalidOperationException("Could not find FogLightPatch prefix/postfix methods!");
            }

            harmony.Patch(updateFogLightMethod,
                prefix: new HarmonyMethod(prefixMethod),
                postfix: new HarmonyMethod(postfixMethod));
        }

        public static void UpdateFogLight_Prefix()
        {
            var mod = HeadTrackingMod.Instance;
            if (mod == null || !mod.IsTrackingEnabled()) return;

            var cameraTransform = SimpleCameraPatch._cameraTransform;
            var headTracking = SimpleCameraPatch._lastHeadTrackingRotation;
            if (headTracking == Quaternion.identity) return;

            var baseRotation = SimpleCameraPatch._baseRotationBeforeHeadTracking;
            _rotationHelper = CameraRotationHelper.ApplyBaseRotation(cameraTransform, baseRotation, headTracking);
        }

        public static void UpdateFogLight_Postfix()
        {
            _rotationHelper?.Dispose();
            _rotationHelper = null;
        }
    }
}
