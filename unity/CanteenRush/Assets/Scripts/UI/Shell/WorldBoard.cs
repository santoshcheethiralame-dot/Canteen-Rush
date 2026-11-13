using CanteenRush.Core;
using UnityEngine;
namespace CanteenRush.UI
{
    /// Base for the menu and results boards: a world-space canvas that appears in front of the player
    /// (1.4 m ahead, slightly below eye height, facing them on Y only) in the round states it belongs to.
    public abstract class WorldBoard : MonoBehaviour
    {
        [SerializeField] protected float _distance = 1.4f;
        [SerializeField] protected float _belowEyes = 0.15f;
        protected Canvas _canvas;

        public bool IsShown => _canvas != null && _canvas.gameObject.activeSelf;
        public Canvas Canvas => _canvas;

        protected abstract Vector2 SizeMm { get; }
        protected abstract void Build(RectTransform root);
        protected abstract bool VisibleIn(RoundState s);

        protected virtual void Awake()
        {
            _canvas = UiFactory.Canvas(transform, GetType().Name + "_Canvas", SizeMm);
            Build((RectTransform)_canvas.transform);
            _canvas.gameObject.SetActive(false);
        }

        protected virtual void OnEnable() => GameEvents.OnRoundStateChanged += RoundChanged;
        protected virtual void OnDisable() => GameEvents.OnRoundStateChanged -= RoundChanged;

        protected virtual void RoundChanged(RoundState s) => SetShown(VisibleIn(s));

        public void SetShown(bool show)
        {
            if (_canvas == null) return;
            if (show && !_canvas.gameObject.activeSelf) PlaceInFront();
            _canvas.gameObject.SetActive(show);
        }

        void PlaceInFront()
        {
            var cam = Camera.main;
            if (cam == null) return;
            _canvas.worldCamera = cam;
            var fwd = cam.transform.forward; fwd.y = 0;
            if (fwd.sqrMagnitude < 1e-4f) fwd = Vector3.forward;
            fwd.Normalize();
            var pos = cam.transform.position + fwd * _distance + Vector3.down * _belowEyes;
            _canvas.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(fwd, Vector3.up));
        }
    }
}
