using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace CanteenRush.Gameplay.Desktop
{
    /// PC-only finishing pass for desktop mode: ACES tonemapping, soft bloom on the warm lights, a little contrast
    /// and saturation, a light vignette and SMAA. The VR / Quest-class path keeps the post-free budget in the spec.
    public sealed class DesktopLook : MonoBehaviour
    {
        Volume _volume;
        Camera _camera;
        bool _wasPost; AntialiasingMode _wasAa;

        public void Apply(Camera camera, bool on)
        {
            _camera = camera;
            var data = camera != null ? camera.GetUniversalAdditionalCameraData() : null;
            if (on)
            {
                if (_volume == null) Build();
                _volume.enabled = true;
                if (data != null) { _wasPost = data.renderPostProcessing; _wasAa = data.antialiasing; data.renderPostProcessing = true; data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing; }
            }
            else
            {
                if (_volume != null) _volume.enabled = false;
                if (data != null) { data.renderPostProcessing = _wasPost; data.antialiasing = _wasAa; }
            }
        }

        void Build()
        {
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var tone = profile.Add<Tonemapping>(true); tone.mode.Override(TonemappingMode.ACES);
            var bloom = profile.Add<Bloom>(true); bloom.threshold.Override(1.05f); bloom.intensity.Override(.45f); bloom.scatter.Override(.6f);
            var color = profile.Add<ColorAdjustments>(true);
            color.postExposure.Override(.35f); color.contrast.Override(14f); color.saturation.Override(22f);
            var white = profile.Add<WhiteBalance>(true); white.temperature.Override(6f);
            var vignette = profile.Add<Vignette>(true); vignette.intensity.Override(.22f); vignette.smoothness.Override(.45f);
            var go = new GameObject("DesktopLookVolume"); go.transform.SetParent(transform, false);
            _volume = go.AddComponent<Volume>(); _volume.isGlobal = true; _volume.priority = 10; _volume.sharedProfile = profile;
        }
    }
}
