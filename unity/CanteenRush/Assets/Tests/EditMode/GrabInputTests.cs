using System.Collections;
using System.Collections.Generic;
using CanteenRush.Core;
using CanteenRush.Gameplay;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.XR;
using UnityEngine.TestTools;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

public sealed class GrabInputTests
{
    static Keyboard _keyboard;
    static InputSettings _originalSettings, _testSettings;
    [UnityTearDown] public IEnumerator Cleanup()
    {
        if (_keyboard != null && _keyboard.added) InputSystem.RemoveDevice(_keyboard);
        _keyboard = null;
        if (_originalSettings != null) InputSystem.settings = _originalSettings;
        if (_testSettings != null) Object.Destroy(_testSettings);
        _originalSettings = _testSettings = null;
        if (Application.isPlaying) yield return new ExitPlayMode();
        GameEvents.ResetAll();
    }
    static NearFarInteractor PrepareRightHand()
    {
        _originalSettings = InputSystem.settings;
        _testSettings = Object.Instantiate(_originalSettings);
        _testSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        _testSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings = _testSettings;
        NearFarInteractor hand = null;
        // Freeze tracking for this input/detection test; batch workers have no focused XR display.
        foreach (var modality in Object.FindObjectsByType<XRInputModalityManager>(FindObjectsSortMode.None)) modality.enabled = false;
        foreach (var interactor in Object.FindObjectsByType<NearFarInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (interactor.handedness == InteractorHandedness.Right) hand = interactor;
        Assert.NotNull(hand);
        foreach (var driver in Object.FindObjectsByType<TrackedPoseDriver>(FindObjectsInactive.Include, FindObjectsSortMode.None)) driver.enabled = false;
        var controller = hand.GetComponentInParent<TrackedPoseDriver>(true);
        Assert.NotNull(controller); controller.gameObject.SetActive(true); hand.gameObject.SetActive(true);
        return hand;
    }

    [UnityTest] public IEnumerator GTogglesDonutGripAndKeepsItHeldWhileMoving()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Sandbox_P2.unity");
        yield return new EnterPlayMode();
        yield return null; yield return null;
        var hand = PrepareRightHand();
        ItemBehaviour donut = null;
        foreach (var item in Object.FindObjectsByType<ItemBehaviour>(FindObjectsSortMode.None)) if (item.Id == ItemId.Donut) donut = item;
        Assert.NotNull(donut);
        var body = donut.GetComponent<Rigidbody>(); body.useGravity = false; body.linearVelocity = Vector3.zero;
        body.position = new Vector3(0, 1.4f, 0);
        hand.transform.position = body.position;
        foreach (var caster in hand.GetComponents<SphereInteractionCaster>())
        {
            Assert.AreNotEqual(0, caster.physicsLayerMask.value & (1 << donut.gameObject.layer));
            caster.enableStabilization = false;
            caster.castOrigin.position = body.position;
        }
        foreach (var caster in hand.GetComponents<CurveInteractionCaster>())
            Assert.AreNotEqual(0, caster.raycastMask.value & (1 << donut.gameObject.layer));
        Physics.SyncTransforms();
        yield return null; yield return null; yield return null;
        var targets = new List<IXRInteractable>(); hand.GetValidTargets(targets);
        Assert.Contains(donut.GetComponent<XRGrabInteractable>(), targets, "The rig must detect the donut's Items physics layer.");
        _keyboard = InputSystem.AddDevice<Keyboard>();
        InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Space));
        InputSystem.Update();
        yield return null; yield return null;
        InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Space, Key.G));
        InputSystem.Update();
        yield return null; yield return null; yield return null; yield return null;
        Assert.IsTrue(donut.IsSelected, "Space + G must travel through simulator grip, XRI input actions and the rig's detector.");
        var grab = donut.GetComponent<XRGrabInteractable>();
        Assert.AreSame(hand, grab.firstInteractorSelecting);
        InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Space));
        InputSystem.Update();
        yield return null; yield return null; yield return null;
        Assert.AreSame(hand, grab.firstInteractorSelecting, "The first key-up must leave the grip latched.");

        Vector3 before = body.position;
        hand.transform.position += Vector3.up * .4f;
        float deadline = Time.time + .75f;
        while (body.position.y - before.y <= .15f && Time.time < deadline)
            yield return new WaitForFixedUpdate();
        Assert.AreSame(hand, grab.firstInteractorSelecting);
        Assert.Greater(body.position.y - before.y, .15f, "The donut should follow the hand with G released.");

        InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Space, Key.G));
        InputSystem.Update();
        yield return null; yield return null; yield return null;
        Assert.IsFalse(donut.IsSelected, "The second G press must release the donut.");
        InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Space));
        InputSystem.Update();
    }

    [UnityTest] public IEnumerator MainTrayCanBePickedUpFromItsVisibleRack()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
        yield return new EnterPlayMode();
        yield return null; yield return null;
        var hand = PrepareRightHand();
        var rack = Object.FindFirstObjectByType<TrayRack>();
        Assert.NotNull(rack);
        var support = rack.GetComponent<BoxCollider>();
        var visual = rack.transform.Find("Visual").GetComponentInChildren<Renderer>();
        Assert.AreEqual(support.bounds.max.y, visual.bounds.max.y, .02f, "The visible shelf must reach the trays.");

        var eye = Camera.main.transform;
        var origin = eye.position + Vector3.down * .18f;
        Tray closest = null;
        foreach (var tray in rack.GetComponentsInChildren<Tray>())
            if (closest == null || (tray.transform.position - origin).sqrMagnitude < (closest.transform.position - origin).sqrMagnitude)
                closest = tray;
        Assert.NotNull(closest);
        var grab = closest.GetComponent<XRGrabInteractable>();
        yield return new WaitForSeconds(.35f);
        var target = grab.colliders[0].bounds.center;
        Assert.AreEqual(support.bounds.max.y, grab.colliders[0].bounds.min.y, .025f, "The tray must settle onto the shelf.");

        var aim = Quaternion.LookRotation(target - origin);
        hand.transform.SetPositionAndRotation(origin, aim);
        foreach (var caster in hand.GetComponents<InteractionCasterBase>())
        {
            caster.enableStabilization = false;
            caster.castOrigin.SetPositionAndRotation(origin, aim);
        }
        Physics.SyncTransforms();
        yield return null; yield return null; yield return null;
        var targets = new List<IXRInteractable>();
        hand.GetValidTargets(targets);
        Assert.Contains(grab, targets, "The controller ray should see the stocked tray from the player start.");

        _keyboard = InputSystem.AddDevice<Keyboard>();
        InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Space));
        InputSystem.Update();
        yield return null; yield return null;
        InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Space, Key.G));
        InputSystem.Update();
        yield return new WaitForSeconds(.25f);
        Assert.AreSame(hand, grab.firstInteractorSelecting);
        Assert.Less(Vector3.Angle(closest.transform.up, Vector3.up), 8, "Picking up a level tray should preserve its orientation.");
        InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Space));
        InputSystem.Update();
        yield return null; yield return null;
        InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Space, Key.G));
        InputSystem.Update();
        yield return null; yield return null;
        Assert.IsFalse(grab.isSelected);
    }
}
