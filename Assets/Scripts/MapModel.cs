﻿/*
 * MapModel.java
 */

using System;

/**
 * A model that lets the user move through a maze.
 */

public class MapModel : IModel
{

    // --- fields ---

    private Map map;
    private Colorizer colorizer;
    private RenderAbsolute renderAbsolute;

    // --- construction ---

    public MapModel(int dimSpace, OptionsMap om, OptionsColor oc, OptionsSeed oe, OptionsView ov)
    {

        map = new Map(dimSpace, om, oe.mapSeed);
        colorizer = new Colorizer(dimSpace, om.dimMap, om.size, oc, oe.colorSeed);
        renderAbsolute = new RenderAbsolute(dimSpace, map, colorizer, ov);
    }

    // --- implementation of IModel ---

    public override void initPlayer(double[] origin, double[][] axis)
    {

        Grid.fromCell(origin, map.getStart());

        // cycle the axes so that we're correctly oriented when dimMap < dimSpace
        // axis[dimSpace-1] points in the forward direction, which should be unitVector(0) ... etc.
        // everything else is random, so it's OK for the axes to be deterministic
        //
        for (int a = 0; a < axis.Length; a++) Vec.unitVector(axis[a], (a + 1) % axis.Length);
    }

    public override void testOrigin(double[] origin, int[] reg1, int[] reg2)
    {

        // check that origin is in bounds and open
        // this is clumsy, because normally the walls keep us in bounds.

        // we might be on multiple boundaries, in which case toCell can't return all cells,
        // but that doesn't matter here.
        // the cells are all adjacent, so if any one is strictly in bounds,
        // the rest are enough in bounds not to cause an array fault in isOpen.

        Grid.toCell(reg1, reg2, origin); // ignore result
        if (!map.inBounds(reg1)) throw new Exception();

        if (!Grid.isOpen(origin, map, reg1)) throw new Exception();
    }

    public override void setColorMode(int colorMode)
    {
        colorizer.setColorMode(colorMode);
    }

    public override void setDepth(int depth)
    {
        renderAbsolute.setDepth(depth);
    }

    public override void setTexture(bool[] texture)
    {
        renderAbsolute.setTexture(texture);
    }

    public override void setTransparency(double transparency)
    {
        renderAbsolute.setTransparency(transparency);
    }

    public override void setOptions(OptionsColor oc, int seed, int depth, bool[] texture, OptionsDisplay od)
    {
        colorizer.setOptions(oc, seed);
        renderAbsolute.setDepth(depth);
        renderAbsolute.setTexture(texture);
        renderAbsolute.setTransparency(od.transparency);
        renderAbsolute.useEdgeColor = od.useEdgeColor;
    }

    public override bool isAnimated()
    {
        return false;
    }

    public override int getSaveType()
    {
        return IModel.SAVE_MAZE;
    }

    public override bool canMove(double[] p1, double[] p2, int[] reg1, double[] reg2)
    {
        return Grid.isOpenMove(p1, p2, map, reg1, reg2);
    }

    public override bool atFinish(double[] origin, int[] reg1, int[] reg2)
    {
        int dir = Grid.toCell(reg1, reg2, origin);
        return (Grid.equals(reg1, map.getFinish())
                 || (dir != Dir.DIR_NONE && Grid.equals(reg2, map.getFinish())));
    }

    public override bool dead() { return false; }

    public override void setBuffer(PolygonBuffer buf)
    {
        renderAbsolute.setBuffer(buf);
    }

    public override void animate()
    {
    }

    public override void render(double[] origin)
    {
        renderAbsolute.run(origin);
    }

}
