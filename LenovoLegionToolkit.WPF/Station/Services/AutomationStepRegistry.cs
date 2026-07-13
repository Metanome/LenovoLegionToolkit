using System;
using System.Collections.Generic;
using System.Linq;
using LenovoLegionToolkit.Lib.Station.Services;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.WPF.Station.Services;

public sealed class AutomationStepRegistry : IAutomationStepRegistry
{
    private readonly List<ExtensionAutomationStepInfo> _steps = [];

    public IReadOnlyCollection<ExtensionAutomationStepInfo> Steps => _steps.AsReadOnly();

    public event EventHandler? StepsChanged;

    public void Register(ExtensionAutomationStepInfo item)
    {
        Log.Instance.Trace($"Registering automation step StepType={item.StepType.FullName}, ControlType={item.ControlType.FullName}");

        if (_steps.Any(s => s.StepType == item.StepType))
        {
            Log.Instance.Trace($"Skipping duplicate automation step StepType={item.StepType.FullName}");
            return;
        }

        _steps.Add(item);
        Log.Instance.Trace($"Automation step registered successfully. Total steps: {_steps.Count}");
        StepsChanged?.Invoke(this, EventArgs.Empty);
        Log.Instance.Trace($"AutomationSteps StepsChanged event raised");
    }
}
