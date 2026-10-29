using HoldItTogether.Services.Settings;

namespace HoldItTogether.Services.HitMenus;

public sealed class HitMenuNavigator(int count)
{
    private const double WheelThreshold = 30;
    private const long GestureGap = 180;
    private double _accumulated;
    private long _lastWheel = long.MinValue;
    private long _lastStep = long.MinValue;
    private int _lastWheelDirection;
    private int _lastStepDirection;

    public int Count { get; } = Math.Max(0, count);
    public int SelectedIndex { get; private set; }

    public bool Select(int index, bool wrap = false)
    {
        if (wrap && Count > 0) index = (int)(((long)index % Count + Count) % Count);
        if (index < 0 || index >= Count || index == SelectedIndex) return false;
        SelectedIndex = index;
        _accumulated = 0;
        return true;
    }

    public bool Scroll(
        double deltaX, double deltaY, long deltaMode, bool controlKey, long now,
        HitSettings? settings = null)
    {
        if (Count < 2 || controlKey || !double.IsFinite(deltaX) ||
            !double.IsFinite(deltaY) || deltaY == 0 || Math.Abs(deltaX) > Math.Abs(deltaY))
        {
            return false;
        }

        settings = (settings ?? new HitSettings()).Normalize();
        var direction = Math.Sign(deltaY);
        if (_lastWheel == long.MinValue || now - _lastWheel > GestureGap ||
            direction != _lastWheelDirection)
        {
            _accumulated = 0;
        }

        _lastWheel = now;
        _lastWheelDirection = direction;
        if (settings.ScrollCooldownEnabled && _lastStep != long.MinValue &&
            direction == _lastStepDirection && now - _lastStep < settings.ScrollCooldownMs)
        {
            return false;
        }

        var units = deltaMode == 1 ? 16 : deltaMode == 2 ? 400 : 1;
        var pixels = Math.Clamp(deltaY * units * settings.ScrollSensitivity, -100000, 100000);
        _accumulated += pixels;
        if (Math.Abs(_accumulated) < WheelThreshold) return false;

        var steps = settings.ScrollCooldownEnabled
            ? 1
            : (int)Math.Min(Count - 1d, Math.Floor(Math.Abs(_accumulated) / WheelThreshold));
        var remainder = settings.ScrollCooldownEnabled
            ? 0
            : _accumulated - direction * steps * WheelThreshold;

        var next = (long)SelectedIndex + (long)direction * steps;
        var target = settings.InfiniteMenu ? (int)((next % Count + Count) % Count) : (int)Math.Clamp(next,0,Count-1L);
        if (!Select(target))
        {
            _accumulated = 0;
            return false;
        }

        _accumulated = !settings.InfiniteMenu && (target is 0 || target == Count - 1) ? 0 : remainder;
        _lastStep = now;
        _lastStepDirection = direction;
        return true;
    }
}
