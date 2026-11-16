namespace CanteenRush.Audio
{
    public enum AudioZone { Indoor, Outdoor }

    /// Mixer buses (MASTER_SPEC §10: Master -> SFX, Ambience, Music, UI, Voice).
    public enum AudioBus { SFX, Ambience, Music, UI, Voice }

    /// Contracts §6. AudioEventBinder and AudioZoneTrigger are its only callers; gameplay never calls it.
    public interface IAudioService
    {
        void PlayAt(string cueId, UnityEngine.Vector3 worldPos, float volumeScale = 1f);
        void PlayUi(string cueId);
        void SetSnapshot(AudioZone zone);   // Indoor / Outdoor, cross-fade 0.4 s
    }
}
