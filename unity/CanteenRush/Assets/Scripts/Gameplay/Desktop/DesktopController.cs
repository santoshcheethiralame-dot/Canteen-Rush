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
        public static readonly Vector3 TrayHoldOffset = new Vector3(0f, -.42f, .95f);   // a tray is big: carry it low and out front
        Vector3 HoldFor(XRBaseInteractable it) => it != null && it.GetComponent<Tray>() != null ? TrayHoldOffset : HoldOffset;
        public float Sensitivity = .12f;
        public Kind Focus { get; private set; }
        public string FocusLabel { get; private set; } = "";
        public bool Paused { get; private set; }
        public bool Ready => _ready;
        public bool Holding => Held != null;
        /// The item or tray in hand. The rig also lets the hand select scenery such as teleport floors; those do not count.
        public XRBaseInteractable Held
        {
            get
            {
                if (_hand == null || !_hand.hasSelection) return null;
                foreach (var it in _hand.interactablesSelected)
                    if (it is XRBaseInteractable b && (b.GetComponent<ItemBehaviour>() != null || b.GetComponent<Tray>() != null)) return b;
                return null;
            }
        }
        public XRBaseInteractable Target => _target;
        public string DebugSelected()
        {
            if (_hand == null) return "-";
            var sb = new System.Text.StringBuilder();
            foreach (var it in _hand.interactablesSelected) sb.Append(((Component)it).name).Append(' ');
            return sb + "| hover " + (_hand.hasHover ? ((Component)_hand.interactablesHovered[0]).name : "-") + " | target " + (_target != null ? _target.name : "-");
        }
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
        ServeSocket[] _sockets = new ServeSocket[0];
        ServeSocket _serve;
        Customer _customer;
        XRBaseInteractable.MovementType _heldMovement = XRBaseInteractable.MovementType.VelocityTracking;
        OrderService _orders;
        public Customer LookedAtCustomer => _customer;
        XRSocketInteractor _traySlot;
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
            for (int i = 0; i < 90; i++) yield return null;
            if (_active) HideHapticsHud();   // it may have built itself after we switched on
        }

        void Bind(Transform rig)
        {
            _rig = rig; _cc = rig.GetComponent<CharacterController>();
            _cam = rig.GetComponentInChildren<Camera>();
            _mgr = FindFirstObjectByType<XRInteractionManager>();
            _game = FindFirstObjectByType<GameManager>();
            _cooler = FindFirstObjectByType<WaterCooler>();
            _sockets = FindObjectsByType<ServeSocket>(FindObjectsSortMode.None);
            _orders = FindFirstObjectByType<OrderService>();
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
            SetVrParts(!on);
            var look = GetComponent<DesktopLook>() ?? gameObject.AddComponent<DesktopLook>();
            look.Apply(_cam, on);
            if (on)
            {
                _yaw = _rig.eulerAngles.y; _pitch = 0; _tilt = 0; Paused = false; Time.timeScale = 1f;
                PlaceCamera(); SetCursor(true);
            }
            else { Drop(false); Time.timeScale = 1f; SetCursor(false); }
        }

        // XR-only parts that fight desktop control: XRI locomotion (body transformer, gravity, grab-move) moves the same
        // CharacterController, the modality manager switches controllers off when none are tracked, the left
        // controller (wrist HUD, teleport ray) and controller models would float in view, the comfort vignette darkens
        // the screen whenever the player walks.
        readonly System.Collections.Generic.List<(GameObject go, bool was)> _vrObjects = new System.Collections.Generic.List<(GameObject, bool)>();
        readonly System.Collections.Generic.List<(Behaviour b, bool was)> _vrBehaviours = new System.Collections.Generic.List<(Behaviour, bool)>();
        void SetVrParts(bool vr)
        {
            if (!vr)
            {
                _vrObjects.Clear(); _vrBehaviours.Clear();
                foreach (var name in new[] { "Locomotion", "Left Controller", "Gaze Interactor", "Right Controller Visual", "Teleport Interactor", "Poke Interactor", "ComfortVignette" })
                    foreach (var t in _rig.GetComponentsInChildren<Transform>(true))
                        if (t.name == name && (name != "Poke Interactor" && name != "Teleport Interactor" || t.IsChildOf(_handRoot)))
                        { _vrObjects.Add((t.gameObject, t.gameObject.activeSelf)); t.gameObject.SetActive(false); }
                foreach (var b in _rig.GetComponentsInChildren<Behaviour>(true))
                    if (b != null && (b.GetType().Name == "XRInputModalityManager" || b.GetType().Name == "XRGazeAssistance"))
                    { _vrBehaviours.Add((b, b.enabled)); b.enabled = false; }
                _handRoot.gameObject.SetActive(true); _hand.gameObject.SetActive(true);
                var line = _hand.transform.Find("LineVisual"); if (line != null) { _vrObjects.Add((line.gameObject, line.gameObject.activeSelf)); line.gameObject.SetActive(false); }
                HideHapticsHud();
            }
            else
            {
                foreach (var (go, was) in _vrObjects) if (go != null) go.SetActive(was);
                foreach (var (b, was) in _vrBehaviours) if (b != null) b.enabled = was;
                foreach (var c in _vrCanvases) if (c != null) c.enabled = true;
                _vrObjects.Clear(); _vrBehaviours.Clear();
            }
        }

        // Cr.Gameplay may not reference Cr.Haptics, so the debug HUD (F9) is switched off by name.
        static void HideHapticsHud()
        {
            foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                if (mb.GetType().Name == "HapticsDebugHud")
                {
                    var visible = mb.GetType().GetProperty("Visible");
                    if (visible != null) visible.SetValue(mb, false);
                }
        }

        void PlaceCamera()
        {
            var offset = _cam.transform.parent;
            float y = offset.InverseTransformPoint(new Vector3(offset.position.x, _rig.position.y + EyeHeight, offset.position.z)).y;
            _cam.transform.localPosition = new Vector3(0, y, 0);
            _cc.height = 1.7f; _cc.center = new Vector3(0, .85f + _cc.skinWidth, 0); _cc.radius = Mathf.Min(_cc.radius, .3f);
            _cc.stepOffset = .42f; _cc.slopeLimit = 50f;   // the front platform is a 31 cm step
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

        bool _pressQueued, _pumpQueued;
        /// Scripted input for the demo runner and tests: same code path as a click or E.
        public void QueuePress() { _pressQueued = true; }
        public void QueuePump(bool on) { _pumpQueued = on; }
        public void Teleport(Vector3 floorPosition)
        {
            _cc.enabled = false; _rig.position = floorPosition; _cc.enabled = true; Physics.SyncTransforms();
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

        static readonly string[] VrBoards = { "MenuBoard", "ResultsBoard", "WristHud" };
        readonly System.Collections.Generic.List<Canvas> _vrCanvases = new System.Collections.Generic.List<Canvas>();
        float _boardScan;
        // The world-space menu, results and wrist boards are the VR UI; desktop mode draws its own screen UI instead.
        void HideVrBoards()
        {
            if (Time.unscaledTime > _boardScan)
            {
                _boardScan = Time.unscaledTime + 1f; _vrCanvases.Clear();
                foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                    if (System.Array.IndexOf(VrBoards, mb.GetType().Name) >= 0) _vrCanvases.AddRange(mb.GetComponentsInChildren<Canvas>(true));
            }
            foreach (var c in _vrCanvases) if (c != null && c.enabled) c.enabled = false;
        }

        void LateUpdate()
        {
            if (!_active || !_ready) return;
            HideVrBoards();
            // The hand rides in front of the camera; when empty it points at the crosshair target.
            var ct = _cam.transform;
            var held = Held;
            if (held != null && held.GetComponent<Tray>() != null)
            {
                // A tray stays level and turns only with your heading: following the camera's pitch tipped it past
                // the spill angle whenever you looked down at it. Right mouse still tilts it on purpose.
                var heading = Quaternion.Euler(0, _yaw, 0);
                if (!(Mouse.current != null && Mouse.current.rightButton.isPressed)) _tilt = Mathf.MoveTowards(_tilt, 0f, 120f * Time.unscaledDeltaTime);
                _handRoot.SetPositionAndRotation(ct.position + heading * TrayHoldOffset, heading * Quaternion.Euler(0, 0, _tilt));
                return;
            }
            Vector3 pos = ct.TransformPoint(HoldFor(Held));
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
            _target = null; _customer = null; Focus = Kind.None; FocusLabel = ""; _hasAim = false;
            var ct = _cam.transform; var held = Held;
            var hits = Physics.SphereCastAll(ct.position, .12f, ct.forward, Reach, Mask, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            XRBaseInteractable best = null; RaycastHit bestHit = default; float bestScore = float.MaxValue;
            foreach (var h in hits)
            {
                if (h.collider.transform.IsChildOf(_rig)) continue;
                var who = h.collider.GetComponentInParent<Customer>();
                if (who != null) { if (_customer == null && h.distance < Reach + 2f) _customer = who; continue; }
                var it = h.collider.GetComponentInParent<XRBaseInteractable>();
                if (it != null && it.isSelected && it.firstInteractorSelecting is XRSocketInteractor socketHolder)
                {
                    var tray = socketHolder.GetComponentInParent<Tray>();   // clicking food on a tray picks up the tray
                    if (tray != null) it = tray.GetComponent<XRBaseInteractable>();
                }
                if (it != null && it == held) continue;
                if (it != null && !Usable(it)) it = null;   // teleport floors and the like are scenery here
                if (it == null || it.GetComponent<XRSocketInteractor>() != null) { if (!_hasAim) { _aim = h; _hasAim = true; } continue; }
                // Prefer what is closest to the crosshair line, then what is nearest.
                float perp = Vector3.Cross(ct.forward, h.collider.bounds.center - ct.position).magnitude;
                float score = perp + h.distance * .05f;
                if (score < bestScore) { bestScore = score; best = it; bestHit = h; }
            }
            // No wall test on purpose: the display cases are closed collision meshes around the donuts.
            if (best != null) { _target = best; _aim = bestHit; _hasAim = true; }
            if (_customer == null) _customer = CustomerInView();
            Classify(held);
        }

        void Classify(XRBaseInteractable held)
        {
            _serve = null; _traySlot = null;
            if (held != null)
            {
                Focus = Kind.Drop; FocusLabel = "Drop " + Nice(held);
                var overTray = _target != null ? _target.GetComponent<Tray>() : null;
                if (overTray != null && held.GetComponent<ItemBehaviour>() != null && overTray.TryGetFreeSocket(out var slot)) { _traySlot = slot; FocusLabel = "Put " + Nice(held) + " on the tray"; return; }
                if (held.GetComponent<Tray>() != null)
                {
                    _serve = _customer != null ? SocketForSeat(_customer.SeatIndex) : SocketInView();
                    if (_serve != null) { FocusLabel = _customer != null ? "Serve " + OrderText(_customer.SeatIndex, "this customer") : "Serve the tray here"; return; }
                }
                if (held.GetComponent<CupFill>() != null && AimingAtCooler()) FocusLabel = "Place cup under the tap";
                else if (_hasAim && _aim.normal.y > .5f) FocusLabel = "Put " + Nice(held) + " here";
                return;
            }
            if (_target == null && _customer != null)
            {
                FocusLabel = _orders != null && _orders.HasOrder(_customer.SeatIndex) ? "Wants " + OrderText(_customer.SeatIndex, "") + "  (bring a tray, click them)" : "";
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

        Customer[] _guests = new Customer[0]; float _guestScan;
        // Customers have no collider, so pick the one whose body is nearest the crosshair line.
        Customer CustomerInView()
        {
            if (Time.unscaledTime > _guestScan) { _guests = FindObjectsByType<Customer>(FindObjectsSortMode.None); _guestScan = Time.unscaledTime + .5f; }
            var ct = _cam.transform; Customer best = null; float bestPerp = .5f;
            foreach (var g in _guests)
            {
                if (g == null || !g.isActiveAndEnabled) continue;
                for (float up = .5f; up <= 1.5f; up += .5f)
                {
                    Vector3 to = g.transform.position + Vector3.up * up - ct.position; float along = Vector3.Dot(to, ct.forward);
                    if (along < .3f || along > Reach + 3f) continue;
                    float perp = (to - ct.forward * along).magnitude;
                    if (perp < bestPerp) { bestPerp = perp; best = g; }
                }
            }
            return best;
        }

        ServeSocket SocketForSeat(int seat)
        {
            foreach (var sock in _sockets) if (sock != null && sock.SeatIndex == seat) return sock;
            return null;
        }

        public string OrderText(int seat, string fallback)
        {
            var order = _orders != null ? _orders.GetOrder(seat) : null;
            if (order == null || order.Items == null) return fallback;
            var names = new string[order.Items.Length];
            for (int i = 0; i < names.Length; i++) names[i] = order.Items[i].ToString().ToLowerInvariant();
            return string.Join(" + ", names);
        }

        ServeSocket SocketInView()
        {
            var ct = _cam.transform; ServeSocket best = null; float bestPerp = .6f;
            foreach (var sock in _sockets)
            {
                if (sock == null) continue;
                Vector3 to = sock.transform.position - ct.position; float along = Vector3.Dot(to, ct.forward);
                if (along < .3f || along > Reach + 1.5f) continue;
                float perp = (to - ct.forward * along).magnitude;
                if (_orders != null && _orders.HasOrder(sock.SeatIndex)) perp -= .25f;   // a table that is waiting wins ties
                if (perp < bestPerp) { bestPerp = perp; best = sock; }
            }
            return best;
        }

        static bool Usable(XRBaseInteractable it) =>
            it.GetComponent<ItemBehaviour>() != null || it.GetComponent<Tray>() != null || it.GetComponentInParent<FridgeDoor>() != null || it.GetComponentInParent<WaterCooler>() != null;

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
            bool press = (mouse != null && mouse.leftButton.wasPressedThisFrame) || kb.eKey.wasPressedThisFrame || _pressQueued; _pressQueued = false;
            if (kb.enterKey.wasPressedThisFrame && (_game.State == RoundState.Menu || _game.State == RoundState.Results)) _game.Play();
            if (press)
            {
                if (Holding) Drop(true);
                else if (Focus == Kind.Grab) Grab(_target);
                else if (Focus == Kind.Door) _target.GetComponentInParent<FridgeDoor>().Toggle();
            }
            if (_cooler != null)
            {
                bool pump = (kb.fKey.isPressed || _pumpQueued) && (Focus == Kind.Pump || CupAtTap());
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
                bool tray = target.GetComponent<Tray>() != null;
                Vector3 hold = tray ? _cam.transform.position + Quaternion.Euler(0, _yaw, 0) * TrayHoldOffset : _cam.transform.TransformPoint(HoldFor(target));
                if (body.isKinematic == false) { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; }
                body.position = hold; target.transform.position = hold;
                if (tray)
                {
                    var flat = Quaternion.Euler(0, _yaw, 0); body.rotation = flat; target.transform.rotation = flat;
                    // the tray grabs with a dynamic attach, so the hand must already be level or the offset bakes in a tilt
                    _tilt = 0; _handRoot.SetPositionAndRotation(hold, flat);
                }
                Physics.SyncTransforms();
            }
            _mgr.SelectEnter((IXRSelectInteractor)_hand, (IXRSelectInteractable)target);
            IgnorePlayer(target, true);   // a held tray or cup must never shove the player's capsule
            // Follow the hand exactly: velocity tracking lags and wobbles when the "hand" is a camera on a mouse.
            if (target is XRGrabInteractable g) { _heldMovement = g.movementType; g.movementType = XRBaseInteractable.MovementType.Instantaneous; }
            _tilt = 0;
            return Holding;
        }

        public void Drop(bool smart)
        {
            var held = Held; if (held == null) return;
            Vector3? place = null;
            if (smart)
            {
                if (_traySlot != null) place = _traySlot.attachTransform != null ? _traySlot.attachTransform.position : _traySlot.transform.position;
                else if (_serve != null) place = _serve.transform.position;
                else if (held.GetComponent<CupFill>() != null && AimingAtCooler()) place = RestOn(held, _cooler.TapPoint);
                else if (_hasAim && _aim.normal.y > .5f) place = _aim.point + Vector3.up * (PivotAboveBottom(held) + .01f);
            }
            _mgr.SelectExit((IXRSelectInteractor)_hand, (IXRSelectInteractable)held);
            IgnorePlayer(held, false);
            if (held is XRGrabInteractable g) g.movementType = _heldMovement;
            _tilt = 0;
            if (!place.HasValue) return;
            var body = held.GetComponent<Rigidbody>();
            if (body != null) { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; body.position = place.Value; }
            held.transform.position = place.Value;
            // Snap straight into the slot so the item never lands on the tray and shoves it off the rack.
            if (_traySlot != null && !_traySlot.hasSelection) { Physics.SyncTransforms(); _mgr.SelectEnter((IXRSelectInteractor)_traySlot, (IXRSelectInteractable)held); }
            if (_serve != null && held.GetComponent<Tray>() != null)
            {
                var socket = _serve.GetComponent<XRSocketInteractor>();
                Physics.SyncTransforms();
                if (socket != null && !socket.hasSelection) _mgr.SelectEnter((IXRSelectInteractor)socket, (IXRSelectInteractable)held);
            }
        }

        void IgnorePlayer(XRBaseInteractable it, bool ignore)
        {
            if (it == null || _cc == null) return;
            foreach (var c in it.GetComponentsInChildren<Collider>(true)) if (c != null) Physics.IgnoreCollision(c, _cc, ignore);
            // food riding in the tray's sockets comes along
            foreach (var socket in it.GetComponentsInChildren<XRSocketInteractor>(true))
                foreach (var food in socket.interactablesSelected)
                    foreach (var c in ((Component)food).GetComponentsInChildren<Collider>(true)) if (c != null) Physics.IgnoreCollision(c, _cc, ignore);
        }

        // Distance from the item's pivot down to its lowest collider point (pivots sit mid-item for donuts, at the base for cups).
        static float PivotAboveBottom(XRBaseInteractable it)
        {
            float min = float.PositiveInfinity;
            foreach (var c in it.GetComponentsInChildren<Collider>()) if (c != null && !c.isTrigger) min = Mathf.Min(min, c.bounds.min.y);
            return float.IsPositiveInfinity(min) ? .05f : Mathf.Max(0f, it.transform.position.y - min);
        }

        // Drop straight down from a point onto whatever solid surface is below it, so the item never starts inside it.
        static Vector3 RestOn(XRBaseInteractable it, Vector3 point)
        {
            var mine = it.GetComponentsInChildren<Collider>();
            var hits = Physics.RaycastAll(point + Vector3.up * .3f, Vector3.down, 1.5f, ~0, QueryTriggerInteraction.Ignore);
            float best = float.PositiveInfinity; Vector3 ground = point;
            foreach (var h in hits)
            {
                if (System.Array.IndexOf(mine, h.collider) >= 0 || h.collider.attachedRigidbody != null && !h.collider.attachedRigidbody.isKinematic) continue;
                if (h.distance < best) { best = h.distance; ground = h.point; }
            }
            return new Vector3(point.x, ground.y + PivotAboveBottom(it) + .005f, point.z);
        }
    }
}
