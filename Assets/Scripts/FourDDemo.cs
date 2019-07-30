using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * A model that lets the user move around geometric shapes.
 */
// 元のコードの GeomModel（特にその中の render(..)） を主体とした、プログラムの本体。

public class FourDDemo : I4DSoftware
{
    private Geom.Shape[] shapes;
    private bool[][] inFromt;
    private PolygonBuffer buf;
    private IDraw currentDraw;
    private PolygonBuffer bufRelative;
    private RenderRelative renderRelative;
    private Clip.Draw[] clipUnits;
    private bool[][] inFront;
    private Geom.Separator[][] separators;

    private double[] origin;
    private double[][] axis;
    private double[] reg1, reg2, reg3, reg4;

    private List<Vector3> verts;
    private List<int> tris;
    private List<Color> cols;

    public FourDDemo()
    {
        // 以下の shapes の定義をコメントアウトしたものと交換したり自分で設定することで、表示する図形を変えられる。

        // 正五胞体8つ
        /*shapes = new Geom.Shape[8];
        for (int i=0; i<8; i++)
        {
            shapes[i] = Shapes.pentachoron();
            shapes[i].aligncenter[i / 2] = 1.8 * ((double)i % 2 * 2 - 1);
            shapes[i].place();
        }*/

        // 正五胞体2つ
        /*shapes = new Geom.Shape[] { Shapes.pentachoron(), Shapes.pentachoron() };
        shapes[0].aligncenter[0] = 1.1;
        shapes[0].place();
        shapes[1].aligncenter[0] = -1.1;
        shapes[1].place();*/

        // 正八胞体
        shapes = new Geom.Shape[] { Shapes.superCell() };

        // 正八胞体（色配置２）
        // shapes = new Geom.Shape[] { Shapes.superCell() };

        // 正十六胞体
        // shapes = new Geom.Shape[] { Shapes.hexadecachoron() };

        // 正十六胞体（色配置２）
        // shapes = new Geom.Shape[] { Shapes.hd2() };

        origin = new double[] { 0, 0, 0, -3 };
        reg1 = new double[4];
        reg2 = new double[4];
        reg3 = new double[4];
        reg4 = new double[3];
        axis = new double[4][];
        for (int i = 0; i < 4; i++) axis[i] = new double[4];
        Vec.unitMatrix(axis);
        clipUnits = new Clip.Draw[shapes.Length];
        for (int i = 0; i < shapes.Length; i++) clipUnits[i] = new Clip.Draw(4);
        inFront = new bool[shapes.Length][];
        for (int i = 0; i < shapes.Length; i++) inFront[i] = new bool[shapes.Length];
        separators = new Geom.Separator[shapes.Length][];
        for (int i = 0; i < shapes.Length; i++) separators[i] = new Geom.Separator[shapes.Length];

        buf = new PolygonBuffer(4);
        bufRelative = new PolygonBuffer(3);
        renderRelative = new RenderRelative(buf, bufRelative, 4, 1);

        verts = new List<Vector3>();
        tris = new List<int>();
        cols = new List<Color>();

    }

    public void Run(ref Vector3[] vertices, ref int[] triangles, ref Color[] colors, double[][] rotate)
    {
        Vec.zero(reg1);
        for (int i = 0; i < shapes.Length; i++) // 回転
        {
            shapes[i].rotateFrame(rotate[0], rotate[1], reg1, reg2, reg3);
            shapes[i].rotateFrame(rotate[2], rotate[3], reg1, reg2, reg3);
            shapes[i].place();
        }

        buf.clear();

        // calcViewBoundaries is expensive, but we always need the boundaries
        // to draw the scenery correctly
        currentDraw = buf;
        for (int i = 0; i < shapes.Length; i++)
        {
            if (shapes[i] == null) continue;
            calcVisShape(shapes[i]);
            clipUnits[i].setBoundaries(Clip.calcViewBoundaries(origin, shapes[i]));
        }

        calcInFront();

        for (int i = 0; i < shapes.Length; i++)
        {
            if (shapes[i] == null) continue;
            currentDraw = buf;
            for (int h = 0; h < shapes.Length; h++)
            {
                if (shapes[h] == null) continue;
                if (inFront[h][i]) currentDraw = clipUnits[h].chain(currentDraw);
            }
            drawShape(shapes[i]);
        }

        renderRelative.run(axis);

        convert(bufRelative, ref vertices, ref triangles, ref colors);
    }

