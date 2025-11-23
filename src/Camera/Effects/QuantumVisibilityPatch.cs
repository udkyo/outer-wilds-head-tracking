extern alias UnityCoreModule;
using System;
using HarmonyLib;
using HeadTracking.Camera.Utilities;
using HeadTracking.Camera.Core;
using Quaternion = UnityCoreModule::UnityEngine.Quaternion;

namespace HeadTracking.Camera.Effects
{
    /// <summary>
    /// Patches VisibilityObject.Update to ensure quantum objects check visibility
    /// against the camera's head-tracked rotation.
    ///
    /// Problem: VisibilityObject.Update() may run before SimpleCameraPatch applies
    /// head tracking, causing quantum objects to disappear when viewed with head
    /// turned off-center (they check visibility against base rotation, not actual
    /// camera rotation with head tracking).
    ///
    /// Solution: Temporarily apply head tracking during visibility check, then restore.
    /// </summary>
    public static class QuantumVisibilityPatch
    {
        private static CameraRotationHelper? _rotationHelper;

        public static void ApplyPatches(Harmony harmony)
        {
            var visibilityObjectType = AccessTools.TypeByName("VisibilityObject");
            if (visibilityObjectType == null)
            {
                return;
            }

            var updateMethod = AccessTools.Method(visibilityObjectType, "Update");
            if (updateMethod == null)
            {
                return;
            }

            var prefixMethod = AccessTools.Method(typeof(QuantumVisibilityPatch), nameof(VisibilityObject_Update_Prefix));
            var postfixMethod = AccessTools.Method(typeof(QuantumVisibilityPatch), nameof(VisibilityObject_Update_Postfix));

            if (prefixMethod == null || postfixMethod == null)
            {
                return;
            }

            harmony.Patch(updateMethod,
                prefix: new HarmonyMethod(prefixMethod),
                postfix: new HarmonyMethod(postfixMethod));
        }

        public static void VisibilityObject_Update_Prefix()
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
                // Silently handle errors - reflection may fail if game updates
            }
        }

        public static void VisibilityObject_Update_Postfix()
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
