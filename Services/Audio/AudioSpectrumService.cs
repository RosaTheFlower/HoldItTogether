using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace HoldItTogether.Services.Audio;

/// <summary>Optional default-output loopback capture. It never opens the microphone or writes recordings.</summary>
public sealed class AudioSpectrumService : IDisposable
{
    private readonly HoldItTogether.Services.Diagnostics.HitDiagnosticLog? _diagnostics;
    public AudioSpectrumService(HoldItTogether.Services.Diagnostics.HitDiagnosticLog? diagnostics=null)=>_diagnostics=diagnostics;
    public int ActiveCaptures {get{lock(_lifecycle)return _capture is null?0:1;}}
    private readonly object _lifecycle = new();
    private readonly AudioSpectrumAnalyzer _analyzer = new();
    private WasapiLoopbackCapture? _capture;
    private MMDevice? _device;
    private string? _deviceId;
    private string? _error;
    private int _consumers;private bool _disposed;private IDisposable? _legacy;
    public IDisposable Acquire()
    {
        lock(_lifecycle){ObjectDisposedException.ThrowIf(_disposed,this);if(_capture is null)StartCore();_consumers++;return new CaptureLease(this);}
    }
    private sealed class CaptureLease(AudioSpectrumService owner):IDisposable
    {private AudioSpectrumService? _owner=owner;public void Dispose(){var current=Interlocked.Exchange(ref _owner,null);if(current is null)return;lock(current._lifecycle){if(current._consumers>0&&--current._consumers==0)current.StopCore();}}}

    public string? Error => Volatile.Read(ref _error);
    public double[] Read() => _analyzer.Read();
    public void CopyTo(Span<double> destination) => _analyzer.CopyTo(destination);

    public void Start(){lock(_lifecycle)_legacy??=Acquire();}
    private void StartCore()
    {
        lock (_lifecycle)
        {
            if (_capture is not null) return;
            _analyzer.Reset();
            Volatile.Write(ref _error, null);
            using var enumerator = new MMDeviceEnumerator();
            var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            WasapiLoopbackCapture? capture = null;
            try
            {
                capture = new WasapiLoopbackCapture(device);
                capture.DataAvailable += (_, args) => _analyzer.AddPcm(args.Buffer.AsSpan(0, args.BytesRecorded), capture.WaveFormat);
                capture.RecordingStopped += (_, args) =>
                {
                    if (args.Exception is not null)
                    {
                        _diagnostics?.Record("audio","capture.failed");
                        Volatile.Write(ref _error, "Audio output became unavailable. Reopen the visualizer to try again.");
                    }
                };
                capture.StartRecording();
                _capture = capture;
                _device = device;
                _deviceId = device.ID;
                _diagnostics?.Record("audio","capture.started");
            }
            catch { _diagnostics?.Record("audio","capture.start.failed"); capture?.Dispose(); device.Dispose(); throw; }
        }
    }

    public void RefreshOutput()
    {
        using var enumerator = new MMDeviceEnumerator();
        using var device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        lock (_lifecycle)
        {
            if(_consumers>0&&_capture is null){StartCore();return;}
            if (_capture is not null && (_deviceId != device.ID || Error is not null))
            {
                StopCore();
                StartCore();
            }
        }
    }

    public void Stop(){lock(_lifecycle){_legacy?.Dispose();_legacy=null;}}
    private void StopCore()
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
                _diagnostics?.Record("audio","capture.stopped");
                try { capture.StopRecording(); }
                finally { try { capture.Dispose(); } finally { device?.Dispose(); } }
            }
            _analyzer.Reset();
        }
    }

    public void Dispose(){lock(_lifecycle){if(_disposed)return;_disposed=true;_legacy=null;_consumers=0;StopCore();}}
}
