using System;
using System.Threading;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Controllers;
using LenovoLegionToolkit.Lib.Features;
using LenovoLegionToolkit.Lib.System;
using LenovoLegionToolkit.Lib.Utils;
using Newtonsoft.Json;

namespace LenovoLegionToolkit.Lib.Automation.Steps;

[method: JsonConstructor]
public class AIEngineAutomationStep(ToggleState state) : IAutomationStep<ToggleState>
{
    private readonly AIController _aiController = IoCContainer.Resolve<AIController>();
    private readonly PowerModeFeature _powerModeFeature = IoCContainer.Resolve<PowerModeFeature>();

    public ToggleState State { get; } = state;

    public async Task<bool> IsSupportedAsync()
    {
        if (AppFlags.Instance.Debug)
            return true;

        var mi = await Compatibility.GetMachineInformationAsync().ConfigureAwait(false);
        return mi.Properties.SupportsAIMode;
    }

    public Task<ToggleState[]> GetAllStatesAsync() => Task.FromResult(Enum.GetValues<ToggleState>());

    public IAutomationStep DeepCopy() => new AIEngineAutomationStep(State);

    public async Task RunAsync(AutomationContext context, AutomationEnvironment environment, CancellationToken token)
    {
        var enable = State switch
        {
            ToggleState.On => true,
            ToggleState.Off => false,
            ToggleState.Toggle => !_aiController.IsAIModeEnabled,
            _ => throw new ArgumentOutOfRangeException()
        };

        if (_aiController.IsAIModeEnabled == enable)
            return;

        _aiController.IsAIModeEnabled = enable;

        await _aiController.StopAsync().ConfigureAwait(false);

        if (await _powerModeFeature.GetStateAsync().ConfigureAwait(false) == PowerModeState.Balance)
            await _powerModeFeature.SetStateAsync(PowerModeState.Balance).ConfigureAwait(false);

        await _aiController.StartIfNeededAsync().ConfigureAwait(false);
    }
}
