using System.Collections;
using CanteenRush.Audio;
using CanteenRush.Core;
using CanteenRush.Haptics;
using UnityEngine;
using UnityEngine.InputSystem;
namespace CanteenRush.P3Sandbox
{
    /// Sandbox_P3 debug panel (F10): raise any GameEvents event with a button, at a chosen world position,
    /// and see which cue played and which haptic preset was selected. Also "every cue" / "every preset" sweeps.
    public sealed class P3DebugPanel : MonoBehaviour
    {
        static readonly string[] PositionNames = { "Head", "Left 4 m", "Right 4 m", "Behind 4 m", "Front 8 m", "Seat 0", "Seat 3", "Seat 7" };
        [SerializeField] bool _visible = true;
        int _pos;
        string _lastCue = "-", _lastPreset = "-";
        Vector2 _scroll;
        Hand _hand = Hand.Right;
        float _level, _angle = 25f;

        void OnEnable() { AudioService.CuePlayed += Cue; HapticsServiceBase.PresetRequested += Preset; }
        void OnDisable() { AudioService.CuePlayed -= Cue; HapticsServiceBase.PresetRequested -= Preset; }
        void Cue(string id, Vector3 p) => _lastCue = id + "  at " + p.ToString("F1");
        void Preset(string name, Hand hand) => _lastPreset = name + "  (" + hand + ")";

        Vector3 Where()
        {
            var cam = Camera.main;
            var head = cam != null ? cam.transform : transform;
            var flat = Quaternion.Euler(0, head.eulerAngles.y, 0);
            switch (_pos)
            {
                case 1: return head.position + flat * new Vector3(-4, 0, 0);
                case 2: return head.position + flat * new Vector3(4, 0, 0);
                case 3: return head.position + flat * new Vector3(0, 0, -4);
                case 4: return head.position + flat * new Vector3(0, 0, 8);
                case 5: return SyntheticRound.SeatPosition(0);
                case 6: return SyntheticRound.SeatPosition(3);
                case 7: return SyntheticRound.SeatPosition(7);
                default: return head.position + head.forward * 0.4f;
            }
        }

