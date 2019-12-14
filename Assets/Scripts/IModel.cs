using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * An interface for defining the model that an engine runs.
 */

public abstract class IModel
{

    public abstract void initPlayer(double[] origin, double[][] axis);
    public abstract void testOrigin(double[] origin, int[] reg1, int[] reg2);

    public abstract void setColorMode(int colorMode);
    public abstract void setDepth(int depth);
    public abstract void setTexture(bool[] texture);
    public abstract void setOptions(OptionsColor oc, int seed, int depth, bool[] texture);

    public abstract bool isAnimated();
    public abstract int getSaveType();
    public abstract bool canMove(double[] p1, double[] p2, int[] reg1, double[] reg2);
    public abstract bool atFinish(double[] origin, int[] reg1, int[] reg2);
    public abstract bool dead();

    public abstract void setBuffer(PolygonBuffer buf);
    public abstract void animate();
    public abstract void render(double[] origin);

    public const int SAVE_NONE = 0;
    public const int SAVE_MAZE = 1;
    public const int SAVE_GEOM = 2;
    public const int SAVE_ACTION = 3;
    public const int SAVE_BLOCK = 4;
    public const int SAVE_SHOOT = 5;

}

