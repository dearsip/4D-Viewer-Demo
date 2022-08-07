﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Valve.VR;
using Valve.VR.InteractionSystem;
using System;
using System.IO;
using SimpleFileBrowser;
using static FourDDemo;
using WebSocketSharp;
using System.Threading.Tasks;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(Interactable))]
public class Core : MonoBehaviour
{
    public delegate void Command();
    private Options optDefault;
    private Options opt; // the next three are used only during load
    private int dim;
    private string gameDirectory;
    //private string imageDirectory;
    private string reloadFile;

    private OptionsAll oa;
    private Engine engine;

    private Mesh mesh;

    private bool engineAlignMode;
    private bool active, excluded;
    private int[] param;
    private double timeMove, timeRotate, timeAlignMove, timeAlignRotate;
    private int nMove, nRotate, nAlignMove, nAlignRotate;
    private double dMove, dRotate, dAlignedMove, dAlignedRotate, dAlignMove, dAlignRotate;
    private bool alwaysRun;
    private IMove target;
    private double[] saveOrigin;
    private double[][] saveAxis;
    public bool alignMode;
    private int ad0, ad1;
    private int nActive;
    private Align alignActive;

    private int interval;

    public Command command;
    public Command menuCommand;
    public SteamVR_Action_Boolean trigger, move, menu;
    public SteamVR_Action_Pose pose;
    public SteamVR_Input_Sources left, right;
    private Vector3 posLeft, lastPosLeft, fromPosLeft, posRight, lastPosRight, fromPosRight, dlPosLeft, dfPosLeft, dlPosRight, dfPosRight;
    private Quaternion rotLeft, lastRotLeft, fromRotLeft, rotRight, lastRotRight, fromRotRight, dlRotLeft, dfRotLeft, dlRotRight, dfRotRight, relarot;
    private bool leftTrigger, rightTrigger, lastLeftTrigger, lastRightTrigger, leftTriggerPressed, rightTriggerPressed,
        leftMove, rightMove;
    public Menu menuPanel;

    private Vector3 reg0, reg1;
    private double[] reg2, reg3, reg4, reg5, reg6;
    public Player player;
    public Camera fixedCamera;
    public SteamVR_Action_Boolean headsetOnHead = SteamVR_Input.GetBooleanAction("HeadsetOnHead");
    private double[] eyeVector;
    private double[] cursor;
    private double[][] cursorAxis;

    private bool error = true; // 振動装置と接続しているときはfalseにする
    private string adrr = "ws://172.20.10.6:9999";
    public bool hapActive;
    private double[] haptics;
    private bool[] cutting; // 手の形を調べる v手の形
    private float[] output;
    private static int opFrame = 20; // 出力フレーム
    private int opf;
    private double max_;
    private WebSocket ws;
    public HapticsTester hapticsTester;

    // --- option accessors ---

    // some of these also implement IOptions

    private OptionsMap om()
    {
        // omCurrent is always non-null, so can be used directly
        return /*(dim == 3) ? oa.opt.om3 :*/ oa.opt.om4;
    }

    public OptionsColor oc()
    {
        if (oa.ocCurrent != null) return oa.ocCurrent;
        return /*(dim == 3) ? oa.opt.oc3 :*/ oa.opt.oc4;
    }

    public OptionsView ov()
    {
        if (oa.ovCurrent != null) return oa.ovCurrent;
        return /*(dim == 3) ? oa.opt.ov3 :*/ oa.opt.ov4;
    }

    //public OptionsStereo os()
    //{
    //    return oa.opt.os;
    //}

    //private OptionsKeys ok()
    //{
    //    return (dim == 3) ? oa.opt.ok3 : oa.opt.ok4;
    //}

    private OptionsMotion ot()
    {
        return /*(dim == 3) ? oa.opt.ot3 :*/ oa.opt.ot4;
    }

    //public OptionsImage oi()
    //{
    //    return oa.opt.oi;
    //}

    //private KeyMapper keyMapper()
    //{
    //    return (dim == 3) ? keyMapper3 : keyMapper4;
    //}

    public int getSaveType()
    {
        return engine.getSaveType();
    }

    public void saveMaze(IStore store) {

      store.putString(KEY_CHECK,VALUE_CHECK);

      store.putInteger(KEY_DIM,dim);
      store.putObject(KEY_OPTIONS_MAP,oa.omCurrent);
      store.putObject(KEY_OPTIONS_COLOR,oc()); // ocCurrent may be null
      store.putObject(KEY_OPTIONS_VIEW,ov());  // ditto
      store.putObject(KEY_OPTIONS_SEED,oa.oeCurrent);
      store.putBool(KEY_ALIGN_MODE,alignMode);

      engine.save(store,om());
   }

   // Start is called before the first frame update
    void Start() // ルーチン開始
    {
        SteamVR_Actions.control.Activate(left);
        SteamVR_Actions.control.Activate(right);

        addEvevts();

        posLeft = pose.GetLocalPosition(left); rotLeft = pose.GetLocalRotation(left);
        posRight = pose.GetLocalPosition(right); rotRight = pose.GetLocalRotation(right);

        optDefault = ScriptableObject.CreateInstance<Options>();
        opt = ScriptableObject.CreateInstance<Options>();
        // ロード
        doInit();
        // dim and rest of oa are initialized when new game started

        oa = new OptionsAll();
        oa.opt = opt;
        oa.omCurrent = new OptionsMap(0); // blank for copying into
        oa.oeNext = new OptionsSeed();

        eyeVector = new double[3];
        mesh = new Mesh();
        GetComponent<MeshFilter>().sharedMesh = mesh;
        engine = new Engine(mesh);

        //initHaptics();

        newGame(dim);
        active = true;

        reg2 = new double[3];
        reg3 = new double[4];
        reg4 = new double[4];
        reg5 = new double[3];
        reg6 = new double[4];

        FileBrowser.HideDialog();
        menuPanel.gameObject.SetActive(false);
    }

    private void addEvevts()
    {
        move.AddOnStateDownListener(LeftDown, left);
        move.AddOnStateDownListener(RightDown, right);
        menu.AddOnStateUpListener(OpenMenu_, left);
        menu.AddOnStateUpListener(OpenMenu_, right);
        trigger.AddOnStateDownListener(RightClick, right);
    }

