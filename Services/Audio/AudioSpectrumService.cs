using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace HoldItTogether.Services.Audio;

/// <summary>Optional default-output loopback capture. It never opens the microphone or writes recordings.</summary>
public sealed class AudioSpectrumService : IDisposable
{
    private readonly object _lifecycle = new();
    private readonly AudioSpectrumAnalyzer _analyzer = new();
    private WasapiLoopbackCapture? _capture;
    private MMDevice? _device;
    private string? _deviceId;
    private string? _error;

    public string? Error => Volatile.Read(ref _error);
    public double[] Read() => _analyzer.Read();

    public void Start()
    {
        lock (_lifecycle)
        {
            if (_capture is not null) return;
            _analyzer.Reset();
            Volatile.Write(ref _error, null);
            using var enumerator = new MMDeviceEnumerator();
            var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            var capture = new WasapiLoopbackCapture(device);
            try
            {
                capture.DataAvailable += (_, args) => _analyzer.AddPcm(args.Buffer.AsSpan(0, args.BytesRecorded), capture.WaveFormat);
                capture.RecordingStopped += (_, args) =>
                {
                    if (args.Exception is not null)
                        Volatile.Write(ref _error, "A saída de áudio ficou indisponível. Reabra o visualizador para tentar novamente.");
                };
                capture.StartRecording();
                _capture = capture;
                _device = device;
                _deviceId = device.ID;
            }
            catch { capture.Dispose(); device.Dispose(); throw; }
        }
    }

    public void RefreshOutput()
    {
        using var enumerator = new MMDeviceEnumerator();
        using var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        lock (_lifecycle)
        {
            if (_capture is not null && (_deviceId != device.ID || Error is not null))
            {
                Stop();
                Start();
            }
        }
    }

    public void Stop()
    {
        lock (_lifecycle)
        {
            var capture = _capture;
            _capture = null;
            var device = _device;
            _device = null;
            _deviceId = null;
            if (capture is not null)
            {
                try { capture.StopRecording(); }
                finally { try { capture.Dispose(); } finally { device?.Dispose(); } }
            }
            _analyzer.Reset();
        }
    }

    public void Dispose() => Stop();
}