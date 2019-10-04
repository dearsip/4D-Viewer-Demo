﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class HapticsTester : MonoBehaviour
{
    private Mesh mesh;
    private Vector3[] vertices;
    private Vector3[] centers;
    private int[] triangles;
    private int hNum = FourDDemo.hNum;
    private int hNum2 = FourDDemo.hNum2;
    private int hNum3 = FourDDemo.hNum3;
    private float hNumh = FourDDemo.hNumh;

    // Start is called before the first frame update
    void Start()
    {
        mesh = new Mesh();
        vertices = new Vector3[hNum3 * 8];
        triangles = new int[hNum3 * 36];
        centers = new Vector3[hNum3];
        int[] cubeTriangles = new int[]
        {
            0, 2, 1,
            1, 2, 3,
            0, 1, 4,
            1, 5, 4,
            2, 6, 3,
            3, 6, 7,
            0, 4, 2,
            2, 4, 6,
            1, 3, 5,
            3, 7, 5,
            4, 5, 6,
            5, 7, 6,
        };
        for (int i = 0; i < hNum3; i++)
        {
            centers[i] = new Vector3();
            centers[i].Set(i % hNum - hNumh, i / hNum % hNum - hNumh, i / hNum2 - hNumh);
            centers[i] *= 1 / hNumh;
            for (int j = 0; j < 8; j++) vertices[8 * i + j].Set(centers[i].x, centers[i].y, centers[i].z);

            Array.ConstrainedCopy(cubeTriangles, 0, triangles, 36 * i, 36);
            for (int j = 0; j < 36; j++) cubeTriangles[j] += 8;
        }
        mesh.vertices = vertices;
        mesh.triangles = triangles;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void draw(double[] haptics)
    {
        for (int i = 0; i < hNum3; i++)
            for (int j = 0; j < 8; j++)
                vertices[8 * i + j].Set(centers[i].x + (0.3f + (float)haptics[i]) / 4 * (j % 2 * 2 - 1) / hNumh,
                                        centers[i].y + (0.3f + (float)haptics[i]) / 4 * (j / 2 % 2 * 2 - 1) / hNumh,
                                        centers[i].z + (0.3f + (float)haptics[i]) / 4 * (j / 4 * 2 - 1) / hNumh);
        mesh.vertices = vertices;

        mesh.RecalculateNormals();
        GetComponent<MeshFilter>().sharedMesh = mesh;
    }
}