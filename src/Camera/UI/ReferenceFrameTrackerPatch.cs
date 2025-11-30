extern alias UnityCoreModule;
using System;
using HarmonyLib;
using HeadTracking.Configuration;
using HeadTracking.Camera.Core;
using Quaternion = UnityCoreModule::UnityEngine.Quaternion;

namespace HeadTracking.Camera.UI
{
    /// <summary>
    /// Patches ReferenceFrameTracker to use BASE rotation (reticle direction) for targeting raycasts.
    /// Without this patch, LOOK markers flicker because the raycast uses HEAD rotation,
    /// causing targets to be found/lost as the player moves their head.
    /// </summary>
    public static class ReferenceFrameTrackerPatch
    {
        private static bool _rotationModified = false;

        public static void ApplyPatches(Harmony harmony)
        {
            var trackerType = AccessTools.TypeByName("ReferenceFrameTracker");
            if (trackerType == null)
            {
                throw new InvalidOperationException("Could not find ReferenceFrameTracker type!");
            }

            // Patch FindReferenceFrameInLineOfSight
            var findInLineOfSightMethod = AccessTools.Method(trackerType, "FindReferenceFrameInLineOfSight");
            if (findInLineOfSightMethod != null)
            {
                var prefix = AccessTools.Method(typeof(ReferenceFrameTrackerPatch), nameof(FindReferenceFrame_Prefix));
                var postfix = AccessTools.Method(typeof(ReferenceFrameTrackerPatch), nameof(FindReferenceFrame_Postfix));
                harmony.Patch(findInLineOfSightMethod,
                    prefix: new HarmonyMethod(prefix),
                    postfix: new HarmonyMethod(postfix));
            }

            // Patch FindReferenceFrameInMapView
            var findInMapViewMethod = AccessTools.Method(trackerType, "FindReferenceFrameInMapView");
            if (findInMapViewMethod != null)
            {
                var prefix = AccessTools.Method(typeof(ReferenceFrameTrackerPatch), nameof(FindReferenceFrame_Prefix));
                var postfix = AccessTools.Method(typeof(ReferenceFrameTrackerPatch), nameof(FindReferenceFrame_Postfix));
                harmony.Patch(findInMapViewMethod,
                    prefix: new HarmonyMethod(prefix),
                    postfix: new HarmonyMethod(postfix));
            }
        }

        /// <summary>
        /// Before raycast methods, temporarily set camera to BASE rotation so raycasts use reticle direction.
        /// </summary>
        public static void FindReferenceFrame_Prefix()
        {
            var mod = HeadTrackingMod.Instance;
            if (mod == null || !mod.IsTrackingEnabled()) return;

            var cameraTransform = SimpleCameraPatch._cameraTransform;
            if (cameraTransform == null) return;

            var baseRotation = SimpleCameraPatch._baseRotationBeforeHeadTracking;
            if (baseRotation == default || baseRotation == Quaternion.identity) return;

            var headTracking = SimpleCameraPatch._lastHeadTrackingRotation;
            if (headTracking == Quaternion.identity) return;

            _rotationModified = true;

            // Temporarily set camera to BASE rotation for the raycast
            if (cameraTransform.parent != null)
            {
                cameraTransform.localRotation = Quaternion.Inverse(cameraTransform.parent.rotation) * baseRotation;
            }
            else
            {
                cameraTransform.localRotation = baseRotation;
            }
        }

        /// <summary>
        /// After raycast methods, restore camera to HEAD rotation.
        /// </summary>
        public static void FindReferenceFrame_Postfix()
        {
            if (!_rotationModified) return;
            _rotationModified = false;

            var mod = HeadTrackingMod.Instance;
            if (mod == null || !mod.IsTrackingEnabled()) return;

            var cameraTransform = SimpleCameraPatch._cameraTransform;
            if (cameraTransform == null) return;

            var baseRotation = SimpleCameraPatch._baseRotationBeforeHeadTracking;
            var headTracking = SimpleCameraPatch._lastHeadTrackingRotation;

            if (baseRotation == default || baseRotation == Quaternion.identity) return;
            if (headTracking == Quaternion.identity) return;

            // Restore camera to HEAD rotation (base * headTracking)
            Quaternion headRotationWorld = baseRotation * headTracking;
            if (cameraTransform.parent != null)
            {
                cameraTransform.localRotation = Quaternion.Inverse(cameraTransform.parent.rotation) * headRotationWorld;
            }
            else
            {
                cameraTransform.localRotation = headRotationWorld;
            }
        }
    }
}
