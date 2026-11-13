using System.Collections;
using System.Collections.Generic;
using CanteenRush.Audio;
using CanteenRush.Core;
using CanteenRush.Haptics;
using CanteenRush.P3Sandbox;
using CanteenRush.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
namespace CanteenRush.Tests.PlayMode
{
    /// Role P3 task 9: (a) scenes load without errors, (b) a scripted round reaches Results,
    /// (c) every cue resolves to a clip and every preset resolves, (d) 3 rounds without leaks,
    /// (e) GameEvents subscriber counts return to baseline. Any Debug.LogError or exception fails a test
    /// (Unity Test Framework's LogAssert default).
    public sealed class P3SmokeTests
    {
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            yield return null;
        }

        // ---------- (a) ----------
        [UnityTest]
        public IEnumerator A1_SandboxLoadsWithAllP3Systems()
        {
            yield return P3TestUtil.Load(P3TestUtil.SandboxScene);
            Assert.NotNull(AudioService.Instance, "AudioService");
            Assert.AreEqual(AudioService.DefaultPoolSize, AudioService.Instance.PoolSize, "one-shot pool size");
            Assert.NotNull(Object.FindFirstObjectByType<HapticsServiceBase>(), "haptics service");
            Assert.NotNull(Object.FindFirstObjectByType<HapticsEventBinder>(), "haptics binder");
            Assert.NotNull(Object.FindFirstObjectByType<HapticsDebugHud>(), "haptics HUD");
            var shell = Object.FindFirstObjectByType<UiShell>();
            Assert.NotNull(shell, "UI shell");
            Assert.IsTrue(shell.Menu.IsShown, "menu board visible at start");
            Assert.NotNull(Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>(), "EventSystem");
        }

        [UnityTest]
        public IEnumerator A2_MainLoadsWithP3Systems()
        {
            yield return P3TestUtil.Load(P3TestUtil.MainScene, 30);
            Assert.NotNull(AudioService.Instance, "Audio installed in Main (prefab or Resources fallback)");
            Assert.NotNull(Object.FindFirstObjectByType<HapticsServiceBase>(), "Haptics installed in Main");
            Assert.NotNull(Object.FindFirstObjectByType<UiShell>(), "UI shell installed in Main");
            Assert.Greater(AudioSceneInstaller.Emitters.Count, 0, "ambience emitters placed");
        }

        // ---------- (b) ----------
        [UnityTest]
        public IEnumerator B_ScriptedRoundReachesResults()
        {
            yield return P3TestUtil.Load(P3TestUtil.SandboxScene);
            var shell = Object.FindFirstObjectByType<UiShell>();
            yield return SyntheticRound.Play(0f);
            yield return null;
            Assert.IsTrue(shell.Results.IsShown, "results board visible");
            Assert.IsFalse(shell.Menu.IsShown, "menu hidden");
            Assert.IsFalse(shell.Wrist.IsShown, "wrist HUD hidden after the round");
            var s = shell.Results.Stats;
            Assert.AreEqual(SyntheticRound.LastScore, s.Score);
            Assert.AreEqual(SyntheticRound.ExpectedServed, s.Served);
            Assert.AreEqual(SyntheticRound.ExpectedCorrect, s.Correct);
            Assert.AreEqual(SyntheticRound.ExpectedWrong, s.Wrong);
            Assert.AreEqual(SyntheticRound.ExpectedSpills, s.Spills);
            Assert.AreEqual(SyntheticRound.ExpectedExpired, s.Expired);
            Assert.AreEqual(RoundStats.Stars(s.Score, new[] { 300, 700, 1200 }), shell.Results.LastStars);

            // Replay from the results board returns to a clean round (the sandbox stands in for GameManager).
            shell.Results.ReplayButton.onClick.Invoke();
            yield return null; yield return null;
            Assert.IsFalse(shell.Results.IsShown, "results hidden after Replay");
        }

        // ---------- (c) ----------
        [UnityTest]
        public IEnumerator C1_EveryCueHasARealClip()
        {
            yield return P3TestUtil.Load(P3TestUtil.SandboxScene);
            var audio = AudioService.Instance;
            foreach (var id in AudioCueIds.All)
            {
                Assert.IsTrue(audio.TryGetCue(id, out var cue), "cue set missing: " + id);
                Assert.IsTrue(cue.HasClip, "no clip assigned: " + id);
                Assert.NotNull(audio.GroupFor(cue), "no mixer group: " + id);
            }
            var mixer = audio.Mixer;
            Assert.NotNull(mixer, "mixer");
            Assert.NotNull(mixer.FindSnapshot("Indoor"), "Indoor snapshot");
            Assert.NotNull(mixer.FindSnapshot("Outdoor"), "Outdoor snapshot");
            foreach (var g in new[] { "Master", "SFX", "Ambience", "Music", "UI", "Voice" })
                Assert.IsNotEmpty(mixer.FindMatchingGroups(g), "mixer group " + g);
        }

