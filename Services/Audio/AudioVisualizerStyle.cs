namespace HoldItTogether.Services.Audio;

public enum AudioVisualizerStyle
{
    Bars = 0,
    Line = 1,
    Smooth = 2,
    // Value 3 was Filled; settings migration maps it to Smooth. Do not reuse it.
    Mirrored = 4,
    Radial = 5,
    Ring = 6,
    Wave = 7,
    WaveSoft = 8,
    WaveRibbons = 9,
    WavePulse = 10
}
