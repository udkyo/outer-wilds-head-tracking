# Changelog

## [1.0.1] - 2025-11-30

### Fixed

- Fix LOOK marker flickering when using head tracking (markers now respond to reticle direction, not head direction)
- Fix camera jerk when switching between Nomai translator text nodes
- Disable head tracking while game is paused
- Recenter head tracking automatically when resuming from pause menu

### Changed

- Remove dead code and unused methods

## [1.0.0] - 2025-01-22

Initial release of Head Tracking for Outer Wilds.

### Features

- **OpenTrack Integration** - Receive head tracking data via UDP (port 5252)
- **Multiple Input Methods** - Works with phone, webcam, Tobii eye tracker, TrackIR, and any other OpenTrack-supported device
- **Full 6DOF Control** - Yaw, pitch, and roll camera rotation
- **Configurable Sensitivity** - Per-axis sensitivity settings for yaw, pitch, and roll
- **Smart Behavior** - Automatically reduces or disables head tracking during:
  - Dialogue scenes (when game controls camera)
  - Model ship flight (prevents camera lock-ups)
  - Signalscope zoom (for precise aiming)
- **Visual Fixes** - Proper handling of:
  - Map markers and UI elements
  - Reticle positioning
  - Flashlight direction
  - Quantum object visibility
  - Dark Bramble fog lights
- **Hotkeys**
  - F8: Re-center view
  - F9: Toggle head tracking on/off

[1.0.0]: https://github.com/udkyo/outer-wilds-head-tracking/releases/tag/v1.0.0

