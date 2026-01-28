using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace LenovoLegionToolkit.WPF.Utils;

public class DragAdorner : Adorner
{
    private readonly UIElement _child;
    private readonly VisualBrush _brush;
    private Point _location;
    private Point _offset;

    public DragAdorner(UIElement adornedElement, UIElement child, Point offset) : base(adornedElement)
    {
        _child = child;
        _offset = offset;
        _brush = new VisualBrush(_child) { Opacity = 0.7 };
        IsHitTestVisible = false;
    }

    public void UpdatePosition(Point location)
    {
        _location = location;
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        var rect = new Rect(_location.X - _offset.X, _location.Y - _offset.Y, _child.RenderSize.Width, _child.RenderSize.Height);
        drawingContext.DrawRectangle(_brush, null, rect);
    }
}
