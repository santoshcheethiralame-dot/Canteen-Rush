using System.Collections;
using CanteenRush.Core;
using CanteenRush.Haptics;
using UnityEngine;
using UnityEngine.InputSystem;
namespace CanteenRush.P3Sandbox
{
    /// Sandbox_P3: everything P3 owns, running without P1's world or P2's gameplay.
    /// Builds a floor, a light, a "head" camera with two stand-in controllers, instantiates the Audio, Haptics
    /// and UIShell prefabs, and stands in for GameManager: Play/Replay runs SyntheticRound, Menu returns to Menu.
    /// Head controls (to hear direction): arrow keys or Q/E turn, W/A/S/D walk, hold right mouse to look.
    public sealed class P3Sandbox : MonoBehaviour
    {
        [SerializeField] GameObject _audioPrefab, _hapticsPrefab, _uiPrefab;
        [SerializeField] float _beat = 0.35f;
        [SerializeField] float _turnSpeed = 90f, _walkSpeed = 1.8f, _mouseLook = 0.15f;
        Transform _head;
        Coroutine _round;
        float _yaw, _pitch;

        public Transform Head => _head;

        void Awake()
        {
            BuildWorld();
            Spawn(_audioPrefab, "Audio");
            Spawn(_hapticsPrefab, "Haptics");
            Spawn(_uiPrefab, "UIShell");
            var hud = FindFirstObjectByType<HapticsDebugHud>();
            if (hud != null) hud.SetFallbackAnchors(_head.Find("Left Controller"), _head.Find("Right Controller"));
        }

        void Spawn(GameObject prefab, string childName)
        {
            if (prefab == null) { Debug.LogWarning("[Sandbox_P3] Missing prefab " + childName); return; }
            Instantiate(prefab, transform).name = childName;
        }

        void BuildWorld()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor"; floor.transform.localScale = new Vector3(6, 1, 6);
            floor.transform.position = new Vector3(0, 0, 8);
            var lightGo = new GameObject("Sun");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.1f;
            lightGo.transform.rotation = Quaternion.Euler(50, -30, 0);

            var head = new GameObject("Head");
            head.tag = "MainCamera";
            head.transform.position = new Vector3(0, 1.6f, 0);
            head.AddComponent<Camera>().nearClipPlane = 0.05f;
            head.AddComponent<AudioListener>();
            _head = head.transform;
            MakeHand(head.transform, "Left Controller", new Vector3(-0.22f, -0.35f, 0.45f));
            MakeHand(head.transform, "Right Controller", new Vector3(0.22f, -0.35f, 0.45f));

            // The 8 seats as small posts, so you can see where each bell comes from.
            for (int s = 0; s < 8; s++)
            {
                var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                post.name = "Seat_" + s;
                Destroy(post.GetComponent<Collider>());
                post.transform.position = SyntheticRound.SeatPosition(s);
                post.transform.localScale = new Vector3(0.3f, 0.45f, 0.3f);
                try { post.tag = "SeatPoint"; } catch (UnityException) { }
            }
        }

        static void MakeHand(Transform head, string handName, Vector3 local)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = handName;
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(head, false);
            go.transform.localPosition = local;
            go.transform.localScale = new Vector3(0.05f, 0.05f, 0.12f);
        }

        void OnEnable()
        {
            GameEvents.OnPlayRequested += Play;
            GameEvents.OnMenuRequested += Menu;
            GameEvents.OnComfortChanged += Comfort;
        }

        void OnDisable()
        {
            GameEvents.OnPlayRequested -= Play;
            GameEvents.OnMenuRequested -= Menu;
            GameEvents.OnComfortChanged -= Comfort;
        }

        void Start() => GameEvents.RoundStateChanged(RoundState.Menu);

        public void Play()
        {
            if (_round != null) StopCoroutine(_round);
            _round = StartCoroutine(RunRound());
        }

        IEnumerator RunRound() { yield return SyntheticRound.Play(_beat); _round = null; }

        public void Menu()
        {
            if (_round != null) { StopCoroutine(_round); _round = null; }
            GameEvents.RoundStateChanged(RoundState.Menu);
        }

        void Comfort(bool seated, bool teleport) =>
            Debug.Log("[Sandbox_P3] ComfortChanged seated=" + seated + " teleport=" + teleport + " (P2's ComfortSettings applies this in Main)");

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null)
            {
                float turn = (kb.rightArrowKey.isPressed || kb.eKey.isPressed ? 1 : 0) - (kb.leftArrowKey.isPressed || kb.qKey.isPressed ? 1 : 0);
                _yaw += turn * _turnSpeed * Time.deltaTime;
                var move = new Vector3((kb.dKey.isPressed ? 1 : 0) - (kb.aKey.isPressed ? 1 : 0), 0, (kb.wKey.isPressed ? 1 : 0) - (kb.sKey.isPressed ? 1 : 0));
                _head.position += Quaternion.Euler(0, _yaw, 0) * move * (_walkSpeed * Time.deltaTime);
            }
            var mouse = Mouse.current;
            if (mouse != null && mouse.rightButton.isPressed)
            {
                var d = mouse.delta.ReadValue();
                _yaw += d.x * _mouseLook; _pitch = Mathf.Clamp(_pitch - d.y * _mouseLook, -70, 70);
            }
            _head.rotation = Quaternion.Euler(_pitch, _yaw, 0);
        }
    }
}
