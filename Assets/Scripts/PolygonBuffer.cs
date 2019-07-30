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
    private ArrayList polygons;
    private int size;

    public PolygonBuffer (int dim)
    {
        this.dim = dim;
        polygons = new ArrayList();
        size = 0;
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
        } else
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
}
