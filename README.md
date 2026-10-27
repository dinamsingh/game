# Roxx Parking India

A Unity parking-game prototype featuring a supplied Thar Roxx model, 24 courses across five locations, and 12 immediately accessible Expert Trials. Current version: **0.6.1**.

## Play on Android

[Download the 0.6.1 APK](https://github.com/dinamsingh/game/raw/refs/heads/main/RoxxParkingIndia/Builds/Android/RoxxParkingIndia-0.6.1.apk) ? approximately 52.3 MB.

Open the APK on an ARM64 Android 8.0+ phone and install it. The installed game runs offline. Browser and Android app saves are separate. The APK is a development/testing build; production/store signing is not configured. [Installation notes](RoxxParkingIndia/ANDROID-PLAY.md).

## Features

- Twelve progressive campaign courses and twelve Expert Trials: reverse-only routes, timed gates, parallel parking, switchbacks and a 700 m speed sprint.
- WheelCollider driving with D/R, brakes, a tested 100 km/h forward cap and an 18 km/h reverse cap.
- Camera drag sensitivity from 25% to 200%, saved left/right steering placement, larger wheel and button controls.
- 2K scanned key surfaces, normal/roughness/AO maps, soft shadows, contact shading and High/Balanced graphics modes.
- Ordered route points, footprint/alignment/hold parking rules, best times, medals, earned paints and saved progression.

## Open and build

1. Install Unity **6000.3.27f1** through Unity Hub. Add Android Build Support (SDK, NDK, OpenJDK) for APK builds, or Web Build Support for browser builds.
2. Activate your own eligible Unity license and open **RoxxParkingIndia/** as the project.
3. Open `Assets/Scenes/Parking.unity` and press Play. Use **Roxx Parking > Prepare prototype** if generated assets need preparation.
4. Build with the **Roxx Parking** Editor menu: Android APK, Windows preview or phone browser version. Outputs go into `RoxxParkingIndia/Builds/`.

The built-in render pipeline, linear colour and uGUI are used. Assets, `.meta` files, Packages and ProjectSettings are included; Unity regenerates the Library/cache folders. The PowerShell launch/install helpers additionally expect the original workspace-local Editor under `.tools/Unity/6000.3.27f1/`; use Hub and the Editor menus on a fresh checkout instead. The downloaded Editor, SDK/NDK, compiler packages and machine-specific settings are not committed.

## Controls

| Action | Keyboard / touch |
|---|---|
| Drive / reverse | W/S or up/down; touch accelerator with selected D/R |
| Steer | A/D or left/right; wheel or directional buttons |
| Brake | Space or BRAKE |
| Gear | G or D/R button |
| Camera | Right mouse drag / touch drag above the controls; scroll to zoom |
| Settings | Main Menu > Controls or Pause > Controls; Back / Save |
| Pause / retry | Escape / R |

## Verification and limits

Unity compilation, camera and actual acceleration/braking tests, controls/settings tests, all 24 course rule checks and swept-footprint path checks have passed. The packaged browser menu and APK signature/package/alignment/download integrity were verified. Checkpoint rule tests use teleportation and do not establish full driven finishes. Phone installation/native gameplay, human difficulty, gold times and retention still need playtesting; this is a prototype rather than a claim of parity with the reference game.

The explicit Editor smoke check is `PrototypeBuild.SmokeTest` with `--roxx-smoke`; it writes to `Builds/Checks/`. The optional `Tools/Check-Prototype.ps1` also needs the workspace-local .NET/Roslyn tools. `node RoxxParkingIndia/Tools/check-web-server.cjs` checks the LAN server's HTTP handling and download isolation.

## Assets and provenance

See [ART-CREDITS.md](RoxxParkingIndia/ART-CREDITS.md) for the CC0 Poly Haven materials/props and the supplied Thar model. The supplied model archive contained no author/license/readme; a redistribution license has not been established. No assets or original method implementations from the analyzed reference APK are included. Reference APKs, extracted analysis and machine logs are excluded.

More detail: [gameplay](RoxxParkingIndia/GAMEPLAY.md), [quality status](RoxxParkingIndia/QUALITY-TARGET.md), [Android app](RoxxParkingIndia/ANDROID-PLAY.md), [browser play](RoxxParkingIndia/WEB-PLAY.md).
