using UnityEngine;
namespace CanteenRush.Core
{
[CreateAssetMenu] public sealed class GameConfig : ScriptableObject
{
    public float RoundSeconds = 180f;
    public float[] WaveSeconds = { 60f, 60f, 60f };
    public int[] WaveMaxCustomers = { 2, 3, 4 };
    public float[] WaveSpawnInterval = { 14f, 11f, 9f };
    public float PatienceBase = 25f, PatiencePerItem = 15f;
    public int PointsPerItem = 100, SpeedBonusMax = 50;
    public float ComboPerExtraItem = 0.25f;
    public int WrongItemPenalty = 25, SpillPenalty = 10, TimeoutPenalty = 50;
    public int[] StarThresholds = { 300, 700, 1200 };
    public float DonutRespawn = 5f, ShakeRespawn = 8f, CakeRespawn = 10f, ColaRespawn = 6f;
    public float CupFillPerSecond = 0.25f;
    public float TrayTiltWarnDeg = 20f, TrayTiltSpillDeg = 35f;
    public float SpillHeight = 0.30f;
    public float EatSeconds = 4f, TrayReturnSeconds = 3f;
    public float MoveSpeed = 1.8f; public float SnapTurnDeg = 45f;
    public int MaxActiveItems = 34;
    public int ItemsPerSpawner = 4;
    public float ItemDespawnSeconds = 2f, SlotRadius = .15f;
    public float ImpactInterval = .1f, SocketCooldown = .5f;
    public float FridgeLatchDeg = 8f, FridgeStopDeg = 100f, FridgeMaxDeg = 110f;
    public float CustomerSpeed = 1.8f, CustomerAngrySeconds = 1f;
    public float CustomerLookDistance = 6f, CustomerBobHeight = .025f, CustomerBobSpeed = 8f;
    public float SeatedOffset = .5f, FootstepDistance = .5f;
    [SerializeField] internal OrderTemplate[] Templates = System.Array.Empty<OrderTemplate>();
}
}
