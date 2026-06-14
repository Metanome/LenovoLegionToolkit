using System;
using System.Linq;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.System.Management;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.Lib.Features.Hybrid;

public class BiosHybridMode : IFeature<HybridModeState>
{
    private const string GRAPHICS_DEVICE = "GraphicsDevice";
    private const string UMA_GRAPHICS = "UMA Graphics";
    private const string SWITCHABLE_GRAPHICS = "Switchable Graphics";
    private const string DISCRETE_GRAPHICS = "Discrete Graphics";

    public async Task<bool> IsSupportedAsync()
    {
        if (AppFlags.Instance.Debug)
        {
            return true;
        }

        var mi = await Compatibility.GetMachineInformationAsync().ConfigureAwait(false);
        return mi.Properties.SupportsGSync && await WMI.LenovoBiosSetting.ExistAsync().ConfigureAwait(false);
    }

    public async Task<HybridModeState[]> GetAllStatesAsync()
    {
        var mi = await Compatibility.GetMachineInformationAsync().ConfigureAwait(false);

        if (mi.Properties.SupportsGSync && await IsUMASupportedAsync().ConfigureAwait(false))
        {
            return [HybridModeState.On, HybridModeState.Off, HybridModeState.UMA];
        }

        return (mi.Properties.SupportsGSync, mi.Properties.SupportsIGPUMode) switch
        {
            (true, true) => [HybridModeState.On, HybridModeState.OnIGPUOnly, HybridModeState.OnAuto, HybridModeState.Off],
            (false, true) => [HybridModeState.On, HybridModeState.OnIGPUOnly, HybridModeState.OnAuto],
            (true, false) => [HybridModeState.On, HybridModeState.Off],
            _ => []
        };
    }

    private async Task<bool> IsUMASupportedAsync()
    {
        try
        {
            var selections = await WMI.LenovoBiosSetting.GetBiosSelectionsAsync(GRAPHICS_DEVICE).ConfigureAwait(false);
            return selections.Any(item => item.Contains("UMA", StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            Log.Instance.Trace($"Failed to check UMA support", ex);
            return false;
        }
    }

    public async Task<HybridModeState> GetStateAsync()
    {
        try
        {
            var setting = await WMI.LenovoBiosSetting.GetBiosSettingAsync(GRAPHICS_DEVICE).ConfigureAwait(false);
            return GetStateFromBiosValue(setting);
        }
        catch (Exception ex)
        {
            Log.Instance.Trace($"Failed to get Hybrid Mode", ex);
            return HybridModeState.Off;
        }
    }

    public async Task SetStateAsync(HybridModeState state)
    {
        await WMI.LenovoBiosSetting.SetBiosSettingAsync(GRAPHICS_DEVICE, GetBiosValueForState(state)).ConfigureAwait(false);
        await WMI.LenovoBiosSetting.SaveBiosSettingAsync().ConfigureAwait(false);
    }

    private string GetBiosValueForState(HybridModeState state)
    {
        return state switch
        {
            HybridModeState.On => SWITCHABLE_GRAPHICS,
            HybridModeState.OnIGPUOnly => SWITCHABLE_GRAPHICS,
            HybridModeState.OnAuto => SWITCHABLE_GRAPHICS,
            HybridModeState.UMA => UMA_GRAPHICS,
            HybridModeState.Off => DISCRETE_GRAPHICS,
            _ => throw new ArgumentOutOfRangeException(nameof(state), $"Unsupported state: {state}")
        };
    }

    private HybridModeState GetStateFromBiosValue(string biosValue)
    {
        return biosValue switch
        {
            SWITCHABLE_GRAPHICS => HybridModeState.On,
            UMA_GRAPHICS => HybridModeState.UMA,
            DISCRETE_GRAPHICS => HybridModeState.Off,
            _ => throw new ArgumentOutOfRangeException(nameof(biosValue), $"Unsupported BIOS value: {biosValue}")
        };
    }
}