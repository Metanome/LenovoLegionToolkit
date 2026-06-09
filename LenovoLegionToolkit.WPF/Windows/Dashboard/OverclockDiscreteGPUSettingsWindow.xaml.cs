using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Controllers;
using LenovoLegionToolkit.WPF.Resources;

namespace LenovoLegionToolkit.WPF.Windows.Dashboard;

public partial class OverclockDiscreteGPUSettingsWindow
{

    private readonly GPUOverclockController _gpuOverclockController = IoCContainer.Resolve<GPUOverclockController>();

    public OverclockDiscreteGPUSettingsWindow()
    {
        InitializeComponent();

        var (enabled, info) = _gpuOverclockController.GetState();

        _applyCloseGrid.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
        _saveGrid.Visibility = enabled ? Visibility.Collapsed : Visibility.Visible;

        _coreSlider.Maximum = GPUOverclockController.GetMaxCoreDeltaMhz();
        _coreSlider.Value = info.CoreDeltaMhz;
        _memorySlider.Maximum = GPUOverclockController.GetMaxMemoryDeltaMhz();
        _memorySlider.Value = info.MemoryDeltaMhz;
        
        _voltageOffsetSlider.Minimum = GPUOverclockController.GetMinVoltageOffsetMv();
        _voltageOffsetSlider.Maximum = GPUOverclockController.GetMaxVoltageOffsetMv();
        _voltageOffsetSlider.Value = info.VoltageOffsetMv;
        
        int minLock = GPUOverclockController.GetMinVoltageLockMv();
        _voltageLockSlider.Minimum = minLock - _voltageLockSlider.TickFrequency;
        _voltageLockSlider.Maximum = GPUOverclockController.GetMaxVoltageLockMv();
        if (info.VoltageLockMv >= minLock)
        {
            _voltageLockSlider.Value = info.VoltageLockMv;
        }
        else
        {
            _voltageLockSlider.Value = _voltageLockSlider.Minimum;
        }

        _coreLabel.Content = $"{(int)_coreSlider.Value:+0;-0;0} {Resource.MHz}";
        _memoryLabel.Content = $"{(int)_memorySlider.Value:+0;-0;0} {Resource.MHz}";
        _voltageOffsetLabel.Content = $"{(int)_voltageOffsetSlider.Value:+0;-0;0} {Resource.mV}";
        _voltageLockLabel.Content = _voltageLockSlider.Value <= _voltageLockSlider.Minimum ? Resource.Off : $"{(int)_voltageLockSlider.Value} {Resource.mV}";
    }

    private void CoreSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_coreLabel != null && _coreSlider != null)
            _coreLabel.Content = $"{(int)_coreSlider.Value:+0;-0;0} {Resource.MHz}";
    }

    private void MemorySlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_memoryLabel != null && _memorySlider != null)
            _memoryLabel.Content = $"{(int)_memorySlider.Value:+0;-0;0} {Resource.MHz}";
    }

    private void VoltageOffsetSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_voltageOffsetLabel != null && _voltageOffsetSlider != null)
            _voltageOffsetLabel.Content = $"{(int)_voltageOffsetSlider.Value:+0;-0;0} {Resource.mV}";
    }

    private void VoltageLockSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_voltageLockLabel == null || _voltageLockSlider == null) return;

        if (_voltageLockSlider.Value <= _voltageLockSlider.Minimum)
            _voltageLockLabel.Content = Resource.Off;
        else
            _voltageLockLabel.Content = $"{(int)_voltageLockSlider.Value} {Resource.mV}";
    }



    private async void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
        Save();
        await ApplyAsync();
    }

    private async void ApplyAndCloseButton_Click(object sender, RoutedEventArgs e)
    {
        Save();
        await ApplyAsync();
        Close();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        Save();
        Close();
    }

    private void Save()
    {
        var (enabled, _) = _gpuOverclockController.GetState();
        int voltageLock = _voltageLockSlider.Value <= _voltageLockSlider.Minimum ? 0 : (int)_voltageLockSlider.Value;
        var info = new GPUOverclockInfo((int)_coreSlider.Value, (int)_memorySlider.Value, (int)_voltageOffsetSlider.Value, voltageLock);

        _gpuOverclockController.SaveState(enabled, info);
    }

    private async Task ApplyAsync() => await _gpuOverclockController.ApplyStateAsync();
}
