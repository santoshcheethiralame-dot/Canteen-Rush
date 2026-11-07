using UnityEngine;
namespace CanteenRush.Core
{
    public enum AudioMaterial { Plastic, Glass, Paper, Metal }
    [CreateAssetMenu] public sealed class ItemDef : ScriptableObject
    {
        public ItemId Id;
        public string DisplayName;
        public GameObject Prefab;
        public Sprite Icon;
        public AudioMaterial AudioMaterial;
        public float Mass;
        public float RespawnSeconds;
    }
}
