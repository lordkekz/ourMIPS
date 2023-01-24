using Avalonia.Controls;
using Avalonia.Media;

namespace ourMIPSSharp_App.Views; 

public static class FindResourceExtensions {
    public static bool TryFindColor(this IResourceHost node, string resourceName, out Color color) {
        var r = node.TryFindResource(resourceName, out var x);
        color = r ? (Color)x : default;
        return r;
    }
}