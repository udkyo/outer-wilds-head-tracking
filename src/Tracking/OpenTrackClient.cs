extern alias UnityCoreModule;
extern alias OWMLCommon;
using System;
using System.Net;
using System.Net.Sockets;
using HeadTracking.Configuration;
using Quaternion = UnityCoreModule::UnityEngine.Quaternion;
using Vector3 = UnityCoreModule::UnityEngine.Vector3;

namespace HeadTracking.Tracking
{
    /// <summary>
    /// UDP client for receiving head tracking data from OpenTrack.
    /// OpenTrack sends 6DOF data (X, Y, Z position + Yaw, Pitch, Roll) over UDP.
    /// </summary>
    public class OpenTrackClient : IDisposable
    {
        private UdpClient? _udpClient;
        private IPEndPoint? _remoteEndPoint;
        private readonly int _port;
        private bool _isConnected;
        private DateTime _lastDataReceived = DateTime.MinValue;
        private readonly TimeSpan _connectionTimeout = TimeSpan.FromSeconds(TrackingConstants.CONNECTION_TIMEOUT_SECONDS);
        private HeadPose _lastValidPose = new HeadPose { IsValid = false };

        private double _lastYaw = 0;
        private double _lastPitch = 0;
        private double _lastRoll = 0;
        private int _lastProcessedFrame = -1;

        private DateTime _cachedUtcNow = DateTime.MinValue;
        private int _cachedUtcNowFrame = -1;

        public OpenTrackClient(int port = TrackingConstants.DEFAULT_OPENTRACK_PORT)
        {
            _port = port;
        }

        public bool Initialize()
        {
            try
            {
                _udpClient = new UdpClient(_port);
                _udpClient.Client.ReceiveTimeout = TrackingConstants.UDP_RECEIVE_TIMEOUT_MS;
                _remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                _isConnected = true;
                return true;
            }
            catch (Exception)
            {
                _isConnected = false;
                return false;
            }
        }

        public bool IsConnected()
        {
            if (!_isConnected || _udpClient == null)
            {
                return false;
            }

            // Consider disconnected if we haven't received data in a while
            // But don't permanently disconnect - just indicate no current data
            if (_lastDataReceived != DateTime.MinValue &&
                (GetCachedUtcNow() - _lastDataReceived) > _connectionTimeout)
            {
                // Return false to indicate no tracking, but don't close socket
                // This allows reconnection when data starts flowing again
                return false;
            }

            return true;
        }

        /// <summary>
        /// Check if we have recent data (for determining if tracking should be active)
        /// </summary>
        public bool HasRecentData()
        {
            if (_lastDataReceived == DateTime.MinValue)
                return false;

            return (GetCachedUtcNow() - _lastDataReceived) < _connectionTimeout;
        }

        /// <summary>
        /// Gets cached UTC time for current frame to avoid repeated DateTime.UtcNow calls.
        /// DateTime.UtcNow has overhead and we call it multiple times per frame.
        /// </summary>
        private DateTime GetCachedUtcNow()
        {
            int currentFrame = UnityCoreModule::UnityEngine.Time.frameCount;
            if (_cachedUtcNowFrame != currentFrame)
            {
                _cachedUtcNow = DateTime.UtcNow;
                _cachedUtcNowFrame = currentFrame;
            }
            return _cachedUtcNow;
        }

        private void ProcessUdpData()
        {
            int currentFrame = UnityCoreModule::UnityEngine.Time.frameCount;
            if (_lastProcessedFrame == currentFrame)
            {
                return;
            }

            _lastProcessedFrame = currentFrame;

            if (_udpClient == null || !_isConnected)
            {
                return;
            }

            try
            {
                byte[]? mostRecentData = null;

                while (_udpClient.Available > 0)
                {
                    mostRecentData = _udpClient.Receive(ref _remoteEndPoint);
                }

                if (mostRecentData != null && mostRecentData.Length >= TrackingConstants.OPENTRACK_PACKET_SIZE)
                {
                    _lastDataReceived = GetCachedUtcNow();

                    _lastYaw = BitConverter.ToDouble(mostRecentData, 24);
                    _lastPitch = BitConverter.ToDouble(mostRecentData, 32);
                    _lastRoll = BitConverter.ToDouble(mostRecentData, 40);

                    Quaternion rotation = Quaternion.Euler((float)_lastPitch, (float)_lastYaw, (float)_lastRoll);

                    _lastValidPose = new HeadPose
                    {
                        Rotation = rotation,
                        IsValid = true
                    };
                }
            }
            catch (SocketException)
            {
            }
            catch (Exception)
            {
            }
        }

        public HeadPose GetHeadPose()
        {
            ProcessUdpData();

            if (_lastValidPose.IsValid && IsConnected())
            {
                return _lastValidPose;
            }

            return new HeadPose { IsValid = false };
        }

        public void Shutdown()
        {
            _isConnected = false;
            _udpClient?.Close();
            _udpClient?.Dispose();
            _udpClient = null;
        }

        public void Dispose()
        {
            Shutdown();
        }

        public struct RawEulerAngles
        {
            public float Yaw { get; set; }
            public float Pitch { get; set; }
            public float Roll { get; set; }
            public bool IsValid { get; set; }
        }

        public RawEulerAngles PeekRawEulerAngles()
        {
            bool hasData = _lastDataReceived != DateTime.MinValue &&
                (GetCachedUtcNow() - _lastDataReceived).TotalSeconds < TrackingConstants.CONNECTION_TIMEOUT_SECONDS;

            return new RawEulerAngles
            {
                Yaw = (float)_lastYaw,
                Pitch = (float)_lastPitch,
                Roll = (float)_lastRoll,
                IsValid = hasData
            };
        }

        public RawEulerAngles GetRawEulerAngles()
        {
            ProcessUdpData();

            bool hasData = _lastDataReceived != DateTime.MinValue &&
                (GetCachedUtcNow() - _lastDataReceived).TotalSeconds < TrackingConstants.CONNECTION_TIMEOUT_SECONDS;

            return new RawEulerAngles
            {
                Yaw = (float)_lastYaw,
                Pitch = (float)_lastPitch,
                Roll = (float)_lastRoll,
                IsValid = hasData
            };
        }
    }

    /// <summary>
    /// Represents a head pose with rotation and validity status.
    /// </summary>
    public struct HeadPose
    {
        public Quaternion Rotation { get; set; }
        public bool IsValid { get; set; }
    }
}
