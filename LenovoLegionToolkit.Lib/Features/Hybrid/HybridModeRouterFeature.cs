using LenovoLegionToolkit.Lib.Features;

namespace LenovoLegionToolkit.Lib.Features.Hybrid;

public class HybridModeRouterFeature(
    BiosHybridMode biosHybridMode,
    HybridModeFeature hybridModeFeature
) : AbstractCompositeFeature<HybridModeState>(biosHybridMode, hybridModeFeature);
