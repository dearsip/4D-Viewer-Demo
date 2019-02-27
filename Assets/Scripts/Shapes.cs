using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

// Shape を返すメソッド。
public class Shapes
{
    // 正五胞体
    public static Geom.Shape pentachoron()
    {
        double[][] vertex = new double[][]
        {
            new double[] { 0, 0, 0, 1 },
            new double[] { 0, 0, Math.Sqrt(15)/4.0, -1/4.0 },
            new double[] { Math.Sqrt(30)/6.0, 0, -Math.Sqrt(15)/12.0, -1/4.0 },
            new double[] { -Math.Sqrt(30)/12.0, Math.Sqrt(10)/4.0, -Math.Sqrt(15)/12.0, -1/4.0 },
            new double[] { -Math.Sqrt(30)/12.0, -Math.Sqrt(10)/4.0, -Math.Sqrt(15)/12.0, -1/4.0 }
        };
        int[][] eiv = new int[10][];
        int count = 0;
        for (int i = 0; i < 4; i++)
            for (int j = i + 1; j < 5; j++) eiv[count++] = new int[] { i, j };
        int[][] fiv = new int[10][];
        count = 0;
        for (int i = 0; i < 3; i++)
            for (int j = i + 1; j < 4; j++)
                for (int k = j + 1; k < 5; k++) fiv[count++] = new int[] { i, j, k };
        int[][] cie = new int[][]
        {
            new int[] { 0,1,2,4,5,7 },
            new int[] { 0,1,3,4,6,8 },
            new int[] { 0,2,3,5,6,9 },
            new int[] { 1,2,3,7,8,9 },
            new int[] { 4,5,6,7,8,9 }
        };
        int[][] cif = new int[][]
        {
            new int[] { 0,1,3,6 },
            new int[] { 0,2,4,7 },
            new int[] { 1,2,5,8 },
            new int[] { 3,4,5,9 },
            new int[] { 6,7,8,9 }
        };
        double[][] cn = new double[5][];
        for (int i = 0; i < 5; i++)
        {
            cn[i] = new double[4];
            Vec.scale(cn[i], vertex[4 - i], -1);
        }
        return new Geom.Shape(vertex, eiv, fiv, cie, cif, cn);
    }

    // 正八胞体の胞一つ
    public static Geom.Shape monoCell()
    {
        double[][] vertex = new double[][]
        {
            new double[] { 1, 1, 1, 1 },
            new double[] { 1, 1, -1, 1 },
            new double[] { 1, -1, -1, 1 },
            new double[] { 1, -1, 1, 1 },
            new double[] { -1, -1, 1, 1 },
            new double[] { -1, -1, -1, 1 },
            new double[] { -1, 1, -1, 1 },
            new double[] { -1, 1, 1, 1 },
        };
        int[][] eiv = new int[][]
        {
            new int[] {0,1 },
            new int[] {1,2 },
            new int[] {2,3 },
            new int[] {3,0 },
            new int[] {0,7 },
            new int[] {1,6 },
            new int[] {2,5 },
            new int[] {3,4 },
            new int[] {4,5 },
            new int[] {5,6 },
            new int[] {6,7 },
            new int[] {7,4 }
        };
        int[][] fiv = new int[][]
        {
            new int[] {0,1,2,3 },
            new int[] {0,1,6,7 },
            new int[] {1,2,5,6 },
            new int[] {2,3,4,5 },
            new int[] {3,0,7,4 },
            new int[] {4,5,6,7 }
        };
        int[][] cie = new int[][]
        {
            new int[] { 0,1,2,3,4,5,6,7,8,9,10,11 }
        };
        int[][] cif = new int[][]
        {
            new int[] {0,1,2,3,4,5 }
        };
        double[][] cn = new double[][]
        {
            new double[] {0,0,0,1 }
        };
        return new Geom.Shape(vertex, eiv, fiv, cie, cif, cn);
    }
}