# ROLE P2 — GAMEPLAY & XR (1.0 person, ≈ 12 person-days)

You are the AI assistant working for **Person 2**. Read, in order: `docs/00_MASTER_SPEC.md` (all; §6 is your bible), `docs/04_CONTRACTS.md` (all), then this file. Skim `docs/05_ASSET_BIBLE.md` §1, §3.1 and §6 for item sizes. You do not have the conversation behind these documents; if something is ambiguous ask your human and record the answer in `docs/DECISIONS.md`.

## Paste this at the start of your session

> You are the AI pair for Person 2 on the Canteen Rush VR mini-project (Unity 6000.0.43f1, URP, XR Interaction Toolkit 3.x). Read docs/00_MASTER_SPEC.md, docs/04_CONTRACTS.md and docs/02_ROLE_P2_GAMEPLAY.md before writing code. You own only the paths under "Owns". Write pure logic test-first (EditMode tests) before implementation. Verify every XR Interaction Toolkit type name and API against the installed package version and its samples; do not rely on memory. Audio, haptics and UI never get called from gameplay — raise events on GameEvents instead.

## Mission

Build the game: XR rig and comfort, every interaction in master spec §6.2, orders/customers/waves/scoring, the game state machine, the order-related UI, and the unit tests. Deliver a fully playable round on primitive placeholder art by M1, so others can integrate.

## Owns (only you edit)

`Assets/Scripts/Core/**` (shared; PR rules in master spec §3), `Assets/Scripts/Gameplay/**`, `Assets/Scripts/UI/Gameplay/**` (order rail, ticket, customer bubble, vignette — inside Cr.UI assembly folder), `Assets/Prefabs/Gameplay/**`, `Assets/Prefabs/UIGameplay/**`, `Assets/Prefabs/Systems/Systems.prefab` and `Rig.prefab`, `Assets/Data/**` except `Haptics/` and `AudioCues/`, `Assets/Scenes/Sandbox_P2.unity`, `Assets/Tests/EditMode/**`, `docs/LOG_P2.md`.

You consume: Unity project from P1 (M0), `docs/LAYOUT.md`, world prefabs. You provide: `Cr.Core` (everyone depends on it), all gameplay prefabs with placeholder `Visual`s, events.

## Task list with acceptance criteria

### M0 — Core contracts (first 2 hours after P1 creates the project) — blocks P1 & P3

1. Create `Assets/Scripts/Core/Cr.Core.asmdef` and implement every type in contracts §4: enums, `Order`, `ServeResult`, `GameEvents` (all events, raisers, `ResetAll()` that nulls every event via reflection or an explicit list), `GameConfig`, `ItemDef`, `OrderTemplate`, and the stubs for `OrderMatcher`, `ScoreCalculator`, `WaveDirector` (compilable, throwing `NotImplementedException` is acceptable for the logic bodies until task 2). Create the asmdefs for Gameplay (references per contracts §1).
   *Done when:* project compiles with the asmdefs in place; P3 can reference `GameEvents` and `Hand`; pushed to `main` via PR the same day.

### M1 — Playable on primitives — ≈ 6 days

2. **Pure logic, TDD.** Write EditMode tests first, then the code:
   - `OrderMatcherTests`: exact match; order-independent; extra item; missing item; duplicates (`Donut,Donut` requested, one on tray); `ItemId.None` ignored; empty tray; empty order.
   - `ScoreCalculatorTests`: every formula in master spec §6.4 with a table of cases (perfect single, perfect 3-item combo multiplier, partial, wrong item penalty, speed bonus at patience 1.0/0.5/0.0, empty tray gives 0 and no speed bonus, `Stars` thresholds, total floored at 0 is enforced by `ScoreService`).
   - `WaveDirectorTests`: with a fake clock, wave 1 → 2 → 3 → Results at 60/120/180 s; spawn cadence per wave; max concurrent customers respected; no spawn when no free seats; `NextTemplate()` only returns templates with `MinWave <= current wave`; deterministic with a seeded `System.Random`.
   *Done when:* all tests green in Test Runner (EditMode), no engine dependencies in these classes.
