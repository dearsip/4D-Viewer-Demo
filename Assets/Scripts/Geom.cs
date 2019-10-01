using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Classes that are used to represent geometric shapes.
 * The same representation works for both 3D and 4D since we don't (currently) care about 2D faces in 4D.
 */
// 描画対象とするために Face （2次元）を追加したので、元のコードの Face を Cell に書き換えてある。
// このプログラムでは使用しないメソッド等も含む。
public class Geom
{

    // --- helpers ---

    public static double[] clone1(double[] d)
    {
        return (double[])d.Clone();
    }

    public static double[][] clone2(double[][] d)
    {
        d = (double[][])d.Clone();
        for (int i = 0; i < d.Length; i++) d[i] = (double[])d[i].Clone();
        return d;
    }

    public static Cell[] clone2(Cell[] c)
    {
        c = (Cell[])c.Clone();
        for (int i = 0; i < c.Length; i++) c[i] = c[i].copy();
        return c;
    }

    public static Face[] clone2(Face[] f)
    {
        f = (Face[])f.Clone();
        for (int i = 0; i < f.Length; i++) f[i] = f[i].copy();
        return f;
    }

    public static Edge[] clone2(Edge[] e)
    {
        e = (Edge[])e.Clone();
        for (int i = 0; i < e.Length; i++) e[i] = e[i].copy();
        return e;
    }

    private static void vrotate(double[] vec, double[] from, double[] to, double[] origin, double[] reg1, double[] reg2)
    {
        Vec.sub(vec, vec, origin);
        Vec.rotate(vec, vec, from, to, reg1, reg2);
        Vec.add(vec, vec, origin);
    }

    // --- place helper ---

    // could generalize and use this same interface for rotations
    // and other things, but for now it's just for the place call

    public class PlaceHelper
    {

        private double[][] axis;
        private double[] originNew;
        private double[] originOld;
        private double[] reg1;

        public PlaceHelper(double[][] axis, double[] originNew, double[] originOld)
        {
            this.axis = axis;
            this.originNew = originNew;
            this.originOld = originOld;
            reg1 = new double[axis.Length];
        }

        /**
         * Change originNew to make src map to dest.
         */
        public void solvePos(double[] dest, double[] src)
        {
            Vec.sub(reg1, src, originOld);
            Vec.fromAxisCoordinates(originNew, reg1, axis);
            Vec.addScaled(originNew, dest, originNew, -1);
        }

        public void placePos(double[] dest, double[] src)
        {
            Vec.sub(reg1, src, originOld);
            Vec.fromAxisCoordinates(dest, reg1, axis);
            Vec.add(dest, dest, originNew);
        }

        public void placeDir(double[] dest, double[] src)
        {
            Vec.fromAxisCoordinates(dest, src, axis);
        }

        public void idealPos(double[] dest, double[] src)
        {
            Vec.sub(reg1, src, originNew);
            Vec.toAxisCoordinates(dest, reg1, axis);
            Vec.add(dest, dest, originOld);
        }
    }

    // --- shape ---

    public class Shape : Clip.BoundaryList {

        public Cell[] cell;
        public Subface[] subface;
        public Face[] face;
        public Edge[] edge;
        public double[][] vertex;
        public double[] shapecenter; // for size testing; connected to radius
        public double[] aligncenter; // for alignment, snapping, and rotation
        public double radius;
        public double[][] axis; // for block motion only
        public Shape ideal;
        //public bool systemMove;
        //public bool noUserMove;
        //public HintInterface hint;
        //public Face bottomFace; // for railcars only

        private Shape() { }

