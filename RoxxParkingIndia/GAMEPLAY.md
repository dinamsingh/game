# Expert Trials update 0.5.0

## Controls update 0.6.1

Open CONTROLS from the main menu, or PAUSE > CONTROLS while driving. Camera drag sensitivity adjusts from 25% to 200% (default 100%) for touch and right-mouse drag. It does not change the automatic follow-camera damping. STEERING / LEFT or RIGHT swaps the wheel and directional buttons to that side; accelerator, brake, gear and speed move to the opposite side. Reset Defaults restores 100% and left steering.

The wheel diameter is now 300 reference units (previously 234), about 28% larger. Directional steering buttons are 145 x 155 (previously 120 x 135). Layout changes release held steering/pedal input. BACK / SAVE commits sensitivity and side to local PlayerPrefs; Resume or returning to the main menu also saves. Browser and native preferences remain separate; browser site-data deletion removes preferences. Settings do not alter level unlocks or records.


## Update 0.6.0: speed and six new Expert Trials

24 courses: the original 12-course campaign and 12 immediately accessible Expert Trials. Previous course names/order and saved records are retained. Trial stars now total 36; existing paint unlocks remain at 6 and 15 stars.

| Trial / course | Challenge | Gold target |
|---|---|---|
| 07 / 19 Century sprint | 700 m proving ground, 90+ km/h sensor, braking and precision parking | 0:50 |
| 08 / 20 Twin signal gauntlet | Two independently timed crossings, lane change, forward setup and reverse slot | 2:20 |
| 09 / 21 Harbour switchback | Three alternating container offsets and reverse parallel dock | 2:50 |
| 10 / 22 Courtyard parallel | Constrained drive setup and reverse parallel pocket | 1:50 |
| 11 / 23 Reverse horseshoe | Reverse-only full U route around the central structure | 3:10 |
| 12 / 24 Precision endurance | Alternating narrow offsets, two timed gates, final reverse slot | 3:10 |

Forward speed cap is 100 km/h; reverse remains 18 km/h. The Unity physics check measured 100.0 km/h peak, approximately 11.3 seconds to 99.5 km/h, and 63.9 m / 4.64 seconds to brake to below 0.3 m/s. These are flat-track simulation results, not real-car specifications. Steering lock reduces progressively from 34 to 4 degrees above 29 km/h; gear reversal brakes before applying opposite torque. The chase camera smoothly widens its view at speed. Use the proving ground for high speed; parking courses require controlled braking and slow turns.

Environment changes: 2K scanned asphalt, concrete, brick and rock with normal/roughness/AO maps, metre-scaled UVs, contact shading, filmic highlight roll-off, road repair/wear, shaped concrete barriers, and a dedicated gantry-lined proving ground. Pause > Graphics switches High/Balanced; Balanced disables contact shading and reduces shadows. This setting is session-only.

All 24 layouts passed the full swept-footprint path search. Actual high-speed physics, rendering and browser build validation are recorded in `Builds/Checks`. Teleported checkpoint tests do not establish a full driven finish. Gold times, human difficulty, retention and phone frame rate require playtesting.


The game now has **12 campaign courses plus 6 separate Expert Trials**. Choose **EXPERT TRIALS** on the main menu to play any expert course immediately; campaign unlocks are not required or advanced by a trial clear. Existing campaign progress, records and paints remain available.

| Trial | Actual driving problem | Authored gold target |
|---|---|---|
| 13 Steel snake | Opposite container-end clearances; plan the front/rear sweep through both offsets | 90 s |
| 14 Mirror alley | Reverse-only passage, narrow wall intrusions and alternate mirror clearances | 75 s |
| 15 Three point escape | Short dead-end pocket; forward heading gate, reverse setup and forward exit heading | 80 s |
| 16 Red light precision | Physical lifting gate with red/green signal, then an offset 3.5 m opening | 85 s |
| 17 Backwards dogleg | Entire opposite-offset route in R, not just changing gear at the parking bay | 120 s |
| 18 Freight master exam | Container offsets, a live gate crossing, forward setup and final reverse dock | 150 s |

