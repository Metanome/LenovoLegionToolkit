using System;
using System.Collections.Generic;
using Windows.Devices.Lights;
using Windows.UI;

namespace LenovoLegionToolkit.Lib.Utils.LampEffects;

public class CustomKeyEffect : BaseLampEffect
{
    public override string Name => "Custom";
    
    private readonly Dictionary<int, Color> _lampColors = new();
    private readonly Dictionary<ushort, Color> _keyColors = new();

    public CustomKeyEffect()
    {
        Parameters["DefaultColor"] = Color.FromArgb(255, 0, 0, 0); // 默认黑色(关闭)
    }

    public void SetKeyColor(ushort scanCode, Color color)
    {
        _keyColors[scanCode] = color;
    }

    public void SetLampColor(int lampIndex, Color color)
    {
        _lampColors[lampIndex] = color;
    }

    public void ClearKeyColor(ushort scanCode)
    {
        _keyColors.Remove(scanCode);
    }
    public void ClearAll()
    {
        _lampColors.Clear();
        _keyColors.Clear();
    }

    public Color? GetKeyColor(ushort scanCode)
    {
        return _keyColors.TryGetValue(scanCode, out var color) ? color : null;
    }

    public IReadOnlyDictionary<ushort, Color> GetAllKeyColors() => _keyColors;

    public override Color GetColorForLamp(int lampIndex, double time, LampInfo lampInfo, int totalLamps)
    {
        if (_lampColors.TryGetValue(lampIndex, out var lampColor))
            return lampColor;

        return (Color)Parameters["DefaultColor"];
    }

    public override void Reset()
    {
    }
    public void MapKeyToLamps(ushort scanCode, IEnumerable<int> lampIndices, Color color)
    {
        _keyColors[scanCode] = color;
        foreach (var index in lampIndices)
        {
            _lampColors[index] = color;
        }
    }
}
