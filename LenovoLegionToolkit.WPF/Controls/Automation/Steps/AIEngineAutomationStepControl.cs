using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Automation.Steps;
using LenovoLegionToolkit.WPF.Resources;
using Wpf.Ui.Common;

namespace LenovoLegionToolkit.WPF.Controls.Automation.Steps;

public class AIEngineAutomationStepControl : AbstractComboBoxAutomationStepCardControl<ToggleState>
{
    public AIEngineAutomationStepControl(IAutomationStep<ToggleState> step) : base(step)
    {
        Icon = SymbolRegular.Bot24;
        Title = Resource.AIEngineAutomationStepControl_Title;
        Subtitle = Resource.AIEngineAutomationStepControl_Message;
    }
}
