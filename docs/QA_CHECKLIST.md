# QA checklist (P3)

How to use: run in the Unity Editor 6000.0.43f1 with the XR Device Simulator (`Main.unity`), headphones on, haptics HUD visible (F9). Fill **Result** with `PASS`, `FAIL #issue` or `N/A`, and the date and initials. File each failure as a GitHub issue titled `[P1]`/`[P2]`/`[P3]` (who must act) with repro steps, expected and actual.

Status legend used below:
- **Not run**: needs the Unity Editor; not executed yet.
- **Code-checked**: behaviour reviewed in code and type-checked against the Unity DLLs (Roslyn), but not run.
- **Auto**: covered by a PlayMode test in `Assets/Tests/PlayMode/P3SmokeTests.cs` (still needs a Test Runner run).

**Execution record**

| Date | Who | Build / commit | Scope | Outcome |
|---|---|---|---|---|
| 2026-11-13 | P3 (with AI assistant) | branch `p3/audio-haptics-ui-qa` | Static checks only: every P3 C# file type-checked with Roslyn against the Unity 6 engine + editor DLLs (package types stubbed); each P3 assembly compiled alone with only its asmdef references (Audio: Core only; Haptics: Core + XRI + Input System; UI: Core + uGUI + XRI). Generated clips checked for length, peak, RMS and loop seams. | Compiles, 0 errors. **No Play-mode run:** no working Unity Editor on this machine (see `DECISIONS.md` 2026-11-13). |

---

## 0. Automated (Test Runner > PlayMode)
| ID | Test | Covers | Result |
|---|---|---|---|
| T-a1 | `A1_SandboxLoadsWithAllP3Systems` | Sandbox_P3 loads, no errors, every P3 system present, menu visible | Not run |
| T-a2 | `A2_MainLoadsWithP3Systems` | Main loads with no errors; Audio/Haptics/UI shell installed (prefab or fallback); ambience placed | Not run |
| T-b | `B_ScriptedRoundReachesResults` | full synthetic round reaches Results; tallies and stars correct; Replay hides results | Not run |
| T-c1 | `C1_EveryCueHasARealClip` | 22 cue IDs resolve to assigned clips and mixer groups; mixer has 6 groups and both snapshots | Not run |
| T-c2 | `C2_EveryPresetResolvesAndMatchesTheSpecTable` | 13 presets resolve with §11 amplitude/duration/ramp values | Not run |
| T-c3 | `C3_EventsSelectTheRightPresetAndPlayASound` | event → preset mapping, event → cue mapping, every haptic has a paired sound | Not run |
| T-c4 | `C4_RateLimitAndDoublePulse` | 30 ms rate limit; ServeOk sends two pulses | Not run |
| T-de | `D_E_ThreeRoundsNoLeaksAndSubscriptionsReturnToBaseline` | 3 rounds: managed memory delta < 5 MB, no exceptions; GameEvents subscriber counts unchanged | Not run |

## 1. Stations (MASTER_SPEC §6.2)
| ID | Step | Expected | Result |
|---|---|---|---|
| S1 | Grab a donut from the display case | Donut follows hand; `grab_soft` at the hand; Grab tick on HUD; slot respawns after 5 s | Not run |
| S2 | Pull the upper fridge door open past 8° | Latch click (open variant) from the fridge; DoorLatch flash; hum louder as the door opens | Not run |
| S3 | Open fully | Door stops at 100–110°; dull thunk; DoorStop flash | Not run |
| S4 | Close the door | Close-variant click as it passes 8° | Not run |
| S5 | Take a cola with the door closed | Not possible (cola protected) | Not run |
| S6 | Cup under the spout, hold the pump | `pump_press` clank; water loop starts, pitch rises 0.9 → 1.2 over 4 s; WaterFill ramp grows on the cup hand | Not run |
| S7 | Release the pump halfway | Water loop and ramp stop; cup is not Water | Not run |
| S8 | Take a shake and a cake from the counter | Glass-pitched grab for the shake; respawn 8 s / 10 s | Not run |
| S9 | Put two items on the tray | Snap + `tray_clink`; SocketSnap flash | Not run |
| S10 | Tilt the tray 20° → 35° | Rattle speeds up; TrayTiltWarn ramp 0.15 → 0.7; above 35° items slide off | Not run |
| S11 | Drop a donut / a shake on the floor | Thud / glass break at the floor; Spill flash (0.85, 0.10 s); −10 score | Not run |
| S12 | Serve a perfect order | `customer_happy` at the table + `ui_confirm`; ServeOk double pulse on both hands | Not run |
| S13 | Serve a partial order | Lower `customer_happy`; ServePartial (0.35, 0.12 s) | Not run |
| S14 | Let an order expire | `customer_angry` at the seat + `ui_error`; OrderExpired long buzz on both hands | Not run |

