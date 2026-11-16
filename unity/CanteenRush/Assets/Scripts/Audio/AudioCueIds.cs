namespace CanteenRush.Audio
{
    /// Every cue ID of the MASTER_SPEC §10 table (the table is exhaustive; tests check each one resolves to a clip).
    /// item_drop_glass is the "glass break variant for Shake" of item_drop, kept as its own set so the thud
    /// variants and the glass variants never mix.
    public static class AudioCueIds
    {
        public const string FridgeHum = "amb_fridge_hum", CanteenRoom = "amb_canteen_room", PatioBirds = "amb_patio_birds",
            PatioCrowd = "amb_patio_crowd", FridgeLatch = "fridge_latch", PumpPress = "pump_press", WaterFill = "water_fill_loop",
            Grab = "grab_soft", Release = "release_soft", TrayClink = "tray_clink", ItemDrop = "item_drop", ItemDropGlass = "item_drop_glass",
            OrderBell = "order_bell", CustomerHappy = "customer_happy", CustomerAngry = "customer_angry",
            FootstepsTile = "footsteps_tile", FootstepsStone = "footsteps_stone", UiClick = "ui_click", UiConfirm = "ui_confirm",
            UiError = "ui_error", Music = "music_loop", RoundEnd = "round_end_sting";

        public static readonly string[] All =
        {
            FridgeHum, CanteenRoom, PatioBirds, PatioCrowd, FridgeLatch, PumpPress, WaterFill, Grab, Release, TrayClink,
            ItemDrop, ItemDropGlass, OrderBell, CustomerHappy, CustomerAngry, FootstepsTile, FootstepsStone,
            UiClick, UiConfirm, UiError, Music, RoundEnd
        };
    }
}
