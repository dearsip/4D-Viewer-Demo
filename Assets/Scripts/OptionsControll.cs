

public class OptionsControll// : IValidate
{

    // --- fields ---

    public int moveInputType, rotateInputType;
    public bool toggleLeftAndRight, toggleForward, toggleAlignMode;

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
        dest.toggleLeftAndRight = src.toggleLeftAndRight;
        dest.toggleForward = src.toggleForward;
        dest.toggleAlignMode = src.toggleAlignMode;
    }

}