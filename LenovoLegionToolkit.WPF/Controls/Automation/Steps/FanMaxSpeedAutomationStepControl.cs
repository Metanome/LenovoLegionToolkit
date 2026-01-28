using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Automation.Steps;
using LenovoLegionToolkit.WPF.Resources;
using Wpf.Ui.Common;

namespace LenovoLegionToolkit.WPF.Controls.Automation.Steps;

public class FanMaxSpeedAutomationStepControl : AbstractComboBoxAutomationStepCardControl<FanMaxSpeedState>
{
    public FanMaxSpeedAutomationStepControl(IAutomationStep<FanMaxSpeedState> step) : base(step)
    {
        Icon = SymbolRegular.Gauge24;
        Title = Resource.FanMaxSpeedAutomationStepControl_Title;
        Subtitle = Resource.FanMaxSpeedAutomationStepControl_Message;
    }
}
