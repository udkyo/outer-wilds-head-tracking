extern alias UnityCoreModule;
using System;
using Quaternion = UnityCoreModule::UnityEngine.Quaternion;

namespace HeadTracking.Camera.Utilities
{
    /// <summary>
    /// Utility for temporarily modifying camera rotation and safely restoring it.
    /// Eliminates code duplication across all patch files.
    /// </summary>
    public sealed class CameraRotationHelper : IDisposable
    {
        private Quaternion _savedRotation;
        private UnityCoreModule::UnityEngine.Transform? _transform;
        private bool _isModified;

        private CameraRotationHelper()
        {
            _savedRotation = Quaternion.identity;
            _transform = null;
            _isModified = false;
        }

        /// <summary>
        /// Temporarily applies a rotation to the camera transform.
        /// Must be disposed to restore original rotation.
        /// </summary>
        public static CameraRotationHelper? ApplyTemporaryRotation(
            UnityCoreModule::UnityEngine.Transform? cameraTransform,
            Quaternion newRotation)
        {
            if (cameraTransform == null)
            {
                return null;
            }

            var helper = new CameraRotationHelper
            {
                _savedRotation = cameraTransform.localRotation,
                _transform = cameraTransform,
                _isModified = true
            };

            cameraTransform.localRotation = newRotation;
            return helper;
        }

        /// <summary>
        /// Temporarily applies base rotation without head tracking.
        /// Returns null if head tracking is not active.
        /// </summary>
        public static CameraRotationHelper? ApplyBaseRotation(
            UnityCoreModule::UnityEngine.Transform? cameraTransform,
            Quaternion baseRotation,
            Quaternion headTracking)
        {
            if (cameraTransform == null || baseRotation == default || baseRotation == Quaternion.identity)
            {
                return null;
            }

            Quaternion headTrackedWorld = baseRotation * headTracking;
            Quaternion localRotation = cameraTransform.parent != null
                ? Quaternion.Inverse(cameraTransform.parent.rotation) * headTrackedWorld
                : headTrackedWorld;

            return ApplyTemporaryRotation(cameraTransform, localRotation);
        }

        /// <summary>
        /// Restores the saved rotation to the camera transform.
        /// </summary>
        public void Dispose()
        {
            if (!_isModified || _transform == null)
            {
                return;
            }

            _transform.localRotation = _savedRotation;
            _isModified = false;
            _transform = null;
            _savedRotation = Quaternion.identity;
        }
    }
}
