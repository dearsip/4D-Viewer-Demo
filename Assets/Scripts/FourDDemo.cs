using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

/**
 * A model that lets the user move around geometric shapes.
 */
// 元のコードの GeomModel（特にその中の render(..)） を主体とした、プログラムの本体。

public class FourDDemo
{
    private Geom.Shape[] shapelist;

    private Geom.Shape[] shapes;
    private bool[][] inFromt;
    private PolygonBuffer buf;
    private IDraw currentDraw;
    private PolygonBuffer bufRelative;
    private RenderRelative renderRelative;
    private Clip.Draw[] clipUnits;
    private bool[][] inFront;
    private Geom.Separator[][] separators;
    private Clip.Result clipResult;

    private double[] origin;
    private double[][] axis;
    private double[] reg1, reg2, reg3, reg4;

    private List<Vector3> verts;
    private List<int> tris;
    private List<Color> cols;

    private int shapeNum = 1;
    private int colorNum = 1;

    private float cellAlpha = 0.6f;
    private Color faceColor = Color.white;
    private double offset = 0.8;
    private double border = -1;
    private int[] colors;
    private int colorVal = 12;
    private int selectedCell = -1;

    private static double PHI = (1 + Math.Sqrt(5)) / 2;
    private double[][] spinv = new double[][]
    {
        new double[] {     0,     0,  1, -1 },
        new double[] { PHI-1, PHI+1,  0,  0 },
        new double[] {     0,     0,  1,  1 },
        new double[] { 1+PHI, 1-PHI,  0,  0 }
    };
    private double count = 0;

    public FourDDemo()
    {
        origin = new double[] { 0, 0, 0, -3 };
        reg1 = new double[4];
        reg2 = new double[4];
        reg3 = new double[4];
        reg4 = new double[3];
        axis = new double[4][];
        for (int i = 0; i < 4; i++) axis[i] = new double[4];
        Vec.unitMatrix(axis);
        
        shapelist = new Geom.Shape[] { Shapes.cell_5(), Shapes.cell_8(), Shapes.cell_16(),
                                       Shapes.cell_24(), Shapes.cell_120(), Shapes.cell_600() };
        shapes = new Geom.Shape[1];
        setShape();
        colors = new int[shapes[0].cell.Length];

        buf = new PolygonBuffer(4);
        bufRelative = new PolygonBuffer(3);
        renderRelative = new RenderRelative(buf, bufRelative, 4, 1);
        clipResult = new Clip.Result();

        verts = new List<Vector3>();
        tris = new List<int>();
        cols = new List<Color>();

        for (int i = 0; i < spinv.Length; i++) Vec.normalize(spinv[i], spinv[i]);
    }

    public void changeShape(int shapeNum)
    {
        this.shapeNum = shapeNum;
        colorNum = 1;
        setShape();
    }

    public void changeColor(int colorNum)
    {
        this.colorNum = colorNum;
        setShape();
    }

    public void changeFaceAlpha(float a) { faceColor.a = a; }

    public void changeCellAlpha(float a) { cellAlpha = a; }

    public void changeRetina(double r)
    {
        origin[3] = -(r * r + 1) * 3;
        renderRelative.setRetina(1 / (r * r + 1) * (6 / (6 + r)));
    }

    public void changeBorder(double r) { border = r; }

    public void changeOffset(double r) { offset = r; }

    private void setShape()
    {
        shapes[0] = shapelist[shapeNum];
        colorVal = Vec.max(Shapes.colorList[shapeNum][colorNum]) + 1;
        if (colorVal == 1) colorVal = 12;
        colors = Shapes.colorList[shapeNum][colorNum];
        for (int i = 0; i < shapes[0].cell.Length; i++) shapes[0].cell[i].color = Color.HSVToRGB(1f * colors[i] / colorVal, 1, 1);
        clipUnits = new Clip.Draw[shapes.Length];
        for (int i = 0; i < shapes.Length; i++) clipUnits[i] = new Clip.Draw(4);
        inFront = new bool[shapes.Length][];
        for (int i = 0; i < shapes.Length; i++) inFront[i] = new bool[shapes.Length];
        separators = new Geom.Separator[shapes.Length][];
        for (int i = 0; i < shapes.Length; i++) separators[i] = new Geom.Separator[shapes.Length];
    }

    public void Run(ref Vector3[] vertices, ref int[] triangles, ref Color[] colors, 
        double[][] rotate, double[] eyeVector, double[] cursor, bool edit, bool spin)
    {
        if (spin)
        {
            double theta = Time.deltaTime;
            if (count < 1)
            {
                count += theta;
                if (count >= 1) theta = 1 + theta - count;
            }
            else
            {
                count += theta;
                if (count >= 2)
                {
                    count -= 2;
                    theta = count;
                }
                else theta = 0;
            }
            if (theta > 0)
            {
                theta *= 72;
                Vec.rotateAngle(reg1, reg2, spinv[0], spinv[1], 2 * theta);
                for (int i = 0; i < shapes.Length; i++) shapes[i].rotateRelative(spinv[0], reg1, reg2, reg3);
                Vec.rotateAngle(reg1, reg2, spinv[2], spinv[3], theta);
                for (int i = 0; i < shapes.Length; i++) shapes[i].rotateRelative(spinv[2], reg1, reg2, reg3);
            }
        }

        Vec.zero(reg1);

        for (int i = 0; i < shapes.Length; i++) // 回転
        {
            shapes[i].rotateFrame(rotate[0], rotate[1], reg1, reg2, reg3);
            shapes[i].rotateFrame(rotate[2], rotate[3], reg1, reg2, reg3);
            shapes[i].place();
        }

        click(cursor, edit);

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
            drawShape(shapes[i], eyeVector);
        }

