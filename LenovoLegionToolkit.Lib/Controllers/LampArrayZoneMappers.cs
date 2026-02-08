using System;

namespace LenovoLegionToolkit.Lib.Controllers;

public class FourZoneMapper : ILampArrayZoneMapper
{
    public int ZoneCount => 4;

    public int[] GetLampIndicesForZone(int zone, int totalLampCount)
    {
        if (totalLampCount <= 0)
            return [];

        var lampsPerZone = totalLampCount / 4;
        var remainder = totalLampCount % 4;

        var startIndex = zone * lampsPerZone + Math.Min(zone, remainder);
        var count = lampsPerZone + (zone < remainder ? 1 : 0);

        var indices = new int[count];
        for (int i = 0; i < count; i++)
        {
            indices[i] = startIndex + i;
        }
        return indices;
    }
}

public class TwentyFourZoneMapper : ILampArrayZoneMapper
{
    public int ZoneCount => 24;

    public int[] GetLampIndicesForZone(int zone, int totalLampCount)
    {
        if (totalLampCount <= 0 || zone >= totalLampCount)
            return [];

        if (totalLampCount <= 24)
        {
            return zone < totalLampCount ? [zone] : [];
        }

        var lampsPerZone = totalLampCount / 24;
        var remainder = totalLampCount % 24;

        var startIndex = zone * lampsPerZone + Math.Min(zone, remainder);
        var count = lampsPerZone + (zone < remainder ? 1 : 0);

        var indices = new int[count];
        for (int i = 0; i < count; i++)
        {
            indices[i] = startIndex + i;
        }
        return indices;
    }
}

public class PerKeyMapper : ILampArrayZoneMapper
{
    public int ZoneCount => -1;

    public int[] GetLampIndicesForZone(int zone, int totalLampCount)
    {
        if (zone >= 0 && zone < totalLampCount)
            return [zone];
        return [];
    }
}
