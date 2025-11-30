extern alias UnityCoreModule;
using System;
using HarmonyLib;
using HeadTracking.Camera.Utilities;
using HeadTracking.Camera.Core;
using Quaternion = UnityCoreModule::UnityEngine.Quaternion;

namespace HeadTracking.Camera.Effects
{
    /// <summary>
    /// Patches VisibilityObject.Update to ensure quantum objects check visibility against head-tracked rotation.
    /// </summary>
    public static class QuantumVisibilityPatch
    {
        private static CameraRotationHelper? _rotationHelper;

        public static void ApplyPatches(Harmony harmony)
        {
            var visibilityObjectType = AccessTools.TypeByName("VisibilityObject");
            if (visibilityObjectType == null)
            {
                throw new InvalidOperationException("Could not find VisibilityObject type!");
            }

            var updateMethod = AccessTools.Method(visibilityObjectType, "Update");
            if (updateMethod == null)
            {
                throw new InvalidOperationException("Could not find VisibilityObject.Update method!");
            }

            var prefixMethod = AccessTools.Method(typeof(QuantumVisibilityPatch), nameof(VisibilityObject_Update_Prefix));
            var postfixMethod = AccessTools.Method(typeof(QuantumVisibilityPatch), nameof(VisibilityObject_Update_Postfix));

            if (prefixMethod == null || postfixMethod == null)
            {
                throw new InvalidOperationException("Could not find QuantumVisibilityPatch prefix/postfix methods!");
            }

            harmony.Patch(updateMethod,
                prefix: new HarmonyMethod(prefixMethod),
                postfix: new HarmonyMethod(postfixMethod));
        }

        public static void VisibilityObject_Update_Prefix()
        {
            var mod = HeadTrackingMod.Instance;
            if (mod == null || !mod.IsTrackingEnabled()) return;

            var cameraTransform = SimpleCameraPatch._cameraTransform;
            var headTracking = SimpleCameraPatch._lastHeadTrackingRotation;
            if (headTracking == Quaternion.identity) return;

            var baseRotation = SimpleCameraPatch._baseRotationBeforeHeadTracking;
            _rotationHelper = CameraRotationHelper.ApplyBaseRotation(cameraTransform, baseRotation, headTracking);
        }

        public static void VisibilityObject_Update_Postfix()
        {
            _rotationHelper?.Dispose();
            _rotationHelper = null;
        }
    }
}