        // 各要素を指定してShapeを生成する。
        public Shape(double[][] vertex, int[][] eiv, int[][] fiv, int[][] cie, int[][] cif, double[][] cn, Color[] color)
        {
            this.vertex = vertex;
            edge = new Edge[eiv.Length];
            for (int i = 0; i < eiv.Length; i++) edge[i] = new Edge(eiv[i][0], eiv[i][1]);
            face = new Face[fiv.Length];
            for (int i = 0; i < fiv.Length; i++) face[i] = new Face(fiv[i]);
            cell = new Cell[cie.Length];
            for (int i = 0; i < cie.Length; i++) cell[i] = new Cell(cie[i], cif[i], cn[i]);
            calculate();
            ideal = copy();
            for (int i = 0; i < cell.Length; i++) cell[i].color = color[i];
        }

        public int getSize() { return cell.Length; }
        public Clip.Boundary getBoundary(int i) { return cell[i]; }

        public Shape copy()
        {
            Shape s = new Shape();

            s.cell = clone2(cell);
            s.subface = subface; // share
            s.edge = clone2(edge);
            s.vertex = clone2(vertex);
            s.shapecenter = clone1(shapecenter);
            s.aligncenter = clone1(aligncenter);
            s.radius = radius;
            s.axis = clone2(axis);

            s.ideal = ideal; // share ideals!  we'll make a new copy
                             // whenever we do anything that needs to alter the ideal.
                             // also note, ideals rarely change, it's just memory.

            // systemMove and noUserMove don't need to be copied.
            // systemMove is for train cars, and the copy won't be one.
            // noUserMove is for platform textures
            // (and ramps by analogy), and the copy will lose that.

            return s;
        }

        public double[] getAlignCenter()
        {
            return aligncenter;
        }

        public void calculate()
        {
            calcCenters();
            calcSubfaces();

            int dim = /*getDimension()=*/4;

            shapecenter = new double[dim];
            for (int i = 0; i < vertex.Length; i++) Vec.add(shapecenter, shapecenter, vertex[i]);
            Vec.scale(shapecenter, shapecenter, 1 / (double)vertex.Length);
            // not always the best center value, but we only use it for separators
            aligncenter = clone1(shapecenter);

            calcRadius(); // uses shapecenter

            axis = new double[dim][];
            for (int i = 0; i < dim; i++) axis[i] = new double[dim];
            Vec.unitMatrix(axis);
        }

        public void calcRadius()
        {
            double[] temp = new double[/*getDimension()=*/4];
            double r = 0;
            for (int i = 0; i < vertex.Length; i++)
            {
                Vec.sub(temp, vertex[i], shapecenter);
                double d = Vec.norm(temp);
                if (d > r) r = d;
            }
            radius = r;
        }

        public void calcCenters()
        {
            for (int i = 0; i < cell.Length; i++) calcCenter(cell[i]);
        }

        public bool[] getCellVertices(Cell c)
        {
            bool[] b = new bool[vertex.Length];
            for (int i = 0; i < c.ie.Length; i++)
            {
                Edge e = edge[c.ie[i]];
                b[e.iv1] = true;
                b[e.iv2] = true;
            }
            return b;
        }

        public void calcSubfaces()
        {

            // in 3D, we get a subface when two cells have an edge in common.
            // in 4D, we get a subface when two cells have two or more edges
            // in common.

            List<Subface> list = new List<Subface>();
            int nGoal = /*(getDimension() == 3) ? 1 :*/ 2; // only place we test dimension!

            for (int i1 = 0; i1 < cell.Length - 1; i1++)
            {
                for (int i2 = i1 + 1; i2 < cell.Length; i2++)
                {
                    if (countEdgesInCommon(cell[i1].ie, cell[i2].ie) >= nGoal)
                    {
                        Subface sf = new Subface();
                        sf.ic1 = i1;
                        sf.ic2 = i2;
                        list.Add(sf);
                    }
                }
            }

            subface = (Subface[])list.ToArray();
        }

        public int countEdgesInCommon(int[] ie1, int[] ie2)
        {

            // if we knew the arrays were sorted we could do
            // some kind of fast walk-through, but since the
            // calculation only happens once it hardly matters.

            int n = 0;
            for (int i1 = 0; i1 < ie1.Length; i1++)
            {
                for (int i2 = 0; i2 < ie2.Length; i2++)
                {
                    if (ie1[i1] == ie2[i2]) n++;
                }
            }
            return n;
        }

