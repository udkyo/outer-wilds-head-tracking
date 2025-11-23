extern alias UnityCoreModule;
using System;
using HarmonyLib;
using HeadTracking.Camera.Utilities;
using HeadTracking.Camera.Core;
using Quaternion = UnityCoreModule::UnityEngine.Quaternion;
using Vector3 = UnityCoreModule::UnityEngine.Vector3;

namespace HeadTracking.Camera.UI
{
    /// <summary>
    /// Patches for Signalscope tool - ensures signal detection uses head direction
    /// </summary>
    public static class SignalscopePatches
    {
        private static Quaternion _signalscopeSavedRotation = Quaternion.identity;
        private static bool _signalscopeRotationModified = false;

        public static void ApplyPatches(Harmony harmony)
        {
            var signalscopeType = AccessTools.TypeByName("Signalscope");
            if (signalscopeType == null)
            {
                return;
            }

            PatchSignalscopeUpdate(harmony, signalscopeType);
            PatchGetScopeDirection(harmony, signalscopeType);
        }

        private static void PatchSignalscopeUpdate(Harmony harmony, Type signalscopeType)
        {
            var signalscopeUpdateMethod = AccessTools.Method(signalscopeType, "Update");
            if (signalscopeUpdateMethod != null)
            {
                var signalscopePrefix = new HarmonyMethod(AccessTools.Method(typeof(SignalscopePatches), nameof(Signalscope_Update_Prefix)));
                var signalscopePostfix = new HarmonyMethod(AccessTools.Method(typeof(SignalscopePatches), nameof(Signalscope_Update_Postfix)));
                harmony.Patch(signalscopeUpdateMethod, prefix: signalscopePrefix, postfix: signalscopePostfix);
            }
        }

        private static void PatchGetScopeDirection(Harmony harmony, Type signalscopeType)
        {
            var getScopeDirectionMethod = AccessTools.Method(signalscopeType, "GetScopeDirection");
            if (getScopeDirectionMethod != null)
            {
                var scopeDirPostfix = new HarmonyMethod(AccessTools.Method(typeof(SignalscopePatches), nameof(Signalscope_GetScopeDirection_Postfix)));
                harmony.Patch(getScopeDirectionMethod, postfix: scopeDirPostfix);
            }
        }

        public static void Signalscope_Update_Prefix()
        {
            try
            {
                var mod = HeadTrackingMod.Instance;
                if (mod == null || !mod.IsTrackingEnabled()) return;

                var cameraTransform = SimpleCameraPatch._cameraTransform;
                if (cameraTransform == null) return;

                EnsureFrameReset();

                if (MapMarkerPatch._cameraHasHeadTracking) return;

                var headTracking = SimpleCameraPatch._lastHeadTrackingRotation;
                if (headTracking == Quaternion.identity) return;

                _signalscopeSavedRotation = cameraTransform.localRotation;
                cameraTransform.localRotation = _signalscopeSavedRotation * headTracking;
                _signalscopeRotationModified = true;
                MapMarkerPatch._cameraHasHeadTracking = true;
            }
            catch (System.Exception)
            {
                // Silently handle errors - reflection-based patching may fail if game updates
            }
        }

        public static void Signalscope_Update_Postfix()
        {
            try
            {
                if (!_signalscopeRotationModified) return;

                var cameraTransform = SimpleCameraPatch._cameraTransform;
                if (cameraTransform == null) return;

                cameraTransform.localRotation = _signalscopeSavedRotation;
                _signalscopeSavedRotation = Quaternion.identity;
                _signalscopeRotationModified = false;
                MapMarkerPatch._cameraHasHeadTracking = false;
            }
            catch (System.Exception)
            {
                // Silently handle errors - don't crash if rotation restore fails
            }
        }

        public static void Signalscope_GetScopeDirection_Postfix(ref Vector3 __result)
        {
            try
            {
                var mod = HeadTrackingMod.Instance;
                if (mod == null || !mod.IsTrackingEnabled()) return;

                var cameraTransform = SimpleCameraPatch._cameraTransform;
                if (cameraTransform == null) return;

                var baseRotation = SimpleCameraPatch._baseRotationBeforeHeadTracking;
                if (baseRotation == default || baseRotation == Quaternion.identity)
                {
                    return;
                }

                var headTracking = SimpleCameraPatch._lastHeadTrackingRotation;
                if (headTracking == Quaternion.identity)
                {
                    __result = baseRotation * Vector3.forward;
                }
                else
                {
                    __result = (baseRotation * headTracking) * Vector3.forward;
                }
            }
            catch (System.Exception)
            {
                // Silently handle errors - don't crash signalscope if direction calculation fails
            }
        }

        private static void EnsureFrameReset()
        {
            int currentFrame = UnityCoreModule::UnityEngine.Time.frameCount;
            if (MapMarkerPatch._lastFrameReset != currentFrame)
            {
                // Only reset if SimpleCameraPatch hasn't already applied head tracking this frame
                if (MapMarkerPatch._headTrackingAppliedFrame != currentFrame)
                {
                    MapMarkerPatch._cameraHasHeadTracking = false;
                }
                MapMarkerPatch._lastFrameReset = currentFrame;
            }
        }

    }
}
