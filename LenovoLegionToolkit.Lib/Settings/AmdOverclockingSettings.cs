namespace LenovoLegionToolkit.Lib.Settings;

public class AmdOverclockingSettings() : AbstractSettings<AmdOverclockingSettings.AmdOverclockingSettingsStore>("amd_oc_settings.json")
{
    public class AmdOverclockingSettingsStore
    {
        public bool Enabled { get; set; }
        public bool AllowOnBattery { get; set; }
        public bool AllowInAllPowerModes { get; set; }
    }
}
