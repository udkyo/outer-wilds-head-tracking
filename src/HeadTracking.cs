extern alias OWMLCommon;
extern alias OWMLCore;
extern alias UnityCoreModule;
using HarmonyLib;
using HeadTracking.Configuration;
using HeadTracking.Tracking;
using System;
using System.Reflection;
using UnityEngine;
using IModHelper = OWMLCommon::OWML.Common.IModHelper;
using MessageType = OWMLCommon::OWML.Common.MessageType;
using ModBehaviour = OWMLCore::OWML.ModHelper.ModBehaviour;
using KeyCode = UnityCoreModule::UnityEngine.KeyCode;

namespace HeadTracking
{
    public class HeadTrackingMod : ModBehaviour
    {
        public static HeadTrackingMod? Instance { get; private set; }
        private Harmony? _harmony;
        private OpenTrackClient? _trackingClient;
        private bool _trackingEnabled = true;
        private bool _trackingStateBeforeModelShip = true;
        private bool _trackingStateBeforeSignalscopeZoom = true;

        // Config values
        public static float YawSensitivity = 1.0f;
        public static float PitchSensitivity = 1.0f;
        public static float RollSensitivity = 1.0f;

        public new IModHelper? ModHelper { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            // ModHelper is set by OWML before Start() is called
            ModHelper = base.ModHelper;

            if (ModHelper == null)
            {
                return;
            }

            try
            {
                _harmony = new Harmony("udkyo.HeadTracking");
                _harmony.PatchAll(Assembly.GetExecutingAssembly());

                // Apply manual patches for types that aren't directly accessible
                global::HeadTracking.Camera.UI.MapMarkerPatch.ApplyPatches(_harmony);
            }
            catch (Exception ex)
            {
                ModHelper.Console.WriteLine($"[HeadTracking] Failed to apply patches: {ex.Message}", MessageType.Error);
            }

            try
            {
                // Read config
                int port = (int)ModHelper.Config.GetSettingsValue<long>("opentrackPort");
                if (port <= 0) port = TrackingConstants.DEFAULT_OPENTRACK_PORT;

                YawSensitivity = (float)ModHelper.Config.GetSettingsValue<double>("yawSensitivity");
                PitchSensitivity = (float)ModHelper.Config.GetSettingsValue<double>("pitchSensitivity");
                RollSensitivity = (float)ModHelper.Config.GetSettingsValue<double>("rollSensitivity");

                if (YawSensitivity <= 0) YawSensitivity = 1.0f;
                if (PitchSensitivity <= 0) PitchSensitivity = 1.0f;
                if (RollSensitivity <= 0) RollSensitivity = 1.0f;

                _trackingClient = new OpenTrackClient(port);

                if (_trackingClient.Initialize())
                {
                    ModHelper.Console.WriteLine($"[HeadTracking] Initialized (F8=recenter, F9=toggle)", MessageType.Info);
                }
                else
                {
                    ModHelper.Console.WriteLine("[HeadTracking] Failed to initialize", MessageType.Warning);
                }

                // Listen for model ship events to disable head tracking during model ship control
                GlobalMessenger<OWRigidbody>.AddListener("EnterRemoteFlightConsole", OnEnterModelShip);
                GlobalMessenger.AddListener("ExitRemoteFlightConsole", OnExitModelShip);

                // Listen for signalscope zoom events to disable head tracking when zoomed
                // Note: Signalscope type not directly accessible, using dynamic listener with Callback<T> delegate
                var globalMessengerType = AccessTools.TypeByName("GlobalMessenger`1");
                if (globalMessengerType != null)
                {
                    var signalscopeType = AccessTools.TypeByName("Signalscope");
                    if (signalscopeType != null)
                    {
                        var messengerType = globalMessengerType.MakeGenericType(signalscopeType);
                        var addListenerMethod = AccessTools.Method(messengerType, "AddListener");
                        if (addListenerMethod != null)
                        {
                            // GlobalMessenger uses Callback<T> delegate, not Action<T>
                            var callbackType = AccessTools.TypeByName("Callback`1");
                            if (callbackType != null)
                            {
                                var delegateType = callbackType.MakeGenericType(signalscopeType);
                                var method = AccessTools.Method(typeof(HeadTrackingMod), "OnEnterSignalscopeZoom", new Type[] { signalscopeType });
                                if (method != null)
                                {
                                    var onEnterDelegate = Delegate.CreateDelegate(delegateType, this, method);
                                    addListenerMethod.Invoke(null, new object[] { "EnterSignalscopeZoom", onEnterDelegate });
                                }
                            }
                        }
                    }
                }
                GlobalMessenger.AddListener("ExitSignalscopeZoom", OnExitSignalscopeZoom);
            }
            catch (Exception ex)
            {
                ModHelper.Console.WriteLine($"[HeadTracking] Startup error: {ex.Message}", MessageType.Error);
            }
        }

