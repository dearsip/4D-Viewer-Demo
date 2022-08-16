

public class OptionsDisplay// : IValidate
{

    // --- fields ---
    
    public double transparency, lineThickness;
    public double border;
    public bool usePolygon, useEdgeColor, hidesel, invertNormals, separate, map;
    public int trainSpeed;

    // --- construction ---

    public OptionsDisplay()
    {
    }

    // --- structure methods ---

    public static void copy(OptionsDisplay dest, OptionsDisplay src)
    {
        dest.transparency = src.transparency;
        dest.lineThickness = src.lineThickness;
        dest.usePolygon = src.usePolygon;
        dest.border = src.border;
        dest.useEdgeColor = src.useEdgeColor;
        dest.hidesel = src.hidesel;
        dest.invertNormals = src.invertNormals;
        dest.separate = src.separate;
        dest.map = src.map;
        dest.trainSpeed = src.trainSpeed;
    }

    public const double TRANSPARENCY_MIN = 0;
    public const double TRANSPARENCY_MAX = 1;
    public const double LINETHICKNESS_MIN = 0.001;
    public const double LINETHICKNESS_MAX = 0.01;
    public const double BORDER_MIN = -1;
    public const double BORDER_MAX = 1;
    public const int TRAINSPEED_MIN = -5;
    public const int TRAINSPEED_MAX = 5;
}
