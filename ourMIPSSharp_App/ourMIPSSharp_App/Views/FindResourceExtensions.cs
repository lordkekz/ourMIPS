using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace ourMIPSSharp_App.Views; 

public static class FindResourceExtensions {
    public static bool TryFindColor(this IResourceHost node, string resourceName, out Color color) {
        var r = node.TryFindResource(resourceName, out var x);
        color = r ? (Color)x! : Colors.Black;
        return r;
    }
    
    public static IBrush FindBrushOrDefault(this IResourceHost node, string resourceName) {
        node.TryFindResource(resourceName, out var x);
        App.Current!.TryFindResource(resourceName, out var y);
        return ((x ?? y) as IBrush) ?? Brushes.Black;
    }
}