    private void LeftDown(SteamVR_Action_Boolean fromBoolean, SteamVR_Input_Sources fromSource)
    {
        posLeft = fromPosLeft = pose.GetLocalPosition(left);
        rotLeft = fromRotLeft = pose.GetLocalRotation(left);
    }

    private void RightDown(SteamVR_Action_Boolean fromBoolean, SteamVR_Input_Sources fromSource)
    {
        posRight = fromPosRight = pose.GetLocalPosition(right);
        rotRight = fromRotRight = pose.GetLocalRotation(right);
    }

    private void OpenMenu_(SteamVR_Action_Boolean fromBoolean, SteamVR_Input_Sources fromSource)
    {
        openMenu();
    }

    private void RightClick(SteamVR_Action_Boolean fromBoolean, SteamVR_Input_Sources fromSource)
    {
        if (engine.getSaveType() == IModel.SAVE_GEOM
         || engine.getSaveType() == IModel.SAVE_NONE)
        {
            command = click;
        }
    }

    private void OnDestroy()
    {
        move.RemoveOnStateDownListener(LeftDown, left);
        move.RemoveOnStateDownListener(RightDown, right);
        menu.RemoveOnStateUpListener(OpenMenu_, left);
        menu.RemoveOnStateUpListener(OpenMenu_, right);
        trigger.RemoveOnStateDownListener(RightClick, right);
    }

    private void initHaptics()
    {
        hapActive = true;
        cursor = new double[4];
        cursorAxis = new double[3][];
        for (int i = 0; i < cursorAxis.Length; i++) cursorAxis[i] = new double[3];
        haptics = new double[hNum3];
        for (int i = 0; i < hNum3; i++) if (!cut[i]) haptics[i] = 0;
        cutting = new bool[hNum3];
        for (int i = 0; i < hNum3; i++) cutting[i] = !cut[i];
        output = new float[outputPlc.Length];
        if (!error)
        {
            ws = new WebSocket(adrr);
            ws.OnMessage += (object sender, MessageEventArgs e) =>
            {
                Debug.Log(e.Data);
            };
            ws.Connect();
        }
    }

    private void openMenu()
    {
        SteamVR_Actions.control.Deactivate(left);
        SteamVR_Actions.control.Deactivate(right);
        menuPanel.Activate(oa);
    }

    public void newGame()
    {
        newGame(0);
    }

    private void newGame(int dim)
    {
        if (dim != 0) this.dim = dim; // allow zero to mean "keep the same"

        OptionsMap.copy(oa.omCurrent, om());
        oa.ocCurrent = null; // use standard colors for dimension
        oa.ovCurrent = null; // ditto
        oa.oeCurrent = oa.oeNext;
        oa.oeCurrent.forceSpecified();
        oa.oeNext = new OptionsSeed();

        IModel model = new MapModel(this.dim, oa.omCurrent, oc(), oa.oeCurrent, ov(), null);
        engine.newGame(this.dim, model, ov(), /*oa.opt.os,*/ ot(), true);

        updateOptions();
        setOptions();

        target = engine;
        command = null;
        saveOrigin = new double[this.dim];
        saveAxis = new double[this.dim][];
        for (int i = 0; i < this.dim; i++) saveAxis[i] = new double[this.dim];
    }

    Task renderTask = Task.CompletedTask;
    float now = 0;
    float last = 0;
    float dTime = 0;
    float fps = 60;
    bool nextFrame = true;
    int frameCount = 0;
    void Update()
    {
        if (renderTask.IsCompleted) {
            frameCount++;
            now = Time.realtimeSinceStartup;
            float dTime = now - last;
            if (dTime >= 1) {
                fps = frameCount / dTime;
                frameCount = 0;
                last = now;
                Debug.Log(fps);
            }

            engine.ApplyMesh();
            nextFrame = false;
            calcInput();
            menuCommand?.Invoke();
            menuCommand = null;
            control(fps);
            try {renderTask = Task.Run(() => engine.renderAbsolute(eyeVector, opt.oo));} catch (Exception e) {Debug.Log(e);}
            //doHaptics();
        }
    }

    private void doHaptics()
    {
        if (hapActive) calcHaptics(cursor, cursorAxis);
        else Vec.zero(haptics);
        for (int i = 0; i < output.Length; i++) output[i] = (haptics[i] == 0) ? 0 : Mathf.Min((float)(0.4 + haptics[i] / 1.7 /*実測したおよその最大値*/ * 0.6 /* ある程度の電圧がないと振動しない */ ) , 1f);
        if (!error && (opf = ++opf % opFrame) == 0)
        {
            try {
                ws.Send(Vec.ToString(output));
            } catch (InvalidOperationException e)
            {
                Debug.Log(e);
                error = true;
            }
        }
        hapticsTester.draw(haptics);
    }