        [UnityTest]
        public IEnumerator C2_EveryPresetResolvesAndMatchesTheSpecTable()
        {
            yield return P3TestUtil.Load(P3TestUtil.SandboxScene);
            var svc = Object.FindFirstObjectByType<HapticsServiceBase>();
            foreach (var name in HapticPresetNames.All) Assert.IsTrue(svc.TryGetPreset(name, out _), "preset missing: " + name);
            void Check(string n, float amp, float dur) { svc.TryGetPreset(n, out var p); Assert.AreEqual(amp, p.Amplitude, 1e-4f, n); Assert.AreEqual(dur, p.Duration, 1e-4f, n); }
            Check("Grab", 0.25f, 0.04f); Check("Release", 0.10f, 0.03f); Check("SocketSnap", 0.35f, 0.06f);
            Check("DoorLatch", 0.45f, 0.05f); Check("DoorStop", 0.60f, 0.08f); Check("PumpPress", 0.50f, 0.07f);
            Check("ServeOk", 0.5f, 0.06f); Check("ServePartial", 0.35f, 0.12f); Check("Spill", 0.85f, 0.10f);
            Check("OrderExpired", 0.70f, 0.30f); Check("UiTick", 0.15f, 0.02f);
            svc.TryGetPreset("ServeOk", out var ok); Assert.AreEqual(2, ok.Pulses); Assert.AreEqual(0.05f, ok.Gap, 1e-4f);
            svc.TryGetPreset("WaterFill", out var wf); Assert.IsTrue(wf.IsRamp); Assert.AreEqual(0.10f, wf.RampAmplitudeFor(0), 1e-4f); Assert.AreEqual(0.45f, wf.RampAmplitudeFor(1), 1e-4f);
            svc.TryGetPreset("TrayTiltWarn", out var tt); Assert.IsTrue(tt.IsRamp); Assert.AreEqual(0.15f, tt.RampAmplitudeFor(20), 1e-4f); Assert.AreEqual(0.70f, tt.RampAmplitudeFor(35), 1e-4f);
        }

