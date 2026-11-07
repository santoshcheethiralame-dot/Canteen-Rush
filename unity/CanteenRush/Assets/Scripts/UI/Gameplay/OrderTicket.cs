using CanteenRush.Core;
using UnityEngine;
using UnityEngine.UI;
namespace CanteenRush.UI.Gameplay
{
    public sealed class OrderTicket : MonoBehaviour
    {
        [SerializeField] Text _seat;
        [SerializeField] Image _patience;
        [SerializeField] Image[] _icons = System.Array.Empty<Image>();
        [SerializeField] Text[] _itemLabels = System.Array.Empty<Text>();
        [SerializeField] ItemDef[] _items = System.Array.Empty<ItemDef>();
        float _value;
        public int OrderId { get; private set; }
        public void Show(Order order)
        {
            OrderId = order.Id; gameObject.SetActive(true);
            if (_seat != null) _seat.text = "Seat " + (order.SeatIndex + 1);
            for (int i = 0; i < _icons.Length; i++)
            {
                _icons[i].gameObject.SetActive(i < order.Items.Length);
                if (i >= order.Items.Length) continue;
                _icons[i].sprite = null;
                foreach (var item in _items) if (item != null && item.Id == order.Items[i]) { _icons[i].sprite = item.Icon; break; }
            }
            for (int i = 0; i < _itemLabels.Length; i++)
                if (_itemLabels[i] != null) _itemLabels[i].text = i < order.Items.Length ? order.Items[i].ToString() : "";
            SetPatience(order.Patience01);
        }
        public void SetPatience(float value)
        {
            _value = Mathf.Clamp01(value);
            if (_patience != null) { _patience.fillAmount = _value; _patience.color = Color.Lerp(Color.red, Color.green, _value); }
        }
        void Update()
        {
            if (_value <= .25f && _patience != null)
                _patience.color = Color.Lerp(new Color(1, .1f, .05f), Color.yellow, .5f + .5f * Mathf.Sin(Time.unscaledTime * 8));
        }
        public void ShowAngry()
        {
            gameObject.SetActive(true);
            if (_seat != null) _seat.text = "Too late!";
            foreach (var icon in _icons) icon.gameObject.SetActive(false);
            foreach (var label in _itemLabels) if (label != null) label.text = "";
            SetPatience(1); if (_patience != null) _patience.color = Color.red;
        }
        public void Hide() { OrderId = 0; gameObject.SetActive(false); }
    }
}