    private List<Vector3> verts;
    private List<int> tris;
    private List<Color> cols;
    private void calcHaptics(double[] cursor, double[][] cursorAxis)
    {
        verts = new List<Vector3>(mesh.vertices);
        tris = new List<int>(mesh.triangles);
        cols = new List<Color>(mesh.colors);
        int count = mesh.vertices.Length;
        for (int i = 0; i < haptics.Length; i++) if (cut[i])
        {
            reg2[0] = i % hNum - hNumh; // 立方体形に配置
            reg2[1] = i / hNum % hNum - hNumh;
            reg2[2] = i / hNum2 - hNumh;
            Vec.scale(reg2, reg2, 0.2 / hNumh / opt.ov4.scale); // 解像度・画面サイズに合わせて縮小
            reg2[0] = reg2[0] + 0.07 / opt.ov4.scale; // 手の位置へ平行移動
            reg2[1] = reg2[1] + 0.08 / opt.ov4.scale;
            reg2[2] = reg2[2] - 0.12 / opt.ov4.scale;
            Vec.fromAxisCoordinates(reg5, reg2, cursorAxis); // 向きを変更
            for (int j = 0; j < 3; j++) reg4[j] = reg5[j]; // reg4[3] (= 0) は編集されない
            if (i == 0) Debug.Log("corner: " + Vec.ToString(reg4));
            Vec.add(reg4, cursor, reg4);
            //verts.Add(new Vector3((float)reg4[0], (float)reg4[1], (float)reg4[2]));
            //verts.Add(new Vector3((float)reg4[0]+0.03f, (float)reg4[1], (float)reg4[2]));
            //verts.Add(new Vector3((float)reg4[0], (float)reg4[1]+0.03f, (float)reg4[2]));
            //    tris.Add(count++);
            //    tris.Add(count++);
            //    tris.Add(count++);
            //    cols.Add(Color.white);
            //    cols.Add(Color.white);
            //    cols.Add(Color.white);
            if (Vec.max(reg4) <= 1 && Vec.min(reg4) >= -1) haptics[i] = engine.retrieveModel().touch(reg4);
            else haptics[i] = 1;
            haptics[i] = 1 - haptics[i]; // 近いほど大きく
        }
        //if (verts.Count < mesh.vertices.Length) // triangles の参照する項が vertices から消えるとエラーを吐くため注意する
        //{
        //    mesh.triangles = tris.ToArray();
        //    mesh.vertices = verts.ToArray();
        //}
        //else
        //{
        //    mesh.vertices = verts.ToArray();
        //    mesh.triangles = tris.ToArray();
        //}
        //mesh.colors = cols.ToArray();
        double max = Vec.max(haptics); // 最も近い点
        if (max > 0) for (int i = 0; i < hNum3; i++) if (cut[i]) haptics[i] = Math.Max((haptics[i] - max + 0.00005) / 0.00005, 0); // 上限を設定
        max = 0; // 総和が小さい->一部しか触れていない->高圧力と考える
        int touch = 0;
        for (int i = 0; i < hNum3; i++)
        {
            if (haptics[i] != 0)
            {
                max += haptics[i];
                touch++;
                cutting[i] = true;
            }
        }
        if (touch > 0) for (int i = 0; i < hNum3; i++) haptics[i] *= ( 2 * touch - max) / touch;
        //max_ = Math.Max(Vec.max(haptics), max_);
        //Debug.Log(max_);
    }

    private void calcInput()
    {
        relarot = Quaternion.Inverse(transform.rotation);
        lastPosLeft = posLeft; lastPosRight = posRight;
        lastRotLeft = rotLeft; lastRotRight = rotRight;
        posLeft = pose.GetLocalPosition(left); posRight = pose.GetLocalPosition(right);
        rotLeft = pose.GetLocalRotation(left); rotRight = pose.GetLocalRotation(right);
        dlPosLeft = relarot * (posLeft - lastPosLeft); dlPosRight = relarot * (posRight - lastPosRight);
        dfPosLeft = relarot * (posLeft - fromPosLeft); dfPosRight = relarot * (posRight - fromPosRight);
        Quaternion lRel = Quaternion.Inverse(fromRotLeft);
        dlRotLeft = lRel * rotLeft * Quaternion.Inverse(lRel * lastRotLeft);
        dlRotRight = relarot * rotRight * Quaternion.Inverse(relarot * lastRotRight);
        dfRotLeft = lRel * rotLeft * Quaternion.Inverse(lRel * fromRotLeft);
        dfRotRight = relarot * rotRight * Quaternion.Inverse(relarot * fromRotRight);
        lastLeftTrigger = leftTrigger; lastRightTrigger = rightTrigger;
        leftTrigger = trigger.GetState(left); rightTrigger = trigger.GetState(right);

        leftMove = move.GetState(left); rightMove = move.GetState(right);
        reg1 = relarot * 
               (transform.position - ((headsetOnHead.GetState(SteamVR_Input_Sources.Head)) ? 
                player.hmdTransform.position : fixedCamera.transform.position));
        for (int i = 0; i < 3; i++) eyeVector[i] = reg1[i];
        Vec.normalize(eyeVector, eyeVector);

        //reg1 = (pose.GetLocalPosition(right) - transform.position) / 0.3f * 1.2f / (float)opt.ov4.scale;
        //for (int i = 0; i < 3; i++) cursor[i] = reg1[i];
        //reg1 = pose.GetLocalRotation(right)*Vector3.right;
        //for (int i = 0; i < 3; i++) cursorAxis[0][i] = reg1[i];
        //reg1 = pose.GetLocalRotation(right)*Vector3.up;
        //for (int i = 0; i < 3; i++) cursorAxis[1][i] = reg1[i];
        //reg1 = pose.GetLocalRotation(right)*Vector3.forward;
        //for (int i = 0; i < 3; i++) cursorAxis[2][i] = reg1[i];
    }

