using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using CanteenRush.Core;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace CanteenRush.Tests.PlayMode
{
    public static class P3TestUtil
    {
        public const string SandboxScene = "Assets/Scenes/Sandbox_P3.unity";
        public const string MainScene = "Assets/Scenes/Main.unity";

        public static IEnumerator Load(string path, int frames = 5)
        {
            EditorSceneManager.LoadSceneInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Single));
            for (int i = 0; i < frames; i++) yield return null;
        }

        /// Subscriber count of every GameEvents event, read from the compiler-generated backing fields.
        public static Dictionary<string, int> SubscriberCounts()
        {
            var counts = new Dictionary<string, int>();
            foreach (var f in typeof(GameEvents).GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
                if (typeof(Delegate).IsAssignableFrom(f.FieldType))
                {
                    var d = (Delegate)f.GetValue(null);
                    counts[f.Name] = d == null ? 0 : d.GetInvocationList().Length;
                }
            return counts;
        }

        public static string Diff(Dictionary<string, int> a, Dictionary<string, int> b)
        {
            var s = new System.Text.StringBuilder();
            foreach (var kv in a)
            {
                b.TryGetValue(kv.Key, out var other);
                if (other != kv.Value) s.Append(kv.Key).Append(' ').Append(kv.Value).Append("->").Append(other).Append("; ");
            }
            return s.ToString();
        }
    }
}
