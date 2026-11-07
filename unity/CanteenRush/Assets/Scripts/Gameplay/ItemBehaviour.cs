using CanteenRush.Core;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
namespace CanteenRush.Gameplay
{
    [RequireComponent(typeof(XRGrabInteractable), typeof(Rigidbody))]
    public sealed class ItemBehaviour : MonoBehaviour
    {
        [SerializeField] ItemId _id;
        [SerializeField] GameConfig _config;
        XRGrabInteractable _grab;
        Rigidbody _body;
        float _lastImpact, _despawn;
        bool _dropped;
        static int _activeCount;
        public static int ActiveCount => _activeCount;
        public ItemId Id => _id;
        public bool IsSelected => _grab != null && _grab.isSelected;
        public Hand HoldingHand { get; private set; }
        public void SetItemId(ItemId id) { _id = id; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ClearCount() { _activeCount = 0; }
        void Awake() { _grab = GetComponent<XRGrabInteractable>(); _body = GetComponent<Rigidbody>(); }
        void OnEnable()
        {
            _activeCount++; _dropped = false; _lastImpact = float.NegativeInfinity;
            _grab.selectEntered.AddListener(Selected); _grab.selectExited.AddListener(Released);
        }
        void OnDisable()
        {
            _activeCount = Mathf.Max(0, _activeCount - 1);
            _grab.selectEntered.RemoveListener(Selected); _grab.selectExited.RemoveListener(Released);
        }
        void Selected(SelectEnterEventArgs args)
        {
            if (args.interactorObject is XRSocketInteractor) GameEvents.ItemSocketed(_id, transform.position);
            else { HoldingHand = HandResolver.Resolve(args.interactorObject); GameEvents.ItemGrabbed(_id, HoldingHand, transform.position); }
        }
        void Released(SelectExitEventArgs args)
        {
            if (!(args.interactorObject is XRSocketInteractor)) GameEvents.ItemReleased(_id, HandResolver.Resolve(args.interactorObject), transform.position);
        }
        public void SetAccessible(bool accessible)
        {
            // Items already taken out of a fridge remain usable when it closes.
            if (IsSelected) return;
            _grab.enabled = accessible; _body.isKinematic = !accessible;
        }
        public void Recycle()
        {
            if (_grab != null && _grab.interactionManager != null)
                _grab.interactionManager.CancelInteractableSelection((IXRSelectInteractable)_grab);
            gameObject.SetActive(false);
        }
        void Update()
        {
            if (_dropped) { _despawn -= Time.deltaTime; if (_despawn <= 0) Recycle(); return; }
            if (_config != null && !_grab.isSelected && transform.position.y < _config.SpillHeight)
            {
                _dropped = true; _despawn = _config.ItemDespawnSeconds;
                GameEvents.ItemDropped(_id, _body.linearVelocity.magnitude, transform.position);
            }
        }
        void OnCollisionEnter(Collision collision)
        {
            if (!_dropped && Time.time - _lastImpact >= (_config != null ? _config.ImpactInterval : .1f))
            { _lastImpact = Time.time; GameEvents.ItemImpact(_id, collision.relativeVelocity.magnitude, transform.position); }
        }
    }
}
