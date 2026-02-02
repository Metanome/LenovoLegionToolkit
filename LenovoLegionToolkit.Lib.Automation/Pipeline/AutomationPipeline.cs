using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.Automation.Pipeline.Triggers;
using LenovoLegionToolkit.Lib.Automation.Resources;
using LenovoLegionToolkit.Lib.Automation.Steps;
using LenovoLegionToolkit.Lib.Extensions;
using LenovoLegionToolkit.Lib.Utils;
using Newtonsoft.Json;

namespace LenovoLegionToolkit.Lib.Automation.Pipeline;

public class AutomationPipeline
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string? IconName { get; set; }

    public string? Name { get; set; }

    public IAutomationPipelineTrigger? Trigger { get; set; }

    public List<IAutomationStep> Steps { get; init; } = [];

    public bool IsExclusive { get; set; } = true;

    public bool RunOnStartup { get; set; }

    [JsonIgnore]
    public IEnumerable<IAutomationPipelineTrigger> AllTriggers
    {
        get
        {
            if (Trigger is not null && Trigger is not ICompositeAutomationPipelineTrigger)
                yield return Trigger;

            if (Trigger is ICompositeAutomationPipelineTrigger compositeTrigger)
                foreach (var trigger in compositeTrigger.Triggers)
                    yield return trigger;
        }
    }

    public AutomationPipeline() { }

    public AutomationPipeline(string name) => Name = name;

    public AutomationPipeline(IAutomationPipelineTrigger trigger) => Trigger = trigger;

    internal async Task RunAsync(List<AutomationPipeline> otherPipelines, CancellationToken token = default)
    {
        if (token.IsCancellationRequested)
        {
            Log.Instance.Trace($"Pipeline interrupted.");
            return;
        }

        var context = new AutomationContext();
        var environment = new AutomationEnvironment();
        var stepExceptions = new List<Exception>();

        AllTriggers.ForEach(t => t.UpdateEnvironment(environment));

        foreach (var step in GetAllSteps(otherPipelines))
        {
            if (token.IsCancellationRequested)
            {
                Log.Instance.Trace($"Pipeline interrupted.");
                break;
            }

            if (environment.Startup && IsDangerousOnStartup(step))
            {
                Log.Instance.Trace($"Skipping dangerous step on startup. [type={step.GetType().Name}]");
                continue;
            }

            Log.Instance.Trace($"Running step... [type={step.GetType().Name}]");

            try
            {
                await step.RunAsync(context, environment, token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Instance.Trace($"Step run failed. [name={step.GetType().Name}]", ex);

                stepExceptions.Add(ex);
            }

            Log.Instance.Trace($"Step completed successfully. [type={step.GetType().Name}]");
        }

        if (stepExceptions.Count != 0)
            throw new AggregateException(stepExceptions);
    }

    private static bool IsDangerousOnStartup(IAutomationStep step)
    {
        var stepType = step.GetType().Name;
        return stepType is "CloseAutomationStep" or "TurnOffMonitorsAutomationStep" or "TurnOffWiFiAutomationStep";
    }

    private IEnumerable<IAutomationStep> GetAllSteps(List<AutomationPipeline> pipelines)
    {
        foreach (var step in Steps)
        {
            if (step is QuickActionAutomationStep qas)
            {
                var matchingPipeline = pipelines.FirstOrDefault(p => p.Id != Id && p.Id == qas.PipelineId && p.AllTriggers.IsEmpty());
                if (matchingPipeline is null)
                    continue;

                foreach (var matchingPipelineStep in matchingPipeline.GetAllSteps(pipelines))
                    yield return matchingPipelineStep;
            }

            yield return step;
        }
    }

    public AutomationPipeline DeepCopy() => new()
    {
        Id = Id,
        IconName = IconName,
        Name = Name,
        Trigger = Trigger?.DeepCopy(),
        Steps = Steps.Select(s => s.DeepCopy()).ToList(),
        IsExclusive = IsExclusive,
        RunOnStartup = RunOnStartup,
    };

    public IEnumerable<string> GetValidationWarnings(IEnumerable<AutomationPipeline>? pipelines = null)
    {
        var steps = GetAllSteps(pipelines?.ToList() ?? []).ToList();
        var powerModeStepIndex = steps.FindLastIndex(s => s is PowerModeAutomationStep);

        if (powerModeStepIndex == -1)
            yield break;

        for (int i = 0; i < powerModeStepIndex; i++)
        {
            var step = steps[i];
            if (step is DisplayBrightnessAutomationStep or RefreshRateAutomationStep)
            {
                yield return Resource.AutomationPipeline_Warning_Power_Mode_Visual;
                break;
            }

            if (step is not FanMaxSpeedAutomationStep)
            {
                continue;
            }

            yield return Resource.AutomationPipeline_Warning_Power_Mode_Fan;
            break;
        }

        var hybridModeStepIndex = steps.FindIndex(s => s is HybridModeAutomationStep);
        if (hybridModeStepIndex != -1 && hybridModeStepIndex != steps.Count - 1)
        {
             yield return Resource.AutomationPipeline_Warning_Hybrid_Mode;
        }

        var deactivateGPUIndex = steps.FindIndex(s => s is DeactivateGPUAutomationStep);
        var overclockIndex = steps.FindIndex(s => s is OverclockDiscreteGPUAutomationStep);
        if (deactivateGPUIndex != -1 && overclockIndex != -1 && overclockIndex > deactivateGPUIndex)
        {
            yield return Resource.AutomationPipeline_Warning_GPU_OC;
        }

        var hdrStepIndex = steps.FindIndex(s => s is HDRAutomationStep);
        var brightnessStepIndex = steps.FindLastIndex(s => s is DisplayBrightnessAutomationStep);
        if (hdrStepIndex != -1 && brightnessStepIndex != -1 && brightnessStepIndex > hdrStepIndex)
        {
             yield return Resource.AutomationPipeline_Warning_HDR;
        }

        var resolutionIndex = steps.FindLastIndex(s => s is ResolutionAutomationStep);
        var refreshRateIndex = steps.FindIndex(s => s is RefreshRateAutomationStep);
        if (resolutionIndex != -1 && refreshRateIndex != -1 && refreshRateIndex < resolutionIndex)
        {
             yield return Resource.AutomationPipeline_Warning_Resolution;
        }
    }
}
