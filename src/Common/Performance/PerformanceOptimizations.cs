extern alias UnityCoreModule;
using System;
using Quaternion = UnityCoreModule::UnityEngine.Quaternion;

namespace HeadTracking.Common.Performance
{
    /// <summary>
    /// Performance optimization utilities to reduce allocations and GC pressure.
    /// </summary>
    public static class PerformanceOptimizations
    {
        /// <summary>
        /// Fast quaternion identity check without allocation.
        /// More efficient than comparing to Quaternion.identity.
        /// </summary>
        public static bool IsIdentity(this Quaternion q)
        {
            return q.x == 0f && q.y == 0f && q.z == 0f && q.w == 1f;
        }

        /// <summary>
        /// Fast quaternion approximate equality check.
        /// Avoids Quaternion.Angle which uses Mathf.Acos (expensive).
        /// </summary>
        public static bool ApproximatelyEqual(this Quaternion a, Quaternion b, float threshold = 0.001f)
        {
            float dot = a.x * b.x + a.y * b.y + a.z * b.z + a.w * b.w;
            return UnityCoreModule::UnityEngine.Mathf.Abs(dot) > (1.0f - threshold);
        }

        /// <summary>
        /// Cached frame counter to avoid repeated Time.frameCount calls.
        /// </summary>
        private static int _cachedFrameCount = -1;
        private static int _lastFrameUpdate = -1;

        public static int GetCachedFrameCount()
        {
            int rawFrame = UnityCoreModule::UnityEngine.Time.frameCount;
            if (_lastFrameUpdate != rawFrame)
            {
                _cachedFrameCount = rawFrame;
                _lastFrameUpdate = rawFrame;
            }
            return _cachedFrameCount;
        }
    }

    /// <summary>
    /// Struct-based rotation cache to avoid allocations.
    /// Used for caching quaternion calculations across frames.
    /// </summary>
    public struct RotationCache
    {
        public Quaternion Rotation;
        public float Yaw;
        public float Pitch;
        public float Roll;
        public float Influence;
        public int Frame;
        public bool IsValid;

        public bool NeedsUpdate(int currentFrame, float yaw, float pitch, float roll, float influence, float threshold = 0.01f)
        {
            if (!IsValid || Frame != currentFrame)
                return true;

            return UnityCoreModule::UnityEngine.Mathf.Abs(yaw - Yaw) > threshold ||
                   UnityCoreModule::UnityEngine.Mathf.Abs(pitch - Pitch) > threshold ||
                   UnityCoreModule::UnityEngine.Mathf.Abs(roll - Roll) > threshold ||
                   UnityCoreModule::UnityEngine.Mathf.Abs(influence - Influence) > threshold;
        }

        public void Update(int frame, float yaw, float pitch, float roll, float influence, Quaternion rotation)
        {
            Frame = frame;
            Yaw = yaw;
            Pitch = pitch;
            Roll = roll;
            Influence = influence;
            Rotation = rotation;
            IsValid = true;
        }

        public void Invalidate()
        {
            IsValid = false;
        }
    }

    /// <summary>
    /// Frame-based caching helper to avoid redundant calculations within the same frame.
    /// </summary>
    public struct FrameCache<T> where T : struct
    {
        private T _value;
        private int _frame;
        private bool _hasValue;

        public bool TryGet(int currentFrame, out T value)
        {
            if (_hasValue && _frame == currentFrame)
            {
                value = _value;
                return true;
            }
            value = default;
            return false;
        }

        public void Set(int currentFrame, T value)
        {
            _value = value;
            _frame = currentFrame;
            _hasValue = true;
        }

        public void Clear()
        {
            _hasValue = false;
        }
    }
}
