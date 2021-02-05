using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Valve.VR;
using Valve.VR.InteractionSystem;
using System;
using SimpleFileBrowser;

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
    private double[] haptics;
    public HapticsTester hapticsTester;

    public Transform hand_;
    public SteamVR_Input_Sources hand;
    public SteamVR_Action_Boolean grab;
    public SteamVR_Action_Boolean menu;
    public SteamVR_Action_Pose pose;
    public GameObject menuPanel;

    public Hand laserHand;
    public GameObject laser;
    public Transform menuParent;
    public Transform head;

    private double[] eyeVector;
    private double[] cursor;
    private double[][] cursorAxis;

    private Vector3 reg1;
    private Vector3 reg2;
    private double[] relapos;
    private Quaternion relarot;

    private double[][] rotate; // 4DSoftware への出力。double[0]からdouble[1]、double[2]からdouble[3]への回転を意味する。

    public Slider faceSlider, cellSlider, retinaSlider, sizeSlider, borderSlider, offsetSlider;
    public Transform display;
    private bool wRotate, zRotate, edit, spin;

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
        cursor = new double[4];
        cursorAxis = new double[3][];
        for (int i = 0; i < cursorAxis.Length; i++) cursorAxis[i] = new double[3];
        wRotate = true;
        zRotate = true;

        menu.AddOnStateUpListener(ToggleMenu, hand);
    }

    private void ToggleMenu(SteamVR_Action_Boolean fromBoolean, SteamVR_Input_Sources fromSource)
    {
        if (menuPanel.activeSelf)
        {
            menuPanel.SetActive(false);
            laser.SetActive(false);
            laserHand.useRaycastHover = false;
        }
        else
        {
            menuPanel.SetActive(true);
            menuParent.LookAt(head);
            menuParent.LookAt(new Vector3(menuParent.forward.x, 0, menuParent.forward.z) + menuParent.position);
            laser.SetActive(true);
            laserHand.useRaycastHover = true;
        }
        GetComponent<BoxCollider>().enabled = !menuPanel.activeSelf;
    }

    void OnDestroy()
    {
        menu.RemoveOnStateUpListener(ToggleMenu, hand);
    }

    void Update()
    {
        calcInput();
        soft.Run(ref vertices, ref triangles, ref colors, ref haptics, rotate, 
            eyeVector, cursor, cursorAxis, grab.GetStateDown(hand) && edit, edit, spin); // ソフトの出力が vertices, triangles に収められる
        hapticsTester.draw(haptics);
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
                if (!zRotate) reg1.z = 0;
                for (int i = 0; i < 3; i++) relapos[i] = (double)reg1[i]; // double に変換
                double t = 2 * Math.PI * Vec.norm(relapos); // ノルムを定数倍して回転角とする
                Vec.normalize(relapos, relapos);
                rotate[1] = new double[] { -relapos[0] * Math.Sin(t), -relapos[1] * Math.Sin(t), -relapos[2] * Math.Sin(t), Math.Cos(t) };
                // relapos の向きへ、角度 t だけ回転（rotate[0] が奥向き（正）なのでマイナスを付ける）
            } else Vec.unitVector(rotate[1], 3);

            relarot = pose.GetLocalRotation(hand) * Quaternion.Inverse(pose.GetLastLocalRotation(hand)); // コントローラーの回転
            if (!zRotate) { relarot[0] = 0; relarot[1] = 0; }
            reg1.Set(relarot[0], relarot[1], relarot[2]); // 回転軸方向
            reg2.Set(1, 0, 0);
            Vector3.OrthoNormalize(ref reg1, ref reg2); // reg2 を回転軸と垂直に
            reg1 = relarot * reg2; // reg2 を reg1 に送る回転
            for (int i = 0; i < 3; i++) rotate[2][i] = reg2[i]; // double に変換
            for (int i = 0; i < 3; i++) rotate[3][i] = reg1[i];
            Vec.normalize(rotate[2], rotate[2]); // 誤差を補正
            Vec.normalize(rotate[3], rotate[3]);
        }
        else
        {
            Vec.unitVector(rotate[1], 3); // reg1 = reg2, reg3 = reg4 （回転を行わない）
            Vec.unitVector(rotate[2], 0);
            Vec.unitVector(rotate[3], 0);
        }

        reg1 = this.transform.position - head.position;
        for (int i = 0; i < 3; i++) eyeVector[i] = (double)reg1[i];
        Vec.normalize(eyeVector, eyeVector);

        reg1 = (pose.GetLocalPosition(hand) - this.transform.position) / 0.3f * 2f / display.localScale[0];
        for (int i = 0; i < 3; i++) cursor[i] = reg1[i];

        for (int i = 0; i < 3; i++) cursorAxis[0][i] = hand_.right[i];
        for (int i = 0; i < 3; i++) cursorAxis[1][i] = hand_.up[i];
        for (int i = 0; i < 3; i++) cursorAxis[2][i] = hand_.forward[i];
        /*relarot = Quaternion.Inverse(pose.GetLocalRotation(hand));
        for (int i = 0; i < 3; i++)
        {
            reg1.Set(0, 0, 0);
            reg1[i] = 1;
            reg1 = relarot * reg1;
            for (int j = 0; j < 3; j++) cursorAxis[i][j] = reg1[j];
        }    */
    }

    public void changeShape(int shapeNum) { soft.changeShape(shapeNum); }

    public void changeColor(int colorNum) { soft.changeColor(colorNum); }

    public void toggleDrawEdge() { soft.drEdge = !soft.drEdge; }

    public void toggleDrawFace() { soft.drFace = !soft.drFace; }

    public void toggleDrawCell() { soft.drCell = !soft.drCell; }

    public void changeFaceAlpha() { soft.changeFaceAlpha(faceSlider.value); }

    public void changeCellAlpha() { if (soft != null) soft.changeCellAlpha(cellSlider.value); }

    public void toggleWRotate() { wRotate = !wRotate; }

    public void toggleZRotate() { zRotate = !zRotate; }

    public void toggleEdit() { edit = !edit; }

    public void toggleSlice() { soft.sliceMode = !soft.sliceMode; }

    public void changeRetina() { soft.changeRetina(retinaSlider.value * 3); }

    public void changeSize() { float s = sizeSlider.value * 2 + 1; ; display.localScale = new Vector3(s, s, s); soft.size = s; }

    public void changeBorder() { if (soft != null) soft.changeBorder(borderSlider.value * 2 - 1); }

    public void changeOffset() { soft.changeOffset(offsetSlider.value); }

    public void save() { soft.save(); }

    public void toggleSpin() { spin = !spin; }

    public void toggleHaptics() { soft.hapActive = !soft.hapActive; hapticsTester.GetComponent<MeshRenderer>().enabled = soft.hapActive; }

    public void reset() { soft.reset(); }
}
