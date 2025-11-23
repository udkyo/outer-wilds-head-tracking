extern alias UnityCoreModule;
using System;
using HarmonyLib;
using HeadTracking.Configuration;
using HeadTracking.Camera.Utilities;
using HeadTracking.Camera.Core;
using HeadTracking.Camera.Effects;
using HeadTracking.Common.Logging;
using Quaternion = UnityCoreModule::UnityEngine.Quaternion;
using Vector3 = UnityCoreModule::UnityEngine.Vector3;

namespace HeadTracking.Camera.UI
{
    /// <summary>
    /// Patches OWExtensions.WorldToCanvasPosition and OffScreenIndicator to use base rotation for screen position calculations
    /// Must be applied manually since OWExtensions type isn't directly accessible
    /// </summary>
    public static class MapMarkerPatch
    {
        // State for WorldToCanvasPosition patch
        private static Quaternion _savedRotation = Quaternion.identity;
        private static bool _rotationWasModified = false;
        private static int _nestedCallCount = 0;
        private static int _lastModifiedFrame = -1;
        private static UnityCoreModule::UnityEngine.Transform? _modifiedTransform = null;

        // State for OffScreenIndicator patch
        private static CameraRotationHelper? _offscreenRotationHelper;

        public static int _lastDrainedFrame = -1;
        public static int _headTrackingAppliedFrame = -1;
        public static bool _cameraHasHeadTracking = false;
        public static int _lastFrameReset = -1;

        public static void ApplyPatches(Harmony harmony)
        {
            var owExtensionsType = AccessTools.TypeByName("OWExtensions");
            if (owExtensionsType == null)
            {
                ModLogger.LogError("Could not find OWExtensions type!");
                return;
            }

            var targetMethod = FindWorldToCanvasPositionMethod(owExtensionsType);
            if (targetMethod == null)
            {
                ModLogger.LogError("Could not find WorldToCanvasPosition method!");
                return;
            }

            var prefixMethod = AccessTools.Method(typeof(MapMarkerPatch), nameof(WorldToCanvasPosition_Prefix));
            var postfixMethod = AccessTools.Method(typeof(MapMarkerPatch), nameof(WorldToCanvasPosition_Postfix));

            if (prefixMethod == null || postfixMethod == null)
            {
                ModLogger.LogError("Could not find patch methods!");
                return;
            }

            harmony.Patch(targetMethod,
                prefix: new HarmonyMethod(prefixMethod),
                postfix: new HarmonyMethod(postfixMethod));

            // Patch OffScreenIndicator.SetCanvasPosition for off-screen markers (especially in Dark Bramble)
            var offScreenIndicatorType = AccessTools.TypeByName("OffScreenIndicator");
            if (offScreenIndicatorType != null)
            {
                var setCanvasPositionMethod = AccessTools.Method(offScreenIndicatorType, "SetCanvasPosition", new Type[] { typeof(Vector3) });
                if (setCanvasPositionMethod != null)
                {
                    var offscreenPrefixMethod = AccessTools.Method(typeof(MapMarkerPatch), nameof(OffScreenIndicator_SetCanvasPosition_Prefix));
                    var offscreenPostfixMethod = AccessTools.Method(typeof(MapMarkerPatch), nameof(OffScreenIndicator_SetCanvasPosition_Postfix));

                    if (offscreenPrefixMethod != null && offscreenPostfixMethod != null)
                    {
                        harmony.Patch(setCanvasPositionMethod,
                            prefix: new HarmonyMethod(offscreenPrefixMethod),
                            postfix: new HarmonyMethod(offscreenPostfixMethod));
                    }
                }
            }

            NomaiTranslatorPatches.ApplyPatches(harmony);
            SignalscopePatches.ApplyPatches(harmony);
            FogLightPatch.ApplyPatches(harmony);
            QuantumVisibilityPatch.ApplyPatches(harmony);
            FlashlightPatch.ApplyPatches(harmony);
        }

        private static System.Reflection.MethodInfo? FindWorldToCanvasPositionMethod(Type owExtensionsType)
        {
            var methods = owExtensionsType.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

            foreach (var method in methods)
            {
                if (method.Name == "WorldToCanvasPosition")
                {
                    var parameters = method.GetParameters();
                    if (parameters.Length == 3 &&
                        parameters[0].ParameterType.Name == "Canvas" &&
                        parameters[1].ParameterType.Name == "Camera")
                    {
                        return method;
                    }
                }
            }

            return null;
        }

