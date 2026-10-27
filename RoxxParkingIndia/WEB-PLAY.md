# Play on Android without an APK

1. Keep the laptop and phone on the same Wi-Fi.
2. Double-click `Play on Phone.cmd` in `D:\Game Clone`. It prints the current phone link.
3. Open that **http** link in Chrome on Android, tap **PLAY IN BROWSER**, and turn the phone sideways. The first visit downloads the game files.
4. Keep the laptop awake and connected while playing. No APK installation or Unity account is needed on the phone. Use `Stop Phone Game.cmd` when finished to stop serving the game.

The browser version 0.6.1 has 12 campaign courses and 12 Expert Trials (24 total), the supplied Thar and touch controls. Six new trials include a 700 m speed sprint, dual signals, parallel reverse pockets, a reverse-only horseshoe and an endurance route. **EXPERT TRIALS > NEXT > NEXT** opens courses 19?21; NEXT again opens 22?24. Forward speed reaches 100 km/h; reverse stays at 18 km/h. Brake early on parking courses.

Graphics now use 2K key surface maps, roughness/AO, contact shading and soft shadows. **PAUSE > GRAPHICS / BALANCED** reduces rendering cost if your phone struggles. The High/Balanced choice resets when the page reloads. The compressed game payload is about 57 MB (54.7 MiB). First-load download size is recorded in `Builds/Checks/web-verification-0.6.1.json`; HTTP LAN browsers may download it again on reload.

Version 0.6.1 controls: open **CONTROLS** on the main menu, or **PAUSE > CONTROLS**. Drag Camera Sensitivity from 25% to 200%. Tap **STEERING / LEFT** to switch to RIGHT; wheel/buttons move right and pedals/gear/speed move left. Wheel is 28% larger; directional buttons are larger too. **BACK / SAVE** remembers both choices on this browser. Reset Defaults restores 100% and left steering.

Progress is separate from the Windows/APK save. Keep using the same browser and link; clearing site data or changing the laptop's IP can make the old browser save unavailable. Private browsing may discard progress.

If the link does not open, check that the server launcher succeeded and that the phone uses the same Wi-Fi. Some guest networks isolate devices. Do not switch off the firewall. The server uses TCP 8877 and serves `Builds/Web-0.6.1` plus the explicitly selected APK download.

Development: `Tools/Install-Web.ps1` installs the official matching Unity module. `Tools/Invoke-Unity.ps1 -Action Web` produces the browser build; `node Tools/check-web-server.cjs` checks HTTP handling and build-folder isolation. The LAN server uses the existing Node installation with no npm dependencies.

The user confirmed version 0.6.0 runs smoothly on physical Android with High graphics. This is qualitative feedback; frame rate, memory and complete human course runs are not measured. Desktop Chrome rendered the packaged 0.6.0 menu; HTTP/file integrity and REA evidence are under `Builds/Checks/web-*`.

Installable app 0.6.1 is also available through the same Wi-Fi server: [Download APK](http://10.181.105.74:8877/download/RoxxParkingIndia-0.6.1.apk). Installation/offline use: [ANDROID-PLAY.md](ANDROID-PLAY.md).