        int Seat => _pos == 6 ? 3 : _pos == 7 ? 7 : 0;

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.f10Key.wasPressedThisFrame) _visible = !_visible;
        }

        void OnGUI()
        {
            if (!_visible) return;
            GUILayout.BeginArea(new Rect(10, 10, 360, Screen.height * 0.62f), GUI.skin.box);
            GUILayout.Label("P3 DEBUG PANEL (F10)    head: arrows/QE turn, WASD walk, RMB look");
            GUILayout.Label("Cue: " + _lastCue);
            GUILayout.Label("Haptic: " + _lastPreset);
            _pos = GUILayout.SelectionGrid(_pos, PositionNames, 4);
            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(_hand == Hand.Left, "Left hand")) _hand = Hand.Left;
            if (GUILayout.Toggle(_hand == Hand.Right, "Right hand")) _hand = Hand.Right;
            GUILayout.EndHorizontal();
            _scroll = GUILayout.BeginScrollView(_scroll);
            var p = Where();
            Row("ItemGrabbed (Donut)", () => GameEvents.ItemGrabbed(ItemId.Donut, _hand, p), "ItemGrabbed (Shake)", () => GameEvents.ItemGrabbed(ItemId.Shake, _hand, p));
            Row("ItemReleased", () => GameEvents.ItemReleased(ItemId.Donut, _hand, p), "ItemSocketed", () => GameEvents.ItemSocketed(ItemId.Cola, p));
            Row("ItemImpact 2 m/s", () => GameEvents.ItemImpact(ItemId.Cola, 2f, p), "ItemDropped (Donut)", () => GameEvents.ItemDropped(ItemId.Donut, 3f, p));
            Row("ItemDropped (Shake)", () => GameEvents.ItemDropped(ItemId.Shake, 3f, p), "ItemSpawned", () => GameEvents.ItemSpawned(ItemId.Donut, p));
            Row("Fridge detent open", () => GameEvents.FridgeDoorMoved(0, 0.08f, true), "Fridge open limit", () => GameEvents.FridgeDoorLimit(0, true));
            Row("Fridge detent close", () => { GameEvents.FridgeDoorMoved(0, 0.5f, false); GameEvents.FridgeDoorMoved(0, 0.06f, true); }, "Fridge closed", () => GameEvents.FridgeDoorLimit(0, false));
            Row("Pump press", () => GameEvents.PumpActivated(true, p), "Pump release", () => GameEvents.PumpActivated(false, p));
            GUILayout.BeginHorizontal();
            _level = GUILayout.HorizontalSlider(_level, 0, 1, GUILayout.Width(150));
            if (GUILayout.Button("CupFilling " + _level.ToString("0.00"))) GameEvents.CupFilling(_level, _hand);
            GUILayout.EndHorizontal();
            Row("Fill cup 0 -> 1 (4 s)", () => StartCoroutine(Fill(p)), "CupFilled", () => GameEvents.CupFilled(p));
            GUILayout.BeginHorizontal();
            _angle = GUILayout.HorizontalSlider(_angle, 0, 45, GUILayout.Width(150));
            if (GUILayout.Button("TrayTilt " + _angle.ToString("0") + " deg")) GameEvents.TrayTilt(_angle, _hand);
            GUILayout.EndHorizontal();
            Row("Tilt sweep 20 -> 35", () => StartCoroutine(Tilt()), "TrayServedPlaced", () => GameEvents.TrayServedPlaced(p, true));
            Row("OrderPlaced (bell)", () => GameEvents.OrderPlaced(new Order { Id = Random.Range(100, 999), SeatIndex = Seat, SeatPosition = p, Items = new[] { ItemId.Donut }, PatienceMax = 40, PatienceRemaining = 40 }),
                "All 8 bells", () => StartCoroutine(Bells()));
            Row("Served perfect", () => GameEvents.OrderServed(new ServeResult(0, ServeOutcome.Perfect, 2, 0, 0, 300, 40)), "Served partial", () => GameEvents.OrderServed(new ServeResult(0, ServeOutcome.Partial, 1, 1, 0, 75, 10)));
            Row("Served empty", () => GameEvents.OrderServed(new ServeResult(0, ServeOutcome.Empty, 0, 0, 1, 0, 0)), "OrderExpired", () => GameEvents.OrderExpired(new Order { Id = 0, SeatIndex = Seat, SeatPosition = p, Items = new[] { ItemId.Cake } }));
            Row("Customers 0", () => GameEvents.CustomerCount(0), "Customers 4", () => GameEvents.CustomerCount(4));
            Row("Footstep tile", () => GameEvents.Footstep(Surface.Tile, p), "Footstep stone", () => GameEvents.Footstep(Surface.Stone, p));
            Row("UiHover", GameEvents.UiHover, "UiClick", GameEvents.UiClick);
            Row("State Wave1 (music)", () => GameEvents.RoundStateChanged(RoundState.Wave1), "State Results", () => GameEvents.RoundStateChanged(RoundState.Results));
            Row("Tutorial step 0", () => { GameEvents.RoundStateChanged(RoundState.Tutorial); GameEvents.TutorialStep(0); }, "Tutorial next", () => GameEvents.TutorialStep(Mathf.Min(3, TutorialStepGuess++)));
            Row("Zone Indoor", () => { if (AudioService.Instance != null) AudioService.Instance.SetSnapshot(AudioZone.Indoor); }, "Zone Outdoor", () => { if (AudioService.Instance != null) AudioService.Instance.SetSnapshot(AudioZone.Outdoor); });
            Row("EVERY CUE (sweep)", () => StartCoroutine(EveryCue(p)), "EVERY PRESET (sweep)", () => StartCoroutine(EveryPreset()));
            Row("Full synthetic round", GameEvents.PlayRequested, "Back to menu", GameEvents.MenuRequested);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        int TutorialStepGuess = 1;

        static void Row(string a, System.Action fa, string b, System.Action fb)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(a)) fa();
            if (GUILayout.Button(b)) fb();
            GUILayout.EndHorizontal();
        }

        IEnumerator Fill(Vector3 p)
        {
            GameEvents.PumpActivated(true, p);
            for (float l = 0; l <= 1f; l += Time.deltaTime * 0.25f) { GameEvents.CupFilling(l, _hand); yield return null; }
            GameEvents.CupFilled(p); GameEvents.PumpActivated(false, p);
        }

        IEnumerator Tilt()
        {
            for (float a = 20; a <= 36; a += Time.deltaTime * 6f) { GameEvents.TrayTilt(a, _hand); yield return null; }
        }

        IEnumerator Bells()
        {
            for (int s = 0; s < 8; s++)
            {
                GameEvents.OrderPlaced(new Order { Id = 500 + s, SeatIndex = s, SeatPosition = SyntheticRound.SeatPosition(s), Items = new[] { ItemId.Donut }, PatienceMax = 40, PatienceRemaining = 40 });
                yield return new WaitForSeconds(0.9f);
            }
        }

        IEnumerator EveryCue(Vector3 p)
        {
            var audio = AudioService.Instance;
            if (audio == null) yield break;
            foreach (var id in AudioCueIds.All)
            {
                audio.Play(id, p, 1f, 1f, -1);
                yield return new WaitForSeconds(1.1f);
            }
        }

        IEnumerator EveryPreset()
        {
            var svc = FindFirstObjectByType<HapticsServiceBase>();
            if (svc == null) yield break;
            foreach (var name in HapticPresetNames.All)
            {
                if (svc.TryGetPreset(name, out var preset) && preset.IsRamp)
                    for (float t = 0; t < 1f; t += Time.deltaTime) { svc.PlayRamp(name, _hand, Mathf.Lerp(preset.RampInput.x, preset.RampInput.y, t)); yield return null; }
                else svc.Play(name, _hand);
                yield return new WaitForSeconds(0.7f);
            }
        }
    }
}
