# Waypoint Share Mod for Vintage Story

A multiplayer mod for Vintage Story that allows players to share waypoints with each other on a server using an ImGui interface.

## ⚠️ Dependencies

**This mod requires the VSImGui mod to function:**

- **VSImGui**: Download from [https://mods.vintagestory.at/vsimgui](https://mods.vintagestory.at/vsimgui)
- Minimum version: 1.1.16
- **Can be installed locally OR downloaded from a server that has it**
- **Must be available on both client and server**

## Features

- Share any of your waypoints with other online players
- Select specific players from a list to send waypoints to
- ImGui-based interface for selecting waypoints and recipients
- Waypoints retain their original title, icon, and color
- Recipients see who shared the waypoint with them

## Installation

### Prerequisites

- **VSImGui Mod**: This mod requires the VSImGui library mod to be available
  - **Option 1**: Download and install VSImGui locally from: https://mods.vintagestory.at/vsimgui
  - **Option 2**: Join a server that has VSImGui installed (it will be automatically downloaded)
  - Minimum version: 1.1.16

### Steps

1. Install the ImGui mod (see prerequisites above)
2. Download the latest release of WaypointShare.zip
3. Place the zip file in your Vintage Story `Mods` folder:
   - Windows: `%appdata%/VintagestoryData/Mods/`
   - Linux: `~/.config/VintagestoryData/Mods/`
   - macOS: `~/Library/Application Support/VintagestoryData/Mods/`
4. Start Vintage Story

## Usage

1. Press `Shift + P` to open the Waypoint Share window
2. Use the "Refresh Lists" button if needed to update waypoints and players
3. Select a waypoint from the left column
4. Select a player to send it to from the right column
5. Click "Send Waypoint" (the button will be disabled until both selections are made)

The recipient will receive a chat notification and the waypoint will be added to their map with a note indicating who shared it.

**Note**: The interface uses ImGui for a modern, responsive user experience with resizable columns and better organization.

## Building from Source

### Prerequisites

- .NET 8.0 SDK or later
- Vintage Story installed
- **VSImGui mod** (can be installed or just have the DLL files)

### Build Methods

#### Method 1: Manual DLL Extraction (Recommended)

1. Clone this repository
2. Get the VSImGui mod files:
   - Download `vsimgui.zip` from [VSImGui releases](https://github.com/blakdragan7/vsimgui/releases)
   - Or find it in your server mods: `%APPDATA%\VintagestoryData\ModsByServer\<server-ip>\vsimgui.zip`
3. Extract the DLL files:
   - Unzip `vsimgui.zip`
   - Copy `ImGui.NET.dll` and `VSImGui.dll` to the `WaypointShare` project folder
   - (Optional: Create a `lib` subfolder and place DLLs there instead)
4. Build the project:
   ```bash
   dotnet build -c Release
   ```

#### Method 2: Automatic Detection

1. Clone this repository
2. Install VSImGui mod in your Vintage Story installation
3. (Optional) Set environment variable for custom VS location:

   ```bash
   # Windows (PowerShell)
   $env:VINTAGE_STORY="C:\Program Files\Vintage Story"

   # Linux/macOS
   export VINTAGE_STORY="/path/to/vintagestory"
   ```

4. Build the project:
   ```bash
   dotnet build -c Release
   ```

### Output

- The compiled mod will be in `bin/Release/net8.0/`
- For release builds, a `WaypointShare.zip` file will be automatically created

### Manual Packaging

If you need to manually package the mod:

```bash
cd bin/Release/net7.0/
zip -r WaypointShare.zip WaypointShare.dll modinfo.json
```

### Troubleshooting Build Issues

If you encounter build errors:

1. **Check DLL locations:**

   ```bash
   dotnet msbuild -t:DiagnoseImGui
   ```

   This will show which DLL files were found and their locations.

2. **Manual DLL placement:**

   - Make sure `ImGui.NET.dll` and `VSImGui.dll` are in your project root
   - Or place them in a `lib` subfolder
   - The build will prioritize local files over auto-detection

3. **Common locations for vsimgui.zip:**
   - Downloads folder
   - `%APPDATA%\VintagestoryData\ModsByServer\<server-ip>\vsimgui.zip`
   - Vintage Story `Mods` folder if previously extracted

## Compatibility

- Requires Vintage Story 1.21.0 or later
- Requires ImGui mod 1.1.16 or later
- Must be installed on both client and server
- All players who want to share waypoints must have both this mod and ImGui installed

## Configuration

No configuration required - the mod works out of the box!

### Build Troubleshooting

If you encounter build errors:

1. **ImGui/VSImGui references not found**:

   - **Diagnose your setup**: Run `dotnet msbuild -t:DiagnoseImGui` to see what ImGui files are found
   - **Local installation**: Check for `VSImGui.dll` and `ImGui.NET.dll` in `[VintageStory]\Mods\VSImGui\` folder
   - **Server-downloaded**: The build will automatically extract `vsimgui.zip` from `VintagestoryData\ModsByServer\[server-ip]\`
   - **Manual extraction**: If you have `vsimgui.zip`, the build will extract it to `obj\VSImGuiExtracted\` automatically
   - **Alternative methods**:
     - Install VSImGui locally: Download and install from the mod site
     - Join a server with VSImGui: It will download to your mod cache automatically
   - The build will show diagnostic messages about extraction and what files it finds
   - Common VS paths: `C:\Users\[USERNAME]\AppData\Roaming\Vintagestory`, `C:\Program Files (x86)\Vintagestory`

2. **.NET Framework version errors**:

   - Ensure you have .NET 8.0 SDK installed (matches Vintage Story's runtime)

3. **Vintage Story path not found**:
   - Set environment variable: `VINTAGE_STORY=C:\path\to\your\vintagestory\installation`
   - Or let auto-detection find common installation paths

## Troubleshooting

**Window doesn't open when pressing Shift+P:**

- Ensure ImGui mod is installed and working
- Check the Vintage Story client log for errors
- Verify both mods are properly installed
- Try rebinding the hotkey in the controls menu

**ImGui window appears corrupted or unusable:**

- Update to the latest ImGui mod version
- Try pressing Ctrl+- or Ctrl+= to adjust GUI size
- Delete imgui.ini in your ModConfig folder to reset window settings

**Waypoint not received by other player:**

- Ensure both players have this mod AND ImGui installed
- Verify the recipient is online
- Check server logs for errors

**Waypoints appear in wrong location:**

- This is typically a game bug, not a mod issue
- Try re-sharing the waypoint

## Development

The mod consists of three main components:

1. **WaypointShareMod.cs** - Main mod system handling initialization and networking
2. **WaypointSharePacket.cs** - Network packet definition for waypoint data
3. **WaypointShareImGui.cs** - ImGui-based interface for player and waypoint selection

### Network Protocol

The mod uses a custom network channel called "waypointshare" with the following flow:

1. Client opens dialog and selects waypoint + recipient
2. Client sends WaypointSharePacket to server
3. Server validates and forwards packet to recipient client
4. Recipient client adds waypoint to their map

## License

This mod is released under the MIT License. Feel free to modify and redistribute.

## Credits

Created for the Vintage Story modding community.

## Support

For issues, questions, or suggestions, please open an issue on the GitHub repository.
