using UnityEngine;
namespace CanteenRush.Core
{
    [CreateAssetMenu] public sealed class OrderTemplate : ScriptableObject
    {
        public ItemId[] Items;
        public int MinWave;
    }
}