Direction gates paint their required nose direction on the ground. Reverse-only trials start in R and reject gear toggling; keyboard W/S and the accelerator apply throttle in R. No-contact collision rules remain active. The fixed difficulty labels and gold targets are authored values requiring human calibration; no claim about measured difficulty or retention follows from them.

Moving gates use kinematic physics and take 1.2 seconds to lift/lower. Red/green status and a nearby HUD countdown communicate the crossing window. An occupied crossing holds an open gate, and a closing gate reverses toward open if the vehicle enters its safety zone. Pause stops the cycle. Retry resets the route, car and gate timing while reusing existing scenery; R retries immediately on PC, and TRY AGAIN does the same on touch.

Each trial stores its best medal as 1/2/3 stars for bronze/silver/gold, up to **18 total**. Replaying the same medal does not farm extra stars. **Midnight Blue unlocks at 6 stars; Desert Gold at 15**. Both are selectable in the garage once earned. Results show personal bests, time improvement, newly gained stars, paint unlocks and the improvement needed for gold. There is no time-limit failure: a clean slower finish still earns bronze and persists progress.

Swept-footprint searches cover all 18 routes, reverse-only travel and direction constraints. The sampled three-point route uses four gear changes; that is solver evidence, not a claim about a human's minimum or enjoyment. Unity checks cover closed/open gate collision clearance, occupied hold, pause, retry reuse/reset, trial access from a locked campaign, star/paint eligibility and campaign progress isolation. Test rule traversal teleports between objectives. Captured 4–8 second medal times in result/test-earned images come from these tests and are not driving records. Test preferences are restored afterward.

Full driving runs and user retention remain unmeasured. Calibrate each trial from clean finishes, contacts, retries, steering corrections, completion times and whether players choose another attempt; tune widths/targets from that evidence. This update adds concrete manoeuvres and a repeatable mastery/reward loop, not a guaranteed retention outcome or a graphics overhaul.

## Workshop update 0.4.0

The new menu button **NEW / WORKSHOP PRACTICE** opens the updated level 05 immediately, including on a fresh install. Practice supports pause/retry and completion without changing unlocks, best times or medals. Normal progression still opens this course after level 04. The changed layout is named **Workshop reverse delivery**, so its records start separately from the old Back into service course; other course records and saved unlocks are retained.

The representative course is now a 32 x 44 m service yard. A 5 m clear crate corridor leads around freight and into a setup area, followed by an actual backward-motion gate and a 3 m reverse bay. Four ordered gates replace the old three-point open approach. The 85-second gold target and difficulty 3/5 are authored starting values, not calibrated human timings.

Workshop scenery uses asymmetric service blocks, an open bay with bench/tool storage, pitched roofs, gutters, a parts office, loading canopy and drainage. Licensed CC0 crate, barrel and trolley models retain their UVs and use diffuse, normal and AO maps; worn-metal/shutter/timber surfaces replace plain prop materials. See [ART-CREDITS.md](ART-CREDITS.md). Lighting has less ambient fill to retain material and shadow contrast. The supplied Thar rig, controls and camera behaviour are retained.

Review the normal chase-camera image in `Builds/Checks/workshop-driving.png`, the layout in `workshop-overview.png`, and the workshop frontage in `workshop-service.png`. These are rendered scene captures, not a completed human driving run. Rendering and automated checks do not establish parity with the original game or phone performance. Reference-game driving remains blocked by ad/loading behaviour described in [EMULATOR-SETUP.md](../apk-analysis/comparison/EMULATOR-SETUP.md).

## Existing progression