3. **Data assets:** `GameConfig.asset` with the defaults; `ItemDef` ×5; `OrderTemplate` assets: Wave 1: `[Donut]`, `[Cola]`, `[Shake]`, `[Water]`, `[Cake]`; Wave 2 (MinWave 2): `[Donut,Cola]`, `[Cake,Water]`, `[Shake,Donut]`; Wave 3 (MinWave 3): `[Donut,Cola,Cake]`, `[Shake,Donut,Water]`, `[Cola,Cola,Donut]`.
4. **XR rig (`Rig.prefab`):** based on the XRI Starter Assets rig: continuous move (left stick, 1.8 m/s), snap turn 45° (right stick), no teleport by default (toggle in a settings object read from the menu), comfort vignette (a small script + material that fades alpha with `PlayerMoveSpeed`), seated-mode height offset toggle. `PlayerMotionReporter` raises `PlayerMoveSpeed` and `Footstep(surface, pos)` every ~0.5 m travelled (surface via downward raycast tag `SurfaceTile/SurfaceStone`, default Tile). Must work with the XR Device Simulator.
5. **Item system:**
   - `ItemBehaviour(ItemId)` on each item prefab: wraps the XRI grab events → `GameEvents.ItemGrabbed/Released` with the correct `Hand`; detects "dropped" (position below `SpillHeight`, not held, not socketed) → `ItemDropped`, despawn after 2 s via pool; collision impacts → `ItemImpact` (rate-limited to 1 per 0.1 s per item).
   - `ItemSpawner(ItemId, respawn seconds, slot transform)` keeps one item present; when the item leaves the slot radius (0.15 m) start the respawn timer; uses a small object pool per `ItemId`. Raises `ItemSpawned`.
   - Prefabs `Item_Donut/Cola/Shake/Cake/Cup` (placeholder visuals per contracts §7), Rigidbody mass 0.2–0.4 kg, continuous-dynamic, interpolation on.