    private double tAlign = 0.5;
    private double limitAng = 30;
    private double limitAngRoll = 30;
    private double limitAngForward = 30;
    private double maxAng = 60;
    private double limit = 0.1; // controler Transform Unit
    private double limitLR = 0.3; // LR Drag Unit
    private double max = 0.2; // YP Drag Unit
    private const double epsilon = 0.000001;
    private void control(float fps)
    {
        nMove = (int)Math.Ceiling(fps * timeMove + epsilon);
        nRotate = (int)Math.Ceiling(fps * timeRotate + epsilon);
        nAlignMove = (int)Math.Ceiling(fps * timeAlignMove + epsilon);
        nAlignRotate = (int)Math.Ceiling(fps * timeAlignRotate + epsilon);

        dMove = 1 / (double)nMove;
        dRotate = 90 / (double)nRotate;
        dAlignMove = 1 / (double)nAlignMove;
        dAlignRotate = 90 / (double)nAlignRotate;

        IMove saveTarget = target;
        target.save(saveOrigin, saveAxis);
        if (command != null) command();
        else
        {
            // left hand
            if (!alignMode && opt.oo.inputTypeLeftAndRight == OptionsControl.INPUTTYPE_DRAG) {
                for (int i = 0; i < 3; i++) reg2[i] = dlPosLeft[i];
                Vec.scale(reg2, reg2, 1.0 / limitLR / dMove);
            }
            else {
                for (int i = 0; i < 3; i++) reg2[i] = dfPosLeft[i];
                Vec.scale(reg2, reg2, 1.0 / Math.Max(limit, Vec.norm(reg2)));
            }
            Array.Copy(reg2, reg3, 3);
            if (!alignMode && opt.oo.inputTypeForward == OptionsControl.INPUTTYPE_DRAG) {
                relarot = dlRotLeft;
                reg3[3] = -Math.Asin(relarot.z) * Math.Sign(relarot.w);
                reg3[3] /= maxAng * Math.PI / 180 * dMove;
            }
            else {
                relarot = dfRotLeft;
                reg3[3] = -Math.Asin(relarot.z) * Math.Sign(relarot.w);
                reg3[3] /= Math.Max(limitAngForward * Math.PI / 180, Math.Abs(reg3[3]));
            }

            if (opt.oo.limit3D) reg3[2] = 0;
            if (opt.oo.invertLeftAndRight) for (int i=0; i<reg3.Length-1; i++) reg3[i] = -reg3[i];
            if (opt.oo.invertForward) reg3[reg3.Length-1] = -reg3[reg3.Length-1];
            if (!leftMove) Vec.zero(reg3);
            keyControl(KEYMODE_SLIDE);

            if (alignMode)
            {
                for (int i = 0; i < reg3.Length; i++)
                {
                    if (Math.Abs(reg3[i]) > tAlign)
                    {
                        nActive = nMove;
                        dAlignedMove = 1 / (double)nMove;
                        ad0 = Dir.forAxis(i, reg3[i] < 0);
                        if (target.canMove(Dir.getAxis(ad0), Dir.getSign(ad0))) command = alignMove;
                    }
                }
            }
            else
            {
                Vec.scale(reg3, reg3, dMove);
                target.move(reg3);
            }

            // right hand
            if (alignMode)
            {
                for (int i = 0; i < 3; i++) reg2[i] = dfPosRight[i];
                Vec.scale(reg2, reg2, 1.0 / Math.Max(limit, Vec.norm(reg2)));
                if (opt.oo.limit3D) reg2[2] = 0;
                if (opt.oo.invertYawAndPitch) for (int i = 0; i < reg2.Length; i++) reg2[i] = -reg2[i];
                if (!rightMove) Vec.zero(reg2);
                keyControl(KEYMODE_TURN);
                for (int i = 0; i < reg2.Length; i++)
                {
                    if (Math.Abs(reg2[i]) > tAlign)
                    {
                        nActive = nRotate;
                        dAlignedRotate = 90 / (double)nRotate;
                        ad0 = Dir.forAxis(dim - 1);
                        ad1 = Dir.forAxis(i, reg2[i] < 0);
                        command = alignRotate;
                        break;
                    }
                }
                if (command == null)
                {
                    relarot = dfRotRight;
                    for (int i = 0; i < 3; i++) reg0[i] = Mathf.Asin(relarot[i]) * Mathf.Sign(relarot.w) / (float)limitAng / Mathf.PI * 180;
                    if (opt.oo.limit3D) { reg0[0] = 0; reg0[1] = 0; }
                    if (opt.oo.invertRoll) reg0 = -reg0;
                    if (!rightMove) reg0 = Vector3.zero;
                    keyControl(KEYMODE_SPIN);
                    for (int i = 0; i < 3; i++)
                    {
                        if (Mathf.Abs(reg0[i]) > tAlign)
                        {
                            nActive = nRotate;
                            dAlignedRotate = 90 / (double)nRotate;
                            ad0 = Dir.forAxis((i + 1) % 3);
                            ad1 = Dir.forAxis((i + 2) % 3, reg0[i] < 0);
                            command = alignRotate;
                            break;
                        }
                    }
                }
            }
            else
            {
                Vec.unitVector(reg3, 3);
                double t;
                if (opt.oo.inputTypeYawAndPitch == OptionsControl.INPUTTYPE_DRAG) {
                    for (int i = 0; i < 3; i++) reg2[i] = dlPosRight[i];
                    t = Vec.norm(reg2);
                    if (t>0) Vec.scale(reg2, reg2, 90 / dRotate * Math.Min(max, t) / max / t);
                }
                else {
                    for (int i = 0; i < 3; i++) reg2[i] = dfPosRight[i];
                    t = Vec.norm(reg2);
                    if (t>0) Vec.scale(reg2, reg2, Math.Min(limit, t) / limit / t);
                }
                if (opt.oo.limit3D) reg2[2] = 0;
                if (opt.oo.invertYawAndPitch) for (int i = 0; i < reg2.Length; i++) reg2[i] = -reg2[i];
                if (!rightMove) Vec.zero(reg2);
                keyControl(KEYMODE_TURN);
                t = Vec.norm(reg2);
                if (t != 0)
                {
                    t *= dRotate * Math.PI / 180;
                    Vec.normalize(reg2, reg2);
                    for (int i = 0; i < 3; i++) reg4[i] = reg2[i] * Math.Sin(t);
                    reg4[3] = Math.Cos(t);
                    target.rotateAngle(reg3, reg4);
                }

                float f;
                if (opt.oo.inputTypeRoll == OptionsControl.INPUTTYPE_DRAG) {
                    relarot = dlRotRight;
                }
                else {
                    relarot = dfRotRight;
                    f = Mathf.Acos(relarot.w);
                    if (f>0) f = (float)(dRotate / limitAngRoll) * Mathf.Min((float)limitAngRoll * Mathf.PI / 180, f) / f;
                    relarot = Quaternion.Slerp(Quaternion.identity, relarot, f);
                }
                if (opt.oo.limit3D) { relarot[0] = 0; relarot[1] = 0; }
                if (opt.oo.invertRoll) relarot = Quaternion.Inverse(relarot);
                if (!rightMove) relarot = Quaternion.identity;
                keyControl(KEYMODE_SPIN2);
                if (relarot.w < 1f) {
                    relarot.ToAngleAxis(out f, out reg0);
                    //f = Math.PI / 180 * (float)dRotate * f / Mathf.Max((float)limitAng, f);
                    reg1.Set(1, 0, 0);
                    Vector3.OrthoNormalize(ref reg0, ref reg1);
                    //for (int i = 0; i < 3; i++) relarot[i] = reg0[i] * Mathf.Sin(f);
                    //relarot[3] = Mathf.Cos(f);
                    reg0 = relarot * reg1;
                    for (int i = 0; i < 3; i++) reg3[i] = reg0[i];
                    reg3[3] = 0;
                    for (int i = 0; i < 3; i++) reg4[i] = reg1[i];
                    reg4[3] = 0;
                    Vec.normalize(reg3, reg3);
                    Vec.normalize(reg4, reg4);
                    target.rotateAngle(reg4, reg3);
                }
            }

            if (leftTrigger && ! lastLeftTrigger)
            {
                opt.oo.sliceDir = (opt.oo.sliceDir + 1) % ((opt.oo.sliceMode) ? 4 : 2);
            }
            if (rightTrigger)
            {

            }
        }

        // update state

        // the click command is exclusive, so if the target changed,
        // no update needed.
        if (target == saveTarget
             && !target.update(saveOrigin, saveAxis, engine.getOrigin()))
        { // bonk

            target.restore(saveOrigin, saveAxis);

            //if (commandActive != null)
            //{
            //    commandActive = null;
            //    alignActive = null; // not a big deal but let's do it
            //}

            if (alignMode && !target.isAligned())
            {
                alignMode = false;
            }
        }
    }

