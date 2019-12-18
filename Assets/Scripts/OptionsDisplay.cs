

public class OptionsDisplay// : IValidate
{

    // --- fields ---
    
    public double transparency;
    public double border;

    // --- construction ---

    public OptionsDisplay()
    {
    }

    // --- structure methods ---

    public static void copy(OptionsDisplay dest, OptionsDisplay src)
    {
        dest.transparency = src.transparency;
        dest.border = src.border;
    }

}
