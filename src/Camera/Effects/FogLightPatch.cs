extern alias UnityCoreModule;
using System;
using HarmonyLib;
using HeadTracking.Camera.Utilities;
using HeadTracking.Camera.Core;
using Quaternion = UnityCoreModule::UnityEngine.Quaternion;
using Vector3 = UnityCoreModule::UnityEngine.Vector3;

namespace HeadTracking.Camera.Effects
{
    /// <summary>
    /// Patches FogLight.UpdateFogLight to remove head tracking during WorldToScreenPoint calculations.
    /// This fixes Dark Bramble fog lights (anglerfish, eggs) from following head movement.
    /// FogLight directly calls Camera.WorldToScreenPoint(), bypassing WorldToCanvasPosition.
    /// </summary>
    public static class FogLightPatch
    {
        private static CameraRotationHelper? _rotationHelper;

        public static void ApplyPatches(Harmony harmony)
        {
            var fogLightType = AccessTools.TypeByName("FogLight");
            if (fogLightType == null)
            {
                return;
            }

            var updateFogLightMethod = AccessTools.Method(fogLightType, "UpdateFogLight");
            if (updateFogLightMethod == null)
            {
                return;
            }

            var prefixMethod = AccessTools.Method(typeof(FogLightPatch), nameof(UpdateFogLight_Prefix));
            var postfixMethod = AccessTools.Method(typeof(FogLightPatch), nameof(UpdateFogLight_Postfix));

            if (prefixMethod == null || postfixMethod == null)
            {
                return;
            }

            harmony.Patch(updateFogLightMethod,
                prefix: new HarmonyMethod(prefixMethod),
                postfix: new HarmonyMethod(postfixMethod));
        }

        public static void UpdateFogLight_Prefix()
        {
            try
            {
                var mod = HeadTrackingMod.Instance;
                if (mod == null || !mod.IsTrackingEnabled()) return;

                var cameraTransform = SimpleCameraPatch._cameraTransform;
                var headTracking = SimpleCameraPatch._lastHeadTrackingRotation;
                if (headTracking == Quaternion.identity) return;

                var baseRotation = SimpleCameraPatch._baseRotationBeforeHeadTracking;
                _rotationHelper = CameraRotationHelper.ApplyBaseRotation(cameraTransform, baseRotation, headTracking);
            }
            catch (System.Exception)
            {
                // Silently handle errors - reflection-based patching may fail if game updates
            }
        }

        public static void UpdateFogLight_Postfix()
        {
            try
            {
                _rotationHelper?.Dispose();
                _rotationHelper = null;
            }
            catch (System.Exception)
            {
                // Silently handle errors - don't crash if rotation restore fails
            }
        }
    }
}