## 2. Audio cues (MASTER_SPEC §10), one row per cue
| ID | Cue | How to trigger | Expected | Result |
|---|---|---|---|---|
| A1 | amb_fridge_hum | stand by the fridge | audible within 9 m, from the fridge; louder with door open | Not run |
| A2 | amb_canteen_room | stand inside the kiosk | room murmur; fades out (0.4 s) on leaving | Not run |
| A3 | amb_patio_birds | walk onto the patio | wind + birds from 4 emitters around the patio | Not run |
| A4 | amb_patio_crowd | wait for customers | louder with more customers (0 → 4) | Not run |
| A5 | fridge_latch | S2/S4 | open and close variants differ | Not run |
| A6 | pump_press | S6 | clank at the cooler | Not run |
| A7 | water_fill_loop | S6 | pitch rises with level | Not run |
| A8 | grab_soft / release_soft | S1 | per-material pitch (glass higher, paper lower) | Not run |
| A9 | tray_clink | S9, knock items | volume follows impact speed, ±5 % pitch | Not run |
| A10 | item_drop / item_drop_glass | S11 | thud; glass for the shake | Not run |
| A11 | order_bell | new order | rings AT the customer's seat; higher pitch = farther table (8 semitone steps) | Not run |
| A12 | customer_happy / customer_angry | S12 / S14 | from the customer | Not run |
| A13 | footsteps_tile / footsteps_stone | walk inside / outside | tile click indoors, gritty stone outdoors (2D) | Not run |
| A14 | ui_click / ui_confirm / ui_error | menu buttons, serve, expire | 2D, UI bus | Not run |
| A15 | music_loop | start a round | fades in on Wave 1, out at Results; duller indoors (Indoor snapshot low-pass) | Not run |
| A16 | round_end_sting | round ends | plays once at Results | Not run |
| A17 | Snapshot switch | walk through the door | 0.4 s cross-fade Indoor ↔ Outdoor; reverb only inside | Not run |
| A18 | Occlusion | stand outside behind the kiosk wall from the fridge | hum muffled (low-pass 1.2 kHz, ×0.7) within ~0.25 s | Not run |
| A19 | Direction | turn the head with a bell ringing | bell pans left/right correctly (headphones) | Not run |
| A20 | Voice budget | trigger many clinks at once | no more than 16 voices (Audio settings), no dropouts of UI sounds | Not run |

## 3. Haptics (MASTER_SPEC §11); check on the HUD (F9) and in the log
| ID | Preset | Expected HUD line | Result |
|---|---|---|---|
| H1 | Grab | grabbing hand, 0.25, 0.040 s | Not run |
| H2 | Release | 0.10, 0.030 s | Not run |
| H3 | SocketSnap | last holder hand, 0.35, 0.060 s | Not run |
| H4 | DoorLatch | 0.45, 0.050 s at 8° | Not run |
| H5 | DoorStop | 0.60, 0.080 s at the open limit | Not run |
| H6 | PumpPress | hand not holding the cup, 0.50, 0.070 s | Not run |
| H7 | WaterFill | `~ramp` on the cup hand, amplitude 0.10 → 0.45 | Not run |
| H8 | TrayTiltWarn | `~ramp` on the tray hand, 0.15 → 0.70 from 20° to 35° | Not run |
| H9 | ServeOk | two lines per hand, 0.50 / 0.060 s, ~0.11 s apart | Not run |
| H10 | ServePartial | both hands, 0.35, 0.120 s | Not run |
| H11 | Spill | 0.85, 0.100 s | Not run |
| H12 | OrderExpired | both hands, 0.70, 0.300 s | Not run |
| H13 | UiTick | hover/click a button, 0.15, 0.020 s | Not run |
| H14 | Rate limit | grab and release very fast | never two lines within 30 ms on one hand | Not run |
| H15 | Real device | (only with a headset) HUD hides itself, pulses felt | N/A (no headset) |

## 4. UI shell
| ID | Step | Expected | Result |
|---|---|---|---|
| U1 | Start Main | Menu board ~1.4 m in front, readable, high contrast | Not run |
| U2 | Point at each button with the controller ray | hover highlight + soft click + UiTick | Not run |
| U3 | Seated toggle | label flips; camera offset rises 0.5 m (P2's ComfortSettings) | Not run |
| U4 | Teleport toggle | label flips; teleport works on floors | Not run |
| U5 | Play | tutorial starts; hint card + pulsing ring on the nearest donut | Not run |
| U6 | Tutorial steps | ring moves to tray, then to the customer's seat; "Great!" then hidden | Not run |
| U7 | Wrist HUD | on the left controller: time m:ss (red under 15 s), score, wave | Not run |
| U8 | Results | score, stars, served/correct/wrong/spill/expired match what happened | Not run |
| U9 | Replay | back to Playing with score 0 and clean stations | Not run |
| U10 | Menu (from Results) | menu board returns | Not run |
| U11 | Quit | exits the player (stops Play mode in the Editor) | Not run |
| U12 | Text size | nothing smaller than 24 units at canvas scale (code: `UiFactory.Label` clamps to 24) | Code-checked |

## 5. Comfort, robustness, performance
| ID | Step | Expected | Result |
|---|---|---|---|
| C1 | Move with the left stick | vignette fades in while moving | Not run |
| C2 | Snap turn | 45° steps, no smooth rotation | Not run |
| C3 | Seated mode round | counter reachable while seated | Not run |
| C4 | Restart ×3 (Replay three times) | no duplicate sounds/haptics, memory flat in the Profiler, no errors | Not run (Auto: T-de) |
| C5 | Low frame rate (Application.targetFrameRate = 20 via the Profiler or a slow machine) | ramps still smooth (time-based), water stops when the cup leaves, no stuck loops | Not run |
| C6 | Audio voices in the Profiler (Audio module) during wave 3 | ≤ 16 playing voices | Not run |

## Bugs found
None filed yet: nothing has been executed in Unity. Integration requests (not bugs) are in the PR description and `DECISIONS.md` 2026-11-13.
