using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

// Shape を返すメソッド。
public class Shapes
{
    public static Color defc = new Color(0.5f, 0.5f, 1.0f, 0.25f);

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
        Color[] color = new Color[5];
        for (int i = 0; i < 5; i++)
        {
            cn[i] = new double[4];
            Vec.scale(cn[i], vertex[4 - i], -1);
            color[i] = defc;
        }
        return new Geom.Shape(vertex, eiv, fiv, cie, cif, cn, color);
    }

    // 正八胞体の胞一つ
    public static Geom.Shape monoCell()
    {
        double[][] vertex = new double[][]
        {
            new double[] {  1,  1,  1,  1 },
            new double[] {  1,  1, -1,  1 },
            new double[] {  1, -1, -1,  1 },
            new double[] {  1, -1,  1,  1 },
            new double[] { -1, -1,  1,  1 },
            new double[] { -1, -1, -1,  1 },
            new double[] { -1,  1, -1,  1 },
            new double[] { -1,  1,  1,  1 },
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
        Color[] color = new Color[]
        {
            defc
        };
        return new Geom.Shape(vertex, eiv, fiv, cie, cif, cn, color);
    }

    // 正八胞体
    public static Geom.Shape superCell()
    {
        double[][] vertex = new double[][]
        {
            new double[] {  1,  1,  1,  1 },
            new double[] {  1,  1, -1,  1 },
            new double[] {  1, -1, -1,  1 },
            new double[] {  1, -1,  1,  1 },
            new double[] { -1, -1,  1,  1 },
            new double[] { -1, -1, -1,  1 },
            new double[] { -1,  1, -1,  1 },
            new double[] { -1,  1,  1,  1 },
            new double[] {  1,  1,  1, -1 },
            new double[] {  1,  1, -1, -1 },
            new double[] {  1, -1, -1, -1 },
            new double[] {  1, -1,  1, -1 },
            new double[] { -1, -1,  1, -1 },
            new double[] { -1, -1, -1, -1 },
            new double[] { -1,  1, -1, -1 },
            new double[] { -1,  1,  1, -1 }
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
            new int[] {7,4 },

            new int[] {0,8 },
            new int[] {1,9 },
            new int[] {2,10 },
            new int[] {3,11 },
            new int[] {4,12 },
            new int[] {5,13 },
            new int[] {6,14 },
            new int[] {7,15 },

            new int[] {8,9 },
            new int[] {9,10 },
            new int[] {10,11 },
            new int[] {11,8 },
            new int[] {8,15 },
            new int[] {9,14 },
            new int[] {10,13 },
            new int[] {11,12 },
            new int[] {12,13 },
            new int[] {13,14 },
            new int[] {14,15 },
            new int[] {15,12 }
        };
        int[][] fiv = new int[][]
        {
            new int[] {0,1,2,3 },
            new int[] {0,1,6,7 },
            new int[] {1,2,5,6 },
            new int[] {2,3,4,5 },
            new int[] {3,0,7,4 },
            new int[] {4,5,6,7 },

            new int[] {0,1,9,8 },
            new int[] {1,2,10,9 },
            new int[] {2,3,11,10 },
            new int[] {3,0,8,11 },
            new int[] {0,7,15,8 },
            new int[] {1,6,14,9 },
            new int[] {2,5,13,10 },
            new int[] {3,4,12,11 },
            new int[] {4,5,13,12 },
            new int[] {5,6,14,13 },
            new int[] {6,7,15,14 },
            new int[] {7,4,12,15 },

            new int[] {8,9,10,11 },
            new int[] {8,9,14,15 },
            new int[] {9,10,13,14 },
            new int[] {10,11,12,13 },
            new int[] {11,8,15,12 },
            new int[] {12,13,14,15 }
        };
        int[][] cie = new int[][]
        {
            new int[] { 0,1,2,3,4,5,6,7,8,9,10,11 },
            new int[] { 20,21,22,23,24,25,26,27,28,29,30,31 },
            new int[] { 0,1,2,3,12,13,14,15,20,21,22,23 },
            new int[] { 8,9,10,11,16,17,18,19,28,29,30,31 },
            new int[] { 0,4,5,10,12,13,18,19,20,24,25,30 },
            new int[] { 2,6,7,8,14,15,16,17,22,26,27,27 },
            new int[] { 3,4,7,11,12,15,16,19,23,24,27,31 },
            new int[] { 1,5,6,9,13,14,17,18,21,25,26,29 }
        };
        int[][] cif = new int[][]
        {
            new int[] {0,1,2,3,4,5 },
            new int[] {18,19,20,21,22,23 },
            new int[] {0,6,7,8,9,18 },
            new int[] {5,14,15,16,17,23 },
            new int[] {1,6,10,11,16,19 },
            new int[] {3,8,12,13,14,21 },
            new int[] {4,9,10,13,17,22 },
            new int[] {2,7,11,12,15,20 }
        };
        double[][] cn = new double[][]
        {
            new double[] {0,0,0,1 },
            new double[] {0,0,0,-1 },
            new double[] {1,0,0,0 },
            new double[] {-1,0,0,0 },
            new double[] {0,1,0,0 },
            new double[] {0,-1,0,0 },
            new double[] {0,0,1,0 },
            new double[] {0,0,-1,0 }
        };
        Color[] color = new Color[]
        {
            new Color( 1.0f, 1.0f, 1.0f, 0.3f ),
            new Color( 0.5f, 0.5f, 0.5f, 0.3f ),
            new Color( 1.0f, 1.0f, 0.0f, 0.3f ),
            new Color( 0.0f, 0.0f, 1.0f, 0.3f ),
            new Color( 1.0f, 0.0f, 1.0f, 0.3f ),
            new Color( 0.0f, 1.0f, 0.0f, 0.3f ),
            new Color( 0.0f, 1.0f, 1.0f, 0.3f ),
            new Color( 1.0f, 0.0f, 0.0f, 0.3f ),
        };
        return new Geom.Shape(vertex, eiv, fiv, cie, cif, cn, color);
    }

    // 正八胞体（配色２）
    public static Geom.Shape torCell()
    {
        Geom.Shape shape = superCell();
        for (int i = 0; i < 4; i++)
        {
            shape.cell[i].color = new Color(1.0f, 0.0f, 0.0f, 0.3f);
            shape.cell[i + 4].color = new Color(0.0f, 1.0f, 1.0f, 0.3f);
        }
        return shape;
    }

    // 正十六胞体
    public static Geom.Shape hexadecachoron()
    {
        double[][] vertex = new double[][]
        {
            new double[] { 1, 0, 0, 0 },
            new double[] { -1, 0, 0, 0 },
            new double[] { 0, 1, 0, 0 },
            new double[] { 0, -1, 0, 0 },
            new double[] { 0, 0, 1, 0 },
            new double[] { 0, 0, -1, 0 },
            new double[] { 0, 0, 0, 1 },
            new double[] { 0, 0, 0, -1 },
        };
        int[][] eiv = new int[][] 
        {
            new int[] { 0,2 },
            new int[] { 0,3 },
            new int[] { 0,4 },
            new int[] { 0,5 },
            new int[] { 0,6 },
            new int[] { 0,7 },
            new int[] { 1,2 },
            new int[] { 1,3 },
            new int[] { 1,4 },
            new int[] { 1,5 },
            new int[] { 1,6 },
            new int[] { 1,7 },
            new int[] { 2,4 },
            new int[] { 2,5 },
            new int[] { 2,6 },
            new int[] { 2,7 },
            new int[] { 3,4 },
            new int[] { 3,5 },
            new int[] { 3,6 },
            new int[] { 3,7 },
            new int[] { 4,6 },
            new int[] { 4,7 },
            new int[] { 5,6 },
            new int[] { 5,7 }
        };
        int[][] fiv = new int[][] 
        {
            new int[] { 0,2,4 }, //0
            new int[] { 0,2,6 }, //1
            new int[] { 0,4,6 }, //2
            new int[] { 2,4,6 }, //3

            //new int[] { 0,2,4 }, 0
            new int[] { 0,2,7 }, //4
            new int[] { 0,4,7 }, //5
            new int[] { 2,4,7 }, //6

            new int[] { 0,2,5 }, //7
            //new int[] { 0,2,6 }, 1
            new int[] { 0,5,6 }, //8
            new int[] { 2,5,6 }, //9

            //new int[] { 0,2,5 }, 7
            //new int[] { 0,2,7 }, 4
            new int[] { 0,5,7 }, //10
            new int[] { 2,5,7 }, //11

            new int[] { 0,3,4 }, //12
            new int[] { 0,3,6 }, //13
            //new int[] { 0,4,6 }, 2
            new int[] { 3,4,6 }, //14

            //new int[] { 0,3,4 }, 12
            new int[] { 0,3,7 }, //15
            //new int[] { 0,4,7 }, 5
            new int[] { 3,4,7 }, //16

            new int[] { 0,3,5 }, //17
            //new int[] { 0,3,6 }, 13
            //new int[] { 0,5,6 }, 8
            new int[] { 3,5,6 }, //18

            //new int[] { 0,3,5 }, 17
            //new int[] { 0,3,7 }, 15
            //new int[] { 0,5,7 }, 10
            new int[] { 3,5,7 }, //19

            new int[] { 1,2,4 }, //20
            new int[] { 1,2,6 }, //21
            new int[] { 1,4,6 }, //22
            //new int[] { 2,4,6 }, 3
            
            //new int[] { 1,2,4 }, 20
            new int[] { 1,2,7 }, //23
            new int[] { 1,4,7 }, //24
            //new int[] { 2,4,7 }, 6

            new int[] { 1,2,5 }, //25
            //new int[] { 1,2,6 }, 21
            new int[] { 1,5,6 }, //26
            //new int[] { 2,5,6 }, 9

            //new int[] { 1,2,5 }, 25
            //new int[] { 1,2,7 }, 23
            new int[] { 1,5,7 }, //27
            //new int[] { 2,5,7 }, 11

            new int[] { 1,3,4 }, //28
            new int[] { 1,3,6 }, //29
            //new int[] { 1,4,6 }, 22
            //new int[] { 3,4,6 }, 14
            
            //new int[] { 1,3,4 }, 28
            new int[] { 1,3,7 }, //30
            //new int[] { 1,4,7 }, 24
            //new int[] { 3,4,7 }, 16

            new int[] { 1,3,5 }, //31
            //new int[] { 1,3,6 }, 29
            //new int[] { 1,5,6 }, 26
            //new int[] { 3,5,6 }, 18

            //new int[] { 1,3,5 }, 31
            //new int[] { 1,3,7 }, 30
            //new int[] { 1,5,7 }, 27
            //new int[] { 3,5,7 }  19
        };
        int[][] cie = new int[][]
        {
            new int[] { 0,2,4,12,14,20 },
            new int[] { 0,2,5,12,15,21 },
            new int[] { 0,3,4,13,14,22 },
            new int[] { 0,3,5,13,15,23 },
            new int[] { 1,2,4,16,18,20 },
            new int[] { 1,2,5,16,19,21 },
            new int[] { 1,3,4,17,18,22 },
            new int[] { 1,3,5,17,19,23 },
            new int[] { 6,8,10,12,14,20 },
            new int[] { 6,8,11,12,15,21 },
            new int[] { 6,9,10,13,14,22 },
            new int[] { 6,9,11,13,15,23 },
            new int[] { 7,8,10,16,18,20 },
            new int[] { 7,8,11,16,19,21 },
            new int[] { 7,9,10,17,18,22 },
            new int[] { 7,9,11,17,19,23 }
        };
        int[][] cif = new int[][]
        {
            new int[] { 0,1,2,3 },
            new int[] { 0,4,5,6 },
            new int[] { 7,1,8,9 },
            new int[] { 7,4,10,11 },
            new int[] { 12,13,2,14 },
            new int[] { 12,15,5,16 },
            new int[] { 17,13,8,18 },
            new int[] { 17,15,10,19 },
            new int[] { 20,21,22,3 },
            new int[] { 20,23,24,6 },
            new int[] { 25,21,26,9 },
            new int[] { 25,23,27,11 },
            new int[] { 28,29,22,14 },
            new int[] { 28,30,24,16 },
            new int[] { 31,29,26,18 },
            new int[] { 31,30,27,19 }
        };
        double[][] cn = new double[][]
        {
            new double[] {  1,  1,  1,  1 },
            new double[] {  1,  1,  1, -1 },
            new double[] {  1,  1, -1,  1 },
            new double[] {  1,  1, -1, -1 },
            new double[] {  1, -1,  1,  1 },
            new double[] {  1, -1,  1, -1 },
            new double[] {  1, -1, -1,  1 },
            new double[] {  1, -1, -1, -1 },
            new double[] { -1,  1,  1,  1 },
            new double[] { -1,  1,  1, -1 },
            new double[] { -1,  1, -1,  1 },
            new double[] { -1,  1, -1, -1 },
            new double[] { -1, -1,  1,  1 },
            new double[] { -1, -1,  1, -1 },
            new double[] { -1, -1, -1,  1 },
            new double[] { -1, -1, -1, -1 }
        };
        Color[] color = new Color[]
        {
            new Color(1.0f, 1.0f, 1.0f, 0.3f),
            new Color(1.0f, 0.5f, 0.5f, 0.3f),
            new Color(0.5f, 1.0f, 0.5f, 0.3f),
            new Color(1.0f, 1.0f, 0.0f, 0.3f),
            new Color(0.5f, 0.5f, 1.0f, 0.3f),
            new Color(1.0f, 0.0f, 1.0f, 0.3f),
            new Color(0.0f, 1.0f, 1.0f, 0.3f),
            new Color(0.6f, 0.6f, 0.6f, 0.3f),
            new Color(0.8f, 0.8f, 0.8f, 0.3f),
            new Color(1.0f, 0.0f, 0.0f, 0.3f),
            new Color(0.0f, 1.0f, 0.0f, 0.3f),
            new Color(0.0f, 0.0f, 0.5f, 0.3f),
            new Color(0.0f, 0.0f, 1.0f, 0.3f),
            new Color(0.0f, 0.5f, 0.0f, 0.3f),
            new Color(0.5f, 0.0f, 0.0f, 0.3f),
            new Color(0.4f, 0.4f, 0.4f, 0.3f),
        };
        return new Geom.Shape(vertex, eiv, fiv, cie, cif, cn, color);
    }

    // 正十六胞体（配色２）
    public static Geom.Shape hd2()
    {
        Geom.Shape shape = hexadecachoron();
        for (int i = 0; i < 16; i++)
        {
            shape.cell[i].color = (((i + i/2 + i/4 + i/8) & 1) == 0) ? 
                new Color(1.0f, 0.0f, 0.0f, 0.3f) : new Color(0.0f, 1.0f, 1.0f, 0.3f);
        }
        return shape;
    }
}