# Head Tracking for Outer Wilds

![Mod GIF](assets/readme-clip.gif)

Look around the solar system by moving your head! Turn your real head to peek around the cabin of your ship or gaze up at the stars through your helmet visor. This mod brings immersive head tracking to Outer Wilds.

Works with your phone, webcam, Tobii eye tracker, or any other tracking device supported by OpenTrack.

## What You'll Need

- [OWML (Outer Wilds Mod Manager)](https://outerwildsmods.com/mod-manager/)
- [OpenTrack](https://github.com/opentrack/opentrack/releases) - free head tracking software
- A way to track your head:
  - **Your phone** (easiest - just download an app!)
  - **Webcam**
  - **Tobii eye tracker**
  - **TrackIR, PS3 Eye + IR LEDs, or anything else OpenTrack supports**

## Installation

### Using the Outer Wilds Mod Manager (recommended):

1. Open the [Outer Wilds Mod Manager](https://outerwildsmods.com/mod-manager/)
2. Search for "Head Tracking"
3. Click Install

### Manual Installation:

1. Download the latest release ZIP from the [releases page](https://github.com/udkyo/outer-wilds-head-tracking/releases)
2. Extract it to your `OWML/Mods/` folder
3. You should see "Head Tracking" in the mod list

## Setting Up Head Tracking

You'll need to set up OpenTrack to capture your head movements.

### Step 1: Install OpenTrack

1. Download OpenTrack from https://github.com/opentrack/opentrack/releases
2. Run the installer
3. Launch OpenTrack - you should see a window with "Input" and "Output" dropdowns

### Step 2: Choose How You Want to Track

Pick whichever method works for you:

#### 📱 Using Your Phone (Easiest!)

Your phone can track your face and send the data to your computer over WiFi. No extra hardware needed!

**Get the app:**
- **iOS**: [SmoothTrack](https://apps.apple.com/app/smoothtrack/id1528839485) ($9.99 - works great)
- **Android**: [OpenTrack Mobile](https://play.google.com/store/apps/details?id=org.opentrack.opentrackmobile) (free) or [SmoothTrack](https://play.google.com/store/apps/details?id=com.epaga.smoothtrack) ($9.99)

**Setup:**
1. Install one of the apps above (or another opentrack compatible app) on your phone
2. Make sure your phone and computer are on the **same WiFi network**
3. **Find your computer's IP address:**
   - Windows: Open Command Prompt and type `ipconfig` - look for "IPv4 Address" (usually starts with 192.168)
   - Or check your router settings
4. In OpenTrack on your computer:
   - Set **Input** to "UDP over network"
   - Click ⚙️ next to Input, set **Port** to `4242` (SmoothTrack default) or match your app's output port
5. On your phone app:
   - Enter your computer's IP address (e.g., `192.168.1.100`)
   - Set output port to `4242`
   - Start tracking
6. **Windows Firewall**: When prompted, allow OpenTrack through your firewall (or manually add an exception for UDP port 4242)
7. Back in OpenTrack, you should see the preview moving when you move your head

#### 🎥 Using a Webcam

1. In OpenTrack:
   - Set **Input** to "PointTracker 1.1"
   - Click the ⚙️ settings button
   - Follow the calibration instructions (you'll need to print a simple pattern or use the built-in model detection)
2. You should see yourself in a preview window - move your head and watch the tracking data update

#### 👁️ Using Tobii Eye Tracker

If you have a Tobii device (4C, 5, etc.):

1. Install Tobii Game Hub from https://gaming.tobii.com/getstarted/
2. Make sure your Tobii device is connected
3. **Important**: Keep "Tobii Experience" running (it detects the hardware) but **close "Tobii Game Hub"** before playing
   - Why? Game Hub interferes with the tracking data and makes the camera overly sensitive
4. In OpenTrack:
   - Set **Input** to "Tracker | Tobii"
   - You should see tracking data when you move your head

### Step 3: Connect OpenTrack to the Game

Now tell OpenTrack to send its tracking data to Outer Wilds:

1. In OpenTrack, set **Output** to "UDP over network"
2. Click the ⚙️ settings button next to Output
3. Set:
   - IP Address: `127.0.0.1`
   - Port: `5252`
4. Click OK
5. Click the big green **Start** button at the bottom
6. Move your head and verify tracking is working - the octopus head in the preview window should move and the rotation/position numbers should change
7. Keep OpenTrack running

That's it for setup!

## Playing the Game

1. **Launch Outer Wilds through OWML** (not through Steam/Epic directly)
2. Load your save or start a new game
3. Move your head - your view should follow!

The first time you look around, press **F8** to center your view. This tells the mod "this is my neutral head position."

### Controls

- **F8** - Recenter your view (use this whenever things feel off-center)
- **F9** - Toggle head tracking on/off

### Tips for the Best Experience

**Finding the right sensitivity:**
- If the camera moves too much when you turn your head, open OpenTrack's "Mapping" tab and reduce the output curves
- If it doesn't move enough, increase the output curves
- You want subtle movements - you shouldn't need to turn your head 90 degrees to look around!
- For per-axis control, you can also edit the mod's `config.json` file in `OWML/Mods/udkyo.HeadTracking/` to adjust yaw, pitch, and roll sensitivity individually

**Reducing jitter/shakiness:**
- In OpenTrack, click the **Filter** dropdown and select "Accela"
- In the filter settings, increase smoothing to 1.5-2.0
- Add a small deadzone to ignore tiny movements

**When head tracking pauses automatically:**
- The mod automatically reduces or disables head tracking in certain situations:
  - When using the model ship (prevents camera lock-ups)
  - When zoomed in with the signalscope (so you can aim precisely)
- It'll come back on its own when you're done

## Troubleshooting

### Nothing happens when I move my head

**Check OpenTrack:**
- Is it running with a green "tracking" indicator?
- Move your head - do the numbers change?
- Did you click the **Start** button?
- Is Output set to "UDP over network" with port `5252`?

**Check the game:**
- Did you launch through OWML? (Check if the OWML console window is open)
- Press F9 in case you accidentally disabled tracking
- Press F8 to recenter
- Check the OWML console for any error messages

**If you're using your phone:**
- Are your phone and computer on the same WiFi?
- Did you enter the right IP address?
- Check Windows Firewall isn't blocking OpenTrack
- Make sure you set Input to "UDP over network" (not Output - that's different!)

### The camera is shaky or jittery

This is usually fixed with filtering:

1. In OpenTrack, set **Filter** to "Accela"
2. Click the filter settings button
3. Increase smoothing to 1.5-2.0
4. Add a small deadzone (maybe 0.5 degrees)

### The camera is too sensitive or not sensitive enough

1. In OpenTrack, click the **Mapping** button
2. You'll see curves for Yaw, Pitch, and Roll
3. Drag the curves to change sensitivity:
   - Pull them down to make movements smaller
   - Push them up to make movements bigger
4. Click OK and test

You can also adjust sensitivity in the mod's config file if you want different sensitivity for each axis.

### Tobii users: The camera spins wildly

If turning your head slightly makes you look all the way behind you:

1. Close "Tobii Game Hub" (but keep "Tobii Experience" running)
2. Restart OpenTrack
3. The problem is that Game Hub sends its own tracking data that interferes with OpenTrack

You only need Tobii Experience for hardware detection - Game Hub does something weird resulting in graphical artifacts

### The mod doesn't show up in OWML

1. Make sure you installed it through the OWML mod manager, or
2. If manual install, check the folder is in `OWML/Mods/` with `manifest.json` inside
3. Restart OWML
4. Check the OWML console for errors

## Building from Source

Only needed if you want to modify the mod yourself.

**Requirements:**
- .NET SDK 4.8 or higher
- Outer Wilds with OWML installed
- [Pixi](https://pixi.sh/) (optional but recommended)

**Build & Deploy:**
```bash
pixi run deploy
```

This will copy the OWML DLLs, build the mod, and deploy it to your OWML mods folder.

For manual builds without Pixi, see the build scripts in `build/scripts/`.

## Credits

- Built with [OWML](https://github.com/amazingalek/owml)
- Uses [OpenTrack](https://github.com/opentrack/opentrack) for head tracking
- Harmony for runtime patching

Made for the Outer Wilds community with ☄️