    private void alignMove()
    {
        Vec.unitVector(reg3, Dir.getAxis(ad0));
        Vec.scale(reg3, reg3, Dir.getSign(ad0) * dAlignedMove);
        target.move(reg3);
        if (--nActive <= 0)
        {
            target.align().snap();
            command = null;
        }
    }

    private void alignRotate()
    {
        Vec.unitVector(reg3, Dir.getAxis(ad0));
        Vec.scale(reg3, reg3, Dir.getSign(ad0));
        Vec.rotateAbsoluteAngleDir(reg4, reg3, ad0, ad1, dAlignedRotate);
        target.rotateAngle(reg3, reg4);
        if (--nActive <= 0)
        {
            target.align().snap();
            command = null;
        }
    }

    public void align()
    {
        if (engine.getSaveType() == IModel.SAVE_ACTION
         || engine.getSaveType() == IModel.SAVE_BLOCK
         || engine.getSaveType() == IModel.SAVE_SHOOT)
        {
            command = null;
            return;
        }
        if (alignActive == null) alignActive = target.align();
        if (alignActive.align(dAlignMove, dAlignRotate))
        {
            alignActive = null;
            command = null;
        }
    }

    public void click()
    {
        try {target = ((GeomModel)engine.retrieveModel()).click(engine.getOrigin(), engine.getViewAxis(), engine.getAxisArray());}
        catch (InvalidCastException){ return; }
        if (target != null)
        {
            engineAlignMode = alignMode; // save
            alignMode = target.isAligned(); // reasonable default
        }
        else
        {
            target = engine;
            alignMode = engineAlignMode; // restore
        }
        command = null;
    }

    private KeyCode KEY_SLIDELEFT  = KeyCode.S;
    private KeyCode KEY_SLIDERIGHT = KeyCode.F;
    private KeyCode KEY_SLIDEUP    = KeyCode.A;
    private KeyCode KEY_SLIDEDOWN  = KeyCode.Z;
    private KeyCode KEY_SLIDEIN    = KeyCode.W;
    private KeyCode KEY_SLIDEOUT   = KeyCode.R;
    private KeyCode KEY_FORWARD    = KeyCode.E;
    private KeyCode KEY_BACK       = KeyCode.D;
    private KeyCode KEY_TURNLEFT   = KeyCode.J;
    private KeyCode KEY_TURNRIGHT  = KeyCode.L;
    private KeyCode KEY_TURNUP     = KeyCode.I;
    private KeyCode KEY_TURNDOWN   = KeyCode.K;
    private KeyCode KEY_TURNIN     = KeyCode.U;
    private KeyCode KEY_TURNOUT    = KeyCode.O;
    private KeyCode KEY_SPINLEFT   = KeyCode.J;
    private KeyCode KEY_SPINRIGHT  = KeyCode.L;
    private KeyCode KEY_SPINUP     = KeyCode.I;
    private KeyCode KEY_SPINDOWN   = KeyCode.K;
    private KeyCode KEY_SPININ     = KeyCode.U;
    private KeyCode KEY_SPINOUT    = KeyCode.O;
    private const int KEYMODE_SLIDE = 0;
    private const int KEYMODE_TURN = 1;
    private const int KEYMODE_SPIN = 2;
    private const int KEYMODE_SPIN2 = 3;
    private void keyControl(int keyMode) {
        if (keyMode == KEYMODE_SLIDE) {
            if (Input.GetKey(KEY_SLIDELEFT )) reg3[0] = -1;
            if (Input.GetKey(KEY_SLIDERIGHT)) reg3[0] =  1;
            if (Input.GetKey(KEY_SLIDEUP   )) reg3[1] =  1;
            if (Input.GetKey(KEY_SLIDEDOWN )) reg3[1] = -1;
            if (Input.GetKey(KEY_SLIDEIN   )) reg3[2] =  1;
            if (Input.GetKey(KEY_SLIDEOUT  )) reg3[2] = -1;
            if (Input.GetKey(KEY_FORWARD   )) reg3[3] =  1;
            if (Input.GetKey(KEY_BACK      )) reg3[3] = -1;
        }
        if (keyMode == KEYMODE_TURN) {
            if (Input.GetKey(KEY_TURNLEFT ) && !Input.GetKey(KeyCode.LeftShift)) reg2[0] = -1;
            if (Input.GetKey(KEY_TURNRIGHT) && !Input.GetKey(KeyCode.LeftShift)) reg2[0] =  1;
            if (Input.GetKey(KEY_TURNUP   ) && !Input.GetKey(KeyCode.LeftShift)) reg2[1] =  1;
            if (Input.GetKey(KEY_TURNDOWN ) && !Input.GetKey(KeyCode.LeftShift)) reg2[1] = -1;
            if (Input.GetKey(KEY_TURNIN   ) && !Input.GetKey(KeyCode.LeftShift)) reg2[2] =  1;
            if (Input.GetKey(KEY_TURNOUT  ) && !Input.GetKey(KeyCode.LeftShift)) reg2[2] = -1;
        }
        if (keyMode == KEYMODE_SPIN) {
            if (Input.GetKey(KEY_SPINLEFT ) &&  Input.GetKey(KeyCode.LeftShift)) reg0[0] = -1;
            if (Input.GetKey(KEY_SPINRIGHT) &&  Input.GetKey(KeyCode.LeftShift)) reg0[0] =  1;
            if (Input.GetKey(KEY_SPINUP   ) &&  Input.GetKey(KeyCode.LeftShift)) reg0[1] =  1;
            if (Input.GetKey(KEY_SPINDOWN ) &&  Input.GetKey(KeyCode.LeftShift)) reg0[1] = -1;
            if (Input.GetKey(KEY_SPININ   ) &&  Input.GetKey(KeyCode.LeftShift)) reg0[2] =  1;
            if (Input.GetKey(KEY_SPINOUT  ) &&  Input.GetKey(KeyCode.LeftShift)) reg0[2] = -1;
        }
        if (keyMode == KEYMODE_SPIN2) {
            Quaternion q = Quaternion.identity;
            if (Input.GetKey(KEY_SPINLEFT ) &&  Input.GetKey(KeyCode.LeftShift)) q *= Quaternion.Euler(-(float)dRotate,0,0);
            if (Input.GetKey(KEY_SPINRIGHT) &&  Input.GetKey(KeyCode.LeftShift)) q *= Quaternion.Euler( (float)dRotate,0,0);
            if (Input.GetKey(KEY_SPINUP   ) &&  Input.GetKey(KeyCode.LeftShift)) q *= Quaternion.Euler(0, (float)dRotate,0);
            if (Input.GetKey(KEY_SPINDOWN ) &&  Input.GetKey(KeyCode.LeftShift)) q *= Quaternion.Euler(0,-(float)dRotate,0);
            if (Input.GetKey(KEY_SPININ   ) &&  Input.GetKey(KeyCode.LeftShift)) q *= Quaternion.Euler(0,0, (float)dRotate);
            if (Input.GetKey(KEY_SPINOUT  ) &&  Input.GetKey(KeyCode.LeftShift)) q *= Quaternion.Euler(0,0,-(float)dRotate);
            if (q != Quaternion.identity) relarot = q;
        }
    }

