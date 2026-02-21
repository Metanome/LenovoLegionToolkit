using System;
using System.Linq;
using LenovoLegionToolkit.Lib.Utils;
using WindowsDisplayAPI;

namespace LenovoLegionToolkit.Lib.System;

public static class Displays
{
    public static Display[] Get() => Display.GetDisplays().ToArray();

    public static bool HasMultipleGpus()
    {
        try
        {
            return DisplayAdapter.GetDisplayAdapters().Count(da => da.DevicePath.Contains("PCI", StringComparison.OrdinalIgnoreCase)) > 1;
        }
        catch (Exception ex)
        {
            Log.Instance.Trace($"Error checking for multiple GPUs via WindowsDisplayAPI", ex);
            return false;
        }
    }
}
