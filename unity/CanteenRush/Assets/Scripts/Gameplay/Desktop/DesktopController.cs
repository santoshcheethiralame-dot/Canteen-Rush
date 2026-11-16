using CanteenRush.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
namespace CanteenRush.Gameplay.Desktop
{
    /// First-person keyboard and mouse play on top of the same XR rig and interactables.
    /// One invisible hand (the rig's right Near-Far interactor) follows the camera; grabbing goes through
    /// XRInteractionManager, so every audio, haptic and scoring event fires exactly as with a controller.
    /// F5 hands control back to the XR Device Simulator.
    [DefaultExecutionOrder(-100)]
    public sealed class DesktopController : MonoBehaviour
    {
        public enum Kind { None, Grab, Drop, Door, Pump }
        public static DesktopController Instance { get; private set; }
        public static bool Active => Instance != null && Instance._active;

        public const float Reach = 3f, EyeHeight = 1.65f, WalkSpeed = 2.4f, SprintSpeed = 4f, Gravity = -12f;
        public static readonly Vector3 HoldOffset = new Vector3(.22f, -.2f, .55f);
        public float Sensitivity = .12f;
        public Kind Focus { get; private set; }
        public string FocusLabel { get; private set; } = "";
        public bool Paused { get; private set; }
        public bool Ready => _ready;
        public bool Holding => _hand != null && _hand.hasSelection;
        public XRBaseInteractable Held => _hand != null && _hand.hasSelection ? _hand.interactablesSelected[0] as XRBaseInteractable : null;
        public XRBaseInteractable Target => _target;
        public GameManager Game => _game;

