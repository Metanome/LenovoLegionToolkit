using System;
using System.Threading.Tasks;

namespace LenovoLegionToolkit.Lib.Controllers.Sensors;

public class SensorsController(
    SensorsControllerV0 controllerV0,
    SensorsControllerV1 controllerV1,
    SensorsControllerV2 controllerV2,
    SensorsControllerV3 controllerV3,
    SensorsControllerV4 controllerV4,
    SensorsControllerV5 controllerV5)
    : ISensorsController
{
    private ISensorsController? _controller;

    public async Task<bool> IsSupportedAsync() => await GetControllerAsync().ConfigureAwait(false) is not null;

    public async Task PrepareAsync()
    {
        var controller = await GetControllerAsync().ConfigureAwait(false) ?? throw new InvalidOperationException("No supported controller found");
        await controller.PrepareAsync().ConfigureAwait(false);
    }

    public async Task<SensorsData> GetDataAsync()
    {
        var controller = await GetControllerAsync().ConfigureAwait(false) ?? throw new InvalidOperationException("No supported controller found");
        return await controller.GetDataAsync().ConfigureAwait(false);
    }

    public async Task<FanSpeedTable> GetFanSpeedsAsync()
    {
        var controller = await GetControllerAsync().ConfigureAwait(false) ?? throw new InvalidOperationException("No supported controller found");
        return await controller.GetFanSpeedsAsync().ConfigureAwait(false);
    }

    public async Task<ISensorsController?> GetControllerAsync()
    {
        if (_controller is not null)
            return _controller;

        if (await controllerV5.IsSupportedAsync().ConfigureAwait(false))
        {
            return _controller = controllerV5;
        }

        if (await controllerV4.IsSupportedAsync().ConfigureAwait(false))
        {
            return _controller = controllerV4;
        }

        if (await controllerV3.IsSupportedAsync().ConfigureAwait(false))
        {
            return _controller = controllerV3;
        }

        if (await controllerV2.IsSupportedAsync().ConfigureAwait(false))
        {
            return _controller = controllerV2;
        }

        if (await controllerV1.IsSupportedAsync().ConfigureAwait(false))
        {
            return _controller = controllerV1;
        }

        // SensorsControllerV0 mainly designed for non-gaming series. But also work for other laptops.
        if (await controllerV0.IsSupportedAsync().ConfigureAwait(false))
        {
            return _controller = controllerV0;
        }

        return null;
    }
}
