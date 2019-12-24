/*
 * SceneryBase.java
 */

/**
 * Base class for scenery objects.
 */

public abstract class SceneryBase : IScenery
{

   protected IDraw currentDraw;
protected double[] origin;

protected double[] reg0;
protected double[] reg1;
protected double[] reg2;

public SceneryBase(int dim)
{
    reg0 = new double[dim];
    reg1 = new double[dim];
    reg2 = new double[dim];
}

public void draw(IDraw currentDraw, double[] origin)
{
    this.currentDraw = currentDraw;
    this.origin = origin;
    // so we don't have to pass them around everywhere
    draw();
}

protected abstract void draw();

}

