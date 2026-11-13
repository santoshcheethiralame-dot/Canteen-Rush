using System.Collections.Generic;
using UnityEngine;
namespace CanteenRush.Audio
{
    /// Goes on P1's trigger volumes World/AudioZones/AudioZone_Indoor and AudioZone_Outdoor.
    /// Instead of relying on OnTriggerEnter (the XR rig may have no collider or Rigidbody on the camera),
    /// AudioService asks every 0.1 s which zones contain the listener. Indoor wins over Outdoor because
    /// P1's outdoor box overlaps the kiosk. An Indoor zone can add the canteen AudioReverbZone (preset Room).
    [RequireComponent(typeof(Collider))]
    public sealed class AudioZoneTrigger : MonoBehaviour
    {
        [SerializeField] AudioZone _zone = AudioZone.Indoor;
        [SerializeField] bool _addReverbZone = true;
        static readonly List<AudioZoneTrigger> Active = new List<AudioZoneTrigger>();
        Collider _collider;

        public AudioZone Zone => _zone;
        public void Configure(AudioZone zone, bool reverb) { _zone = zone; _addReverbZone = reverb; SetupReverb(); }

        void Awake() { _collider = GetComponent<Collider>(); _collider.isTrigger = true; SetupReverb(); }
        void OnEnable() { if (!Active.Contains(this)) Active.Add(this); }
        void OnDisable() => Active.Remove(this);

        void SetupReverb()
        {
            if (_zone != AudioZone.Indoor || !_addReverbZone || _collider == null) return;
            var reverb = GetComponent<AudioReverbZone>();
            if (reverb == null) reverb = gameObject.AddComponent<AudioReverbZone>();
            // Full room reverb inside the kiosk, fading out within ~1.5 m of its walls, nothing on the patio.
            var e = _collider.bounds.extents;
            float inner = Mathf.Max(0.5f, Mathf.Min(e.x, e.z));
            reverb.reverbPreset = AudioReverbPreset.Room;
            reverb.minDistance = inner;
            reverb.maxDistance = inner + 1.5f;
        }

        public bool Contains(Vector3 p)
        {
            if (_collider == null || !_collider.enabled) return false;
            if (!_collider.bounds.Contains(p)) return false;
            return (_collider.ClosestPoint(p) - p).sqrMagnitude < 1e-6f;
        }

        /// Indoor if any indoor zone contains the point, else Outdoor if any outdoor zone does.
        /// Returns false when no zone contains it (keep the current snapshot).
        public static bool TryEvaluate(Vector3 p, out AudioZone zone)
        {
            bool outdoor = false;
            for (int i = 0; i < Active.Count; i++)
            {
                if (!Active[i].Contains(p)) continue;
                if (Active[i]._zone == AudioZone.Indoor) { zone = AudioZone.Indoor; return true; }
                outdoor = true;
            }
            zone = AudioZone.Outdoor;
            return outdoor;
        }
    }
}