        [UnityTest]
        public IEnumerator C3_EventsSelectTheRightPresetAndPlayASound()
        {
            yield return P3TestUtil.Load(P3TestUtil.SandboxScene);
            var presets = new List<string>();
            var cues = new List<string>();
            System.Action<string, Hand> onPreset = (n, h) => presets.Add(n);
            System.Action<string, Vector3> onCue = (c, p) => cues.Add(c);
            HapticsServiceBase.PresetRequested += onPreset;
            AudioService.CuePlayed += onCue;
            try
            {
                var p = Vector3.forward * 3;
                void Expect(System.Action raise, string preset, string cue)
                {
                    presets.Clear(); cues.Clear(); raise();
                    if (preset != null) CollectionAssert.Contains(presets, preset, "preset for " + preset);
                    if (cue != null) CollectionAssert.Contains(cues, cue, "cue " + cue);
                    // Every haptic pairs with a sound (MASTER_SPEC §11 principle).
                    if (preset != null) Assert.IsNotEmpty(cues, "haptic " + preset + " has no paired sound");
                }
                Expect(() => GameEvents.ItemGrabbed(ItemId.Donut, Hand.Left, p), "Grab", AudioCueIds.Grab);
                Expect(() => GameEvents.ItemReleased(ItemId.Donut, Hand.Left, p), "Release", AudioCueIds.Release);
                Expect(() => GameEvents.ItemSocketed(ItemId.Donut, p), "SocketSnap", AudioCueIds.TrayClink);
                Expect(() => GameEvents.FridgeDoorMoved(0, 0.08f, true), "DoorLatch", AudioCueIds.FridgeLatch);
                Expect(() => GameEvents.FridgeDoorLimit(0, true), "DoorStop", AudioCueIds.FridgeLatch);
                Expect(() => GameEvents.PumpActivated(true, p), "PumpPress", AudioCueIds.PumpPress);
                Expect(() => GameEvents.ItemDropped(ItemId.Donut, 3f, p), "Spill", AudioCueIds.ItemDrop);
                Expect(() => GameEvents.ItemDropped(ItemId.Shake, 3f, p), "Spill", AudioCueIds.ItemDropGlass);
                Expect(() => GameEvents.OrderServed(new ServeResult(1, ServeOutcome.Perfect, 1, 0, 0, 100, 0)), "ServeOk", AudioCueIds.CustomerHappy);
                Expect(() => GameEvents.OrderServed(new ServeResult(1, ServeOutcome.Partial, 1, 1, 0, 75, 0)), "ServePartial", AudioCueIds.CustomerHappy);
                Expect(() => GameEvents.OrderExpired(new Order { Id = 9, SeatIndex = 2, SeatPosition = p, Items = new[] { ItemId.Cake } }), "OrderExpired", AudioCueIds.CustomerAngry);
                Expect(() => GameEvents.UiHover(), "UiTick", AudioCueIds.UiClick);
                Expect(() => GameEvents.UiClick(), "UiTick", AudioCueIds.UiClick);
                Expect(() => GameEvents.OrderPlaced(new Order { Id = 3, SeatIndex = 5, SeatPosition = p, Items = new[] { ItemId.Cola } }), null, AudioCueIds.OrderBell);
                Expect(() => GameEvents.ItemImpact(ItemId.Cola, 2f, p), null, AudioCueIds.TrayClink);
                Expect(() => GameEvents.Footstep(Surface.Tile, p), null, AudioCueIds.FootstepsTile);
                Expect(() => GameEvents.Footstep(Surface.Stone, p), null, AudioCueIds.FootstepsStone);
                Expect(() => GameEvents.RoundStateChanged(RoundState.Results), null, AudioCueIds.RoundEnd);
                // Ramps: requested on CupFilling / TrayTilt (> 20 deg only).
                presets.Clear(); GameEvents.CupFilling(0.5f, Hand.Left); CollectionAssert.Contains(presets, "WaterFill");
                presets.Clear(); GameEvents.TrayTilt(15f, Hand.Right); CollectionAssert.DoesNotContain(presets, "TrayTiltWarn");
                presets.Clear(); GameEvents.TrayTilt(28f, Hand.Right); CollectionAssert.Contains(presets, "TrayTiltWarn");
            }
            finally
            {
                HapticsServiceBase.PresetRequested -= onPreset;
                AudioService.CuePlayed -= onCue;
            }
            Assert.AreEqual(1.5f, AudioEventBinder.SeatPitch(7) , 0.01f, "farthest seat bell is a fifth up");
            Assert.AreEqual(1f, AudioEventBinder.SeatPitch(0), 1e-4f);
        }

        [UnityTest]
        public IEnumerator C4_RateLimitAndDoublePulse()
        {
            yield return P3TestUtil.Load(P3TestUtil.SandboxScene);
            var svc = Object.FindFirstObjectByType<HapticsServiceBase>();
            svc.ResetTiming();
            int sent = 0;
            System.Action<HapticPulse> count = p => { if (p.Hand == Hand.Left) sent++; };
            HapticsServiceBase.PulseSent += count;
            try
            {
                svc.Play("Grab", Hand.Left); svc.Play("Grab", Hand.Left); svc.Play("Grab", Hand.Left);
                Assert.AreEqual(1, sent, "three pulses inside 30 ms collapse to one");
                svc.ResetTiming(); sent = 0;
                svc.Play("ServeOk", Hand.Left);
                yield return new WaitForSecondsRealtime(0.25f);
                Assert.AreEqual(2, sent, "ServeOk is a double pulse");
            }
            finally { HapticsServiceBase.PulseSent -= count; }
        }

        // ---------- (d) + (e) ----------
        [UnityTest]
        public IEnumerator D_E_ThreeRoundsNoLeaksAndSubscriptionsReturnToBaseline()
        {
            yield return P3TestUtil.Load(P3TestUtil.SandboxScene, 10);
            yield return SyntheticRound.Play(0f);           // warm-up: first-use allocations (clips, curves, GUI)
            System.GC.Collect();
            var baseline = P3TestUtil.SubscriberCounts();
            long before = System.GC.GetTotalMemory(true);
            for (int round = 0; round < 3; round++)
            {
                yield return SyntheticRound.Play(0f);
                GameEvents.MenuRequested();
                yield return null;
            }
            long after = System.GC.GetTotalMemory(true);
            Assert.Less(after - before, 5L * 1024 * 1024, "managed memory delta over 3 rounds");
            var now = P3TestUtil.SubscriberCounts();
            Assert.IsEmpty(P3TestUtil.Diff(baseline, now), "GameEvents subscriber counts changed");
        }
    }
}
