extern alias UnityCoreModule;
using System;
using HarmonyLib;
using HeadTracking.Camera.Utilities;
using HeadTracking.Camera.Core;
using Quaternion = UnityCoreModule::UnityEngine.Quaternion;

namespace HeadTracking.Camera.Effects
{
    /// <summary>
    /// Patches Flashlight to prevent flickering when head tracking is active.
    /// </summary>
    public static class FlashlightPatch
    {
        private static CameraRotationHelper? _rotationHelper;

        public static void ApplyPatches(Harmony harmony)
        {
            var flashlightType = AccessTools.TypeByName("Flashlight");
            if (flashlightType == null)
            {
                throw new InvalidOperationException("Could not find Flashlight type!");
            }

            var fixedUpdateMethod = AccessTools.Method(flashlightType, "FixedUpdate");
            if (fixedUpdateMethod == null)
            {
                throw new InvalidOperationException("Could not find Flashlight.FixedUpdate method!");
            }

            var prefixMethod = AccessTools.Method(typeof(FlashlightPatch), nameof(FixedUpdate_Prefix));
            var postfixMethod = AccessTools.Method(typeof(FlashlightPatch), nameof(FixedUpdate_Postfix));

            if (prefixMethod == null || postfixMethod == null)
            {
                throw new InvalidOperationException("Could not find FlashlightPatch prefix/postfix methods!");
            }

            harmony.Patch(fixedUpdateMethod,
                prefix: new HarmonyMethod(prefixMethod),
                postfix: new HarmonyMethod(postfixMethod));
        }

        public static void FixedUpdate_Prefix()
        {
            var mod = HeadTrackingMod.Instance;
            if (mod == null || !mod.IsTrackingEnabled()) return;

            var cameraTransform = SimpleCameraPatch._cameraTransform;
            var headTracking = SimpleCameraPatch._lastHeadTrackingRotation;
            if (headTracking == Quaternion.identity) return;

            var baseRotation = SimpleCameraPatch._baseRotationBeforeHeadTracking;
            _rotationHelper = CameraRotationHelper.ApplyBaseRotation(cameraTransform, baseRotation, headTracking);
        }

        public static void FixedUpdate_Postfix()
        {
            _rotationHelper?.Dispose();
            _rotationHelper = null;
        }
    }
}