        private static void EnsureFrameReset()
        {
            int currentFrame = UnityCoreModule::UnityEngine.Time.frameCount;
            if (_lastFrameReset != currentFrame)
            {
                _cameraHasHeadTracking = false;
                _lastFrameReset = currentFrame;
            }
        }

        public static void WorldToCanvasPosition_Prefix(object canvas, UnityCoreModule::UnityEngine.Camera camera, Vector3 worldPosition)
        {
            try
            {
                // Defensive frame reset: If we're in a new frame and still have modified state, reset it
                int currentFrame = UnityCoreModule::UnityEngine.Time.frameCount;
                if (_lastModifiedFrame != -1 && _lastModifiedFrame != currentFrame && _rotationWasModified)
                {
                    // Stuck state from previous frame - force reset
                    if (_modifiedTransform != null)
                    {
                        _modifiedTransform.localRotation = _savedRotation;
                    }
                    _rotationWasModified = false;
                    _nestedCallCount = 0;
                    _modifiedTransform = null;
                }

                var mod = HeadTrackingMod.Instance;
                if (mod == null || !mod.IsTrackingEnabled()) return;

                var cameraTransform = SimpleCameraPatch._cameraTransform;
                if (cameraTransform == null || camera.transform != cameraTransform) return;

                _nestedCallCount++;
                if (_nestedCallCount > 1)
                {
                    return;
                }

                var headTracking = SimpleCameraPatch._lastHeadTrackingRotation;
                if (headTracking == Quaternion.identity) return;

                var baseRotation = SimpleCameraPatch._baseRotationBeforeHeadTracking;
                if (baseRotation == default || baseRotation == Quaternion.identity) return;

                // CRITICAL: Always save the CURRENT rotation before we modify it
                // This ensures we restore the exact state regardless of previous calls
                _savedRotation = cameraTransform.localRotation;
                _rotationWasModified = true;
                _lastModifiedFrame = currentFrame;
                _modifiedTransform = cameraTransform;

                Quaternion headTrackedWorld = baseRotation * headTracking;
                if (cameraTransform.parent != null)
                {
                    cameraTransform.localRotation = Quaternion.Inverse(cameraTransform.parent.rotation) * headTrackedWorld;
                }
                else
                {
                    cameraTransform.localRotation = headTrackedWorld;
                }
            }
            catch (System.Exception)
            {
                // Silently handle errors to avoid log spam
            }
        }

        public static void WorldToCanvasPosition_Postfix()
        {
            try
            {
                if (_nestedCallCount > 0)
                {
                    _nestedCallCount--;
                }

                // Only restore rotation when exiting the outermost call
                if (_nestedCallCount > 0) return;
                if (!_rotationWasModified) return;

                // Use the stored transform reference to ensure we restore the right transform
                if (_modifiedTransform != null)
                {
                    // CRITICAL: Always restore saved rotation, even if it's identity
                    // The saved rotation is whatever the camera had before we modified it
                    _modifiedTransform.localRotation = _savedRotation;
                }

                // Reset state for next call
                _rotationWasModified = false;
                _savedRotation = Quaternion.identity;
                _modifiedTransform = null;
            }
            catch (System.Exception)
            {
                // Silently handle errors to avoid log spam
            }
        }

        /// <summary>
        /// Patch OffScreenIndicator.SetCanvasPosition to remove head tracking during direction calculation.
        /// This fixes off-screen markers (especially in Dark Bramble) following head movement.
        /// OffScreenIndicator directly uses activeCamera.transform.InverseTransformDirection(), bypassing WorldToCanvasPosition.
        /// </summary>
        public static void OffScreenIndicator_SetCanvasPosition_Prefix()
        {
            try
            {
                var mod = HeadTrackingMod.Instance;
                if (mod == null || !mod.IsTrackingEnabled()) return;

                var cameraTransform = SimpleCameraPatch._cameraTransform;
                var headTracking = SimpleCameraPatch._lastHeadTrackingRotation;
                if (headTracking == Quaternion.identity) return;

                var baseRotation = SimpleCameraPatch._baseRotationBeforeHeadTracking;
                _offscreenRotationHelper = CameraRotationHelper.ApplyBaseRotation(cameraTransform, baseRotation, headTracking);
            }
            catch (System.Exception)
            {
                // Silently handle errors - reflection-based patching may fail if game updates
            }
        }

        public static void OffScreenIndicator_SetCanvasPosition_Postfix()
        {
            try
            {
                _offscreenRotationHelper?.Dispose();
                _offscreenRotationHelper = null;
            }
            catch (System.Exception)
            {
                // Silently handle errors - don't crash if rotation restore fails
            }
        }

    }
}
