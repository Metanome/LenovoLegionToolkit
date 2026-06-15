using System;
using System.Collections.Generic;

namespace LenovoLegionToolkit.Lib.Station.Services;

public enum ExtensionIcon
{
    None,
    Gauge
}

public interface INavigationService
{
    IReadOnlyCollection<ExtensionNavigationItem> Items { get; }
    event EventHandler? ItemsChanged;
    void Register(ExtensionNavigationItem item);
}

public sealed class ExtensionNavigationItem
{
    public required string Id { get; init; }
    public string Title { get; init; } = "";
    public Func<string>? TitleGetter { get; init; }
    public required string PageTag { get; init; }
    public required Type PageType { get; init; }
    public ExtensionIcon Icon { get; init; }
    public bool IsFooter { get; init; }
}
