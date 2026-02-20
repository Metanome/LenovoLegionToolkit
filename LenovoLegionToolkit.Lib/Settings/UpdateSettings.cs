using System;
using static LenovoLegionToolkit.Lib.Settings.UpdateSettings;

namespace LenovoLegionToolkit.Lib.Settings;

public class UpdateSettings() : AbstractSettings<UpdateSettingsStore>("update_settings.json")
{
    public class UpdateSettingsStore
    {
        public DateTime? LastUpdateCheckDateTime { get; set; }
        public UpdateCheckFrequency UpdateCheckFrequency { get; set; }
        public UpdateChannel UpdateChannel { get; set; }
        public UpdateMethod UpdateMethod { get; set; }
    }

    protected override UpdateSettingsStore Default => new()
    {
        LastUpdateCheckDateTime = null,
        UpdateCheckFrequency = UpdateCheckFrequency.PerDay,
        UpdateChannel = UpdateChannel.Stable,
        UpdateMethod = UpdateMethod.GitHub
    };
}