        bool _active, _ready;
        Transform _rig, _handRoot;
        Camera _cam;
        CharacterController _cc;
        NearFarInteractor _hand;
        XRInteractionManager _mgr;
        GameManager _game;
        GameObject _simulator;
        WaterCooler _cooler;
        float _yaw, _pitch, _vy, _tilt;
        XRBaseInteractable _target;
        RaycastHit _aim;
        bool _hasAim;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (SceneManager.GetActiveScene().name != "Main" || FindFirstObjectByType<DesktopController>() != null) return;
            var go = new GameObject("DesktopMode"); go.AddComponent<DesktopController>(); go.AddComponent<DesktopHud>();
        }

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; if (_active) { Time.timeScale = 1f; SetCursor(false); } }

        System.Collections.IEnumerator Start()
        {
            // Wait for WorldGameplayInstaller to build the rig, rack and cooler.
            for (int i = 0; i < 300 && !_ready; i++)
            {
                var comfort = FindFirstObjectByType<ComfortSettings>();
                if (comfort != null && FindFirstObjectByType<GameManager>() != null) Bind(comfort.transform);
                yield return null;
            }
            if (_ready && !Application.isBatchMode) Enable(true);
        }

        void Bind(Transform rig)
        {
            _rig = rig; _cc = rig.GetComponent<CharacterController>();
            _cam = rig.GetComponentInChildren<Camera>();
            _mgr = FindFirstObjectByType<XRInteractionManager>();
            _game = FindFirstObjectByType<GameManager>();
            _cooler = FindFirstObjectByType<WaterCooler>();
            var sim = FindFirstObjectByType<XRDeviceSimulator>(FindObjectsInactive.Include);
            _simulator = sim != null ? sim.gameObject : null;
            foreach (var hand in rig.GetComponentsInChildren<NearFarInteractor>(true))
                if (hand.handedness == InteractorHandedness.Right) { _hand = hand; _handRoot = hand.transform.parent; break; }
            _ready = _cam != null && _hand != null && _cc != null && _mgr != null && _game != null;
            if (_ready) _hand.selectActionTrigger = XRBaseInputInteractor.InputTriggerType.Toggle;
        }

        /// Test hook: bind and switch on even in batch mode.
        public void EnableForTests()
        {
            if (!_ready) { var c = FindFirstObjectByType<ComfortSettings>(); if (c != null) Bind(c.transform); }
            if (_ready) Enable(true);
        }

        public void Enable(bool on)
        {
            if (!_ready || on == _active) return;
            _active = on;
            foreach (var b in _rig.GetComponentsInChildren<Behaviour>(true))
                if (b != null && b.GetType().Name == "TrackedPoseDriver") b.enabled = !on;
            if (_simulator != null) _simulator.SetActive(!on);
            if (on)
            {
                _yaw = _rig.eulerAngles.y; _pitch = 0; _tilt = 0; Paused = false; Time.timeScale = 1f;
                PlaceCamera(); SetCursor(true);
            }
            else { Drop(false); Time.timeScale = 1f; SetCursor(false); }
        }

        void PlaceCamera()
        {
            var offset = _cam.transform.parent;
            float y = offset.InverseTransformPoint(new Vector3(offset.position.x, _rig.position.y + EyeHeight, offset.position.z)).y;
            _cam.transform.localPosition = new Vector3(0, y, 0);
            _cc.height = 1.7f; _cc.center = new Vector3(0, .85f + _cc.skinWidth, 0); _cc.radius = Mathf.Min(_cc.radius, .3f);
        }

        void SetCursor(bool locked)
        {
            if (Application.isBatchMode) return;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        void Update()
        {
            var kb = Keyboard.current; if (kb == null || !_ready) return;
            if (kb.f5Key.wasPressedThisFrame) { Enable(!_active); return; }
            if (!_active) return;
            if (kb.escapeKey.wasPressedThisFrame) SetPaused(!Paused);
            if (Paused)
            {
                var m = Mouse.current;
                if (m != null && m.leftButton.wasPressedThisFrame && !Application.isBatchMode) SetPaused(false);
                if (kb.mKey.wasPressedThisFrame) { SetPaused(false); _game.Menu(); }
                return;
            }
            Look(); Move(); Aim(); Interact(kb);
        }

        /// Turn the view toward a world point (tests, tutorial nudges).
        public void LookAt(Vector3 point)
        {
            Vector3 d = point - _cam.transform.position;
            _yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            _pitch = Mathf.Clamp(-Mathf.Atan2(d.y, new Vector2(d.x, d.z).magnitude) * Mathf.Rad2Deg, -80f, 80f);
            _rig.rotation = Quaternion.Euler(0, _yaw, 0);
            _cam.transform.localRotation = Quaternion.Euler(_pitch, 0, 0);
        }

        public void SetPaused(bool paused) { Paused = paused; Time.timeScale = paused ? 0f : 1f; SetCursor(!paused); }

        void Look()
        {
            var mouse = Mouse.current; if (mouse == null) return;
            Vector2 d = mouse.delta.ReadValue() * Sensitivity;
            if (mouse.rightButton.isPressed && Holding) { _tilt = Mathf.Clamp(_tilt - d.x * 1.4f, -45f, 45f); d = Vector2.zero; }
            _yaw += d.x; _pitch = Mathf.Clamp(_pitch - d.y, -80f, 80f);
            _rig.rotation = Quaternion.Euler(0, _yaw, 0);
            _cam.transform.localRotation = Quaternion.Euler(_pitch, 0, 0);
        }

        void Move()
        {
            var kb = Keyboard.current;
            float x = (kb.dKey.isPressed ? 1 : 0) - (kb.aKey.isPressed ? 1 : 0), z = (kb.wKey.isPressed ? 1 : 0) - (kb.sKey.isPressed ? 1 : 0);
            Vector3 dir = _rig.forward * z + _rig.right * x; if (dir.sqrMagnitude > 1) dir.Normalize();
            float speed = kb.leftShiftKey.isPressed ? SprintSpeed : WalkSpeed;
            _vy = _cc.isGrounded ? -1f : _vy + Gravity * Time.deltaTime;
            _cc.Move((dir * speed + Vector3.up * _vy) * Time.deltaTime);
        }

        void LateUpdate()
        {
            if (!_active || !_ready) return;
            // The hand rides in front of the camera; when empty it points at the crosshair target.
            var ct = _cam.transform;
            Vector3 pos = ct.TransformPoint(HoldOffset);
            Vector3 aimPoint = _hasAim ? _aim.point : ct.position + ct.forward * 3f;
            Vector3 fwd = aimPoint - pos; if (fwd.sqrMagnitude < .01f || Holding) fwd = ct.forward;
            bool tilting = Holding && Mouse.current != null && Mouse.current.rightButton.isPressed;
            if (!tilting) _tilt = Mathf.MoveTowards(_tilt, 0f, 120f * Time.unscaledDeltaTime);
            _handRoot.SetPositionAndRotation(pos, Quaternion.LookRotation(fwd, ct.up) * Quaternion.Euler(0, 0, _tilt));
        }

        // ---- aiming ----
        static int Mask => LayerMask.GetMask("Items", "Tray", "Stations", "Environment", "Customers");

        void Aim()
        {
            _target = null; Focus = Kind.None; FocusLabel = ""; _hasAim = false;
            var ct = _cam.transform; var held = Held;
            var hits = Physics.SphereCastAll(ct.position, .12f, ct.forward, Reach, Mask, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            XRBaseInteractable best = null; RaycastHit bestHit = default;
            foreach (var h in hits)
            {
                if (h.collider.transform.IsChildOf(_rig)) continue;
                var it = h.collider.GetComponentInParent<XRBaseInteractable>();
                if (it != null && it == held) continue;
                if (it == null || it.GetComponent<XRSocketInteractor>() != null) { if (!_hasAim) { _aim = h; _hasAim = true; } continue; }
                if (best == null) { best = it; bestHit = h; }
            }
            // No wall test on purpose: the display cases are closed collision meshes around the donuts.
            if (best != null) { _target = best; _aim = bestHit; _hasAim = true; }
            Classify(held);
        }

        void Classify(XRBaseInteractable held)
        {
            if (held != null)
            {
                Focus = Kind.Drop; FocusLabel = "Drop " + Nice(held);
                if (held.GetComponent<CupFill>() != null && AimingAtCooler()) FocusLabel = "Place cup under the tap";
                else if (_hasAim && _aim.normal.y > .5f) FocusLabel = "Put " + Nice(held) + " here";
                return;
            }
            if (_target == null)
            {
                if (AimingAtCooler()) FocusLabel = "Water cooler: set a cup under the tap, then hold F";
                return;
            }
            if (_target.GetComponentInParent<FridgeDoor>() != null) { Focus = Kind.Door; FocusLabel = "Open / close the fridge"; return; }
            if (_target.GetComponentInParent<WaterCooler>() != null) { Focus = Kind.Pump; FocusLabel = "Hold F to pump"; return; }
            if (_target.isSelected) return;
            Focus = Kind.Grab; FocusLabel = "Grab " + Nice(_target);
        }

        bool AimingAtCooler()
        {
            if (_cooler == null) return false;
            var ct = _cam.transform; Vector3 to = _cooler.TapPoint - ct.position;
            float along = Vector3.Dot(to, ct.forward);
            return along > .2f && along < Reach + .5f && (to - ct.forward * along).magnitude < .45f;
        }

        static string Nice(XRBaseInteractable it)
        {
            var item = it.GetComponent<ItemBehaviour>();
            if (item != null && item.Id != ItemId.None) return item.Id.ToString().ToLowerInvariant();
            return it.GetComponent<Tray>() != null ? "tray" : it.name.Replace("Item_", "").Replace("(Clone)", "").ToLowerInvariant();
        }

        // ---- interacting ----
        void Interact(Keyboard kb)
        {
            var mouse = Mouse.current;
            bool press = (mouse != null && mouse.leftButton.wasPressedThisFrame) || kb.eKey.wasPressedThisFrame;
            if (kb.enterKey.wasPressedThisFrame && (_game.State == RoundState.Menu || _game.State == RoundState.Results)) _game.Play();
            if (press)
            {
                if (Holding) Drop(true);
                else if (Focus == Kind.Grab) Grab(_target);
                else if (Focus == Kind.Door) _target.GetComponentInParent<FridgeDoor>().Toggle();
            }
            if (_cooler != null)
            {
                bool pump = kb.fKey.isPressed && (Focus == Kind.Pump || CupAtTap());
                if (pump != _cooler.Pumping) _cooler.SetPump(pump);
            }
        }

        bool CupAtTap()
        {
            foreach (var cup in FindObjectsByType<CupFill>(FindObjectsSortMode.None))
                if ((cup.transform.position - _cooler.TapPoint).sqrMagnitude < .12f) return true;
            return false;
        }

        public bool Grab(XRBaseInteractable target)
        {
            if (target == null || !_ready) return false;
            if (target.isSelected) _mgr.CancelInteractableSelection((IXRSelectInteractable)target);
            // Far grabs keep the item where it was; bring it to the hand first so it carries like a held object.
            var body = target.GetComponent<Rigidbody>();
            if (body != null)
            {
                Vector3 hold = _cam.transform.TransformPoint(HoldOffset);
                bool tray = target.GetComponent<Tray>() != null;
                if (body.isKinematic == false) { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; }
                body.position = hold; target.transform.position = hold;
                if (tray) { var flat = Quaternion.Euler(0, _yaw, 0); body.rotation = flat; target.transform.rotation = flat; }
                Physics.SyncTransforms();
            }
            _mgr.SelectEnter((IXRSelectInteractor)_hand, (IXRSelectInteractable)target);
            _tilt = 0;
            return _hand.hasSelection;
        }

        public void Drop(bool smart)
        {
            var held = Held; if (held == null) return;
            Vector3? place = null;
            if (smart)
            {
                if (held.GetComponent<CupFill>() != null && AimingAtCooler()) place = _cooler.TapPoint;
                else if (_hasAim && _aim.normal.y > .5f) place = _aim.point + Vector3.up * (HalfHeight(held) + .02f);
            }
            _mgr.SelectExit((IXRSelectInteractor)_hand, (IXRSelectInteractable)held);
            _tilt = 0;
            if (!place.HasValue) return;
            var body = held.GetComponent<Rigidbody>();
            if (body != null) { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; body.position = place.Value; }
            held.transform.position = place.Value;
        }

        static float HalfHeight(XRBaseInteractable it)
        {
            var c = it.GetComponentInChildren<Collider>();
            return c != null ? c.bounds.extents.y : .05f;
        }
    }
}
