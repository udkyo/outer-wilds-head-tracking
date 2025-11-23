extern alias UnityCoreModule;
using System;
using HarmonyLib;
using HeadTracking.Configuration;
using HeadTracking.Tracking;
using HeadTracking.Camera.Utilities;
using HeadTracking.Camera.UI;
using Quaternion = UnityCoreModule::UnityEngine.Quaternion;
using Vector3 = UnityCoreModule::UnityEngine.Vector3;

namespace HeadTracking.Camera.Core
{
    /// <summary>
    /// Core camera patch that applies head tracking rotation to the player camera
    /// </summary>
    [HarmonyPatch(typeof(PlayerCameraController))]
    public class SimpleCameraPatch
    {
        public static float _centerYaw = 0f;
        public static float _centerPitch = 0f;
        public static float _centerRoll = 0f;
        private static bool _centerSet = false;
        private static int _framesWithoutData = 0;

        public static Quaternion _lastHeadTrackingRotation = Quaternion.identity;
        private static Quaternion _lastGameRotation = Quaternion.identity;
        public static Quaternion _baseRotationBeforeHeadTracking = Quaternion.identity;
        public static UnityCoreModule::UnityEngine.Transform? _cameraTransform = null;

        private static float _lastGameDegreesX = 0f;
        private static float _lastGameDegreesY = 0f;
        private static float _gameCameraChangeSpeed = 0f;

        private static System.Reflection.FieldInfo? _degreesXField;
        private static System.Reflection.FieldInfo? _degreesYField;

        private static float _cachedYawOffset = 0f;
        private static float _cachedPitchOffset = 0f;
        private static float _cachedRollOffset = 0f;
        private static float _cachedHeadTrackingInfluence = 1f;
        private static int _lastRotationCalcFrame = -1;

        public static void RecenterTracking()
        {
            _centerSet = false;
        }

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        public static void Update_Postfix(PlayerCameraController __instance)
        {
            try
            {
                var cameraTransform = __instance.transform;
                if (cameraTransform == null) return;

                _cameraTransform = cameraTransform;

                if (_degreesXField == null || _degreesYField == null)
                {
                    _degreesXField = AccessTools.Field(typeof(PlayerCameraController), "_degreesX");
                    _degreesYField = AccessTools.Field(typeof(PlayerCameraController), "_degreesY");

                    if (_degreesXField == null || _degreesYField == null)
                    {
                        return;
                    }
                }

                float gameDegreesX = (float)_degreesXField.GetValue(__instance);
                float gameDegreesY = (float)_degreesYField.GetValue(__instance);

                float deltaX = gameDegreesX - _lastGameDegreesX;
                float deltaY = gameDegreesY - _lastGameDegreesY;
                _gameCameraChangeSpeed = UnityCoreModule::UnityEngine.Mathf.Sqrt(deltaX * deltaX + deltaY * deltaY);
                _lastGameDegreesX = gameDegreesX;
                _lastGameDegreesY = gameDegreesY;

                var gameWantedRotation = Quaternion.Euler(-gameDegreesY, gameDegreesX, 0f);

                _baseRotationBeforeHeadTracking = cameraTransform.parent != null
                    ? cameraTransform.parent.rotation * gameWantedRotation
                    : gameWantedRotation;

                var mod = HeadTrackingMod.Instance;
                if (mod == null || !mod.IsTrackingEnabled())
                {
                    _centerSet = false;
                    _lastHeadTrackingRotation = Quaternion.identity;
                    return;
                }

                var trackingClient = mod.GetTrackingClient();
                if (trackingClient == null)
                {
                    _lastHeadTrackingRotation = Quaternion.identity;
                    return;
                }

                int currentFrame = UnityCoreModule::UnityEngine.Time.frameCount;
                if (MapMarkerPatch._lastDrainedFrame != currentFrame)
                {
                    trackingClient.GetRawEulerAngles();
                    MapMarkerPatch._lastDrainedFrame = currentFrame;
                }

                var rawAngles = trackingClient.PeekRawEulerAngles();

                HandleTrackingLoss(rawAngles, mod);

                if (!_centerSet)
                {
                    if (!rawAngles.IsValid)
                    {
                        _lastHeadTrackingRotation = Quaternion.identity;
                        return;
                    }

                    SetCenter(rawAngles, mod);
                    return;
                }

                ApplyHeadTracking(cameraTransform, gameWantedRotation, rawAngles, mod);

                MapMarkerPatch._headTrackingAppliedFrame = currentFrame;
                MapMarkerPatch._cameraHasHeadTracking = true;
            }
            catch (System.Exception)
            {
                // Silently handle errors
            }
        }

        /// <summary>
        /// Patch UpdateLockOnTargeting to disable camera lock-on during conversations when head tracking is enabled.
        /// This prevents the game from trying to force the camera to look at NPCs, allowing free head movement.
        /// </summary>
        [HarmonyPatch("UpdateLockOnTargeting")]
        [HarmonyPrefix]
        public static bool UpdateLockOnTargeting_Prefix(PlayerCameraController __instance)
        {
            try
            {
                var mod = HeadTrackingMod.Instance;
                if (mod == null || !mod.IsTrackingEnabled())
                {
                    return true; // Allow normal lock-on behavior
                }

                // Skip lock-on targeting when head tracking is active
                // This prevents camera restriction and flickering during conversations
                return false;
            }
            catch (System.Exception)
            {
                return true;
            }
        }

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void Start_Postfix(PlayerCameraController __instance)
        {
            _centerSet = false;

            var mod = HeadTrackingMod.Instance;
            if (mod == null) return;

            ReticleUpdater.Create();
            UnityCoreModule::UnityEngine.Camera.onPreRender += OnCameraPreRender;
        }

