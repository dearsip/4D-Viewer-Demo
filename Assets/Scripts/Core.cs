using System.Collections;
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
    private int nMove;
    private int nRotate;
    private int nAlignMove; // these two only used inside setOptions
    private int nAlignRotate;
    private double dMove;
    private double dRotate;
    private double dAlignMove;
    private double dAlignRotate;
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
    private Vector3 posLeft, lastPosLeft, fromPosLeft, posRight, lastPosRight, fromPosRight;
    private Quaternion rotLeft, lastRotLeft, fromRotLeft, rotRight, lastRotRight, fromRotRight, relarot;
    private bool leftTrigger, rightTrigger, lastLeftTrigger, lastRightTrigger, leftTriggerPressed, rightTriggerPressed,
        leftMove, rightMove;
    public Menu menuPanel;

    private Vector3 reg0, reg1;
    private double[] reg2, reg3, reg4, reg5, reg6;
    public Player player;
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

    // Start is called before the first frame update
    void Start() // ルーチン開始
    {
        SteamVR_Actions.controll.Activate(left);
        SteamVR_Actions.controll.Activate(right);

        addEvevts();

        posLeft = pose.GetLocalPosition(left); rotLeft = pose.GetLocalRotation(left);
        posRight = pose.GetLocalPosition(right); rotRight = pose.GetLocalRotation(right);

        optDefault = ScriptableObject.CreateInstance<Options>();
        opt = ScriptableObject.CreateInstance<Options>();
        // ロード
        load();
        // dim and rest of oa are initialized when new game started

        oa = new OptionsAll();
        oa.opt = opt;
        oa.omCurrent = new OptionsMap(0); // blank for copying into
        oa.oeNext = new OptionsSeed();

        eyeVector = new double[3];
        mesh = new Mesh();
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
        StartCoroutine(tick());
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
        fromPosLeft = pose.GetLocalPosition(left); fromRotLeft = pose.GetLocalRotation(left);
    }

    private void RightDown(SteamVR_Action_Boolean fromBoolean, SteamVR_Input_Sources fromSource)
    {
        fromPosRight = pose.GetLocalPosition(right); fromRotRight = pose.GetLocalRotation(right);
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
        SteamVR_Actions.controll.Deactivate(left);
        SteamVR_Actions.controll.Deactivate(right);
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
        saveOrigin = new double[this.dim];
        saveAxis = new double[this.dim][];
        for (int i = 0; i < this.dim; i++) saveAxis[i] = new double[this.dim];
    }

    IEnumerator tick()
    {
        int base_ = Environment.TickCount;
        while (true)
        {
            calcInput();
            menuCommand?.Invoke();
            menuCommand = null;
            controll();
            engine.renderAbsolute(eyeVector, opt.oo);
            //doHaptics();
            GetComponent<MeshFilter>().sharedMesh = mesh;

            int now = Environment.TickCount;
            int next = base_ + interval; // unsynchronized use of interval is OK

            if (now < next)
            { // we have time, sleep a bit

                int t = next - now;

                // I don't understand this at all, but sometimes the current time
                // jumps back by 4-6 minutes for no reason.  does it drift forward
                // and then get corrected?  does it jump forward and then back?
                // no idea.  in any case, if we don't detect and stop it, the game
                // will lock up for that many minutes.
                //
                if (t > 1000)
                {
                    t = interval; // standard interval is best guess for sleep time
                    next = now + interval;
                }

                yield return new WaitForSeconds(t * 0.001f);
                base_ = next; // same equation as below would work, but this is clearer

            }
            else
            { // no time left, tick again immediately
                yield return null;
                base_ = Math.Max(next, now - 3 * interval); // see note below
            }

            // on my system, the actual sleep duration is granular,
            // with each grain being approximately 55 ms.
            // so, if you ask to sleep for 100 ms, you usually sleep for 110.

            // the code above is designed to compensate for this.
            // as long as we are producing frames sufficiently quickly,
            // base will advance by the exact interval,
            // so oversleeping will lead to requesting shorter wait times.

            // if we are not producing frames quickly enough,
            // there's no sense accumulating a large sleep debt,
            // just go ahead and reset the base.
            // actually, it would be nice to be able to recover from a few slow frames,
            // so do let debt accumulate, but limit it to a fixed number of multiples
            //else
            //{
            //    yield return new WaitForSeconds(0.1f);

            //}
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
        lastPosLeft = posLeft; lastPosRight = posRight;
        lastRotLeft = rotLeft; lastRotRight = rotRight;
        posLeft = pose.GetLocalPosition(left); rotLeft = pose.GetLocalRotation(left);
        posRight = pose.GetLocalPosition(right); rotRight = pose.GetLocalRotation(right);
        lastLeftTrigger = leftTrigger; lastRightTrigger = rightTrigger;
        leftTrigger = trigger.GetState(left); rightTrigger = trigger.GetState(right);

        leftMove = move.GetState(left); rightMove = move.GetState(right);
        reg1 = transform.position - player.hmdTransform.position;
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
    private double limitAng = 30;

    private double limit = 0.1;
    private void controll()
    {
        // save state

        IMove saveTarget = target;
        target.save(saveOrigin, saveAxis);
        if (command != null) command();
        else
        {
            if (leftMove)
            {
                for (int i = 0; i < 3; i++) reg2[i] = posLeft[i] - fromPosLeft[i];
                if (opt.oo.limit3D) reg2[2] = 0;
                Vec.scale(reg2, reg2, 1.0 / Math.Max(limit, Vec.norm(reg2)));
                Array.Copy(reg2, reg3, 3);
                relarot = rotLeft * Quaternion.Inverse(fromRotLeft);
                reg3[3] = Math.Asin(relarot.y) * Math.Sign(relarot.w);
                reg3[3] /= Math.Max(limitAng * Math.PI / 180, reg3[3]);
                if (opt.oo.invertLeftAndRight) for (int i=0; i<reg3.Length-1; i++) reg3[i] = -reg3[i];
                if (opt.oo.invertForward) reg3[reg3.Length-1] = -reg3[reg3.Length-1];
                if (alignMode)
                {
                    for (int i = 0; i < reg3.Length; i++)
                    {
                        if (Math.Abs(reg3[i]) > 0.8)
                        {
                            nActive = nMove;
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
            }
            if (rightMove)
            {
                if (alignMode)
                {
                    for (int i = 0; i < 3; i++) reg2[i] = posRight[i] - fromPosRight[i];
                    if (opt.oo.limit3D) reg2[2] = 0;
                    Vec.scale(reg2, reg2, 1.0 / Math.Max(limit, Vec.norm(reg2)));
                    if (opt.oo.invertYawAndPitch) for (int i = 0; i < reg2.Length; i++) reg2[i] = -reg2[i];
                    for (int i = 0; i < reg2.Length; i++)
                    {
                        if (Math.Abs(reg2[i]) > 0.8)
                        {
                            nActive = nRotate;
                            ad0 = Dir.forAxis(dim - 1);
                            ad1 = Dir.forAxis(i, reg2[i] < 0);
                            command = alignRotate;
                            break;
                        }
                    }
                    if (command == null)
                    {
                        relarot = rotRight * Quaternion.Inverse(fromRotRight);
                        if (opt.oo.limit3D) { relarot[0] = 0; relarot[1] = 0; }
                        if (opt.oo.invertRoll) relarot = Quaternion.Inverse(relarot);
                        for (int i = 0; i < 3; i++)
                        {
                            float f = Mathf.Asin(relarot[i]) * Mathf.Sign(relarot.w) / (float)limitAng / Mathf.PI * 180;
                            if (Mathf.Abs(f) > 0.8)
                            {
                                nActive = nRotate;
                                ad0 = Dir.forAxis((i + 1) % 3);
                                ad1 = Dir.forAxis((i + 2) % 3, f < 0);
                                command = alignRotate;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    Vec.unitVector(reg3, 3);
                    for (int i = 0; i < 3; i++) reg2[i] = posRight[i] - fromPosRight[i];
                    if (opt.oo.limit3D) reg2[2] = 0;
                    if (opt.oo.invertYawAndPitch) for (int i = 0; i < reg2.Length; i++) reg2[i] = -reg2[i];
                    double t = Vec.norm(reg2);
                    if (t != 0)
                    {
                        t = dRotate * Math.PI / 180 * Math.Min(limit, t) / limit;
                        Vec.normalize(reg2, reg2);
                        for (int i = 0; i < 3; i++) reg4[i] = reg2[i] * Math.Sin(t);
                        reg4[3] = Math.Cos(t);
                        target.rotateAngle(reg3, reg4);
                    }

                    relarot = rotRight * Quaternion.Inverse(lastRotRight);
                    if (opt.oo.limit3D) { relarot[0] = 0; relarot[1] = 0; }
                    if (opt.oo.invertRoll) relarot = Quaternion.Inverse(relarot);
                    float f;
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
            if (leftTrigger)
            {

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
        Vec.scale(reg3, reg3, Dir.getSign(ad0) * dMove);
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
        Vec.rotateAbsoluteAngleDir(reg4, reg3, ad0, ad1, dRotate);
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
        target = ((GeomModel)engine.retrieveModel()).click(engine.getOrigin(), engine.getViewAxis(), engine.getAxisArray());
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
        {
            nMove = (int)Math.Ceiling(ot.frameRate * ot.timeMove);
            nRotate = (int)Math.Ceiling(ot.frameRate * ot.timeRotate);
            nAlignMove = (int)Math.Ceiling(ot.frameRate * ot.timeAlignMove);
            nAlignRotate = (int)Math.Ceiling(ot.frameRate * ot.timeAlignRotate);
        }

        // ... therefore, the distances will never exceed 1,
        // and the angles will never exceed 90 degrees

        dMove = 1 / (double)nMove;
        dRotate = 90 / (double)nRotate;
        dAlignMove = 1 / (double)nAlignMove;
        dAlignRotate = 90 / (double)nAlignRotate;
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
        SteamVR_Actions.controll.Activate(left);
        SteamVR_Actions.controll.Activate(right);
        menuPanel.gameObject.SetActive(false);
        lastLeftTrigger = true;
        lastRightTrigger = true;
    }

    public void doQuit()
    {
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

    IEnumerator ShowLoadDialogCoroutine()
    {
        yield return FileBrowser.WaitForLoadDialog(false, null, "Load File", "Load");

        Debug.Log(FileBrowser.Success + " " + FileBrowser.Result);

        if (FileBrowser.Success) {
            reloadFile = FileBrowser.Result;
            if (PropertyFile.test(reloadFile)) menuCommand = doLoadMaze;
            else menuCommand = doLoadGeom;
        }
    }

    private void doLoadMaze()
    {
        Dictionary<string, string> p = PropertyFile.load(reloadFile);
        try {
            PropertyStore store = new PropertyStore(p);
            loadMaze(store);
        } catch (Exception e) {
            Debug.Log(e);
            //throw App.getException("PropertyFile.e2",new Object[] { file.getName(), e.getMessage() });
        }
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
   private static readonly string KEY_ALIGN_MODE    = "align";

    public void loadMaze(PropertyStore store){
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
        // oeNext is not modified by loading a game

        IModel model = new MapModel(dim,oa.omCurrent,oc(),oa.oeCurrent,ov(),store);
        engine.newGame(dim,model,ov(),/*oa.opt.os,*/ot(),false);

        updateOptions();
        setOptions();

        target = engine;
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
        //controller.setOptions(oa.opt.okc, ot());
        //controller.setKeysNew(model);
        //controller.setAlwaysRun(model.isAnimated());
        //clock.setFrameRate(ot().frameRate);

        //keyMapper().releaseAll(); // sync up key mapper, which may have changed with dim

        //controller.reset(dim, model.getAlignMode(/* defaultAlignMode = */ ok().startAlignMode));
        // clock will stop when controller reports idle
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

    private void load()
    {
        //opt.oc3.colorMode = 0;
        //opt.oc3.dimSameParallel = 2;
        //opt.oc3.dimSamePerpendicular = 0;
        //opt.oc3.enable[0] = true;
        //opt.oc3.enable[1] = true;
        //opt.oc3.enable[2] = true;
        //opt.oc3.enable[3] = true;
        //opt.oc3.enable[4] = true;
        //opt.oc3.enable[5] = true;
        //opt.oc3.enable[6] = true;
        //opt.oc3.enable[7] = false;
        //opt.oc3.enable[8] = false;
        //opt.oc3.enable[9] = false;
        //opt.oc3.enable[10] = true;
        //opt.oc3.enable[11] = false;

        opt.oc4.colorMode = 1;
        opt.oc4.dimSameParallel = 0;
        opt.oc4.dimSamePerpendicular = 0;
        opt.oc4.enable[0] = true;
        opt.oc4.enable[1] = true;
        opt.oc4.enable[2] = false;
        opt.oc4.enable[3] = true;
        opt.oc4.enable[4] = true;
        opt.oc4.enable[5] = true;
        opt.oc4.enable[6] = true;
        opt.oc4.enable[7] = true;
        opt.oc4.enable[8] = false;
        opt.oc4.enable[9] = false;
        opt.oc4.enable[10] = true;
        opt.oc4.enable[11] = false;

        //opt.oi.background = 0;
        //opt.oi.invertColors = false;
        //opt.oi.convertColors = 0;
        //opt.oi.lineWidth = 1;
        //opt.oi.oneInch = 1;

        //opt.ok3.key[0].code = 69;
        //opt.ok3.key[1].code = 68;
        //opt.ok3.key[2].code = 74;
        //opt.ok3.key[3].code = 76;
        //opt.ok3.key[4].code = 73;
        //opt.ok3.key[5].code = 75;
        //opt.ok3.key[6].code = 0;
        //opt.ok3.key[7].code = 0;
        //opt.ok3.key[8].code = 74;
        //opt.ok3.key[9].code = 76;
        //opt.ok3.key[10].code = 73;
        //opt.ok3.key[11].code = 75;
        //opt.ok3.key[12].code = 0;
        //opt.ok3.key[13].code = 0;
        //opt.ok3.key[14].code = 85;
        //opt.ok3.key[15].code = 79;
        //opt.ok3.key[16].code = 0;
        //opt.ok3.key[17].code = 0;
        //opt.ok3.key[18].code = 0;
        //opt.ok3.key[19].code = 0;
        //opt.ok3.key[20].code = 10;
        //opt.ok3.key[21].code = 10;
        //opt.ok3.key[0].modifiers = 0;
        //opt.ok3.key[1].modifiers = 0;
        //opt.ok3.key[2].modifiers = 0;
        //opt.ok3.key[3].modifiers = 0;
        //opt.ok3.key[4].modifiers = 0;
        //opt.ok3.key[5].modifiers = 0;
        //opt.ok3.key[6].modifiers = 0;
        //opt.ok3.key[7].modifiers = 0;
        //opt.ok3.key[8].modifiers = 8;
        //opt.ok3.key[9].modifiers = 8;
        //opt.ok3.key[10].modifiers = 8;
        //opt.ok3.key[11].modifiers = 8;
        //opt.ok3.key[12].modifiers = 0;
        //opt.ok3.key[13].modifiers = 0;
        //opt.ok3.key[14].modifiers = 0;
        //opt.ok3.key[15].modifiers = 0;
        //opt.ok3.key[16].modifiers = 0;
        //opt.ok3.key[17].modifiers = 0;
        //opt.ok3.key[18].modifiers = 0;
        //opt.ok3.key[19].modifiers = 0;
        //opt.ok3.key[20].modifiers = 0;
        //opt.ok3.key[21].modifiers = 1;
        //opt.ok3.startAlignMode = true;

        //opt.ok4.key[0].code = 69;
        //opt.ok4.key[1].code = 68;
        //opt.ok4.key[2].code = 74;
        //opt.ok4.key[3].code = 76;
        //opt.ok4.key[4].code = 73;
        //opt.ok4.key[5].code = 75;
        //opt.ok4.key[6].code = 85;
        //opt.ok4.key[7].code = 79;
        //opt.ok4.key[8].code = 74;
        //opt.ok4.key[9].code = 76;
        //opt.ok4.key[10].code = 73;
        //opt.ok4.key[11].code = 75;
        //opt.ok4.key[12].code = 85;
        //opt.ok4.key[13].code = 79;
        //opt.ok4.key[14].code = 85;
        //opt.ok4.key[15].code = 79;
        //opt.ok4.key[16].code = 74;
        //opt.ok4.key[17].code = 76;
        //opt.ok4.key[18].code = 73;
        //opt.ok4.key[19].code = 75;
        //opt.ok4.key[20].code = 10;
        //opt.ok4.key[21].code = 10;
        //opt.ok4.key[0].modifiers = 0;
        //opt.ok4.key[1].modifiers = 0;
        //opt.ok4.key[2].modifiers = 0;
        //opt.ok4.key[3].modifiers = 0;
        //opt.ok4.key[4].modifiers = 0;
        //opt.ok4.key[5].modifiers = 0;
        //opt.ok4.key[6].modifiers = 0;
        //opt.ok4.key[7].modifiers = 0;
        //opt.ok4.key[8].modifiers = 8;
        //opt.ok4.key[9].modifiers = 8;
        //opt.ok4.key[10].modifiers = 8;
        //opt.ok4.key[11].modifiers = 8;
        //opt.ok4.key[12].modifiers = 8;
        //opt.ok4.key[13].modifiers = 8;
        //opt.ok4.key[14].modifiers = 1;
        //opt.ok4.key[15].modifiers = 1;
        //opt.ok4.key[16].modifiers = 1;
        //opt.ok4.key[17].modifiers = 1;
        //opt.ok4.key[18].modifiers = 1;
        //opt.ok4.key[19].modifiers = 1;
        //opt.ok4.key[20].modifiers = 0;
        //opt.ok4.key[21].modifiers = 1;
        //opt.ok4.startAlignMode = true;

        //opt.okc.key[0].code = 113;
        //opt.okc.key[1].code = 48;
        //opt.okc.key[2].code = 49;
        //opt.okc.key[3].code = 50;
        //opt.okc.key[4].code = 51;
        //opt.okc.key[5].code = 52;
        //opt.okc.key[6].code = 53;
        //opt.okc.key[7].code = 54;
        //opt.okc.key[8].code = 55;
        //opt.okc.key[9].code = 56;
        //opt.okc.key[10].code = 57;
        //opt.okc.key[11].code = 45;
        //opt.okc.key[12].code = 91;
        //opt.okc.key[13].code = 44;
        //opt.okc.key[14].code = 40;
        //opt.okc.key[15].code = 37;
        //opt.okc.key[16].code = 0;
        //opt.okc.key[17].code = 61;
        //opt.okc.key[18].code = 93;
        //opt.okc.key[19].code = 46;
        //opt.okc.key[20].code = 38;
        //opt.okc.key[21].code = 39;
        //opt.okc.key[22].code = 0;
        //opt.okc.key[23].code = 27;
        //opt.okc.key[24].code = 115;
        //opt.okc.key[0].modifiers = 0;
        //opt.okc.key[1].modifiers = 0;
        //opt.okc.key[2].modifiers = 0;
        //opt.okc.key[3].modifiers = 0;
        //opt.okc.key[4].modifiers = 0;
        //opt.okc.key[5].modifiers = 0;
        //opt.okc.key[6].modifiers = 0;
        //opt.okc.key[7].modifiers = 0;
        //opt.okc.key[8].modifiers = 0;
        //opt.okc.key[9].modifiers = 0;
        //opt.okc.key[10].modifiers = 0;
        //opt.okc.key[11].modifiers = 0;
        //opt.okc.key[12].modifiers = 0;
        //opt.okc.key[13].modifiers = 0;
        //opt.okc.key[14].modifiers = 0;
        //opt.okc.key[15].modifiers = 0;
        //opt.okc.key[16].modifiers = 0;
        //opt.okc.key[17].modifiers = 0;
        //opt.okc.key[18].modifiers = 0;
        //opt.okc.key[19].modifiers = 0;
        //opt.okc.key[20].modifiers = 0;
        //opt.okc.key[21].modifiers = 0;
        //opt.okc.key[22].modifiers = 0;
        //opt.okc.key[23].modifiers = 0;
        //opt.okc.key[24].modifiers = 8;
        //opt.okc.param[0] = 2;
        //opt.okc.param[1] = 1;
        //opt.okc.param[2] = 4;
        //opt.okc.param[3] = 8;
        //opt.okc.param[4] = 9;
        //opt.okc.param[5] = 0;

        //opt.om3.dimMap = 3;
        //opt.om3.size[0] = 10;
        //opt.om3.size[1] = 10;
        //opt.om3.size[2] = 10;
        //opt.om3.density = 0.1;
        //opt.om3.twistProbability = 0.2;
        //opt.om3.branchProbability = 0.2;
        //opt.om3.allowLoops = true;
        //opt.om3.loopCrossProbability = 0.7;

        opt.om4.dimMap = 4;
        opt.om4.size[0] = 3;
        opt.om4.size[1] = 3;
        opt.om4.size[2] = 3;
        opt.om4.size[3] = 3;
        opt.om4.density = 1;
        opt.om4.twistProbability = 0.4;
        opt.om4.branchProbability = 0.2;
        opt.om4.allowLoops = false;
        opt.om4.loopCrossProbability = 0.7;

        //opt.os.enable = true;
        //opt.os.screenWidth = 12;
        //opt.os.screenDistance = 30;
        //opt.os.eyeSpacing = 3;
        //opt.os.cross = true;
        //opt.os.tiltVertical = -15;
        //opt.os.tiltHorizontal = 30;

        //opt.ot3.frameRate = 30;
        //opt.ot3.timeMove = 0.8;
        //opt.ot3.timeRotate = 0.8;
        //opt.ot3.timeAlignMove = 2;
        //opt.ot3.timeAlignRotate = 2;

        opt.ot4.frameRate = 20;
        opt.ot4.timeMove = 1;
        opt.ot4.timeRotate = 0.7;
        opt.ot4.timeAlignMove = 2;
        opt.ot4.timeAlignRotate = 2;

        //opt.ov3.depth = 6;
        //opt.ov3.texture[0] = false;
        //opt.ov3.texture[1] = true;
        //opt.ov3.texture[2] = false;
        //opt.ov3.texture[3] = true;
        //opt.ov3.texture[4] = false;
        //opt.ov3.texture[5] = true;
        //opt.ov3.texture[6] = false;
        //opt.ov3.texture[7] = true;
        //opt.ov3.texture[8] = false;
        //opt.ov3.texture[9] = true;
        //opt.ov3.retina = 1.45;
        //opt.ov3.scale = 0.95;

        opt.ov4.depth = 5;
        opt.ov4.texture[0] = false;
        opt.ov4.texture[1] = false;
        opt.ov4.texture[2] = false;
        opt.ov4.texture[3] = false;
        opt.ov4.texture[4] = false;
        opt.ov4.texture[5] = false;
        opt.ov4.texture[6] = false;
        opt.ov4.texture[7] = false;
        opt.ov4.texture[8] = false;
        opt.ov4.texture[9] = true;
        opt.ov4.retina = 1.8;
        opt.ov4.scale = 0.6;

        opt.od.transparency = 0.1;
        opt.od.border = 1;
        opt.od.useEdgeColor = false;
        opt.od.hidesel = false;
        opt.od.invertNormals = false;
        opt.od.separate = true;

        opt.oo.moveInputType = 0;
        opt.oo.rotateInputType = 1;
        opt.oo.invertLeftAndRight = false;
        opt.oo.invertForward = false;
        opt.oo.baseTransparency = 0.2f;
        opt.oo.sliceTransparency = 1f;

        dim = 4;
        gameDirectory = null;

        SaveData.SetClass("optDefault", opt);
        opt = SaveData.GetClass("opt", opt);
    }
}
