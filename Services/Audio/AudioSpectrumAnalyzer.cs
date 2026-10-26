using System.Buffers.Binary;
using System.Diagnostics;
using NAudio.Dsp;
using NAudio.Wave;

namespace HoldItTogether.Services.Audio;

/// <summary>Hann-windowed, logarithmic FFT bands from PCM. No raw audio is retained beyond one FFT window.</summary>
public sealed class AudioSpectrumAnalyzer
{
    public const int BandCount = 32;
    private const int FftSize = 2048;
    private readonly object _sync = new();
    private readonly float[] _ring = new float[FftSize];
    private readonly Complex[] _fft = new Complex[FftSize];
    private readonly double[] _levels = new double[BandCount];
    private int _write, _filled;
    private long _lastFrame, _lastData;

    public void Reset()
    {
        lock (_sync)
        {
            Array.Clear(_ring); Array.Clear(_levels);
            _write = _filled = 0;
            _lastFrame = _lastData = 0;
        }
    }

    public void AddPcm(ReadOnlySpan<byte> bytes, WaveFormat format)
    {
        var floating = format.Encoding == WaveFormatEncoding.IeeeFloat ||
            format is WaveFormatExtensible extended && extended.SubFormat == new Guid("00000003-0000-0010-8000-00aa00389b71");
        var sampleBytes = format.BitsPerSample / 8;
        if (format.Channels < 1 || sampleBytes is not (2 or 3 or 4) || (floating && sampleBytes != 4)) return;
        var block = format.BlockAlign;
        if (block < sampleBytes * format.Channels) return;
        lock (_sync)
        {
            for (var offset = 0; offset + block <= bytes.Length; offset += block)
            {
                var sum = 0f;
                for (var channel = 0; channel < format.Channels; channel++)
                {
                    var sample = bytes.Slice(offset + channel * sampleBytes, sampleBytes);
                    var value = floating ? BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(sample)) :
                        sampleBytes switch
                        {
                            2 => BinaryPrimitives.ReadInt16LittleEndian(sample) / 32768f,
                            3 => ((sample[0] | sample[1] << 8 | sample[2] << 16) << 8 >> 8) / 8388608f,
                            _ => BinaryPrimitives.ReadInt32LittleEndian(sample) / 2147483648f
                        };
                    sum += float.IsFinite(value) ? Math.Clamp(value, -1, 1) : 0;
                }
                _ring[_write] = sum / format.Channels;
                _write = (_write + 1) % FftSize;
                _filled = Math.Min(FftSize, _filled + 1);
            }
            _lastData = Stopwatch.GetTimestamp();
            if (_filled < FftSize || (_lastFrame != 0 && Stopwatch.GetElapsedTime(_lastFrame).TotalMilliseconds < 33)) return;
            _lastFrame = _lastData;
            for (var index = 0; index < FftSize; index++)
            {
                _fft[index].X = _ring[(_write + index) % FftSize] * (float)(0.5 * (1 - Math.Cos(2 * Math.PI * index / (FftSize - 1))));
                _fft[index].Y = 0;
            }
            FastFourierTransform.FFT(true, 11, _fft);
            var maximumFrequency = Math.Min(16000, format.SampleRate * 0.48);
            for (var band = 0; band < BandCount; band++)
            {
                var from = Math.Max(1, (int)(40 * Math.Pow(maximumFrequency / 40, band / (double)BandCount) * FftSize / format.SampleRate));
                var to = Math.Clamp((int)(40 * Math.Pow(maximumFrequency / 40, (band + 1d) / BandCount) * FftSize / format.SampleRate), from + 1, FftSize / 2);
                var peak = 0d;
                for (var bin = from; bin < to; bin++)
                    peak = Math.Max(peak, Math.Sqrt(_fft[bin].X * _fft[bin].X + _fft[bin].Y * _fft[bin].Y));
                var decibels = 20 * Math.Log10(Math.Max(peak * 2, 1e-8));
                var target = Math.Clamp((decibels + 75) / 65, 0, 1);
                _levels[band] += (target - _levels[band]) * (target > _levels[band] ? 0.8 : 0.24);
            }
        }
    }

    public double[] Read()
    {
        var result = new double[BandCount];
        CopyTo(result);
        return result;
    }

    public void CopyTo(Span<double> destination)
    {
        if (destination.Length < BandCount) throw new ArgumentException("The buffer must hold all spectrum bands.", nameof(destination));
        lock (_sync)
        {
            // WASAPI does not necessarily deliver buffers when the endpoint is silent.
            if (_lastData == 0 || Stopwatch.GetElapsedTime(_lastData).TotalMilliseconds > 250)
                Array.Clear(_levels);
            _levels.CopyTo(destination);
        }
    }
}
