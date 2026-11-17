using System.Collections;
using CanteenRush.Core;
using CanteenRush.Gameplay;
using CanteenRush.Gameplay.Desktop;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public sealed class DesktopModeTests
{
    static Keyboard _keyboard;
    static InputSettings _original, _test;

    [UnityTearDown] public IEnumerator Cleanup()
    {
        if (_keyboard != null && _keyboard.added) InputSystem.RemoveDevice(_keyboard);
        _keyboard = null;
        if (_original != null) InputSystem.settings = _original;
        if (_test != null) Object.DestroyImmediate(_test);
        _original = _test = null;
        Time.timeScale = 1;
        if (Application.isPlaying) yield return new ExitPlayMode();
        GameEvents.ResetAll();
    }

    static void Keys(params Key[] keys) { InputSystem.QueueStateEvent(_keyboard, new KeyboardState(keys)); }

    static IEnumerator Tap(Key key) { Keys(key); yield return null; yield return null; Keys(); yield return null; yield return null; }

    static string Probe(DesktopController d, Vector3 thing)
    {
        var cam = Camera.main.transform; Vector3 dir = (thing - cam.position).normalized;
        string hit = Physics.Raycast(cam.position, dir, out var h, 10f, ~0, QueryTriggerInteraction.Ignore) ? h.collider.name + " layer " + LayerMask.LayerToName(h.collider.gameObject.layer) + " at " + h.distance.ToString("F2") : "nothing";
        return " | cam " + cam.position + " thing " + thing + " dist " + Vector3.Distance(cam.position, thing).ToString("F2") + " ray hits " + hit + " label '" + d.FocusLabel + "'";
    }

    static DesktopController Prepare()
    {
        _original = InputSystem.settings;
        _test = Object.Instantiate(_original);
        _test.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        _test.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings = _test;
        _keyboard = InputSystem.AddDevice<Keyboard>();
        var desktop = Object.FindFirstObjectByType<DesktopController>();
        Assert.NotNull(desktop, "DesktopMode bootstraps itself in Main");
        return desktop;
    }

    static IEnumerator Ready(DesktopController d)
    {
        for (int i = 0; i < 400 && !d.Ready; i++) yield return null;
        Assert.IsTrue(d.Ready, "desktop controller found the rig, hand and manager");
        d.EnableForTests();
        yield return null; yield return null;
    }

    static T Find<T>(System.Func<T, bool> where) where T : Object
    {
        foreach (var o in Object.FindObjectsByType<T>(FindObjectsSortMode.None)) if (where(o)) return o;
        return null;
    }

    // Stand 1 m in front of a thing at eye level and face it.
    static void Face(DesktopController d, Transform rig, Vector3 thing, float back = 1f)
    {
        Vector3 flat = thing; flat.y = rig.position.y;
        Vector3 dir = (rig.position - flat); dir.y = 0; if (dir.sqrMagnitude < .01f) dir = -rig.forward; dir.Normalize();
        var cc = rig.GetComponent<CharacterController>(); cc.enabled = false; rig.position = flat + dir * back; cc.enabled = true;
        Physics.SyncTransforms();
        d.LookAt(thing);
    }

    [UnityTest] public IEnumerator DesktopPlacesCameraAtEyeHeightAndTakesOverFromTheSimulator()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Main.unity"); yield return new EnterPlayMode(); yield return null;
        var d = Prepare(); yield return Ready(d);
        Assert.IsTrue(DesktopController.Active);
        var cam = Camera.main;
        var rig = Object.FindFirstObjectByType<ComfortSettings>().transform;
        Assert.AreEqual(DesktopController.EyeHeight, cam.transform.position.y - rig.position.y, .05f);
        var sim = Object.FindFirstObjectByType<UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRDeviceSimulator>();
        Assert.IsNull(sim, "the simulator is switched off while desktop mode is on");
        yield return Tap(Key.F5);
        Assert.IsFalse(DesktopController.Active, "F5 hands control back to the simulator");
        yield return Tap(Key.F5);
        Assert.IsTrue(DesktopController.Active);
    }

    [UnityTest] public IEnumerator EGrabsWhatYouLookAtAndPutsItOnTheTray()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Main.unity"); yield return new EnterPlayMode(); yield return null;
        var d = Prepare(); yield return Ready(d);
        var rig = Object.FindFirstObjectByType<ComfortSettings>().transform;
        var donut = Find<ItemBehaviour>(i => i.Id == ItemId.Donut && i.gameObject.activeInHierarchy);
        Assert.NotNull(donut);
        Face(d, rig, donut.transform.position, .9f);
        yield return null; yield return null;
        Assert.AreEqual(DesktopController.Kind.Grab, d.Focus, "crosshair is on a grabbable donut" + Probe(d, donut.transform.position));
        StringAssert.Contains("donut", d.FocusLabel);
        yield return Tap(Key.E);
        Assert.IsTrue(d.Holding, "E grabs the donut");
        Assert.AreEqual(ItemId.Donut, d.Held.GetComponent<ItemBehaviour>().Id);
        // the held item rides in front of the camera
        for (int i = 0; i < 20; i++) yield return new WaitForFixedUpdate();
        float dist = Vector3.Distance(d.Held.transform.position, Camera.main.transform.position);
        Assert.Less(dist, 1.1f, "held donut stays near the camera");
        yield return Tap(Key.E);
        Assert.IsFalse(d.Holding, "E again drops it");
    }

    [UnityTest] public IEnumerator FridgeOpensWithEAndColaBecomesGrabbable()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Main.unity"); yield return new EnterPlayMode(); yield return null;
        var d = Prepare(); yield return Ready(d);
        var rig = Object.FindFirstObjectByType<ComfortSettings>().transform;
        var door = Find<FridgeDoor>(x => x.DoorIndex == 0) ?? Object.FindFirstObjectByType<FridgeDoor>();
        Assert.NotNull(door);
        var grab = door.GetComponent<XRGrabInteractable>();
        Vector3 handle = grab.attachTransform != null ? grab.attachTransform.position : door.transform.position;
        Face(d, rig, handle, 1.5f);
        yield return null; yield return null;
        Assert.AreEqual(DesktopController.Kind.Door, d.Focus, "looking at the fridge handle" + Probe(d, handle));
        yield return Tap(Key.E);
        float deadline = Time.time + 2.5f;
        while (!door.IsOpen && Time.time < deadline) yield return null;
        { float t0 = Time.time; while (Time.time - t0 < 1.2f) yield return null; }
        var hj = door.GetComponent<HingeJoint>();
        Assert.Greater(hj.angle, 20f, "door stays open after physics takes over");
        Assert.IsTrue(door.IsOpen, "E swings the door past the latch");
        var cola = Find<ItemBehaviour>(i => i.Id == ItemId.Cola);
        Assert.NotNull(cola); Assert.IsTrue(cola.GetComponent<XRGrabInteractable>().enabled, "the cola inside is grabbable once the door is open");
    }

    [UnityTest] public IEnumerator CupFillsWhenPlacedUnderTheTapAndFHeld()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Main.unity"); yield return new EnterPlayMode(); yield return null;
        var d = Prepare(); yield return Ready(d);
        var rig = Object.FindFirstObjectByType<ComfortSettings>().transform;
        var cooler = Object.FindFirstObjectByType<WaterCooler>();
        var cup = Object.FindFirstObjectByType<CupFill>();
        Assert.NotNull(cooler); Assert.NotNull(cup);
        Face(d, rig, cup.transform.position, .8f);
        yield return null; yield return null;
        yield return Tap(Key.E);
        Assert.IsTrue(d.Holding, "grabbed the cup");
        cup = d.Held.GetComponent<CupFill>(); Assert.NotNull(cup, "the held thing is a cup");
        Face(d, rig, cooler.TapPoint + Vector3.up * .3f, 1.0f);
        yield return null; yield return null;
        StringAssert.Contains("tap", d.FocusLabel, Probe(d, cooler.TapPoint));
        yield return Tap(Key.E);
        Assert.IsFalse(d.Holding);
        Assert.Less(Vector3.Distance(cup.transform.position, cooler.TapPoint), .5f, "cup placed under the tap");
        var body = cup.GetComponent<Rigidbody>(); body.isKinematic = true; // keep it under the spout for the check
        float before = cup.Level;
        Keys(Key.F);
        float deadline = Time.time + 2f;
        while (cup.Level <= before + .01f && Time.time < deadline) yield return null;
        Keys();
        Assert.Greater(cup.Level, before, "holding F pumps water into the cup");
    }

    [UnityTest] public IEnumerator EscPausesAndEnterStartsTheRound()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Main.unity"); yield return new EnterPlayMode(); yield return null;
        var d = Prepare(); yield return Ready(d);
        Assert.AreEqual(RoundState.Menu, d.Game.State);
        yield return Tap(Key.Enter);
        Assert.AreNotEqual(RoundState.Menu, d.Game.State, "Enter starts from the menu");
        yield return Tap(Key.Escape);
        Assert.IsTrue(d.Paused); Assert.AreEqual(0f, Time.timeScale);
        Keys(Key.Escape); yield return null; Keys(); yield return null;
        Assert.IsFalse(d.Paused); Assert.AreEqual(1f, Time.timeScale);
    }
}
