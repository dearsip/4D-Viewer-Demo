﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * An object that takes a set of lines oriented with respect to absolute coordinates
 * and converts them to relative coordinates, i.e., projects them onto a retina.
 */
// 描画対象が Polygon になった関係で少々修正がある。
public class RenderRelative
{
    // --- fields ---

    private PolygonBuffer bin;
    private PolygonBuffer bout;
    private int dim;

    private double retina;
    private Clip.CustomBoundary[] clip;
    private Polygon reg;

    // --- construction ---

    public RenderRelative(PolygonBuffer bin, PolygonBuffer bout, int dim, double retina)
    {
        this.bin = bin;
        this.bout = bout;
        this.dim = dim;

        clip = new Clip.CustomBoundary[2 * (dim - 1)];
        reg = new Polygon();

        setRetina(retina);
    }

    // --- options ---

    public void setRetina(double retina)
    {
        this.retina = retina;

        int next = 0;
        double[] reg;
        for (int a = 0; a < dim - 1; a++)
        {

            reg = new double[dim];
            reg[a] = 1;
            reg[dim - 1] = retina;
            clip[next] = new Clip.CustomBoundary(reg, 0);
            next++;

            reg = new double[dim];
            reg[a] = -1;
            reg[dim - 1] = retina;
            clip[next] = new Clip.CustomBoundary(reg, 0);
            next++;

            // no need to zero other components,
            // they never become nonzero
        }
    }

    public double getRetina() { return retina; }

    // --- processing ---

    // the call to projectRetina could cause division by zero
    // if the line being projected started or ended on the parallel plane through the origin.
    // that's not a problem here.
    // the clipping planes restrict the lines to a forward cone,
    // so the origin is the only dangerous point,
    // and we know there are no lines through the origin
    // because we're not allowed to move onto the walls.

    private bool convert(Polygon dest, Polygon src, double[][] axis)
    {
        reg.vertex = new double[src.vertex.Length][];
        for (int i = 0; i < src.vertex.Length; i++)
        {
            reg.vertex[i] = new double[4];
            Vec.toAxisCoordinates(reg.vertex[i], src.vertex[i], axis);
        }
        for (int i = 0; i < clip.Length; i++)
        {
            if (Clip.clip(reg, clip[i])) return false;
        }

        dest.vertex = new double[reg.vertex.Length][];
        for (int i = 0; i < reg.vertex.Length; i++)
        {
            dest.vertex[i] = new double[3];
            Vec.projectRetina(dest.vertex[i], reg.vertex[i], retina);
        }

        dest.color = src.color;
        return true;
    }

    public void run(double[][] axis)
    {
      bout.clear();
        for (int i = 0; i <bin.getSize(); i++) {
            Polygon src = bin.get(i);
            Polygon dest = bout.getNext();

            if (!convert(dest, src, axis)) bout.unget();
        }
    }

    //public void runObject(double[][] obj, int mask, PointTransform pt)
    //{
    //    // no need for clear here
    //    for (int i = 0; i < obj.Length; i += 2)
    //    {
    //        if ((mask & 1) == 1)
    //        {
    //            Line dest = out.getNext();

    //            Vec.copy(dest.p1, obj[i]);
    //            Vec.copy(dest.p2, obj[i + 1]);
    //            dest.color = Color.white;

    //            pt.transform(dest.p1);
    //            pt.transform(dest.p2);
    //        }
    //        mask >>= 1;
    //    }
    //}

}
