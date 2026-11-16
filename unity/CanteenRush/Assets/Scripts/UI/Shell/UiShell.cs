using CanteenRush.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace CanteenRush.UI
{
    /// Root of P3's UI shell (Assets/Prefabs/UI/UIShell.prefab): makes sure an EventSystem with the
    /// XR UI Input Module exists and creates the MenuBoard, ResultsBoard, WristHud and TutorialHint children.
    ///
    /// Main.unity is P1's and generated; its builder places Audio.prefab and Haptics.prefab but has no UI hook yet.
    /// Until P1 adds one, Install() runs after every scene load and instantiates the shell from Resources if the
    /// scene has a GameEvents-driven round (a "_Systems" root or P2's runtime) but no UiShell.
    public sealed class UiShell : MonoBehaviour
    {
        public const string ResourcePath = "P3/UIShell";
        [SerializeField] GameConfig _config;

        public MenuBoard Menu { get; private set; }
        public ResultsBoard Results { get; private set; }
        public WristHud Wrist { get; private set; }
        public TutorialHint Tutorial { get; private set; }

        void Awake()
        {
            UiFactory.EnsureEventSystem();
            Menu = Child<MenuBoard>("MenuBoard");
            Results = Child<ResultsBoard>("ResultsBoard");
            Wrist = Child<WristHud>("WristHUD");
            Tutorial = Child<TutorialHint>("TutorialHint");
            if (_config != null) Results.SetConfig(_config);
        }

        bool _sawState;
        void OnEnable() => GameEvents.OnRoundStateChanged += SawState;
        void OnDisable() => GameEvents.OnRoundStateChanged -= SawState;
        void SawState(RoundState s) => _sawState = true;

        void Start()
        {
            // If the round entered Menu before this object existed, no state event will come: show the menu.
            if (!_sawState) Menu.SetShown(true);
        }

        T Child<T>(string childName) where T : Component
        {
            var existing = GetComponentInChildren<T>(true);
            if (existing != null) return existing;
            var go = new GameObject(childName);
            go.transform.SetParent(transform, false);
            return go.AddComponent<T>();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Register() { SceneManager.sceneLoaded -= Loaded; SceneManager.sceneLoaded += Loaded; Loaded(SceneManager.GetActiveScene(), LoadSceneMode.Single); }

        /// P1's MainSceneBuilder creates the roots _Audio, _Haptics and UI and fills the first two from
        /// Assets/Prefabs/Systems/*.prefab when they exist. A Main generated before P3's prefabs existed has them
        /// empty, so fill any empty root from Resources (copies of the same prefabs). Loaded by path only:
        /// Cr.UI does not reference Cr.Audio or Cr.Haptics.
        static void Loaded(Scene scene, LoadSceneMode mode)
        {
            FillRoot("_Audio", "Audio", "P3/Audio");
            FillRoot("_Haptics", "Haptics", "P3/Haptics");
            if (FindFirstObjectByType<UiShell>() == null) FillRoot("UI", "UIShell", ResourcePath);
        }

        static void FillRoot(string rootName, string childName, string resource)
        {
            var root = GameObject.Find(rootName);
            if (root == null || root.transform.Find(childName) != null) return;
            var prefab = Resources.Load<GameObject>(resource);
            if (prefab == null) { Debug.LogWarning("[P3] Missing Resources/" + resource); return; }
            var go = Instantiate(prefab, root.transform);
            go.name = childName;
        }
    }
}
