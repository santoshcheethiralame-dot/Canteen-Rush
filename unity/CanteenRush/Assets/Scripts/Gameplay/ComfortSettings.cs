using CanteenRush.Core;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
namespace CanteenRush.Gameplay
{
    public sealed class ComfortSettings : MonoBehaviour
    {
        [SerializeField] Transform _cameraOffset;
        [SerializeField] GameConfig _config;
        [SerializeField] float _seatedOffset = .5f;
        [SerializeField] Behaviour _teleportProvider;
        [SerializeField] CanvasGroup _vignette;
        [SerializeField] float _fadeSpeed = 5f;
        Vector3 _standingPosition;
        float _target;
        void Awake() { if (_cameraOffset != null) _standingPosition = _cameraOffset.localPosition; ConfigureInteractionMasks(); }
        public void ConfigureInteractionMasks()
        {
            int targets = LayerMask.GetMask("Items", "Tray", "Stations", "WorldUI", "UI");
            foreach (var caster in GetComponentsInChildren<SphereInteractionCaster>(true)) caster.physicsLayerMask |= targets;
            foreach (var caster in GetComponentsInChildren<CurveInteractionCaster>(true)) caster.raycastMask |= targets | LayerMask.GetMask("Environment");
            foreach (var ray in GetComponentsInChildren<XRRayInteractor>(true)) ray.raycastMask |= targets | LayerMask.GetMask("Environment");
        }
        void OnEnable() { GameEvents.OnPlayerMoveSpeed += Movement; }
        void OnDisable() { GameEvents.OnPlayerMoveSpeed -= Movement; }
        void Movement(float speed) { _target = speed > .05f ? 1 : 0; }
        void Update() { if (_vignette != null) _vignette.alpha = Mathf.MoveTowards(_vignette.alpha, _target, _fadeSpeed * Time.deltaTime); }
        public void SetSeated(bool seated) { if (_cameraOffset != null) _cameraOffset.localPosition = _standingPosition + Vector3.up * (seated ? (_config != null ? _config.SeatedOffset : _seatedOffset) : 0); }
        public void SetTeleport(bool enabled) { if (_teleportProvider != null) _teleportProvider.enabled = enabled; }
    }
}
