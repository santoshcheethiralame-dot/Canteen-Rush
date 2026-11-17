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