        public void place()
        {
            PlaceHelper helper = new PlaceHelper(axis, aligncenter, ideal.aligncenter);
            helper.placePos(shapecenter, ideal.shapecenter);

            for (int i = 0; i < cell.Length; i++) cell[i].place(ideal.cell[i], helper);
            for (int i = 0; i < vertex.Length; i++) helper.placePos(vertex[i], ideal.vertex[i]);
            // we took care of shapecenter and aligncenter above
            // radius doesn't change under these transformations
            // axis is fixed
        }

       public void rotateFrame(double[] from, double[] to, double[] origin, double[] reg1, double[] reg2)
        {
            // do aligncenter and axes now, update the rest later
            if (origin == null) origin = clone1(getAlignCenter());
            vrotate(aligncenter, from, to, origin, reg1, reg2);
            for (int i = 0; i < axis.Length; i++) Vec.rotate(axis[i], axis[i], from, to, reg1, reg2);
        }

        public void rotateRelative(double[] from, double[] to, double[] reg1, double[] reg2)
        {
            for (int i = 0; i < vertex.Length; i++) Vec.rotate(ideal.vertex[i], ideal.vertex[i], from, to, reg1, reg2);
            for (int i = 0; i < cell.Length; i++)
            {
                Vec.rotate(ideal.cell[i].center, ideal.cell[i].center, from, to, reg1, reg2);
                Vec.rotate(ideal.cell[i].normal, ideal.cell[i].normal, from, to, reg1, reg2);
            }
        }

        public double vmin(bool[] b, int axis)
        {
            double d = 0;
            bool first = true;
            for (int i = 0; i < vertex.Length; i++)
            {
                if (!b[i]) continue;
                double temp = vertex[i][axis];
                if (first)
                {
                    d = temp;
                    first = false;
                }
                else
                {
                    if (temp < d) d = temp;
                }
            }
            return d;
        }

        public double vmax(bool[] b, int axis)
        {
            double d = 0;
            bool first = true;
            for (int i = 0; i < vertex.Length; i++)
            {
                if (!b[i]) continue;
                double temp = vertex[i][axis];
                if (first)
                {
                    d = temp;
                    first = false;
                }
                else
                {
                    if (temp > d) d = temp;
                }
            }
            return d;
        }

        public void calcCenter(Cell f)
        {

            // find what vertices are involved and average them

            bool[] b = getCellVertices(f);

            double[] d = null;
            int n = 0;

            for (int i = 0; i < vertex.Length; i++)
            {
                if (!b[i]) continue;
                if (d == null) d = clone1(vertex[i]);
                else Vec.add(d, d, vertex[i]);
                n++;
            }

            Vec.scale(d, d, 1 / (double)n);
            f.center = d;
            f.calcThreshold();
        }

        private static bool match(double[] normal, int axis, int sign)
        {
            if (normal == null) return false;
            double e1 = 0.1; // epsilons, use two so there's no ambiguity
            double e2 = 0.000001;
            for (int i = 0; i < normal.Length; i++)
            {
                if (i == axis)
                {
                    if (normal[i] * sign < e1) return false;
                }
                else
                {
                    if (System.Math.Abs(normal[i]) > e2) return false;
                }
            }
            return true;
        }
    }

    // --- cell ---

    public class Cell : Clip.Boundary
    {

        public int[] ie; // indices of edges
        public int[] ifa; 
        public double[] center;
        public double[] normal;
        public double threshold;
        public Color color;
        
        public bool visible;

        public double[] getNormal() { return normal; }
        public double getThreshold() { return threshold; }
        public void calcThreshold() { threshold = (normal != null) ? Vec.dot(center, normal) : 0; }

        public Cell() { }

        public Cell(int[] ie, int[] ifa, double[] normal)
        {
            this.ie = ie;
            this.ifa = ifa;
            this.normal = normal;
        }

