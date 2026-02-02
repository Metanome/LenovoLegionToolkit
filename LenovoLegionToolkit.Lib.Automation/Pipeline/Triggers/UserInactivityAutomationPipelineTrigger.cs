using System;
using System.Threading.Tasks;
using LenovoLegionToolkit.Lib.AutoListeners;
using LenovoLegionToolkit.Lib.Automation.Resources;
using Newtonsoft.Json;

namespace LenovoLegionToolkit.Lib.Automation.Pipeline.Triggers;

[method: JsonConstructor]
public class UserInactivityAutomationPipelineTrigger(TimeSpan inactivityTimeSpan) : IUserInactivityPipelineTrigger
{
    public string DisplayName => InactivityTimeSpan == TimeSpan.Zero
        ? Resource.UserInactivityAutomationPipelineTrigger_DisplayName_Zero
        : Resource.UserInactivityAutomationPipelineTrigger_DisplayName;

    public TimeSpan InactivityTimeSpan { get; } = inactivityTimeSpan;

    public Task<bool> IsMatchingEvent(IAutomationEvent automationEvent)
    {
        if (automationEvent is not (UserInactivityAutomationEvent or StartupAutomationEvent))
            return Task.FromResult(false);

        if (automationEvent is StartupAutomationEvent)
            return IsMatchingState();

        var e = (UserInactivityAutomationEvent)automationEvent;
        var result = InactivityTimeSpan == e.InactivityTimeSpan;
        return Task.FromResult(result);
    }

    public Task<bool> IsMatchingState()
    {
        var listener = IoCContainer.Resolve<UserInactivityAutoListener>();
        var result = InactivityTimeSpan == listener.InactivityTimeSpan;
        return Task.FromResult(result);
    }

    public void UpdateEnvironment(AutomationEnvironment environment) => environment.UserActive = InactivityTimeSpan == TimeSpan.Zero;

    public IAutomationPipelineTrigger DeepCopy() => new UserInactivityAutomationPipelineTrigger(InactivityTimeSpan);

    public IUserInactivityPipelineTrigger DeepCopy(TimeSpan timeSpan) => new UserInactivityAutomationPipelineTrigger(timeSpan);

    public override bool Equals(object? obj) => obj is UserInactivityAutomationPipelineTrigger t && InactivityTimeSpan == t.InactivityTimeSpan;

    public override int GetHashCode() => InactivityTimeSpan.GetHashCode();

    public override string ToString() => $"{nameof(InactivityTimeSpan)}: {InactivityTimeSpan}";
}