**Quality status (26 October 2026):** this is a functional prototype, below the user's requested original/reference quality target. The 1–5 labels and gold times are authored targets, not measured human difficulty or proof of parity. See [QUALITY-TARGET.md](QUALITY-TARGET.md) and the [original/current comparison](../apk-analysis/comparison/COMPARISON.md) before further level/art changes.

12 original courses across four separately generated locations. Each course loads its own scenery, bounds and obstacles; restarting replaces and releases that course's geometry. Existing unlock progress is preserved.

| Level | Location | Challenge | Difficulty | Bay width | Gold target |
|---|---|---|---|---|---|
| 01 | City school | Straight approach and braking | 1/5 | 3.60 m | 0:30 |
| 02 | City school | Cross-lot route points | 1/5 | 3.40 m | 0:45 |
| 03 | City school | Corner judgement, sideways bay | 2/5 | 3.25 m | 0:50 |
| 04 | Brick workshops | Loading-apron turns | 2/5 | 3.10 m | 1:00 |
| 05 | Motor workshop yard | Crate corridor, turns and reverse service slot | 3/5 | 3.00 m | 1:25 |
| 06 | Brick workshops | Reverse side pocket | 3/5 | 2.95 m | 1:15 |
| 07 | Freight terminal | Long container slalom | 3/5 | 2.90 m | 1:40 |
| 08 | Freight terminal | Circling stacks, reverse dock | 4/5 | 2.80 m | 1:35 |
| 09 | Freight terminal | Slalom and tight reverse slot | 4/5 | 2.70 m | 1:55 |
| 10 | Ridge outpost | Long offsets and road judgement | 4/5 | 2.65 m | 1:45 |
| 11 | Ridge outpost | Turnaround and reverse pocket | 5/5 | 2.60 m | 1:50 |
| 12 | Ridge outpost | Five route points, final precision reverse | 5/5 | 2.55 m | 2:10 |

The SUV's tested parking footprint is 1.96 x 4.50 m. Last-course side clearance is about 29.5 cm per side when perfectly centred. All levels still allow a 1.6-second stationary hold and a maximum 12-degree heading error, with the entire rotated footprint inside the bay. Narrow bays naturally require tighter alignment than 12 degrees to fit.

Amber ground rings must be visited in order. Only the next point is shown. A navigation arrow gives its direction relative to the camera. Reverse points require selected R, actual backward motion and the car's nose within 30 degrees of the painted arrow. Reverse bays require R and the exact intended heading within 12 degrees, so driving straight into them in D cannot finish. The first course is deliberately simple and has no route gates.

Every successful park earns bronze; within 150% of the target earns silver, within target earns gold. There is no failure timer. The fastest time and highest medal are saved per course. Route-clear sound/flash and a completion chord acknowledge progress. Four district pages show saved records and locked courses.

Scenery includes textured brick workshops, loading shutters, corrugated container rows, wire fences, benches, kerbstones, drains, streetlights, trees and a distant ridge heightfield. UVs preserve 3 m scanned-material scale across differently sized surfaces. Ridge driving surfaces are currently flat; terrain relief is a backdrop. Building interiors, moving traffic and suspension-driving slopes are outside this update.

`Tools/Check-Prototype.ps1` tests pure parking/checkpoint/medal rules, C# syntax and all course geometry. The Python Hybrid A* checker uses the full oriented car rectangle, an additional 8 cm size margin, a 4.2 m minimum turning radius, both gears and 0.3 m swept samples. It checks every ordered route point and a valid final parking pose. It does not simulate WheelCollider tyre friction, steering lag, human skill or measured gold-time fairness.

The Unity smoke test checks all 12 course loads, route reset/skip rejection, reverse gate motion/gear restrictions, reverse bay gear/heading restrictions, successful parking and persisted records; it restores the user's test-touched preferences. Gate traversal is teleported for rule tests. Separate acceleration/braking/camera tests use actual simulated motion. Human driving and physical Android touch/performance still need playtesting.