        private static void OnCameraPreRender(UnityCoreModule::UnityEngine.Camera cam)
        {
            try
            {
                if (cam != UnityCoreModule::UnityEngine.Camera.main) return;
                if (_cameraTransform == null) return;
                if (_lastHeadTrackingRotation == Quaternion.identity) return;

                ReticleUpdater.GetInstance()?.UpdateReticlePosition();
            }
            catch (System.Exception)
            {
                // Silently handle errors
            }
        }

        private static float NormalizeAngle(float angle)
        {
            if (angle > 180) angle -= 360;
            return angle;
        }

        private static void HandleTrackingLoss(OpenTrackClient.RawEulerAngles rawAngles, HeadTrackingMod mod)
        {
            if (!rawAngles.IsValid)
            {
                _framesWithoutData++;
                if (_framesWithoutData > TrackingConstants.RECENTER_THRESHOLD_FRAMES && _centerSet)
                {
                    _centerSet = false;
                }
            }
            else
            {
                _framesWithoutData = 0;
            }
        }

        private static void SetCenter(OpenTrackClient.RawEulerAngles rawAngles, HeadTrackingMod mod)
        {
            _centerYaw = rawAngles.Yaw;
            _centerPitch = rawAngles.Pitch;
            _centerRoll = rawAngles.Roll;
            _centerSet = true;
            _lastHeadTrackingRotation = Quaternion.identity;
        }

        private static void ApplyHeadTracking(UnityCoreModule::UnityEngine.Transform cameraTransform,
            Quaternion gameWantedRotation, OpenTrackClient.RawEulerAngles rawAngles, HeadTrackingMod mod)
        {
            Quaternion headTrackingRotation;

            if (rawAngles.IsValid)
            {
                int currentFrame = UnityCoreModule::UnityEngine.Time.frameCount;

                float yaw = rawAngles.Yaw - _centerYaw;
                float pitch = rawAngles.Pitch - _centerPitch;
                float roll = rawAngles.Roll - _centerRoll;

                float headTrackingInfluence = CalculateHeadTrackingInfluence();

                // Cache calculations if inputs haven't changed this frame
                bool needsRecalc = _lastRotationCalcFrame != currentFrame ||
                    UnityCoreModule::UnityEngine.Mathf.Abs(yaw - _cachedYawOffset) > 0.01f ||
                    UnityCoreModule::UnityEngine.Mathf.Abs(pitch - _cachedPitchOffset) > 0.01f ||
                    UnityCoreModule::UnityEngine.Mathf.Abs(roll - _cachedRollOffset) > 0.01f ||
                    UnityCoreModule::UnityEngine.Mathf.Abs(headTrackingInfluence - _cachedHeadTrackingInfluence) > 0.01f;

                if (needsRecalc)
                {
                    _cachedYawOffset = yaw;
                    _cachedPitchOffset = pitch;
                    _cachedRollOffset = roll;
                    _cachedHeadTrackingInfluence = headTrackingInfluence;
                    _lastRotationCalcFrame = currentFrame;

                    var headRoll = Quaternion.AngleAxis(roll * HeadTrackingMod.RollSensitivity * headTrackingInfluence, Vector3.forward);
                    var headPitch = Quaternion.AngleAxis(pitch * -HeadTrackingMod.PitchSensitivity * headTrackingInfluence, Vector3.right);
                    var headYaw = Quaternion.AngleAxis(yaw * HeadTrackingMod.YawSensitivity * headTrackingInfluence, Vector3.up);
                    headTrackingRotation = headYaw * headPitch * headRoll;

                    _lastHeadTrackingRotation = headTrackingRotation;
                }
                else
                {
                    headTrackingRotation = _lastHeadTrackingRotation;
                }

                // Apply rotation directly - this happens in Update before culling
                cameraTransform.localRotation = gameWantedRotation * headTrackingRotation;

            }
            else
            {
                // Apply last known head tracking rotation
                cameraTransform.localRotation = gameWantedRotation * _lastHeadTrackingRotation;
            }
        }

        private static float CalculateHeadTrackingInfluence()
        {
            if (_gameCameraChangeSpeed > TrackingConstants.DIALOGUE_CAMERA_SPEED_THRESHOLD)
            {
                float reduction = UnityCoreModule::UnityEngine.Mathf.Clamp01(
                    (_gameCameraChangeSpeed - TrackingConstants.DIALOGUE_CAMERA_SPEED_THRESHOLD) /
                    TrackingConstants.DIALOGUE_CAMERA_SPEED_RANGE
                );
                return UnityCoreModule::UnityEngine.Mathf.Lerp(1f, TrackingConstants.DIALOGUE_MIN_HEAD_TRACKING, reduction);
            }
            return 1f;
        }
    }
}
