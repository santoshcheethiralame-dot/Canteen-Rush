using System.Collections.Generic;
using System.IO;
using CanteenRush.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;
namespace CanteenRush.Gameplay.Editor
{
    public static class LiveGrabDiagnostics
    {
        const string Armed = "CanteenRush.LiveGrabDiagnostics";
        static readonly string Report = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../../p2-live-grab.txt"));
        static int _stage, _frame;
        static double _wait;
        static NearFarInteractor _hand;
        static ItemBehaviour _donut;
        static Keyboard _keyboard;
        static InputSettings _original, _temporary;
        static TrackedPoseDriver[] _drivers;
        static XRInputModalityManager[] _modalities;
        static Vector3 _handLocalPosition;
        static Quaternion _handLocalRotation;
        static readonly Dictionary<InteractionCasterBase, bool> CasterStates = new Dictionary<InteractionCasterBase, bool>();
        [InitializeOnLoadMethod] static void Subscribe()
        {
            EditorApplication.update -= Tick; EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged -= Changed; EditorApplication.playModeStateChanged += Changed;
        }
        [MenuItem("Canteen Rush/Gameplay/Verify live donut grab")]
        public static void Run()
        {
            File.WriteAllText(Report, "Live Main grab diagnostics\n");
            _stage = 0; _wait = EditorApplication.timeSinceStartup + 20;
            SessionState.SetBool(Armed, true);
            if (!EditorApplication.isPlaying) EditorApplication.isPlaying = true;
        }
        [MenuItem("Canteen Rush/Gameplay/Check simulator keyboard focus")]
        public static void CaptureKeyboard()
        {
            if (!Application.isPlaying) { SessionState.SetBool(Armed + ".Focus", true); EditorApplication.isPlaying = true; return; }
            if (_original == null) _original = InputSystem.settings;
            if (_temporary == null) _temporary = Object.Instantiate(_original);
            _temporary.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            _temporary.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings = _temporary;
            Log("Keyboard focus diagnostic enabled; restored on exit from Play mode.");
        }
        static void Changed(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Armed + ".Focus", false))
            { SessionState.SetBool(Armed + ".Focus", false); EditorApplication.delayCall += CaptureKeyboard; }
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Armed, false))
            { _stage = 0; _wait = EditorApplication.timeSinceStartup + 20; }
            if (state == PlayModeStateChange.ExitingPlayMode && (SessionState.GetBool(Armed, false) || _original != null)) Finish();
        }
        static void Log(string text) { File.AppendAllText(Report, text + "\n"); Debug.Log(text); }
        static void Tick()
        {
            if (!SessionState.GetBool(Armed, false) || !Application.isPlaying) return;
            if (EditorApplication.timeSinceStartup > _wait) { Log("Timed out at stage " + _stage); Finish(); return; }
            if (_stage == 0)
            {
                var game = Object.FindFirstObjectByType<GameManager>(); if (game == null) return;
                if (game.State == RoundState.Menu) { game.Play(); return; }
                if (Camera.main == null) return;
                foreach (var item in Object.FindObjectsByType<ItemBehaviour>(FindObjectsSortMode.None))
                    if (item.Id == ItemId.Donut && (_donut == null || (item.transform.position - Camera.main.transform.position).sqrMagnitude < (_donut.transform.position - Camera.main.transform.position).sqrMagnitude)) _donut = item;
                if (_donut == null) return;
                foreach (var hand in Object.FindObjectsByType<NearFarInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (hand.handedness == InteractorHandedness.Right) _hand = hand;
                if (_hand == null) return;
                _handLocalPosition = _hand.transform.localPosition; _handLocalRotation = _hand.transform.localRotation;
                _original = InputSystem.settings; _temporary = Object.Instantiate(_original);
                _temporary.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                _temporary.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                InputSystem.settings = _temporary;
                _modalities = Object.FindObjectsByType<XRInputModalityManager>(FindObjectsSortMode.None);
                foreach (var modality in _modalities) modality.enabled = false;
                _drivers = Object.FindObjectsByType<TrackedPoseDriver>(FindObjectsSortMode.None);
                foreach (var driver in _drivers) driver.enabled = false;
                var controller = _hand.GetComponentInParent<TrackedPoseDriver>(true);
                controller.gameObject.SetActive(true); _hand.gameObject.SetActive(true);
                Vector3 target = _donut.GetComponent<Collider>().bounds.center;
                Vector3 toward = Camera.main.transform.position - target; toward.y = 0; toward.Normalize();
                Camera.main.transform.position = target + toward * 1.0f + Vector3.up * .45f;
                Camera.main.transform.LookAt(target);
                _hand.transform.position = target + toward * .65f + Vector3.up * .2f;
                _hand.transform.rotation = Quaternion.LookRotation(target - _hand.transform.position);
                CasterStates.Clear();
                foreach (var caster in _hand.GetComponents<InteractionCasterBase>()) { CasterStates[caster] = caster.enableStabilization; caster.enableStabilization = false; }
                Physics.SyncTransforms();
                Log("Donut at " + target + "; hand at " + _hand.transform.position);
                _keyboard = InputSystem.AddDevice<Keyboard>();
                InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Space)); InputSystem.Update();
                _frame = Time.frameCount + 5; _stage = 1;
            }
            else if (_stage == 1 && Time.frameCount >= _frame)
            {
                var targets = new List<IXRInteractable>(); _hand.GetValidTargets(targets);
                string names = ""; foreach (var target in targets) names += target.transform.name + " ";
                Log("Actual right-hand targets: " + names);
                var ray = _hand.GetComponent<CurveInteractionCaster>();
                if (ray != null && Physics.Raycast(ray.effectiveCastOrigin.position, ray.effectiveCastOrigin.forward, out var hit, 3, ray.raycastMask))
                    Log("First ray collider: " + hit.collider.name + " / " + LayerMask.LayerToName(hit.collider.gameObject.layer));
                InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Space, Key.G)); InputSystem.Update();
                _frame = Time.frameCount + 5; _stage = 2;
            }
            else if (_stage == 2 && Time.frameCount >= _frame)
            {
                bool selected = _donut.IsSelected && ReferenceEquals(_donut.GetComponent<XRGrabInteractable>().firstInteractorSelecting, _hand);
                Log("LIVE MAIN keyboard grip selected donut: " + selected);
                if (selected) _hand.transform.position += Vector3.up * .25f;
                _wait = EditorApplication.timeSinceStartup + 20;
                _frame = Time.frameCount + 5; _stage = 3;
            }
            else if (_stage == 3 && Time.frameCount >= _frame)
            {
                ScreenCapture.CaptureScreenshot(Path.GetFullPath(Path.Combine(Application.dataPath, "../../../../p2-live-grab.png")));
                _wait = EditorApplication.timeSinceStartup + 10; _stage = 4;
            }
            else if (_stage == 4 && EditorApplication.timeSinceStartup >= _wait - 1)
            {
                InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Space)); InputSystem.Update();
                _frame = Time.frameCount + 5; _stage = 5; _wait = EditorApplication.timeSinceStartup + 10;
            }
            else if (_stage == 5 && Time.frameCount >= _frame)
            { Log("LIVE MAIN grip release released donut: " + !_donut.IsSelected); Finish(); }
        }
        static void Finish()
        {
            SessionState.SetBool(Armed, false);
            if (_keyboard != null && _keyboard.added) InputSystem.RemoveDevice(_keyboard);
            _keyboard = null;
            if (_original != null) InputSystem.settings = _original;
            if (_temporary != null) Object.DestroyImmediate(_temporary);
            _original = _temporary = null;
            if (_hand != null) _hand.transform.SetLocalPositionAndRotation(_handLocalPosition, _handLocalRotation);
            foreach (var pair in CasterStates) if (pair.Key != null) pair.Key.enableStabilization = pair.Value;
            CasterStates.Clear();
            if (_drivers != null) foreach (var driver in _drivers) if (driver != null) driver.enabled = true;
            if (_modalities != null) foreach (var modality in _modalities) if (modality != null) modality.enabled = true;
            _hand = null; _donut = null; _drivers = null; _modalities = null;
        }
    }
}
