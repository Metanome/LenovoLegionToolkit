using System;
using System.Collections.Generic;
using System.Linq;
using LenovoLegionToolkit.Lib;
using LenovoLegionToolkit.Lib.Station.Services;
using LenovoLegionToolkit.Lib.Utils;

namespace LenovoLegionToolkit.WPF.Station.Services;

public sealed class NavigationService : INavigationService
{
    private readonly List<ExtensionNavigationItem> _items = [];

    public IReadOnlyCollection<ExtensionNavigationItem> Items => _items.AsReadOnly();

    public event EventHandler? ItemsChanged;

    public void Register(ExtensionNavigationItem item)
    {
        Log.Instance.Trace($"Registering navigation item Id={item.Id}, Title={item.Title}, PageTag={item.PageTag}, PageType={item.PageType.FullName}, IsFooter={item.IsFooter}");

        if (_items.Any(i => i.Id.Equals(item.Id, StringComparison.OrdinalIgnoreCase) || i.PageTag.Equals(item.PageTag, StringComparison.OrdinalIgnoreCase)))
        {
            Log.Instance.Trace($"Skipping duplicate navigation item Id={item.Id}, PageTag={item.PageTag}");
            return;
        }

        _items.Add(item);
        Log.Instance.Trace($"Navigation item registered successfully. Total items: {_items.Count}");
        ItemsChanged?.Invoke(this, EventArgs.Empty);
        Log.Instance.Trace($"Navigation ItemsChanged event raised");
    }
}