        public Cell copy()
        {
            Cell c = new Cell();
            c.ie = ie; // share
            c.ifa = ifa;
            c.center = clone1(center); // don't copy before adding to shape
            c.normal = (normal != null) ? clone1(normal) : null;
            c.threshold = threshold;
            c.color = color;
            // visible gets recalculated as needed
            return c;
        }

        public int[] getVertices(Edge[] e, int nv)
        {
            bool[] b = new bool[nv];
            for (int i = 0; i < ie.Length; i++)
            {
                b[e[ie[i]].iv1] = true;
                b[e[ie[i]].iv2] = true;
            }
            int fv = 0;
            for (int i = 0; i < nv; i++)
            {
                if (b[i]) fv++;
            }
            int[] iv = new int[fv];
            int j = 0;
            for (int i = 0; i < nv; i++)
            {
                if (b[i]) iv[j++] = i;
            }
            return iv;
        }

        public void place(Cell ideal, PlaceHelper helper)
        {
            helper.placePos(center, ideal.center);
            if (ideal.normal != null)
            {
                helper.placeDir(normal, ideal.normal);
            }
            else
            {
                normal = null;
            }
            calcThreshold(); // can't transform it
        }
    }

    // --- subface ---

    public class Subface
    {

        public int ic1; // index of cell
        public int ic2;
    }

    // --- face ---
    
    public class Face
    {

        public int[] iv;
        public bool visible;

        public Face() { }
        public Face(int[] iv) { this.iv = iv; }

        public Face copy()
        {
            return new Face(iv);
        }
    }

    // --- edge ---

    public class Edge
    {

        public int iv1; // index of vertex 1
        public int iv2; // index of vertex 2
        public Color color;

        public Edge() { }
        public Edge(int iv1, int iv2)
        {
            this.iv1 = iv1;
            this.iv2 = iv2;
        }
        public Edge(int iv1, int iv2, Color color)
        {
            this.iv1 = iv1;
            this.iv2 = iv2;
            this.color = color;
        }

        public Edge copy()
        {
            return new Edge(iv1, iv2, color);
        }

        public bool sameVertices(int iv1, int iv2)
        {
            return (this.iv1 == iv1 && this.iv2 == iv2)
                   || (this.iv1 == iv2 && this.iv2 == iv1);
        }

        public bool sameVertices(Edge edge)
        {
            return (iv1 == edge.iv1 && iv2 == edge.iv2)
                   || (iv1 == edge.iv2 && iv2 == edge.iv1);
        }
    }

    // --- separator ---

    public abstract class Separator
    {

        public virtual int apply(double[] origin) { return 0; }

        public const int S1_FRONT = -1;
        public const int NO_FRONT = 0;
        public const int S2_FRONT = 1;
        public const int UNKNOWN = 2; // only Clip.dynamicSeparate returns this
    }

    public class NormalSeparator : Separator
    {

        public double[] normal;
        public double threshMin;
        public double threshMax;
        public int invert;

        public NormalSeparator(double[] normal, double threshMin, double threshMax, int invert)
        {
            this.normal = normal;
            this.threshMin = threshMin;
            this.threshMax = threshMax;
            this.invert = invert;
        }

        public NormalSeparator(Cell cell, int invert)
        { // convenience
            this.normal = cell.normal;
            this.threshMin = cell.threshold;
            this.threshMax = cell.threshold;
            this.invert = invert;
        }

        public override int apply(double[] origin)
        {
            double value = Vec.dot(origin, normal);
            int result;
            if (value < threshMin) result = S1_FRONT;
            else if (value > threshMax) result = S2_FRONT;
            else result = NO_FRONT;
            // if value is in between, neither is in front!
            return result * invert;
        }
    }

    public class NullSeparator : Separator
    {
        public override int apply(double[] origin) { return NO_FRONT; }
    }

    public static Separator nullSeparator = new NullSeparator();
}