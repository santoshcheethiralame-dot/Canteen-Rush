namespace CanteenRush.Core
{
    public sealed class Order
    {
        public int Id;                    // unique per round, increments from 1
        public int SeatIndex;             // 0..7
        public UnityEngine.Vector3 SeatPosition; // world position of the seat (audio/UI anchor)
        public ItemId[] Items;            // 1..3 entries, duplicates allowed
        public float PatienceMax;         // seconds
        public float PatienceRemaining;   // seconds, ticks down while Waiting
        public float Patience01 => PatienceMax <= 0 ? 0 : PatienceRemaining / PatienceMax;
    }


}