6. **Tray:** `Prop_Tray` prefab with four socket interactors (2×2 layout, 0.1 m apart) accepting only the `Item` interaction layer; `Tray` script exposes `IReadOnlyList<ItemId> Contents` (from the sockets), raises `ItemSocketed` on snap, computes tilt angle each frame while held (angle between `transform.up` and `Vector3.up`), raises `TrayTilt(angle, hand)` at ≥ `TrayTiltWarnDeg`, and above `TrayTiltSpillDeg` releases the socketed items (disable sockets for 0.5 s). `Station_TrayRack` holds 2 trays; a tray returns to its rack slot `TrayReturnSeconds` after being served.
7. **Fridge:** `FridgeDoor(doorIndex)` with `HingeJoint` (axis from the hinge object, limit 0–110°, no spring), grab on the handle; `open01 = angle/110`; fire `FridgeDoorMoved(door, open01, detentCrossed)` when the angle changes by > 1° (and `detentCrossed=true` on crossing 8°), `FridgeDoorLimit(door, true/false)` at the limits. Cola spawners sit inside the upper section and are inert (kinematic, un-grabbable) while the door is below 8° to avoid grabbing through the door.
8. **Water cooler + cup:** `WaterCooler` with a pump `XRSimpleInteractable` (activated by trigger): raises `PumpActivated(true/false, pos)`. `FillZone` trigger under the spout detects an `Item_Cup`; while pump active and cup present: `CupFill.level += CupFillPerSecond × dt`, raise `CupFilling(level, hand)`; at 1.0 → swap to `ItemId.Water` (change the item's `ItemId`, toggle the "water" child visual), raise `CupFilled`. Partially filled cup is not `Water`. `Station_CupDispenser` = an `ItemSpawner` for cups.
9. **Customers & seating:** `CustomerManager` (spawn per `WaveDirector`, `NavMeshAgent` to a free `SeatPoint`, on arrival `Ordering` → create `Order` via `OrderService`); `Customer` state machine `Arriving → Ordering → Waiting → Eating → Leaving` / `Angry`; raises `CustomerState` and `CustomerCount`; head turns toward the player when within 6 m (a single transform rotation); walk bob via sine on the visual child; variants by `MaterialPropertyBlock` colour. `ServeSocket(seatIndex)` on each table: accepts only `Tray`; on placement raises `TrayServedPlaced(pos, hadActiveOrder)` and, if the seat has a `Waiting` order, calls `OrderService.Serve(seatIndex, tray.Contents)`.
10. **OrderService & ScoreService:** `OrderService` owns active `Order`s, ticks patience (raises `OrderPatience` at 4 Hz, `OrderExpired` on 0, applies timeout penalty via `ScoreService`), `Serve(...)` calls `ScoreCalculator.Score` and raises `OrderServed`; `ScoreService` keeps the total (floored at 0), raises `ScoreChanged`; spill penalty subscribes to `ItemDropped`.
11. **`GameManager`:** state machine per master spec §6.5; owns the round clock (raises `RoundTimeChanged` at 10 Hz); on a round start it resets spawners, trays, orders, customers and score (never call `GameEvents.ResetAll()` here — it would unsubscribe Audio/Haptics/UI; see contracts §4.1); Tutorial state with 3 steps (hooks for P3's hint UI — expose `TutorialStep` events through `GameEvents` if needed, add to contracts via PR).
12. **Order UI (gameplay-owned):** `UI_OrderRail` with up to 4 `UI_OrderTicket`s (icons from `ItemDef.Icon`, seat number, patience bar fed by `OnOrderPatience`) and `UI_CustomerBubble` above each customer (billboard on Y, icons, ring). Placeholder icons: coloured circles with letters until final icons exist.
13. **Sandbox scene `Sandbox_P2.unity`** with the rig, a flat floor, one of every station on primitives, 2 seats, so you can run a whole round without `Main.unity`.
   *M1 done when:* in `Sandbox_P2` (and later in P1's greybox) a full 180 s round is completable with the simulator: pick items, tray, serve, score updates, waves progress, round ends in Results.

### M2–M3 — Integration and polish — ≈ 3 days

14. After P1's greybox: place stations in `Main.unity`'s Stations root by giving P1 the prefab list (or open a PR with a `Stations` prefab P1 drops in). Retune colliders when real meshes arrive (≤ 10% differences — you own colliders).
15. **Balance pass:** play 5 rounds; tune `GameConfig` so a competent player reaches 2 stars on first try and 3 stars needs near-perfect play; document the numbers in `docs/LOG_P2.md`.
16. **Edge cases:** item thrown onto the tray from a distance; two trays on one table; tray carried back and forth; serving a table with no order; restart mid-round (Results → Replay → Playing) with no leaked objects/subscribers; seat exhaustion; fridge door grabbed by both hands; cup refilled; item dropped onto a table (should not count as spill unless below `SpillHeight`).
17. **Performance hygiene:** zero allocations in `Update` paths (use Profiler: GC Alloc per frame = 0 in gameplay loops); pooling; ≤ 40 active Rigidbodies.

### M4–M6 — Support

18. Fix bugs from P3's QA checklist. Keep `docs/LOG_P2.md` current. Be ready to explain: event bus decoupling, pure logic vs MonoBehaviour, why sockets rather than parenting items, hinge joint limits, pooling, TDD.

## Behaviour details you must get right

- **Serve evaluation:** `ScoreCalculator.Score(order, tray.Contents, order.Patience01, cfg)`. Correct = multiset match count; Wrong = tray items not requested (or surplus duplicates); Missing = requested minus matched. `Outcome = Perfect` iff Wrong == 0 && Missing == 0 && Correct > 0; `Empty` if tray has no items; else `Partial`. Points = Correct × PointsPerItem × (Perfect && n ≥ 2 ? 1 + ComboPerExtraItem × (n − 1) : 1) + speedBonus − Wrong × WrongItemPenalty, where speedBonus = `round(SpeedBonusMax × patience01)` only if Correct > 0.
- **Patience** ticks only in `Waiting` state (not while Arriving/Ordering/Eating).
- **Seat assignment:** random free seat; never two customers in a seat; free the seat on `Leaving` complete.
- **Items are only valid for serving while socketed in the tray.** Loose items on the table are ignored.
- **XRI wrapper rule:** keep XRI-specific calls inside thin wrappers (`ItemGrabbable`, `TraySockets`, `HandResolver` that maps an interactor to `Hand`). If the XRI API shifts, only wrappers change.

## Constraints and traps

- No `Find*` at runtime in hot paths; cache in `Awake`/`Start`.
- Raise events; never call audio/haptics/UI-shell code. You may reference `Cr.UI` only for the order rail/bubble inside your own subfolder (it is in your folder and compiles into `Cr.UI` per contracts — if that causes asmdef cycles, move those scripts to `Cr.Gameplay` and tell P3).
- Don't edit `Main.unity`; give P1 prefabs.
- Static event leaks: always unsubscribe in `OnDisable`. Test by replaying 3 rounds and checking subscriber counts.
- The XR Device Simulator cannot press physical buttons you haven't mapped; use the Starter Assets input actions (grip/trigger/thumbstick) and make sure every interaction works with them.
- Do not add features outside master spec §6; propose in `docs/DECISIONS.md` first.

## Definition of done for you

Unit tests green; a full round playable in `Main.unity` with real art; every §6.2 interaction implemented; GC Alloc 0 in gameplay loops; three consecutive rounds with no errors or leaks; `docs/LOG_P2.md` written; you can explain every class you wrote without opening the file.
