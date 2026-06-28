using System.Collections.Generic;
using System.Linq;

namespace LenovoLegionToolkit.Lib.Settings;

public class AmdOverclockingSettings() : AbstractSettings<AmdOverclockingSettings.AmdOverclockingSettingsStore>("amd_oc.json")
{
    public class AmdOverclockingSettingsStore
    {
        public bool Enabled { get; set; }
        public bool AllowOnBattery { get; set; }
        public bool AllowInAllPowerModes { get; set; }
        public uint? FMax { get; set; }
        public List<double?> CoreValues { get; set; } = [];
        public short? PowerLimit1 { get; set; }
        public short? PowerLimit2 { get; set; }
        public short? PowerLimit3 { get; set; }
        public short? EDCSoc { get; set; }
        public short? EDCVdd { get; set; }
        public short? TDCSoc { get; set; }
        public short? TDCVdd { get; set; }
    }

    public bool HasProfile =>
        Store.FMax.HasValue ||
        Store.CoreValues.Any(v => v.HasValue) ||
        Store.PowerLimit1.HasValue ||
        Store.PowerLimit2.HasValue ||
        Store.PowerLimit3.HasValue ||
        Store.EDCSoc.HasValue ||
        Store.EDCVdd.HasValue ||
        Store.TDCSoc.HasValue ||
        Store.TDCVdd.HasValue;

    public OverclockingProfile? GetProfile()
    {
        if (!HasProfile) return null;

        return new OverclockingProfile
        {
            FMax = Store.FMax,
            CoreValues = Store.CoreValues,
            PowerLimit1 = Store.PowerLimit1,
            PowerLimit2 = Store.PowerLimit2,
            PowerLimit3 = Store.PowerLimit3,
            EDCSoc = Store.EDCSoc,
            EDCVdd = Store.EDCVdd,
            TDCSoc = Store.TDCSoc,
            TDCVdd = Store.TDCVdd,
        };
    }

    public void SetProfile(OverclockingProfile profile)
    {
        Store.FMax = profile.FMax;
        Store.CoreValues = profile.CoreValues;
        Store.PowerLimit1 = profile.PowerLimit1;
        Store.PowerLimit2 = profile.PowerLimit2;
        Store.PowerLimit3 = profile.PowerLimit3;
        Store.EDCSoc = profile.EDCSoc;
        Store.EDCVdd = profile.EDCVdd;
        Store.TDCSoc = profile.TDCSoc;
        Store.TDCVdd = profile.TDCVdd;
        SynchronizeStore();
    }
}
