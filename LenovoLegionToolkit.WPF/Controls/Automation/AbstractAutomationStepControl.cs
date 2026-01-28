using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using LenovoLegionToolkit.Lib.Automation.Steps;
using LenovoLegionToolkit.WPF.Resources;
using Wpf.Ui.Common;
using Wpf.Ui.Controls;
using Button = Wpf.Ui.Controls.Button;
using CardControl = LenovoLegionToolkit.WPF.Controls.Custom.CardControl;

namespace LenovoLegionToolkit.WPF.Controls.Automation;

public abstract class AbstractAutomationStepControl<T>(T automationStep) : AbstractAutomationStepControl(automationStep)
    where T : IAutomationStep
{
    protected new T AutomationStep => (T)base.AutomationStep;
}

public abstract class AbstractAutomationStepControl : UserControl
{
    protected IAutomationStep AutomationStep { get; }

    private readonly CardControl _cardControl = new()
    {
        Margin = new(0, 0, 0, 8),
    };

    private readonly CardHeaderControl _cardHeaderControl = new();

    private readonly StackPanel _stackPanel = new()
    {
        Orientation = Orientation.Horizontal,
    };

    private readonly SymbolIcon _dragHandle = new()
    {
        Symbol = SymbolRegular.GridDots24,
        Margin = new(-8, 0, 8, 0),
        Cursor = System.Windows.Input.Cursors.SizeAll,
        Opacity = 0.5,
    };

    private readonly Button _deleteButton = new()
    {
        Icon = SymbolRegular.Dismiss24,
        ToolTip = Resource.AbstractAutomationStepControl_Delete,
        MinWidth = 34,
        Height = 34,
        Margin = new(8, 0, 0, 0),
    };

    public SymbolRegular Icon
    {
        get => _cardControl.Icon;
        set => _cardControl.Icon = value;
    }

    public string Title
    {
        get => _cardHeaderControl.Title;
        set => _cardHeaderControl.Title = value;
    }

    public string Subtitle
    {
        get => _cardHeaderControl.Subtitle;
        set => _cardHeaderControl.Subtitle = value;
    }

    public VerticalAlignment TitleVerticalAlignment
    {
        get => _cardHeaderControl.TitleVerticalAlignment;
        set => _cardHeaderControl.TitleVerticalAlignment = value;
    }

    public VerticalAlignment SubtitleVerticalAlignment
    {
        get => _cardHeaderControl.SubtitleVerticalAlignment;
        set => _cardHeaderControl.SubtitleVerticalAlignment = value;
    }

    public event EventHandler? Changed;
    public event EventHandler? Delete;

    protected AbstractAutomationStepControl(IAutomationStep automationStep)
    {
        AutomationStep = automationStep;

        InitializeComponent();

        Loaded += RefreshingControl_Loaded;
    }

    private void InitializeComponent()
    {
        _dragHandle.MouseLeftButtonDown += (_, e) =>
        {
            if (e.ClickCount > 1) return;
            DragDrop.DoDragDrop(this, new DataObject("AutomationStep", this), DragDropEffects.Move);
        };

        _deleteButton.Click += (_, _) => Delete?.Invoke(this, EventArgs.Empty);

        var control = GetCustomControl();
        if (control is not null)
            _stackPanel.Children.Add(control);
        _stackPanel.Children.Add(_deleteButton);

        _cardHeaderControl.Accessory = _stackPanel;
        
        var headerPanel = new StackPanel { Orientation = Orientation.Horizontal };
        headerPanel.Children.Add(_dragHandle);
        headerPanel.Children.Add(_cardHeaderControl);

        _cardControl.Header = headerPanel;

        Content = _cardControl;
    }

    private async void RefreshingControl_Loaded(object sender, RoutedEventArgs e)
    {
        await RefreshAsync();
        OnFinishedLoading();
    }

    public abstract IAutomationStep CreateAutomationStep();

    protected abstract UIElement? GetCustomControl();

    protected abstract void OnFinishedLoading();

    protected abstract Task RefreshAsync();

    protected void RaiseChanged() => Changed?.Invoke(this, EventArgs.Empty);
}