    public OptionsAll getOptionsAll()
    {
        return oa;
    }

    public void setFrameRate(double frameRate)
    {
        interval = (int)Math.Ceiling(1000 / frameRate);
    }

    public void setOptionsMotion(/*OptionsKeysConfig okc,*/ OptionsMotion ot)
    {

        //for (int i = 0; i < 6; i++)
        //{
        //    param[i] = okc.param[i];
        //}

        // the frame rate and command times are all positive,
        // so the number of steps will always be at least 1 ...

        //if (engine.getSaveType() == IModel.SAVE_ACTION
        // || engine.getSaveType() == IModel.SAVE_BLOCK
        // || engine.getSaveType() == IModel.SAVE_SHOOT)
        //{
        //    nMove = (int)Math.Ceiling(ot.frameRate * 0.5);
        //    nRotate = (int)Math.Ceiling(ot.frameRate * 0.5);
        //    nAlignMove = (int)Math.Ceiling(ot.frameRate * 0.5);
        //    nAlignRotate = (int)Math.Ceiling(ot.frameRate * 0.5);
        //}
        //else
        //{
            //nMove = (int)Math.Ceiling(ot.frameRate * ot.timeMove);
            //nRotate = (int)Math.Ceiling(ot.frameRate * ot.timeRotate);
            //nAlignMove = (int)Math.Ceiling(ot.frameRate * ot.timeAlignMove);
            //nAlignRotate = (int)Math.Ceiling(ot.frameRate * ot.timeAlignRotate);
        //}

        // ... therefore, the distances will never exceed 1,
        // and the angles will never exceed 90 degrees

        //dMove = 1 / (double)nMove;
        //dRotate = 90 / (double)nRotate;
        //dAlignMove = 1 / (double)nAlignMove;
        //dAlignRotate = 90 / (double)nAlignRotate;

        timeMove =  ot.timeMove;
        timeRotate =  ot.timeRotate;
        timeAlignMove =  ot.timeAlignMove;
        timeAlignRotate =  ot.timeAlignRotate;
    }

    public void updateOptions()
    {
        engine.setOptions(oc(), ov(), oa.oeCurrent, ot(), oa.opt.od);
    }

    public void setOptions()
    {
        setOptionsMotion(oa.opt.ot4);
        setFrameRate(oa.opt.ot4.frameRate);
    }

    public void closeMenu()
    {
        SteamVR_Actions.control.Activate(left);
        SteamVR_Actions.control.Activate(right);
        menuPanel.gameObject.SetActive(false);
        lastLeftTrigger = true;
        lastRightTrigger = true;
    }

    public void doQuit()
    {
        try {
            PropertyFile.save(fileCurrent,save);
        } catch (Exception e) {
            Debug.Log(e);
        }
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_STANDALONE
        UnityEngine.Application.Quit();
#endif
    }

    public void doLoad()
    {
        StartCoroutine(ShowLoadDialogCoroutine());
    }

    private bool opened;
    IEnumerator ShowLoadDialogCoroutine()
    {
        yield return FileBrowser.WaitForLoadDialog(false, opened ? null : Directory.GetCurrentDirectory(), "Load File", "Load");
        opened = true;

        Debug.Log(FileBrowser.Success + " " + FileBrowser.Result);

        if (FileBrowser.Success) {
            reloadFile = FileBrowser.Result;
            if (PropertyFile.test(reloadFile)) menuCommand = doLoadMaze;
            else menuCommand = doLoadGeom;
        }
    }

    private void doLoadMaze()
    {
        PropertyFile.load(reloadFile, loadMaze);
    }

    private void doLoadGeom()
    {
        try
        {
            loadGeom(reloadFile);
        }
        catch (Exception t)
        {
            string s = "";
            if (t is LanguageException)
            {
                LanguageException e = (LanguageException)t;
                //t = e.getCause();
                s = e.getFile() + "\n" + e.getDetail() + "\n";
                Debug.Log(s);
            }
                Debug.Log(t);
            //t.printStackTrace();
            //JOptionPane.showMessageDialog(this, s + t.getClass().getName() + "\n" + t.getMessage(), App.getString("Maze.s25"), JOptionPane.ERROR_MESSAGE);
        }
    }
   private static readonly string VALUE_CHECK       = "Maze";

   private static readonly string KEY_CHECK         = "game";
   private static readonly string KEY_DIM           = "dim";
   private static readonly string KEY_OPTIONS_MAP   = "om";
   private static readonly string KEY_OPTIONS_COLOR = "oc";
   private static readonly string KEY_OPTIONS_VIEW  = "ov";
   private static readonly string KEY_OPTIONS_SEED  = "oe";
   private static readonly string KEY_OPTIONS_DISPLAY = "od";
   private static readonly string KEY_OPTIONS_CONTROL = "oo";
   private static readonly string KEY_ALIGN_MODE    = "align";

