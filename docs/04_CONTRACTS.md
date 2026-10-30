# CONTRACTS — the integration surface between P1, P2 and P3

This file is the single source of truth for names and signatures shared across people. P2 implements `Cr.Core` from it within hours of M0. If you need to change anything here, edit this file in the same pull request and tag the other two people. The code below is the intended shape; adjust trivial details to compile, keep names and semantics.

Verification note for AI assistants: XR Interaction Toolkit (XRI) 3.x type names (`XRGrabInteractable`, `XRSocketInteractor`, `XRSimpleInteractable`, `HapticImpulsePlayer`, `NearFarInteractor`, `XRInteractionManager`, etc.) are used in prose elsewhere. Check each against the version actually installed (`Packages/manifest.json`, package docs, and the XRI samples) before using it. Contracts in THIS file deliberately avoid XRI types so Core compiles with no XR package.

---

## 1. Unity project layout (root: `unity/CanteenRush/`)

```
Assets/
  Art/                       [P1]  imported FBX, textures, materials, lightmaps source
    Environment/  Props/  Items/  Materials/  Textures/
  Audio/                     [P3]  Clips/{Sfx,Ambience,Music,Ui}/  Mixer/  CREDITS.md
  Data/                      [P2]  GameConfig.asset, Items/*.asset, Orders/*.asset, Haptics/*.asset, AudioCues/*.asset
  Lighting/                  [P1]
  Prefabs/
    Gameplay/                [P2]  Item_*.prefab, Tray, TrayRack, FridgeDoor_*, WaterCooler, Customer, ServeSocket...
    World/                   [P1]  Canteen.prefab, Patio.prefab, Backdrop.prefab (art assemblies)
    UI/                      [P3]  WristHUD, MenuBoard, ResultsBoard, TutorialHint
    UIGameplay/              [P2]  OrderRail, OrderTicket, CustomerBubble
    Systems/                 [P2]  Systems.prefab, Rig.prefab   [P3]: Audio.prefab, Haptics.prefab
  Scenes/                    Main.unity [P1]   Sandbox_P2.unity [P2]   Sandbox_P3.unity [P3]
  Scripts/
    Core/        (Cr.Core.asmdef)          [P2, shared]
    Gameplay/    (Cr.Gameplay.asmdef)      [P2]
    Audio/       (Cr.Audio.asmdef)         [P3]
    Haptics/     (Cr.Haptics.asmdef)       [P3]
    UI/          (Cr.UI.asmdef)            [P3; P2 owns Scripts/UI/Gameplay/ subfolder]
    World/       (Cr.World.asmdef)         [P1]  SunCycle, SeatPoint markers, LOD helpers
  Settings/                  [P1]  URP assets, Quality, XR settings
  Tests/EditMode/ (Cr.Tests.EditMode.asmdef) [P2]
  Tests/PlayMode/ (Cr.Tests.PlayMode.asmdef) [P3]
  XR/ Samples/               imported package samples (leave untouched)
Packages/manifest.json       [P1] only person who changes packages; record versions in docs/VERSIONS.md
```

### Assembly dependency rules (enforced by asmdef references)

```
Cr.Core          -> (no references)
Cr.Gameplay      -> Cr.Core, Unity.XR.Interaction.Toolkit, Unity.InputSystem, Unity.AI.Navigation, Unity.TextMeshPro
Cr.Audio         -> Cr.Core            (NO reference to Cr.Gameplay, Cr.Haptics, Cr.UI)
Cr.Haptics       -> Cr.Core, Unity.XR.Interaction.Toolkit   (NO reference to Cr.Gameplay, Cr.Audio, Cr.UI)
Cr.UI            -> Cr.Core, Unity.TextMeshPro, Unity.XR.Interaction.Toolkit
Cr.World         -> Cr.Core
Cr.Tests.EditMode-> Cr.Core (+ Cr.Gameplay only for pure-logic classes), UnityEngine.TestRunner, UnityEditor.TestRunner, nunit
```
Audio, Haptics and UI are listeners: they subscribe to `GameEvents` and must never call into Gameplay. Gameplay must never call them.

---

## 2. Namespaces, naming, style

