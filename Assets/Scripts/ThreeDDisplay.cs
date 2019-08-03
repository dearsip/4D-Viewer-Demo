using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Valve.VR;
using Valve.VR.InteractionSystem;
using System;

// 毎フレーム、コントローラーからの入力を計算して FourDDemo に投げ、返された mesh の情報を適用する。
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(Interactable))]
public class ThreeDDisplay : MonoBehaviour
{
    private FourDDemo soft;
    private Mesh mesh;
    private Vector3[] vertices;
    private int[] triangles;
    private Color[] colors;
    
    public SteamVR_Input_Sources hand;
    public SteamVR_Action_Boolean grab;
    public SteamVR_Action_Pose pose;

    public Player player;
    private double[] eyeVector;

    private Vector3 reg1;
    private Vector3 reg2;
    private double[] relapos;
    private Quaternion relarot;

    private double[][] rotate; // 4DSoftware への出力。double[0]からdouble[1]、double[2]からdouble[3]への回転を意味する。

    public Slider faceSlider, cellSlider, retinaSlider;
    public bool wRotate;

    // Start is called before the first frame update
    void Start()
    {
        mesh = new Mesh();
        soft = new FourDDemo();
        relapos = new double[3];
        rotate = new double[4][];
        rotate[0] = new double[] { 0, 0, 0, 1 };
        rotate[1] = new double[4];
        rotate[2] = new double[] { 1, 0, 0, 0 };
        rotate[3] = new double[] { 1, 0, 0, 0 };
        reg2 = new Vector3(0, 0, 0);
        eyeVector = new double[3];
        wRotate = true;
    }
    
    void Update()
    {
        calcInput();
        soft.Run(ref vertices, ref triangles, ref colors, rotate, eyeVector); // ソフトの出力が vertices, triangles に収められる
        if (vertices.Length < mesh.vertices.Length) // triangles の参照する項が vertices から消えるとエラーを吐くため注意する
        {
            mesh.triangles = triangles;
            mesh.vertices = vertices;
        }
        else
        {
            mesh.vertices = vertices;
            mesh.triangles = triangles;
        }
        mesh.colors = colors;
        mesh.RecalculateNormals();
        GetComponent<MeshFilter>().sharedMesh = mesh;
    }

    // コントローラーの情報を回転に変換する。
    private void calcInput()
    {
        if (grab.GetState(hand) && GetComponent<Interactable>().isHovering)
        {
            if (wRotate)
            {
                reg1 = pose.GetLocalPosition(hand) - pose.GetLastLocalPosition(hand); // コントローラーの移動距離 (Vector3)
                for (int i = 0; i < 3; i++) relapos[i] = (double)reg1[i]; // double に変換
                double t = 2 * Math.PI * Vec.norm(relapos); // ノルムを定数倍して回転角とする
                Vec.normalize(relapos, relapos);
                rotate[1] = new double[] { -relapos[0] * Math.Sin(t), -relapos[1] * Math.Sin(t), -relapos[2] * Math.Sin(t), Math.Cos(t) };
                // relapos の向きへ、角度 t だけ回転（rotate[0] が奥向き（正）なのでマイナスを付ける）
            } else Vec.unitVector(rotate[1], 3);

            relarot = pose.GetLocalRotation(hand) * Quaternion.Inverse(pose.GetLastLocalRotation(hand)); // コントローラーの回転
            reg1.Set(relarot[0], relarot[1], relarot[2]); // 回転軸方向
            reg2.Set(1, 0, 0);
            Vector3.OrthoNormalize(ref reg1, ref reg2); // reg2 を回転軸と垂直に
            reg1 = relarot * reg2; // reg2 を reg1 に送る回転
            for (int i = 0; i < 3; i++) rotate[2][i] = (double)reg2[i]; // double に変換
            for (int i = 0; i < 3; i++) rotate[3][i] = (double)reg1[i];
            Vec.normalize(rotate[2], rotate[2]); // 誤差を補正
            Vec.normalize(rotate[3], rotate[3]);
        }
        else
        {
            Vec.unitVector(rotate[1], 3); // reg1 = reg2, reg3 = reg4 （回転を行わない）
            Vec.unitVector(rotate[2], 0);
            Vec.unitVector(rotate[3], 0);
        }

        reg1 = this.transform.position - player.hmdTransform.position;
        for (int i = 0; i < 3; i++) eyeVector[i] = (double)reg1[i];
        Vec.normalize(eyeVector, eyeVector);
    }

    public void changeShape(int shapeNum) { soft.changeShape(shapeNum); }

    public void changeColor(int colorNum) { soft.changeColor(colorNum); }

    public void changeFaceAlpha() { soft.changeFaceAlpha(faceSlider.value); }

    public void changeCellAlpha() { soft.changeCellAlpha(cellSlider.value); }

    public void toggleWRotate() { wRotate = !wRotate; }

    public void changeRetina() { soft.changeRetina((double)retinaSlider.value * 3); }
}
