using System.Collections.Generic;
using CanteenRush.Core;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
namespace CanteenRush.Gameplay
{
    public sealed class WaterCooler : MonoBehaviour
    {
        [SerializeField] XRSimpleInteractable _pump;
        readonly Dictionary<Collider, CupFill> _cups = new Dictionary<Collider, CupFill>(8);
        readonly HashSet<CupFill> _filledThisFrame = new HashSet<CupFill>(8);
        bool _pressed;
        void OnEnable()
        {
            _pump.activated.AddListener(Press); _pump.deactivated.AddListener(Release);
            _pump.selectExited.AddListener(Exit);
        }
        void OnDisable()
        {
            _pump.activated.RemoveListener(Press); _pump.deactivated.RemoveListener(Release);
            _pump.selectExited.RemoveListener(Exit); ResetPump(); _cups.Clear();
        }
        // The cup-finding volume floats under the spout; stretch it down to the counter so a cup set down under the tap
        // stays inside it and fills, instead of only filling while held in mid-air.
        void Start()
        {
            foreach (var box in GetComponents<BoxCollider>())
            {
                if (!box.isTrigger) continue;
                var b = box.bounds; Vector3 from = new Vector3(b.center.x, b.min.y + .01f, b.center.z);
                float best = float.PositiveInfinity;
                foreach (var h in Physics.RaycastAll(from, Vector3.down, 1.2f, ~0, QueryTriggerInteraction.Ignore))
                    if (h.collider.attachedRigidbody == null && h.distance < best) best = h.distance;
                if (float.IsPositiveInfinity(best) || best < .05f) continue;
                float scale = Mathf.Max(1e-4f, box.transform.lossyScale.y), grow = (best + .03f) / scale;
                var size = box.size; var center = box.center;
                size.y += grow; center.y -= grow * .5f;
                box.size = size; box.center = center;
            }
        }
        void Press(ActivateEventArgs args) { if (_pressed) return; _pressed = true; GameEvents.PumpActivated(true, _pump.transform.position); }
        void Release(DeactivateEventArgs args) { ResetPump(); }
        void Exit(SelectExitEventArgs args) { ResetPump(); }
        /// Desktop mode: hold-to-pump without needing a second virtual hand on the pump lever.
        public void SetPump(bool on)
        {
            if (on) { if (_pressed) return; _pressed = true; GameEvents.PumpActivated(true, _pump.transform.position); }
            else ResetPump();
        }
        public bool Pumping => _pressed;
        /// Where a cup sits to catch water: low in the trigger volume that finds cups.
        public Vector3 TapPoint
        {
            get
            {
                foreach (var c in GetComponentsInChildren<Collider>(true)) if (c.isTrigger) return c.bounds.center - Vector3.up * c.bounds.extents.y * .6f;
                return _pump.transform.position + Vector3.down * .25f;
            }
        }
        public void ResetPump() { if (!_pressed) return; _pressed = false; GameEvents.PumpActivated(false, _pump.transform.position); }
        void OnTriggerEnter(Collider other) { var cup = other.GetComponentInParent<CupFill>(); if (cup != null) _cups[other] = cup; }
        void OnTriggerExit(Collider other) { _cups.Remove(other); }
        void Update()
        {
            if (!_pressed) return;
            _filledThisFrame.Clear();
            foreach (var pair in _cups)
                if (pair.Key != null && pair.Key.enabled && pair.Value != null && pair.Value.isActiveAndEnabled && _filledThisFrame.Add(pair.Value))
                    pair.Value.Fill(Time.deltaTime);
        }
    }
}