- Namespaces: `CanteenRush.Core`, `.Gameplay`, `.Audio`, `.Haptics`, `.UI`, `.World`.
- Types PascalCase; private fields `_camelCase`; constants `PascalCase`; events `OnX`; ScriptableObjects suffix `Def`/`Config`/`Template`.
- Prefabs: `Item_Donut`, `Station_WaterCooler`, `Prop_Tray`, `UI_OrderTicket`, `Env_Patio`. Materials: `M_<Name>`. Textures: `T_<Name>_<Suffix: D/N/M/R/E>`. FBX: `Env_*.fbx`, `Prop_*.fbx`, `Item_*.fbx`. Audio: `<cueId>_<nn>.wav` (variants numbered).
- No magic numbers in gameplay: put them in `GameConfig`.
- One public class per file, file named after the class. No `Find*` in `Update`. No allocations in per-frame code paths.
- Comments only where intent is non-obvious (why, not what).

---

## 3. Layers, tags, interaction layers

Unity layers (Project Settings > Tags and Layers — P1 creates them at M0):

| Index | Layer | Use |
|---|---|---|
| 6 | `Environment` | static architecture (occlusion raycasts, NavMesh source, floor probe) |
| 7 | `Items` | donut/cola/shake/cake/cup(water) |
| 8 | `Tray` | trays |
| 9 | `Customers` | customer bodies |
| 10 | `Stations` | fridge doors, cooler, spawner volumes |
| 11 | `WorldUI` | world-space UI colliders (Unity's built-in layer 5 `UI` stays for the Canvas/UGUI default and is not used for physics) |
| 12 | `Player` | XR rig body/hands |
| 13 | `Triggers` | fill zone, serve zone, spawn volumes |

Physics collision matrix: `Items` collide with `Environment`, `Items`, `Tray`, `Stations`, `Player`; `Tray` with `Environment`, `Items`, `Stations`, `Player`; `Customers` only with `Environment`; `WorldUI` with nothing physical; `Triggers` do not collide (trigger overlaps only with `Items`, `Tray`).

Tags: `Tray`, `SeatPoint`, `ServeSocket`, `FillZone`, `SurfaceTile`, `SurfaceStone`, `Spawner`.

XRI interaction layers: `Default`, `Item`, `Tray`, `SocketItem`, `SocketTray`, `UI`. Item grabbables are on `Item`; item sockets on tray accept `Item`; the table `ServeSocket` accepts only `Tray`.

---

## 4. Core types (`Cr.Core`) — implement exactly

```csharp
namespace CanteenRush.Core
{
    public enum Hand { Left, Right }

    public enum ItemId { None = 0, Donut, Cola, Shake, Cake, Water }

    public enum RoundState { Boot, Menu, Tutorial, Wave1, Wave2, Wave3, Results }

    public enum CustomerState { Arriving, Ordering, Waiting, Eating, Leaving, Angry }

    public enum Surface { Tile, Stone, Grass }

    /// One customer's request.
    public sealed class Order
    {
        public int Id;                    // unique per round, increments from 1
        public int SeatIndex;             // 0..7
        public UnityEngine.Vector3 SeatPosition; // world position of the seat (audio/UI anchor)
        public ItemId[] Items;            // 1..3 entries, duplicates allowed
        public float PatienceMax;         // seconds
        public float PatienceRemaining;   // seconds, ticks down while Waiting
        public float Patience01 => PatienceMax <= 0 ? 0 : PatienceRemaining / PatienceMax;
    }

    public enum ServeOutcome { Perfect, Partial, Empty }

    public readonly struct ServeResult
    {
        public readonly int OrderId;
        public readonly ServeOutcome Outcome;
        public readonly int Correct;      // items that matched the order
        public readonly int Wrong;        // items on tray that were not requested (or surplus duplicates)
        public readonly int Missing;      // requested items not on tray
        public readonly int Points;       // net points awarded (>= -infinity; round total is floored at 0)
        public readonly int SpeedBonus;
        public ServeResult(int orderId, ServeOutcome outcome, int correct, int wrong, int missing, int points, int speedBonus)
        { OrderId = orderId; Outcome = outcome; Correct = correct; Wrong = wrong; Missing = missing; Points = points; SpeedBonus = speedBonus; }
    }
}
```

### 4.1 `GameEvents` — the static event bus

All events are plain `System.Action` delegates, raised on the main thread. Raising methods are the only way to fire (so there is one place to add logging). Subscribers MUST unsubscribe in `OnDisable`. `GameEvents.ResetAll()` clears every subscriber. It is called only (a) by test `TearDown`s and (b) from a `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]` hook so static events do not keep stale subscribers across Editor Play-mode entries when domain reload is disabled. It must NEVER be called during a session, because that would silently unsubscribe Audio, Haptics and UI.

```csharp
using System;
using UnityEngine;

namespace CanteenRush.Core
{
    public static class GameEvents
    {
        // ---- Round / flow ----
        public static event Action<RoundState> OnRoundStateChanged;
        public static event Action<float> OnRoundTimeChanged;        // seconds remaining, fired at 10 Hz
        public static event Action<int> OnScoreChanged;              // new total (>= 0)

        // ---- Orders / customers ----
        public static event Action<Order> OnOrderPlaced;
        public static event Action<Order, float> OnOrderPatience;    // order, patience01, fired at 4 Hz while Waiting
        public static event Action<ServeResult> OnOrderServed;
        public static event Action<Order> OnOrderExpired;
        public static event Action<int, CustomerState> OnCustomerState; // seatIndex, state
        public static event Action<int> OnCustomerCount;             // active customers

        // ---- Items / hands ----
        public static event Action<ItemId, Hand, Vector3> OnItemGrabbed;      // item, hand, worldPos
        public static event Action<ItemId, Hand, Vector3> OnItemReleased;
        public static event Action<ItemId, Vector3> OnItemSocketed;           // placed in a tray slot
        public static event Action<ItemId, float, Vector3> OnItemDropped;     // item, impactSpeed m/s, worldPos (spill)
        public static event Action<ItemId, float, Vector3> OnItemImpact;      // non-spill collisions, for clinks
        public static event Action<ItemId, Vector3> OnItemSpawned;

        // ---- Stations ----
        public static event Action<int, float, bool> OnFridgeDoorMoved;       // doorIndex(0 upper,1 lower), open01, detentCrossed
        public static event Action<int, bool> OnFridgeDoorLimit;              // doorIndex, atOpenLimit(true)/closed(false)
        public static event Action<bool, Vector3> OnPumpActivated;            // pressed, worldPos
        public static event Action<float, Hand> OnCupFilling;                 // level01, hand holding cup (when known)
        public static event Action<Vector3> OnCupFilled;                      // became Water
        public static event Action<float, Hand> OnTrayTilt;                   // angleDeg, hand holding tray
        public static event Action<Vector3, bool> OnTrayServedPlaced;         // position, hadActiveOrder

        // ---- Player ----
        public static event Action<float> OnPlayerMoveSpeed;                  // m/s, for vignette & footsteps
        public static event Action<Surface, Vector3> OnFootstep;              // surface, position

        // ---- Tutorial ----
        public static event Action<int> OnTutorialStep;                       // 0..2 = active step, 3 = finished

        // ---- UI ----
        public static event Action OnUiHover;
        public static event Action OnUiClick;

        // Raising API (one line each; null-safe)
        public static void RoundStateChanged(RoundState s) => OnRoundStateChanged?.Invoke(s);
        public static void RoundTimeChanged(float t) => OnRoundTimeChanged?.Invoke(t);
        public static void ScoreChanged(int s) => OnScoreChanged?.Invoke(s);
        public static void OrderPlaced(Order o) => OnOrderPlaced?.Invoke(o);
        public static void OrderPatience(Order o, float p01) => OnOrderPatience?.Invoke(o, p01);
        public static void OrderServed(ServeResult r) => OnOrderServed?.Invoke(r);
        public static void OrderExpired(Order o) => OnOrderExpired?.Invoke(o);
        public static void CustomerState(int seat, CustomerState s) => OnCustomerState?.Invoke(seat, s);
        public static void CustomerCount(int n) => OnCustomerCount?.Invoke(n);
        public static void ItemGrabbed(ItemId i, Hand h, Vector3 p) => OnItemGrabbed?.Invoke(i, h, p);
        public static void ItemReleased(ItemId i, Hand h, Vector3 p) => OnItemReleased?.Invoke(i, h, p);
        public static void ItemSocketed(ItemId i, Vector3 p) => OnItemSocketed?.Invoke(i, p);
        public static void ItemDropped(ItemId i, float speed, Vector3 p) => OnItemDropped?.Invoke(i, speed, p);
        public static void ItemImpact(ItemId i, float speed, Vector3 p) => OnItemImpact?.Invoke(i, speed, p);
        public static void ItemSpawned(ItemId i, Vector3 p) => OnItemSpawned?.Invoke(i, p);
        public static void FridgeDoorMoved(int d, float open01, bool detent) => OnFridgeDoorMoved?.Invoke(d, open01, detent);
        public static void FridgeDoorLimit(int d, bool open) => OnFridgeDoorLimit?.Invoke(d, open);
        public static void PumpActivated(bool pressed, Vector3 p) => OnPumpActivated?.Invoke(pressed, p);
        public static void CupFilling(float level01, Hand h) => OnCupFilling?.Invoke(level01, h);
        public static void CupFilled(Vector3 p) => OnCupFilled?.Invoke(p);
        public static void TrayTilt(float angle, Hand h) => OnTrayTilt?.Invoke(angle, h);
        public static void TrayServedPlaced(Vector3 p, bool hadOrder) => OnTrayServedPlaced?.Invoke(p, hadOrder);
        public static void PlayerMoveSpeed(float v) => OnPlayerMoveSpeed?.Invoke(v);
        public static void Footstep(Surface s, Vector3 p) => OnFootstep?.Invoke(s, p);
        public static void TutorialStep(int step) => OnTutorialStep?.Invoke(step);
        public static void UiHover() => OnUiHover?.Invoke();
        public static void UiClick() => OnUiClick?.Invoke();

        public static void ResetAll() { /* set every event field to null; used by tests and GameManager at round start */ }
    }
}
```

### 4.2 Who raises each event (single raiser per event)

| Event | Raised by (class, assembly) |
|---|---|
| `RoundStateChanged`, `RoundTimeChanged` | `GameManager` (Gameplay) |
| `ScoreChanged` | `ScoreService` (Gameplay) |
| `OrderPlaced`, `OrderPatience`, `OrderServed`, `OrderExpired` | `OrderService` (Gameplay) |
| `CustomerState`, `CustomerCount` | `CustomerManager` (Gameplay) |
| `ItemGrabbed/Released/Socketed/Dropped/Impact/Spawned` | `ItemBehaviour` + `ItemSpawner` (Gameplay) |
| `FridgeDoorMoved`, `FridgeDoorLimit` | `FridgeDoor` (Gameplay) |
| `PumpActivated`, `CupFilling`, `CupFilled` | `WaterCooler`, `CupFill` (Gameplay) |
| `TrayTilt`, `TrayServedPlaced` | `Tray`, `ServeSocket` (Gameplay) |
| `PlayerMoveSpeed`, `Footstep` | `PlayerMotionReporter` (Gameplay) |
| `TutorialStep` | `GameManager` (Gameplay) — advances when the player completes each step |
| `UiHover`, `UiClick` | UI button wrappers (UI assembly, P3 — the only events P3 raises) |

---

## 5. Pure logic (`Cr.Core`) — testable without the engine

```csharp
public static class OrderMatcher
{
    /// Order-independent multiset comparison. Items == ItemId.None are ignored.
    /// Correct = sum over ids of min(requested, onTray); Wrong = onTray surplus; Missing = requested surplus.
    public static (int correct, int wrong, int missing) Evaluate(IReadOnlyList<ItemId> requested, IReadOnlyList<ItemId> onTray);
}

public static class ScoreCalculator
{
    /// Implements MASTER_SPEC §6.4. Never returns a speedBonus for an Empty tray.
    public static ServeResult Score(Order order, IReadOnlyList<ItemId> onTray, float patience01, GameConfig cfg);
    public static int Spill(GameConfig cfg);       // -SpillPenalty
    public static int Timeout(GameConfig cfg);     // -TimeoutPenalty
    public static int Stars(int score, GameConfig cfg);
}

/// Plain class. Time is injected by the caller (Tick(dt)) so tests can drive it.
public sealed class WaveDirector
{
    public WaveDirector(GameConfig cfg, System.Random rng);
    public RoundState Current { get; }
    public float Elapsed { get; }
    public void Tick(float dt);                           // advances waves at cfg.WaveSeconds boundaries
    public bool ShouldSpawnCustomer(int activeCustomers, int freeSeats);   // cadence + wave max
    public OrderTemplate NextTemplate();                  // random pick from the current wave's pool
}
```

`GameConfig` (ScriptableObject, `Assets/Data/GameConfig.asset`), defaults from MASTER_SPEC §6:

```csharp
[CreateAssetMenu] public sealed class GameConfig : ScriptableObject
{
    public float RoundSeconds = 180f;
    public float[] WaveSeconds = { 60f, 60f, 60f };
    public int[] WaveMaxCustomers = { 2, 3, 4 };
    public float[] WaveSpawnInterval = { 14f, 11f, 9f };
    public float PatienceBase = 25f, PatiencePerItem = 15f;
    public int PointsPerItem = 100, SpeedBonusMax = 50;
    public float ComboPerExtraItem = 0.25f;
    public int WrongItemPenalty = 25, SpillPenalty = 10, TimeoutPenalty = 50;
    public int[] StarThresholds = { 300, 700, 1200 };
    public float DonutRespawn = 5f, ShakeRespawn = 8f, CakeRespawn = 10f, ColaRespawn = 6f;
    public float CupFillPerSecond = 0.25f;
    public float TrayTiltWarnDeg = 20f, TrayTiltSpillDeg = 35f;
    public float SpillHeight = 0.30f;
    public float EatSeconds = 4f, TrayReturnSeconds = 3f;
    public float MoveSpeed = 1.8f; public float SnapTurnDeg = 45f;
}
```

`ItemDef` (ScriptableObject, one per `ItemId`): `ItemId Id; string DisplayName; GameObject Prefab; Sprite Icon; AudioMaterial AudioMaterial (Plastic/Glass/Paper/Metal); float Mass; float RespawnSeconds`.
`OrderTemplate` (ScriptableObject): `ItemId[] Items; int MinWave;` (a template is eligible from wave `MinWave` onward).
`HapticPreset` (ScriptableObject): `string Name; float Amplitude; float Duration; bool IsRamp;` — P3 owns `Assets/Data/Haptics/` (the one exception to P2's ownership of `Assets/Data`).
`AudioCueSet` (ScriptableObject): `string CueId; AudioClip[] Variants; Vector2 PitchRange; Vector2 VolumeRange; float MinDistance; float MaxDistance; bool Loop; AudioMixerGroup Group` — P3 owns `Assets/Data/AudioCues/`.

---

## 6. Listener interfaces (P3 deliverables)

```csharp
namespace CanteenRush.Haptics
{
    public interface IHapticsService
    {
        void Pulse(CanteenRush.Core.Hand hand, float amplitude, float durationSeconds);
        void Play(string presetName, CanteenRush.Core.Hand hand);   // looks up a HapticPreset
        void Ramp(CanteenRush.Core.Hand hand, float amplitude01);   // call every frame; service coalesces and sends
        void StopRamp(CanteenRush.Core.Hand hand);
    }
    // Implementations: XRHapticsService (real), NullHapticsService (editor/tests).
    // HapticsEventBinder (MonoBehaviour) subscribes to GameEvents and maps events -> presets per MASTER_SPEC §11.
}
namespace CanteenRush.Audio
{
    public interface IAudioService
    {
        void PlayAt(string cueId, UnityEngine.Vector3 worldPos, float volumeScale = 1f);
        void PlayUi(string cueId);
        void SetSnapshot(AudioZone zone);          // Indoor / Outdoor, cross-fade 0.4 s
    }
    public enum AudioZone { Indoor, Outdoor }
    // AudioEventBinder subscribes to GameEvents and maps events -> cues per MASTER_SPEC §10.
}
```

Zone switching: P1 places two `AudioZoneTrigger` volumes (door threshold) that call `IAudioService.SetSnapshot`. P1 owns the placement, P3 owns the component.

---

## 7. Prefab contracts (P2 delivers; P1 only swaps `Visual`)

Every prefab: root GameObject (logic + collider + Rigidbody), child `Visual` (renderers only, no scripts, no colliders). Mesh swaps happen at `Visual` only. Colliders are primitive and owned by P2 (P1 must tell P2 if the real mesh differs in size by > 10% from the placeholder).

| Prefab | Placeholder visual | Key components (P2) | Real mesh module (P1) |
|---|---|---|---|
| `Item_Donut` | cylinder 0.32×0.07×0.32 | Rigidbody, SphereCollider/CapsuleCollider, XRGrabInteractable, `ItemBehaviour(ItemId.Donut)` | `Item_Donut.fbx` |
| `Item_Cola` | cylinder 0.07×0.16 (0.21 m dia in source; verify) | same, `ItemId.Cola` | `Item_Cola.fbx` |
| `Item_Shake` | cylinder 0.11×0.31 | same, `ItemId.Shake` | `Item_Shake.fbx` |
| `Item_Cake` | cylinder 0.25×0.23 | same, `ItemId.Cake` | `Item_Cake.fbx` |
| `Item_Cup` | cylinder 0.08×0.10 | same + `CupFill` (level), becomes `Item_Water` when full | `Item_Cup.fbx` (new) |
| `Prop_Tray` | box 0.45×0.03×0.30 | Rigidbody, XRGrabInteractable, 4 child socket interactors, `Tray` (tilt logic) | `Prop_Tray.fbx` (new) |
| `Station_TrayRack` | 2 boxes | spawns trays, respawn after serve | `Prop_TrayRack.fbx` (new, simple) |
| `Station_FridgeDoorUpper/Lower` | boxes 0.36×0.86×1.28 / 0.12×0.89×0.85 | HingeJoint, handle grab, `FridgeDoor(doorIndex)` | `Prop_Fridge.fbx` (door pivots at hinge) |
| `Station_WaterCooler` | cylinder + box | pump `XRSimpleInteractable`, FillZone trigger, `WaterCooler` | `Prop_WaterCooler.fbx` |
| `Station_CupDispenser` | box | `ItemSpawner(Item_Cup)` | `Prop_CupDispenser.fbx` (new) |
| `Station_ItemSpawner` | none (invisible marker) | `ItemSpawner(ItemId, respawn)` | n/a (items appear on plates/shelves) |
| `Customer` | capsule + sphere | NavMeshAgent, `Customer` state machine, bubble anchor | none (primitive, final) |
| `Table_ServeSocket` | transparent box (editor-visible, runtime-hidden) | socket accepting `Tray`, `ServeSocket(seatIndex)` | n/a |
| `Marker_SeatPoint` | empty with gizmo | tag `SeatPoint`, `SeatPoint(seatIndex 0..7, facing)` — P1 places | n/a |
| `Marker_CustomerEntrance` | empty | tag only | n/a |

Seat indices 0..7: tables numbered 0..3 in the order given in `docs/LAYOUT.md`; seats `2t` and `2t+1` are the two gameplay chairs at table `t`.

---

## 8. World contract (P1 publishes in `docs/LAYOUT.md`)

`docs/LAYOUT.md` MUST state, in Unity world coordinates (metres, Y up, +Z forward): player start position/yaw; canteen shell centre and footprint; counter front line; fridge, cooler and display-case positions; canteen door position; patio centre and footprint; the 4 table centres; the 8 seat points with facing; customer entrance; NavMesh walkable bounds; audio zone trigger volumes; backdrop module positions. Everything P2/P3 place relative to the world uses those numbers. P1 publishes a first version at M1 and amends it (with a changelog table at the bottom) at M2.

---

## 9. Versioning

At M0 P1 writes `docs/VERSIONS.md`: Unity version, URP version, XRI version, OpenXR version, Input System version, AI Navigation version, Test Framework version, Blender version (5.2.0 LTS). Nobody upgrades packages afterwards without a PR that updates this file.