    public void loadMaze(IStore store){
        try {
            if ( ! store.getString(KEY_CHECK).Equals(VALUE_CHECK) ) throw new Exception("getEmpty");//App.getEmptyException();
        } catch (Exception e) {
            throw e;//App.getException("Core.e1");
        }

    // read file, but don't modify existing objects until we're sure of success

        int dimLoad = store.getInteger(KEY_DIM);
        if ( ! (dimLoad == 3 || dimLoad == 4) ) throw new Exception("dimError");//App.getException("Core.e2");

        OptionsMap omLoad = new OptionsMap(dimLoad);
        OptionsColor ocLoad = new OptionsColor();
        OptionsView ovLoad = new OptionsView();
        OptionsSeed oeLoad = new OptionsSeed();

        store.getObject(KEY_OPTIONS_MAP,omLoad);
        store.getObject(KEY_OPTIONS_COLOR,ocLoad);
        store.getObject(KEY_OPTIONS_VIEW,ovLoad);
        store.getObject(KEY_OPTIONS_SEED,oeLoad);
        if ( ! oeLoad.isSpecified() ) throw new Exception("seedError");//App.getException("Core.e3");
        bool alignModeLoad = store.getBool(KEY_ALIGN_MODE);

    // ok, we know enough ... even if the engine parameters turn out to be invalid,
    // we can still start a new game

        // and, we need to initialize the engine before it can validate its parameters

        dim = dimLoad;

        oa.omCurrent = omLoad; // may as well transfer as copy
        oa.ocCurrent = ocLoad;
        oa.ovCurrent = ovLoad;
        oa.oeCurrent = oeLoad;

        oa.opt.om4 = omLoad;
        oa.opt.oc4 = ocLoad;
        oa.opt.ov4 = ovLoad;
        oa.oeCurrent = oeLoad;
        // oeNext is not modified by loading a game

        IModel model = new MapModel(dim,oa.omCurrent,oc(),oa.oeCurrent,ov(),store);
        engine.newGame(dim,model,ov(),/*oa.opt.os,*/ot(),false);

        updateOptions();
        setOptions();

        target = engine;
        command = null;
        saveOrigin = new double[this.dim];
        saveAxis = new double[this.dim][];
        for (int i = 0; i < this.dim; i++) saveAxis[i] = new double[this.dim];

        engine.load(store,alignModeLoad);
    }

    public void loadGeom(string file) //throws Exception
    {

        // read file

        Context c = DefaultContext.create();
        c.libDirs.Add("data" + Path.DirectorySeparatorChar + "lib");
        Language.include(c, file);

        // build the model
        Debug.Log("try");
        GeomModel model = buildModel(c);
        // run this before changing anything since it can fail
        Debug.Log("complete");
        // switch to geom

        dim = model.getDimension();

        // no need to modify omCurrent, just leave it with previous maze values
        oa.ocCurrent = null;
        oa.ovCurrent = null;
        // no need to modify oeCurrent or oeNext

        bool[] texture = model.getDesiredTexture();
        if (texture != null)
        { // model -> ov
            OptionsView ovLoad = new OptionsView();
            OptionsView.copy(ovLoad, ov(), texture);
            oa.ovCurrent = ovLoad;
            // careful, if you set ovCurrent earlier
            // then ov() will return the wrong thing
        }
        else
        { // ov -> model
            texture = ov().texture;
        }
        model.setTexture(texture);

        // model already constructed
        engine.newGame(dim, model, ov(), /*oa.opt.os,*/ ot(), true);

        updateOptions();
        setOptions();

        target = engine;
        command = null;
        saveOrigin = new double[this.dim];
        saveAxis = new double[this.dim][];
        for (int i = 0; i < this.dim; i++) saveAxis[i] = new double[this.dim];
    }

