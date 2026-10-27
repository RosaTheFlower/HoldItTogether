using HoldItTogether.Services.Settings;

namespace HoldItTogether.Services.Audio;

public sealed record AudioEffectOptions(bool Radial, int Lens, int Chromatic, int Vignette, int Grain,
    int Brightness, int Contrast, int Temperature, int Tint, int Hue, int Sepia, int Grayscale, bool OnBeat, int Strength, bool ColorEnabled)
{
    public static bool NeedsBeatSignal(HitSettings value) => value.AudioArtworkOnBeat && value.AudioArtworkBeatStrength > 0 &&
        (value.AudioArtworkRadialBlur && value.AudioArtworkBlur > 0 || value.AudioArtworkZoom > 100 ||
         value.AudioArtworkDistortion > 0 || value.AudioArtworkLens > 0 || value.AudioArtworkChromatic > 0 ||
         value.AudioArtworkVignette > 0 || value.AudioArtworkGrain > 0 || value.AudioArtworkDrift ||
         value.AudioArtworkColorEnabled && (value.AudioArtworkSaturation != 100 || value.AudioArtworkBrightness != 100 ||
            value.AudioArtworkContrast != 100 || value.AudioArtworkTemperature != 0 || value.AudioArtworkTint != 0 ||
            value.AudioArtworkHue != 0 || value.AudioArtworkSepia > 0 || value.AudioArtworkGrayscale > 0));
    public static AudioEffectOptions From(HitSettings value) => new(value.AudioArtworkRadialBlur,
        value.AudioArtworkLens, value.AudioArtworkChromatic, value.AudioArtworkVignette, value.AudioArtworkGrain,
        value.AudioArtworkBrightness, value.AudioArtworkContrast, value.AudioArtworkTemperature,
        value.AudioArtworkTint, value.AudioArtworkHue, value.AudioArtworkSepia, value.AudioArtworkGrayscale,
        value.AudioArtworkOnBeat, value.AudioArtworkBeatStrength,value.AudioArtworkColorEnabled);
}
