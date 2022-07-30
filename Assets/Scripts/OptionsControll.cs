

public class OptionsControll// : IValidate
{

    // --- fields ---

    public int moveInputType, rotateInputType;
    public bool invertLeftAndRight, invertForward, invertYawAndPitch, invertRoll, sliceMode, limit3D;

    public float baseTransparency, sliceTransparency;

    // --- constants ---

    public const int INPUTTYPE_JOYSTICK = 0;
    public const int COLOR_GREEN = 1;

    // --- construction ---

    public OptionsControll()
    {
    }

    // --- structure methods ---

    public static void copy(OptionsControll dest, OptionsControll src)
    {
        dest.moveInputType = src.moveInputType;
        dest.rotateInputType = src.rotateInputType;
        dest.invertLeftAndRight = src.invertLeftAndRight;
        dest.invertForward = src.invertForward;
        dest.invertYawAndPitch = src.invertYawAndPitch;
        dest.invertRoll = src.invertRoll;
        dest.sliceMode = src.sliceMode;
        dest.baseTransparency = src.baseTransparency;
        dest.sliceTransparency = src.sliceTransparency;
        dest.limit3D = src.limit3D;
    }

}