    public static GeomModel buildModel(Context c) //throws Exception
    {

        DimensionAccumulator da = new DimensionAccumulator();
        //Track track = null;
        //LinkedList tlist = new LinkedList();
        List<IScenery> scenery = new List<IScenery>();
        List<Geom.ShapeInterface> slist = new List<Geom.ShapeInterface>();
        //LinkedList elist = new LinkedList();
        Struct.ViewInfo viewInfo = null;
        Struct.DrawInfo drawInfo = null;

        Struct.FinishInfo finishInfo = null;
        Struct.FootInfo footInfo = null;
        //Struct.BlockInfo blockInfo = null;

        // scan for items
        foreach (object o in c.stack)
        {
            if (o is IDimension)
            {
                da.putDimension(((IDimension)o).getDimension());
            }
            else if (o is IDimensionMultiSrc)
            {
                ((IDimensionMultiSrc)o).getDimension(da);
            }
            // this is just a quick check to catch the most obvious mistakes.
            // I can't be bothered to add dimension accessors to the scenery.

            if (o == null)
            {
                throw new Exception("Unused null object on stack.");
            }
            //else if (o is Track) {
            //    if (track != null) throw new Exception("Only one track object allowed (but it can have disjoint loops).");
            //    track = (Track)o;
            //} else if (o is Train) {
            //    tlist.add(o);
            //} 
            else if (o is IScenery)
            {
                scenery.Add((IScenery)o);
            }
            else if (o is Geom.ShapeInterface)
            { // Shape or CompositeShape
                ((Geom.ShapeInterface)o).unglue(slist);
            }
            else if (o is Struct.ViewInfo)
            {
                if (viewInfo != null) throw new Exception("Only one viewinfo command allowed.");
                viewInfo = (Struct.ViewInfo)o;
            }
            else if (o is Struct.DrawInfo)
            {
                if (drawInfo != null) throw new Exception("Only one drawinfo command allowed.");
                drawInfo = (Struct.DrawInfo)o;
            }
            else if (o is Struct.DimensionMarker)
            {
                // ignore, we're done with it
            }
            else if (o is Struct.FinishInfo)
            {
                if (finishInfo != null) throw new Exception("Only one finishInfo command allowed.");
                finishInfo = (Struct.FinishInfo)o;
            }
            else if (o is Struct.FootInfo)
            {
                footInfo = (Struct.FootInfo)o;
            }
            //else if (o is Struct.BlockInfo)
            //{
            //    blockInfo = (Struct.BlockInfo)o;
            //}
            //else if (o is Enemy)
            //{
            //    elist.add(o);
            //}
            else
            {
                throw new Exception("Unused object on stack (" + o.GetType().Name + ").");
            }
        }

        // use items to make model

        if (da.first) throw new Exception("Scene doesn't contain any objects.");
        if (da.error) throw new Exception("The number of dimensions is not consistent.");
        int dtemp = da.dim; // we shouldn't change the Core dim variable yet

        Geom.ShapeInterface[] list = slist.ToArray();

        //Geom.Shape[] shapes = (Geom.Shape[])slist.ToArray();
        Geom.Shape[] shapes = new Geom.Shape[slist.Count];
        for (int i = 0; i < slist.Count; i++) shapes[i] = (Geom.Shape)slist[i];
        //Train[] trains = (Train[])tlist.toArray(new Train[tlist.size()]);
        //Enemy[] enemies = (Enemy[])elist.toArray(new Enemy[elist.size()]);

        //if (track != null) TrainModel.init(track, trains); // kluge needed for track scale

        if (scenery.Count == 0) scenery.Add((dtemp == 3) ? new Mat.Mat3() : (IScenery)new Mat.Mat4());
        //if (track != null) scenery.add(track); // add last so it draws over other scenery

        GeomModel model;
        //if (finishInfo != null) model = new ActionModel(dtemp, shapes, drawInfo, viewInfo, footInfo, finishInfo);
        //else if (enemies.Length > 0) model = new ShootModel(dtemp, shapes, drawInfo, viewInfo, footInfo, enemies);
        //else if (blockInfo != null) model = new BlockModel(dtemp, shapes, drawInfo, viewInfo, footInfo);
        //else model = (track != null) ? new TrainModel(dtemp, shapes, drawInfo, viewInfo, track, trains)
        /*:*/
        model = new GeomModel(dtemp, shapes, drawInfo, viewInfo);
        model.addAllScenery(scenery);

        // gather dictionary info

        List<Color> availableColors = new List<Color>();
        List<Shapes> availableShapes = new List<Shapes>();
        Dictionary<string, Color> colorNames = new Dictionary<string, Color>();
        Dictionary<string, Geom.Shape> idealNames = new Dictionary<string, Geom.Shape>();

        //foreach (KeyValuePair<string, object> entry in c.dict)
        //{
        //    object o = entry.Value;
        //    if (o is Color)
        //    {

        //        availableColors.Add(entry);

        //        String name = (String)entry.getKey();
        //        if (!c.topLevelDef.contains(name))
        //        {
        //            colorNames.Add(name, (Color)o);
        //        }

        //    }
        //    else if (o is Geom.Shape)
        //    { // not ShapeInterface, at least for now
        //        Geom.Shape shape = (Geom.Shape)o;
        //        if (shape.getDimension() == dtemp)
        //        {

        //            availableShapes.add(new NamedObject(entry));

        //            String name = (String)entry.getKey();
        //            if (!c.topLevelDef.contains(name))
        //            {
        //                idealNames.put(shape.ideal, name);
        //            }
        //        }
        //    }
        //    // else it's not something we're interested in
        //}

        //Collections.sort(availableColors);
        //Collections.sort(availableShapes);

        //model.setAvailableColors(availableColors);
        //model.setAvailableShapes(availableShapes);

        model.setSaveInfo(c.topLevelInclude, colorNames, idealNames);

        // done

        return model;
    }

    public void doReload(int delta) {
        if (reloadFile == null) return;

        Debug.Log(reloadFile);
        if (delta != 0) {
            string[] f = Array.ConvertAll<FileInfo, string>(Directory.GetParent(reloadFile).GetFiles(), s => s.ToString());
            Debug.Log(string.Join(",",f));
            Array.Sort(f);
            Debug.Log(string.Join(",",f));

            // results of listFiles have same parent directory so names are sufficient
            // (and probably faster for sorting)

            int i = Array.IndexOf(f,reloadFile);
            Debug.Log(i);
            if (i != -1) {
                i += delta;
                if (i >= 0 && i < f.Length) reloadFile = f[i];
                else return; // we're at the end, don't do a reload
            }
            // else not found, fall through and report that error
        }

        if (PropertyFile.test(reloadFile)) menuCommand = doLoadMaze;
        else menuCommand = doLoadGeom;
    }

    private bool doInit() {
        try {
            PropertyFile.load(nameDefault, delegate(IStore store) { loadDefault(store); });
            if (File.Exists(fileCurrent)) PropertyFile.load(fileCurrent, load);
        } catch (Exception e) {
            Debug.Log(e);
            return false;
        }
        return true;
    }

   private static string nameDefault = "default.properties";
   private static string fileCurrent = "current.properties";

   private static readonly String KEY_OPTIONS = "opt";
   private static readonly String KEY_BOUNDS  = "bounds";
   private static readonly String KEY_GAME_DIRECTORY  = "dir.game";
   private static readonly String KEY_IMAGE_DIRECTORY = "dir.image";
   private static readonly String KEY_VERSION = "version";
   private static readonly String KEY_FISHEYE = "opt.of"; // not part of opt (yet)

   // here we don't have to be careful about modifying an existing object,
   // because if any of the load process fails, the program will exit

    public void loadDefault(IStore store) {
        store.getObject(KEY_OPTIONS,optDefault);

        if (File.Exists(fileCurrent)) return;

        store.getObject(KEY_OPTIONS,opt);
        dim = 4;
        gameDirectory  = null;
    }

    public void load(IStore store) {

        store.getObject(KEY_OPTIONS,opt);
        dim = store.getInteger(KEY_DIM);
        //if ( ! (dim == 3 || dim == 4) ) throw App.getException("Maze.e1");
        //gameDirectory  = store.getString(KEY_GAME_DIRECTORY );

        //int? temp = store.getNullableInteger(KEY_VERSION);
        //int version = (temp == null) ? 1 : temp.Value;

        //if (version >= 2) {
            //store.getObject(KEY_FISHEYE,OptionsFisheye.of);
            //OptionsFisheye.recalculate();
        //}
    }

    public void save(IStore store) {

        store.putObject(KEY_OPTIONS,oa.opt);
        store.putInteger(KEY_DIM,dim);
        //store.putString(KEY_GAME_DIRECTORY, gameDirectory);

        //store.putInteger(KEY_VERSION,2);

        //// version 2
        //store.putObject(KEY_FISHEYE,OptionsFisheye.of);
    }

}
