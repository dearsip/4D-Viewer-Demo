using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * A line buffer that reuses line objects to avoid memory allocation.
 */
// 描画対象は Polygon に変更している。
public class PolygonBuffer : IDraw
{
    private int dim;
    private List<Polygon> polygons;
    private int size;
    private IComparer<Polygon> comparer;

    public PolygonBuffer(int dim)
    {
        this.dim = dim;
        polygons = new List<Polygon>();
        size = 0;
        comparer = new PolygonComparer();
    }

    public void clear()
    {
        size = 0;
    }

    public Polygon getNext()
    {
        Polygon polygon;

        if (size < polygons.Count)
        {
            polygon = (Polygon)polygons[size];
        }
        else
        {
            polygon = new Polygon();
            polygons.Add(polygon);
        }

        size++;
        return polygon;
    }

    public void unget()
    {
        size--;
    }

    public void add(Polygon poly)
    {
        Polygon polygon = getNext();
        polygon.copy(poly);
    }

    public void drawPolygon(Polygon face, double[] origin)
    {
        Polygon polygon = getNext();
        polygon.copy(face);
        for (int i = 0; i < polygon.vertex.Length; i++) Vec.sub(polygon.vertex[i], polygon.vertex[i], origin);
    }

    public int getSize() { return size; }

    public Polygon get(int i)
    {
        return (Polygon)polygons[i];
    }

    public void sort(double[] eyeVector)
    {
        double[] sum = new double[3];
        for (int i = 0; i < size; i++)
        {
            Vec.copy(sum, polygons[i].vertex[0]);
            for (int j = 1; j < polygons[i].vertex.Length; j++) Vec.add(sum, sum, polygons[i].vertex[j]);
            polygons[i].dist = Vec.dot(sum, eyeVector) / polygons[i].vertex.Length;
        }
        polygons.Sort(0, size, comparer);
    }

    class PolygonComparer : IComparer<Polygon>
    {
        public int Compare(Polygon x, Polygon y)
        {
            return (x.dist - y.dist > 0) ? -1 : 1;
        }
    }
}