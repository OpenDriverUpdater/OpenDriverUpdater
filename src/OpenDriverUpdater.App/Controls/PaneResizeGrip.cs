using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;

namespace OpenDriverUpdater.App.Controls;

/// <summary>
/// Thin drag handle on the navigation pane's right edge. Exists as its own
/// class only because <c>ProtectedCursor</c> (the left-right resize cursor)
/// can be set from a subclass.
/// </summary>
public sealed partial class PaneResizeGrip : Grid
{
    public PaneResizeGrip()
    {
        ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast);
    }
}
