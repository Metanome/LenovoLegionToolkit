using System;
using System.Threading;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.Controllers.GodMode;
using LenovoLegionToolkit.Lib.System.Management;
using Newtonsoft.Json;

namespace LenovoLegionToolkit.Lib.Automation.Steps;

[method: JsonConstructor]
public class FanMaxSpeedAutomationStep(FanMaxSpeedState state)
    : IAutomationStep<FanMaxSpeedState>
{
    public FanMaxSpeedState State { get; } = state;

    private readonly GodModeController _godModeController = IoCContainer.Resolve<GodModeController>();

    public Task<bool> IsSupportedAsync() => Task.FromResult(true);

    public Task<FanMaxSpeedState[]> GetAllStatesAsync() => Task.FromResult(Enum.GetValues<FanMaxSpeedState>());

    public IAutomationStep DeepCopy() => new FanMaxSpeedAutomationStep(State);

    public async Task RunAsync(AutomationContext context, AutomationEnvironment environment, CancellationToken token)
    {
        var controller = await _godModeController.GetControllerAsync().ConfigureAwait(false);

        var typeName = controller.GetType().Name;

        switch (typeName)
        {
            case "GodModeControllerV1":
                await HandleLegacyAsync().ConfigureAwait(false);
                break;

            case "GodModeControllerV2":
            case "GodModeControllerV3":
            case "GodModeControllerV4":
                await HandleModernAsync().ConfigureAwait(false);
                break;
        }
    }

    private async Task HandleLegacyAsync()
    {
        bool currentSpeed = await WMI.LenovoFanMethod.FanGetFullSpeedAsync().ConfigureAwait(false);
        bool targetState = State switch
        {
            FanMaxSpeedState.On => true,
            FanMaxSpeedState.Off => false,
            FanMaxSpeedState.Toggle => !currentSpeed,
            _ => currentSpeed
        };

        if (currentSpeed != targetState)
        {
            await WMI.LenovoFanMethod.FanSetFullSpeedAsync(targetState ? 1 : 0).ConfigureAwait(false);
        }
    }

    private async Task HandleModernAsync()
    {
        var currentValue = await WMI.LenovoOtherMethod.GetFeatureValueAsync(CapabilityID.FanFullSpeed).ConfigureAwait(false);

        var targetValue = State switch
        {
            FanMaxSpeedState.On => 1,
            FanMaxSpeedState.Off => 0,
            FanMaxSpeedState.Toggle => currentValue == 0 ? 1 : 0,
            _ => currentValue
        };

        if (currentValue != targetValue)
        {
            await WMI.LenovoOtherMethod.SetFeatureValueAsync(CapabilityID.FanFullSpeed, targetValue).ConfigureAwait(false);
        }
    }
}