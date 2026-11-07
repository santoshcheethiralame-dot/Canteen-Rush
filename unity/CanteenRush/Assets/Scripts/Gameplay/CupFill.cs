using CanteenRush.Core;
using UnityEngine;
namespace CanteenRush.Gameplay
{
    [RequireComponent(typeof(ItemBehaviour))]
    public sealed class CupFill : MonoBehaviour
    {
        [SerializeField] GameConfig _config;
        [SerializeField] GameObject _waterVisual;
        ItemBehaviour _item;
        float _level;
        void Awake() { _item = GetComponent<ItemBehaviour>(); }
        void OnEnable() { _level = 0; _item.SetItemId(ItemId.None); if (_waterVisual != null) _waterVisual.SetActive(false); }
        public void Fill(float dt)
        {
            if (_level >= 1 || _config == null) return;
            _level = Mathf.Clamp01(_level + _config.CupFillPerSecond * dt);
            GameEvents.CupFilling(_level, _item.HoldingHand);
            if (_level >= 1) { _item.SetItemId(ItemId.Water); if (_waterVisual != null) _waterVisual.SetActive(true); GameEvents.CupFilled(transform.position); }
        }
    }
}
