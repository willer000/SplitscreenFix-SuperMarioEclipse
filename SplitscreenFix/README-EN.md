# SplitscreenFix for Super Mario Eclipse

**Windows x64 | Local co-op launcher | Community project**

SplitscreenFix is a Windows launcher for running **Super Mario Eclipse v1.1.0** in two local game instances (Mario as host and Luigi as client), managing their windows, and changing gamepad assignments while the game is running.

> **Important:** This is a community project, not an official Nintendo release. You must provide your own **modified Super Mario Eclipse v1.1.0 ISO**. A standard Super Mario Sunshine ISO is not compatible. The ISO and user save files are not distributed.

## Install

1. Download `SplitscreenFix-Windows-x64.zip` from the **Releases** section.
2. Extract **everything** to a folder, for example `Documents\SplitscreenFix`. Do not run the launcher directly from inside the ZIP.
3. Double-click **`Setup-Install.cmd`**. It runs the included installer, checks the package, and finds your ISO. If the ISO is not detected, select it when prompted.
4. Open the **SplitscreenFix - Mario e Luigi** desktop shortcut, or launch `Eclipse-2-Jogadores-Rebind.exe` in the extracted folder after setup.
5. Connect your gamepads, click **Refresh**, and assign inputs to Mario and Luigi.

No administrator access, MSYS2, or compiler should be needed for the prebuilt release.

## Play

- **Start 1P - SOLO** starts a single player instance. Alternatively, use the installed **Player 1 Solo / Player 2 Solo** shortcuts if they are present in your version.
- **Start 1P + 2P - CO-OP** starts both players. Enter a playable stage as Mario before Luigi joins; the launcher waits for the host to be ready.
- To change a gamepad without restarting the game, press **Refresh**, select the gamepad for each player, and choose **Apply Controllers**.
- **(No gamepad)** allows a player to have no assigned gamepad. Keyboard/mouse input goes to the game window currently in focus, not both windows at once.
- **Update Layout** arranges the game windows in the chosen split-screen layout. **Toggle Border** switches window borders.
- For a top/bottom view without stretched characters, use **Top/Bottom**, **Borderless**, and leave **Stretch Image** disabled.
- Use the **LOG** section (or the status line on earlier builds) for device and startup diagnostics.

> Features labeled SOLO / CO-OP, LOG and No gamepad depend on the particular launcher version packaged. Verify them with both player windows before distributing the release.

## Device troubleshooting

- A Windows Bluetooth device showing as **paired** may still have **no game input** in SDL. Reconnect it in gamepad mode, then refresh the list.
- Gamepads with the **same VID:PID** cannot be distinguished by the current VID/PID filter alone.
- If a controller is assigned to both windows, reassign it so that each player has a different device, or use **No gamepad** for one player.
- Keyboard and mouse work only in the focused game window.
- To check connection problems, open the most recent `Client.stderr.log`. The default localhost multiplayer port is **24002**; both instances must use the same port.

## Files and data

- `Setup-Install.cmd`: bilingual-friendly installer entry point (calls the original installation scripts).
- `Instalar.cmd`, `Instalar.ps1`: compatibility installer scripts, kept under their original names so existing functionality is not broken.
- `Eclipse-2-Jogadores-Rebind.exe`: compatible launcher executable name expected by the existing installer.
- `Runtime\sms-rebind.exe`: modified Windows x64 PC-port executable.
- `Source\`: modified sources and patches, where included.
- `Assets\`, `Licenses\`: UI artwork and applicable notices.
- `README-EN.md` / `README-PT-BR.md`: English and Brazilian Portuguese instructions.

The installer stores machine-specific paths in `SplitscreenFix.config.json`; **do not publish this file**. Game saves and logs are stored outside the application folder. The installer is designed to preserve existing saves, including when migrating data from `%TEMP%\SMS-Eclipse-Multiplayer-Test`.

## Update

Extract a new release into a **new folder** and run `Setup-Install.cmd` again. Keep your ISO and personal saves outside the shared package.

## Legal and compatibility

SplitscreenFix is an independent community tool. Nintendo and the respective game/port contributors retain their rights. Only redistribute the modified executable, libraries, artwork, and source files when their applicable licenses or rights holders permit it. This repository does **not** include a game ISO. The developer should verify compatibility and licensing before each public release.