        renderRelative.run(axis);

        bufRelative.sort(eyeVector); // できる限り自然な描画にするために、meshを大まかに並べ替える。
        //applyBorder();

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

    private void drawShape(Geom.Shape shape, double[] eyeVector)
    {
        // for (int i = 0; i < shape.face.Length; i++) if (shape.face[i].visible) drawFace(shape, shape.face[i]);
        for (int i = 0; i < shape.cell.Length; i++) if (shape.cell[i].visible) drawCell(shape, shape.cell[i], i == selectedCell, eyeVector);
    }

    private void drawFace(Geom.Shape shape, Geom.Face face)
    {
        double[][] vertex = new double[face.iv.Length][];
        for (int i = 0; i < face.iv.Length; i++) vertex[i] = (double[])shape.vertex[face.iv[i]].Clone();
        Polygon polygon = new Polygon(vertex, faceColor);
        currentDraw.drawPolygon(polygon, origin);
    }

    // 隙間を空けた胞表示。
    private void drawCell(Geom.Shape shape, Geom.Cell cell, bool selected, double[] eyeVector)
    {
        Vec.sub(reg1, cell.center, origin);
        Vec.toAxisCoordinates(reg1, reg1, axis);
        Vec.projectRetina(reg4, reg1, renderRelative.getRetina());
        bool beyond = Vec.dot(reg4, eyeVector) < border;
        for (int i = 0; i < cell.ifa.Length; i++) drawFace(shape, cell, shape.face[cell.ifa[i]], selected, beyond);
    }

    private void drawFace(Geom.Shape shape, Geom.Cell cell, Geom.Face face, bool selected, bool beyond)
    {
        double[][] vertex = new double[face.iv.Length][];
        for (int i = 0; i < face.iv.Length; i++)
        {
            vertex[i] = new double[shape.vertex[face.iv[i]].Length];
            Vec.mid(vertex[i], cell.center, shape.vertex[face.iv[i]], offset);
        }
        cell.color.a = (beyond) ? cellAlpha * 0.1f : cellAlpha;
        Polygon polygon = new Polygon(vertex, selected ? cell.color +Color.white * 0.2f : cell.color);
        currentDraw.drawPolygon(polygon, origin);
    }

    private void applyBorder()
    {
        int size = bufRelative.getSize();
        int target = size / 2;
        int n = target / 2;
        while (n > 0)
        {
            if (target < size && bufRelative.get(target).dist < border) target -= n;
            else target += n;
            n /= 2;
        }
        for (int i = target; i < size; i++) bufRelative.get(i).color.a *= 0.1f;
    }

    public void click(double[] vector, bool edit)
    {
        Vec.addScaled(reg3, axis[3], vector, renderRelative.getRetina());
        Vec.addScaled(reg2, origin, reg3, 10000); // infinity
        Geom.Shape shape = shapes[0];
        if (Clip.closestApproach(shape.shapecenter, origin, reg3, reg1) <= shape.radius * shape.radius)
        { // could be a hit
            Clip.clip(origin, reg2, shape, clipResult);
            if ((clipResult.clip & Clip.KEEP_A) != 0)
            {
                selectedCell = clipResult.ia;
                if (edit)
                {
                    colors[selectedCell]++;
                    colors[selectedCell] %= colorVal;
                    shape.cell[selectedCell].color = Color.HSVToRGB(1f * colors[selectedCell] / colorVal, 1, 1);
                }
            }
            else selectedCell = -1;
        }
    }

    public void save()
    {
        StreamWriter sw = new StreamWriter("./LogData.txt", true);
        sw.WriteLine(Vec.ToString(colors));
        sw.Flush();
        sw.Close();
    }

    // Polygon の情報を vertices, triangles, colors に変換する。
    private void convert(PolygonBuffer buf, ref Vector3[] vertices, ref int[] triangles, ref Color[] colors)
    {
        int count = 0;
        Polygon p;
        verts.Clear();
        tris.Clear();
        cols.Clear();
        for (int i=0; i<buf.getSize(); i++)
        {
            p = buf.get(i);
            int v = p.vertex.Length;
            for (int j = 0; j < v; j++)
            {
                reg4 = p.vertex[j];
                verts.Add(new Vector3((float)reg4[0], (float)reg4[1], (float)reg4[2]));
                cols.Add(p.color);
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
