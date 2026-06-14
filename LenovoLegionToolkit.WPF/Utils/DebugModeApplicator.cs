using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using LenovoLegionToolkit.Lib.Utils;
using Wpf.Ui.Controls;

namespace LenovoLegionToolkit.WPF.Utils;

public static class DebugModeApplicator
{
    private static DispatcherTimer? _timer;
    private static readonly HashSet<Type> ExcludedTypes = new()
    {
        typeof(System.Windows.Controls.ProgressBar),
        typeof(Wpf.Ui.Controls.ProgressRing),
        typeof(Popup),
        typeof(ToolTip),
        typeof(ScrollViewer),
        typeof(ContentPresenter),
        typeof(Wpf.Ui.Controls.Snackbar),
        typeof(Border)
    };

    public static void Start()
    {
        if (!AppFlags.Instance.Debug) return;

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _timer.Tick += (s, e) => ApplyGlobalDebugVisibility();
        _timer.Start();
    }

    private static void ApplyGlobalDebugVisibility()
    {
        if (Application.Current?.MainWindow is Window window)
        {
            WalkAndForce(window);
        }
    }

    private static void WalkAndForce(DependencyObject root)
    {
        if (root is UIElement element)
        {
            var type = element.GetType();
            if (!ExcludedTypes.Contains(type))
            {
                if (element is FrameworkElement fe)
                {
                    if (!string.IsNullOrEmpty(fe.Name) && !fe.Name.StartsWith("PART_"))
                    {
                        if (fe.Visibility != Visibility.Visible)
                            fe.Visibility = Visibility.Visible;
                        if (!fe.IsEnabled)
                            fe.IsEnabled = true;
                    }
                }
            }
        }

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child != null)
            {
                WalkAndForce(child);
            }
        }
    }
}
