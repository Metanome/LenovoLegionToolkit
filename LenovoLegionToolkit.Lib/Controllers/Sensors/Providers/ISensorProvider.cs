using System.Collections.Generic;
using LibreHardwareMonitor.Hardware;

namespace LenovoLegionToolkit.Lib.Controllers.Sensors.Providers;

public interface ISensorProvider
{
    HardwareUpdateScope Scope { get; }
    IReadOnlySet<SensorItem> ProvidedSensorItems { get; }
    IReadOnlySet<OsdItem> ProvidedOsdItems { get; }
    bool IsAvailable { get; }

    void Discover(IReadOnlyList<IHardware> hardware);
    void Reset();
}
