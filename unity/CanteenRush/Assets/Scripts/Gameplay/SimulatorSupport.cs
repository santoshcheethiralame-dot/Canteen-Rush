using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
#endif
namespace CanteenRush.Gameplay
{
    [DefaultExecutionOrder(-31000)]
    public sealed class SimulatorSupport : MonoBehaviour
    {
        void Awake()
        {
            // The optional sample help overlay indexes physical keyboard controls; batch workers have none.
            if (Application.isBatchMode) GetComponent<XRDeviceSimulator>().deviceSimulatorUI = null;
        }
#if UNITY_EDITOR
        Keyboard _editorKeyboard;
        readonly List<Key> _pressed = new List<Key>(16);
        float _gripDownAt, _gripReleaseAt = -1;
        // IMGUI receives editor Game-view keys even when the native input backend misses injected keys.
        // Feed the same bindings through a keyboard device; controller actions and XRI still handle the grab.
        void OnGUI()
        {
            var input = Event.current;
            if (input.type != EventType.KeyDown && input.type != EventType.KeyUp) return;
            if (!Map(input.keyCode, out var key)) return;
            bool down = input.type == EventType.KeyDown;
            if (_editorKeyboard == null)
            {
                if (!down || (Keyboard.current != null && Keyboard.current[key].isPressed)) return;
                // Add the fallback only after a missed native key. The sample help UI keeps its normal key labels.
                _editorKeyboard = InputSystem.AddDevice<Keyboard>();
            }
            if (key == Key.G)
            {
                if (down && !_pressed.Contains(key)) { _gripDownAt = Time.unscaledTime; _gripReleaseAt = -1; }
                // Extremely short injected taps can otherwise begin and end between simulator Update calls.
                if (!down && Time.unscaledTime < _gripDownAt + .15f) { _gripReleaseAt = _gripDownAt + .15f; return; }
            }
            if (down && !_pressed.Contains(key)) _pressed.Add(key);
            else if (!down) _pressed.Remove(key);
            else return;
            InputSystem.QueueStateEvent(_editorKeyboard, new KeyboardState(_pressed.ToArray()));
        }
        void Update()
        {
            if (_gripReleaseAt < 0 || Time.unscaledTime < _gripReleaseAt || _editorKeyboard == null) return;
            _gripReleaseAt = -1; _pressed.Remove(Key.G);
            InputSystem.QueueStateEvent(_editorKeyboard, new KeyboardState(_pressed.ToArray()));
        }
        void OnApplicationFocus(bool focused)
        {
            if (!focused && _editorKeyboard != null) { _gripReleaseAt = -1; _pressed.Clear(); InputSystem.QueueStateEvent(_editorKeyboard, new KeyboardState()); }
        }
        void OnDestroy() { if (_editorKeyboard != null && _editorKeyboard.added) InputSystem.RemoveDevice(_editorKeyboard); }
        static bool Map(KeyCode code, out Key key)
        {
            if (code >= KeyCode.A && code <= KeyCode.Z) { key = (Key)((int)Key.A + (int)code - (int)KeyCode.A); return true; }
            if (code >= KeyCode.F1 && code <= KeyCode.F12) { key = (Key)((int)Key.F1 + (int)code - (int)KeyCode.F1); return true; }
            switch (code)
            {
                case KeyCode.Space: key = Key.Space; return true;
                case KeyCode.LeftShift: key = Key.LeftShift; return true;
                case KeyCode.RightShift: key = Key.RightShift; return true;
                case KeyCode.LeftControl: key = Key.LeftCtrl; return true;
                case KeyCode.RightControl: key = Key.RightCtrl; return true;
                case KeyCode.Tab: key = Key.Tab; return true;
                case KeyCode.Return: case KeyCode.KeypadEnter: key = Key.Enter; return true;
                case KeyCode.Escape: key = Key.Escape; return true;
                case KeyCode.Backslash: key = Key.Backslash; return true;
                default: key = Key.None; return false;
            }
        }
#endif
    }
}
