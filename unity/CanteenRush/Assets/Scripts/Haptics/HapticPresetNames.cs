namespace CanteenRush.Haptics
{
    /// Preset names from MASTER_SPEC §11, so the binder, the asset builder and the tests cannot drift apart by a typo.
    public static class HapticPresetNames
    {
        public const string Grab = "Grab", Release = "Release", SocketSnap = "SocketSnap", DoorLatch = "DoorLatch",
            DoorStop = "DoorStop", PumpPress = "PumpPress", WaterFill = "WaterFill", TrayTiltWarn = "TrayTiltWarn",
            ServeOk = "ServeOk", ServePartial = "ServePartial", Spill = "Spill", OrderExpired = "OrderExpired", UiTick = "UiTick";

        public static readonly string[] All =
            { Grab, Release, SocketSnap, DoorLatch, DoorStop, PumpPress, WaterFill, TrayTiltWarn, ServeOk, ServePartial, Spill, OrderExpired, UiTick };
    }
}
