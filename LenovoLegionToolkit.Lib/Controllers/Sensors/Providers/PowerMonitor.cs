using System;

namespace LenovoLegionToolkit.Lib.Controllers.Sensors.Providers;

public class PowerMonitor
{
    private const float MAX_VALID = 400f;
    private const float MIN_VALID = 0f;
    private const int MAX_STUCK_RETRIES = 10;

    private float _cached;
    private int _stuckCount;

    public event Action? ResetNeeded;

    public float Read(float raw)
    {
        if (raw > MAX_VALID) 
        { 
            ResetNeeded?.Invoke(); 
            return -1; 
        }

        if (raw <= MIN_VALID)
        {
            return -1;
        }

        if (Math.Abs(raw - _cached) < float.Epsilon)
        {
            if (++_stuckCount >= MAX_STUCK_RETRIES)
            {
                ResetNeeded?.Invoke();
                return -1;
            }
            return raw;
        }

        _cached = raw;
        _stuckCount = 0;
        return raw;
    }

    public void Reset()
    {
        _cached = 0;
        _stuckCount = 0;
    }
}
