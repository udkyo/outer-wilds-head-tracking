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
    ///
    /// Problem: Flashlight.FixedUpdate() reads camera rotation (_root.rotation, _root.up, _root.forward)
    /// continuously. When other patches temporarily modify camera rotation during their operations,
    /// the flashlight sees inconsistent rotations and flickers.
    ///
    /// Solution: Temporarily restore base rotation (remove head tracking) during FixedUpdate,
    /// so flashlight always reads consistent camera orientation without head tracking applied.
    /// </summary>
    public static class FlashlightPatch
    {
        private static CameraRotationHelper? _rotationHelper;

        public static void ApplyPatches(Harmony harmony)
        {
            var flashlightType = AccessTools.TypeByName("Flashlight");
            if (flashlightType == null)
            {
                return;
            }

            var fixedUpdateMethod = AccessTools.Method(flashlightType, "FixedUpdate");
            if (fixedUpdateMethod == null)
            {
                return;
            }

            var prefixMethod = AccessTools.Method(typeof(FlashlightPatch), nameof(FixedUpdate_Prefix));
            var postfixMethod = AccessTools.Method(typeof(FlashlightPatch), nameof(FixedUpdate_Postfix));

            if (prefixMethod == null || postfixMethod == null)
            {
                return;
            }

            harmony.Patch(fixedUpdateMethod,
                prefix: new HarmonyMethod(prefixMethod),
                postfix: new HarmonyMethod(postfixMethod));
        }

        public static void FixedUpdate_Prefix()
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
                // Silently handle errors
            }
        }

        public static void FixedUpdate_Postfix()
        {
            try
            {
                _rotationHelper?.Dispose();
                _rotationHelper = null;
            }
            catch (System.Exception)
            {
                // Silently handle errors
            }
        }
    }
}
