using System.Collections.Generic;
using UnityEngine;
namespace CanteenRush.Audio
{
    /// Places the ambience emitters and zone components into whatever scene Audio.prefab lands in, once, at Start.
    /// It finds world anchors by name and tag only (Cr.Audio may not reference Cr.World or Cr.Gameplay):
    /// - fridge: "Station_FridgeDoorUpper", "Door_Upper" or "FridgeBody" (P1's FBX nodes, P2's door prefab);
    /// - seats: GameObjects tagged "SeatPoint" (P1's SeatPoint markers);
    /// - zones: "AudioZone_Indoor" / "AudioZone_Outdoor" (P1's trigger boxes) get an AudioZoneTrigger if missing.
    /// Anything not found falls back to positions relative to this object, so the sandbox works too.
    public sealed class AudioSceneInstaller : MonoBehaviour
    {
        static readonly string[] FridgeNames = { "Station_FridgeDoorUpper", "Door_Upper", "FridgeBody" };
        [SerializeField] float _birdRadius = 9f, _birdHeight = 3f;
        [SerializeField] Vector3 _fallbackFridge = new Vector3(-0.5f, 1.2f, 1.5f);
        [SerializeField] Vector3 _fallbackPatio = new Vector3(0f, 1.2f, 14f);

        public static Vector3 FridgePosition { get; private set; }
        public static Vector3 PatioCentre { get; private set; }
        public static readonly List<AmbienceEmitter> Emitters = new List<AmbienceEmitter>();
        public static AmbienceEmitter Find(string cueId)
        {
            foreach (var e in Emitters) if (e != null && e.CueId == cueId) return e;
            return null;
        }

        void Awake()
        {
            // Resolve anchors early so the binder's Start already sees them.
            FridgePosition = FindNamed(FridgeNames, out var f) ? f : transform.TransformPoint(_fallbackFridge);
            PatioCentre = SeatCentroid(out var c) ? c : transform.TransformPoint(_fallbackPatio);
        }

        void Start()
        {
            Emitters.Clear();
            AddEmitter(AudioCueIds.FridgeHum, FridgePosition + Vector3.up * 0.3f, AmbienceEmitter.Gate.Always, 1f);
            AddEmitter(AudioCueIds.CanteenRoom, FridgePosition, AmbienceEmitter.Gate.IndoorOnly, 1f);   // 2D bed
            AddEmitter(AudioCueIds.PatioCrowd, PatioCentre + Vector3.up * 0.4f, AmbienceEmitter.Gate.Always, 0f);
            for (int i = 0; i < 4; i++)
            {
                float a = Mathf.PI * 0.5f * i + Mathf.PI * 0.25f;
                AddEmitter(AudioCueIds.PatioBirds, PatioCentre + new Vector3(Mathf.Cos(a) * _birdRadius, _birdHeight, Mathf.Sin(a) * _birdRadius),
                    AmbienceEmitter.Gate.Always, 1f);
            }
            AttachZone("AudioZone_Indoor", AudioZone.Indoor);
            AttachZone("AudioZone_Outdoor", AudioZone.Outdoor);
        }

        void AddEmitter(string cue, Vector3 pos, AmbienceEmitter.Gate gate, float level)
        {
            var go = new GameObject("Amb_" + cue + "_" + Emitters.Count);
            go.transform.SetParent(transform, true);
            go.transform.position = pos;
            var e = go.AddComponent<AmbienceEmitter>();
            e.Configure(cue, gate, level);
            Emitters.Add(e);
        }

        static void AttachZone(string objectName, AudioZone zone)
        {
            var go = GameObject.Find(objectName);
            if (go == null || go.GetComponent<Collider>() == null) return;
            var trigger = go.GetComponent<AudioZoneTrigger>();
            if (trigger == null) { trigger = go.AddComponent<AudioZoneTrigger>(); trigger.Configure(zone, zone == AudioZone.Indoor); }
        }

        static bool FindNamed(string[] names, out Vector3 pos)
        {
            foreach (var n in names)
            {
                var go = GameObject.Find(n);
                if (go == null) continue;
                var r = go.GetComponentInChildren<Renderer>();
                pos = r != null ? r.bounds.center : go.transform.position;
                return true;
            }
            pos = default;
            return false;
        }

        static bool SeatCentroid(out Vector3 centre)
        {
            centre = default;
            GameObject[] seats;
            try { seats = GameObject.FindGameObjectsWithTag("SeatPoint"); }
            catch (UnityException) { return false; }   // tag not defined in this project/scene
            if (seats.Length == 0) return false;
            foreach (var s in seats) centre += s.transform.position;
            centre /= seats.Length;
            return true;
        }
    }
}
