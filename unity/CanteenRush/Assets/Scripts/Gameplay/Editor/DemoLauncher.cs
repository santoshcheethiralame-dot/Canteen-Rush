using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace CanteenRush.Gameplay.Editor
{
    /// With -crdemo on the editor command line: fix the Game view at 1600x900, open Main and enter play mode
    /// so DesktopDemoRunner can take over and save screenshots.
    [InitializeOnLoad]
    static class DemoLauncher
    {
        static DemoLauncher()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-crdemo") < 0) return;
            EditorApplication.delayCall += () =>
            {
                try { SetGameViewSize(1600, 900); } catch (Exception e) { Debug.LogWarning("Game view size not set: " + e.Message); }
                EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
                EditorApplication.EnterPlaymode();
            };
        }

        static void SetGameViewSize(int width, int height)
        {
            var asm = typeof(UnityEditor.Editor).Assembly;
            var sizesType = asm.GetType("UnityEditor.GameViewSizes");
            var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType).GetProperty("instance").GetValue(null);
            var group = sizesType.GetMethod("GetGroup").Invoke(singleton, new object[] { 0 });   // Standalone
            var sizeType = asm.GetType("UnityEditor.GameViewSize");
            var kind = asm.GetType("UnityEditor.GameViewSizeType");
            var size = sizeType.GetConstructor(new[] { kind, typeof(int), typeof(int), typeof(string) })
                .Invoke(new object[] { Enum.ToObject(kind, 1), width, height, "crdemo" });
            group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { size });
            int index = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null) - 1;
            var view = EditorWindow.GetWindow(asm.GetType("UnityEditor.GameView"));
            view.GetType().GetMethod("SizeSelectionCallback", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Invoke(view, new object[] { index, size });
        }
    }
}