        private void Update()
        {
            try
            {
                if (UnityEngine.InputSystem.Keyboard.current != null)
                {
                    // F8 - Recenter tracking
                    if (UnityEngine.InputSystem.Keyboard.current.f8Key.wasPressedThisFrame)
                    {
                        global::HeadTracking.Camera.Core.SimpleCameraPatch.RecenterTracking();
                    }

                    // F9 - Toggle tracking on/off
                    if (UnityEngine.InputSystem.Keyboard.current.f9Key.wasPressedThisFrame)
                    {
                        _trackingEnabled = !_trackingEnabled;
                    }
                }
            }
            catch
            {
                // Input system not available, skip hotkey check
            }
        }

        private void OnEnterModelShip(OWRigidbody modelShipBody)
        {
            // Save current tracking state and disable tracking while piloting model ship
            // This prevents the camera from getting locked during model ship flight
            _trackingStateBeforeModelShip = _trackingEnabled;
            _trackingEnabled = false;
        }

        private void OnExitModelShip()
        {
            // Restore previous tracking state when exiting model ship
            _trackingEnabled = _trackingStateBeforeModelShip;
        }

        private void OnEnterSignalscopeZoom(object signalscope)
        {
            // Save current tracking state and disable tracking while zoomed in
            // Zoomed signalscope makes head tracking too sensitive for precise aiming
            _trackingStateBeforeSignalscopeZoom = _trackingEnabled;
            _trackingEnabled = false;
        }

        private void OnExitSignalscopeZoom()
        {
            // Restore previous tracking state when exiting zoom
            _trackingEnabled = _trackingStateBeforeSignalscopeZoom;
        }

        private void OnDestroy()
        {
            try
            {
                // Remove event listeners
                GlobalMessenger<OWRigidbody>.RemoveListener("EnterRemoteFlightConsole", OnEnterModelShip);
                GlobalMessenger.RemoveListener("ExitRemoteFlightConsole", OnExitModelShip);

                // Remove signalscope zoom listener (using reflection)
                var globalMessengerType = AccessTools.TypeByName("GlobalMessenger`1");
                if (globalMessengerType != null)
                {
                    var signalscopeType = AccessTools.TypeByName("Signalscope");
                    if (signalscopeType != null)
                    {
                        var messengerType = globalMessengerType.MakeGenericType(signalscopeType);
                        var removeListenerMethod = AccessTools.Method(messengerType, "RemoveListener");
                        if (removeListenerMethod != null)
                        {
                            // GlobalMessenger uses Callback<T> delegate, not Action<T>
                            var callbackType = AccessTools.TypeByName("Callback`1");
                            if (callbackType != null)
                            {
                                var delegateType = callbackType.MakeGenericType(signalscopeType);
                                var method = AccessTools.Method(typeof(HeadTrackingMod), "OnEnterSignalscopeZoom", new Type[] { signalscopeType });
                                if (method != null)
                                {
                                    var onEnterDelegate = Delegate.CreateDelegate(delegateType, this, method);
                                    removeListenerMethod.Invoke(null, new object[] { "EnterSignalscopeZoom", onEnterDelegate });
                                }
                            }
                        }
                    }
                }
                GlobalMessenger.RemoveListener("ExitSignalscopeZoom", OnExitSignalscopeZoom);

                _trackingClient?.Shutdown();
                if (_harmony != null)
                {
                    _harmony.UnpatchAll(_harmony.Id);
                }
            }
            catch (Exception ex)
            {
                ModHelper?.Console.WriteLine($"[HeadTracking] Cleanup error: {ex.Message}", MessageType.Error);
            }
        }

        public bool IsTrackingEnabled()
        {
            // Don't check IsConnected() here - let the tracking client handle reconnection
            // by continuing to read from the socket even after a timeout
            return _trackingEnabled && _trackingClient != null;
        }

        public OpenTrackClient? GetTrackingClient()
        {
            return _trackingClient;
        }
    }
}
