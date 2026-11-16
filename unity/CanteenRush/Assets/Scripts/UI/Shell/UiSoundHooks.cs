using CanteenRush.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace CanteenRush.UI
{
    /// On every shell button: hover raises GameEvents.UiHover, click raises GameEvents.UiClick.
    /// These are the only events P3 raises (contracts §4.2); Audio plays ui_click and Haptics plays UiTick on them.
    [RequireComponent(typeof(Selectable))]
    public sealed class UiSoundHooks : MonoBehaviour, IPointerEnterHandler
    {
        Button _button;
        void Awake() { _button = GetComponent<Button>(); if (_button != null) _button.onClick.AddListener(GameEvents.UiClick); }
        void OnDestroy() { if (_button != null) _button.onClick.RemoveListener(GameEvents.UiClick); }
        public void OnPointerEnter(PointerEventData eventData) { if (GetComponent<Selectable>().interactable) GameEvents.UiHover(); }
    }
}
