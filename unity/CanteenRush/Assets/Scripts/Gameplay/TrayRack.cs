using UnityEngine;
namespace CanteenRush.Gameplay
{
    public sealed class TrayRack : MonoBehaviour
    {
        [SerializeField] Tray _trayPrefab;
        [SerializeField] Transform[] _slots;
        void Awake()
        {
            foreach (var slot in _slots)
            {
                var tray = Instantiate(_trayPrefab, slot.position, slot.rotation, transform);
                tray.SetHome(slot.position, slot.rotation);
            }
        }
    }
}
