using System.Collections;
using System.IO;
using CanteenRush.Core;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
namespace CanteenRush.Gameplay.Desktop
{
    /// Scripted desktop playthrough that saves screenshots, for checking the real window and player build.
    /// Start the app with:  -crdemo OUTPUT_DIR   (editor or player). Plays the tutorial and a fast round, then quits.
    public sealed class DesktopDemoRunner : MonoBehaviour
    {
        string _dir; DesktopController _d; Transform _rig; int _shots;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "-crdemo") { var go = new GameObject("DesktopDemoRunner"); go.AddComponent<DesktopDemoRunner>()._dir = args[i + 1]; DontDestroyOnLoad(go); }
        }

        IEnumerator Start()
        {
            Directory.CreateDirectory(_dir);
            for (int i = 0; i < 600 && (_d = FindFirstObjectByType<DesktopController>()) == null; i++) yield return null;
            for (int i = 0; i < 600 && !_d.Ready; i++) yield return null;
            _d.EnableForTests();
            _rig = FindFirstObjectByType<ComfortSettings>().transform;
            Log("resolution " + Screen.width + "x" + Screen.height);
            yield return Wait(1.5f); yield return Shot("01_start_menu");
            _d.Game.Play(); yield return Wait(1f); yield return Shot("02_tutorial_started");

            var donut = Item(ItemId.Donut);
            Face(donut.transform.position, 1f); yield return Wait(.5f); yield return Shot("03_looking_at_donut");
            Log("focus " + _d.Focus + " '" + _d.FocusLabel + "'");
            yield return Press(); yield return Wait(.8f); yield return Shot("04_holding_donut");
            Log("holding after grab: " + _d.Holding + " sel " + _d.DebugSelected());

            var tray = FindFirstObjectByType<Tray>();
            Face(tray.transform.position, 1.1f); yield return Wait(.5f); yield return Shot("05_aiming_at_tray");
            Log("focus " + _d.Focus + " '" + _d.FocusLabel + "'");
            yield return Press(); yield return Wait(1.2f); yield return Shot("06_donut_on_tray");
            Log("tray contents: " + string.Join(",", tray.Contents) + " state " + _d.Game.State + " sel " + _d.DebugSelected());

            for (int attempt = 0; attempt < 3 && !_d.Holding; attempt++)
            {
                Face(tray.transform.position, 1.1f); yield return Wait(.5f);
                Log("tray at " + tray.transform.position + " cam " + Camera.main.transform.position + " focus " + _d.Focus + " '" + _d.FocusLabel + "' kin " + tray.GetComponent<Rigidbody>().isKinematic + " returning " + tray.Returning);
                yield return Press(); yield return Wait(.8f);
            }
            yield return Shot("07_holding_tray");
            Log("holding tray: " + _d.Holding + " sel " + _d.DebugSelected() + " focus " + _d.Focus + " " + _d.FocusLabel);

            var socket = FindFirstObjectByType<ServeSocket>();
            Face(socket.transform.position + Vector3.up * .3f, 1.4f, socket.transform.position + Vector3.right * 3); yield return Wait(.8f);
            yield return Shot("08_at_customer_table"); Log("focus " + _d.Focus + " '" + _d.FocusLabel + "'");
            yield return Press(); yield return Wait(2f); yield return Shot("09_after_serve");
            Log("state after serve: " + _d.Game.State);

            // fast-forward into a real round
            for (int i = 0; i < 20 && _d.Game.State == RoundState.Tutorial; i++) yield return Wait(.5f);
            Time.timeScale = 1; yield return Wait(1f); yield return Shot("10_round_running");
            Teleport(socket.transform.position, 3.5f); yield return Wait(.5f); yield return Shot("11_patio_view");
            Time.timeScale = 15; float t = 0; while (_d.Game.State != RoundState.Results && t < 40) { t += Time.unscaledDeltaTime; yield return null; }
            Time.timeScale = 1; yield return Wait(1f); yield return Shot("12_results");
            Log("final state " + _d.Game.State);
            Log("DONE");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(0);
#else
            Application.Quit();
#endif
        }

        void Log(string m) { File.AppendAllText(Path.Combine(_dir, "log.txt"), m + "\n"); }
        static IEnumerator Wait(float s) { float t = 0; while (t < s) { t += Time.unscaledDeltaTime; yield return null; } }
        IEnumerator Press() { _d.QueuePress(); yield return null; yield return null; }
        IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(_dir, name + ".png")); _shots++;
            yield return null; yield return null;
        }
        void Face(Vector3 thing, float back, Vector3? from = null)
        {
            Vector3 flat = thing; flat.y = _rig.position.y;
            Vector3 dir = (from ?? _rig.position) - flat; dir.y = 0; if (dir.sqrMagnitude < .01f) dir = -_rig.forward; dir.Normalize();
            _d.Teleport(flat + dir * back); _d.LookAt(thing);
        }
        void Teleport(Vector3 near, float back)
        {
            Vector3 p = near; p.y = _rig.position.y; _d.Teleport(p + Vector3.right * back); _d.LookAt(near + Vector3.up * 1.3f);
        }
        static ItemBehaviour Item(ItemId id)
        {
            foreach (var i in FindObjectsByType<ItemBehaviour>(FindObjectsSortMode.None)) if (i.Id == id && i.gameObject.activeInHierarchy) return i;
            return null;
        }
    }
}