    private void calcInFront()
    {
        for (int i = 0; i < shapes.Length - 1; i++)
        {
            Geom.Shape s1 = shapes[i];
            if (s1 == null) continue;
            for (int j = i + 1; j < shapes.Length; j++)
            {
                Geom.Shape s2 = shapes[j];
                if (s2 == null) continue;

                // prefer dynamic separation even when we know a static
                // separator because dynamic ones are better at finding
                // the desired value NO_FRONT.
                int result = Clip.dynamicSeparate(s1, s2, origin, reg1, reg2);
                if (result == Geom.Separator.UNKNOWN)
                {
                    Geom.Separator sep;


                    sep = separators[i][j];
                    if (sep == null)
                    {
                        sep = Clip.staticSeparate(s1, s2,/* any = */ false);
                        separators[i][j] = sep;
                    }

                    result = sep.apply(origin);
                }

                inFront[i][j] = (result == Geom.Separator.S1_FRONT);
                inFront[j][i] = (result == Geom.Separator.S2_FRONT);
            }
        }
        // note that in general inFront is not transitive.  with long blocks
        // you can easily construct cycles where one is in front of the next
        // all the way around.
    }

    // 見える Cell に属する Face の visible を true にする。
    private void calcVisShape(Geom.Shape shape)
    {
        for (int i = 0; i < shape.face.Length; i++) shape.face[i].visible = false;
        for (int i = 0; i < shape.cell.Length; i++)
            if (calcVisCell(shape.cell[i]))
                for (int j = 0; j < shape.cell[i].ifa.Length; j++)
                    shape.face[shape.cell[i].ifa[j]].visible = true;
    }

    private bool calcVisCell(Geom.Cell cell)
    {
        if (cell.normal != null)
        {

            // late to be adding an epsilon here, but without this you can sometimes
            // see a flat face between two other faces that ought to be in contact.
            // the example is geom3-project4b (delete the front face and slide right).
            const double epsilon = -1e-9;

            Vec.sub(reg1, cell.center, origin); // vector from origin to face center
            cell.visible = (Vec.dot(reg1, cell.normal) < epsilon);
        }
        else
        {
            cell.visible = true; // glass
        }
        return cell.visible;
    }

    private void drawShape(Geom.Shape shape)
    {
        // for (int i = 0; i < shape.face.Length; i++) if (shape.face[i].visible) drawFace(shape, shape.face[i]);
        for (int i = 0; i < shape.cell.Length; i++) if (shape.cell[i].visible) drawCell(shape, shape.cell[i]);
    }

    private void drawFace(Geom.Shape shape, Geom.Face face)
    {
        double[][] vertex = new double[face.iv.Length][];
        for (int i = 0; i < face.iv.Length; i++) vertex[i] = (double[])shape.vertex[face.iv[i]].Clone();
        Polygon polygon = new Polygon(vertex, Color.white);
        currentDraw.drawPolygon(polygon, origin);
    }

    // 隙間を空けた胞表示。
    private void drawCell(Geom.Shape shape, Geom.Cell cell)
    {
        for(int i = 0; i < cell.ifa.Length; i++) { drawFace(shape, cell, shape.face[cell.ifa[i]]); }
    }

    private void drawFace(Geom.Shape shape, Geom.Cell cell, Geom.Face face)
    {
        double[][] vertex = new double[face.iv.Length][];
        for (int i = 0; i < face.iv.Length; i++)
        {
            vertex[i] = new double[shape.vertex[face.iv[i]].Length];
            Vec.mid(vertex[i], cell.center, shape.vertex[face.iv[i]], 0.8);
        }
        Polygon polygon = new Polygon(vertex, cell.color);
        currentDraw.drawPolygon(polygon, origin);
    }

    // Polygon の情報を vertices, triangles, colors に変換する。
    private void convert(PolygonBuffer buf, ref Vector3[] vertices, ref int[] triangles, ref Color[] colors)
    {
        int count = 0;
        verts.Clear();
        tris.Clear();
        cols.Clear();
        for (int i=0; i<buf.getSize(); i++)
        {
            int v = buf.get(i).vertex.Length;
            if (v < 3) continue;
            for (int j = 0; j < v; j++)
            {
                reg4 = buf.get(i).vertex[j];
                verts.Add(new Vector3((float)reg4[0], (float)reg4[1], (float)reg4[2]));
                cols.Add(buf.get(i).color);
            }
            for (int j = 0; j < v - 2; j++)
            {
                tris.Add(count);
                tris.Add(count + j + 1);
                tris.Add(count + j + 2);
            }
            count += v;
        }
        vertices = verts.ToArray();
        triangles = tris.ToArray();
        colors = cols.ToArray();
    }
}
