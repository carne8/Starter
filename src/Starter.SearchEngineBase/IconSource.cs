using Avalonia.Media;

namespace Starter.SearchEngine;

public class StarterIconSource()
{
    public static readonly StarterIconSource Empty = new();

    public StarterIconSource(IImage lightImage, IImage darkImage) : this()
    {
        LightImage = lightImage;
        DarkImage = darkImage;
    }
    public StarterIconSource(Geometry geometry) : this() => Geometry = geometry;

    public readonly Geometry? Geometry;
    public readonly IImage? LightImage;
    public readonly IImage? DarkImage;

    public IImage? GetImage(bool lightMode) => lightMode ? LightImage : DarkImage;
}
