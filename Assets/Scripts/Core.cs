using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;
using System;
using SimpleFileBrowser;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(Interactable))]
public class Core : MonoBehaviour
{
    private Options optDefault;
    private Options opt; // the next three are used only during load
    private int dim;
    private string gameDirectory;
    //private string imageDirectory;
    private string reloadFile;

    private OptionsAll oa;
    private Engine engine;

    private Mesh mesh;

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

    private int interval;

    public Queue<string> queue;
    public SteamVR_Action_Boolean trigger, move, menu;
    public SteamVR_Action_Pose pose;
    public SteamVR_Input_Sources left, right;
    private Vector3 posLeft, lastPosLeft, fromPosLeft, posRight, lastPosRight, fromPosRight;
    private Quaternion rotLeft, lastRotLeft, fromRotLeft, rotRight, lastRotRight, fromRotRight;
    private bool leftTrigger, rightTrigger, lastLeftTrigger, lastRightTrigger, leftTriggerPressed, rightTriggerPressed,
        leftMove, rightMove;
    public Menu menuPanel;

    private Vector3 reg0, reg1;
    private double[] reg2, reg3, reg4;
    public Player player;
    private double[] eyeVector;
    private double[] cursor;
    private double[][] cursorAxis;

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

    // Start is called before the first frame update
    void Start() // ルーチン開始
    {
        SteamVR_Actions.controll.Activate(left);
        SteamVR_Actions.controll.Activate(right);

        addEvevts();

        posLeft = pose.GetLocalPosition(left); rotLeft = pose.GetLocalRotation(left);
        posRight = pose.GetLocalPosition(right); rotRight = pose.GetLocalRotation(right);

        optDefault = new Options();
        opt = new Options();
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
        queue = new Queue<string>();

        newGame(dim);
        active = true;
        StartCoroutine(tick());

        reg2 = new double[3];
        reg3 = new double[4];
        reg4 = new double[4];

        FileBrowser.HideDialog();
    }

    private void addEvevts()
    {
        move.AddOnStateDownListener((SteamVR_Action_Boolean fromBoolean, SteamVR_Input_Sources fromSource) =>
        {
            fromPosLeft = pose.GetLocalPosition(left); fromRotLeft = pose.GetLocalRotation(left);
        }, left);
        move.AddOnStateDownListener((SteamVR_Action_Boolean fromBoolean, SteamVR_Input_Sources fromSource) =>
        {
            fromPosRight = pose.GetLocalPosition(right); fromRotRight = pose.GetLocalRotation(right);
        }, right);
        menu.AddOnStateDownListener((SteamVR_Action_Boolean fromBoolean, SteamVR_Input_Sources fromSource) =>
        {
            openMenu();
        }, left);
        menu.AddOnStateDownListener((SteamVR_Action_Boolean fromBoolean, SteamVR_Input_Sources fromSource) =>
        {
            openMenu();
        }, left);
    }

    private void openMenu()
    {
        SteamVR_Actions.controll.Deactivate(left);
        SteamVR_Actions.controll.Deactivate(right);
        menuPanel.Activate(oa);
    }

    // Update is called once per frame
    // コントローラーの関係
    // 位置の確認、メニュー開く、ボタン入力
    void Update()
    {

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

        IModel model = new MapModel(this.dim, oa.omCurrent, oc(), oa.oeCurrent, ov());
        engine.newGame(this.dim, model, ov(), /*oa.opt.os,*/ ot(), true);

        setOptions(/*oa.opt.okc,*/ ot());
        interval = (int)Math.Ceiling(1000 / ot().frameRate);

        target = engine;
        saveOrigin = new double[dim];
        saveAxis = new double[dim][];
        for (int i = 0; i < dim; i++) saveAxis[i] = new double[dim];
    }

    IEnumerator tick()
    {
        int base_ = System.Environment.TickCount;
        while (true)
        {
            calcInput();
            //string s = takeQueue();
            //if (s == null)
            if (active)
            {
                // メニューからのコマンドはinactive中に行う
                // メニュー開いてたらスキップ
                // 入力処理（位置記録）
                // サークル押下時のposition記録、直前と変化量の記録
                // メインルーチン: target操作と更新、レンダリング（engine）
                // 時計処理
                controll();
                engine.renderAbsolute(eyeVector);
                GetComponent<MeshFilter>().sharedMesh = mesh;

                int now = System.Environment.TickCount;
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
            }
            //else
            //{
            //    yield return new WaitForSeconds(0.1f);

            //}
        }
    }

    private string takeQueue()
    {
        lock (queue)
        {
            try
            {
                return queue.Dequeue();
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }
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

        reg1 = this.transform.position - player.hmdTransform.position;
        for (int i = 0; i < 3; i++) eyeVector[i] = (double)reg1[i];
        Vec.normalize(eyeVector, eyeVector);
    }

    private double limit = 0.2;
    private double limitAng = Math.PI / 6.0;
    private void controll()
    {
        // save state

        IMove saveTarget = target;
        target.save(saveOrigin, saveAxis);

        if (leftMove)
        {
            for (int i = 0; i < 3; i++) reg2[i] = posLeft[i] - fromPosLeft[i];
            Vec.scale(reg2, reg2, 1.0 / Math.Max(limit, Vec.norm(reg2)));
            Array.Copy(reg2, reg3, 3);
            Quaternion relarot = rotLeft * Quaternion.Inverse(lastRotLeft);
            reg3[3] = Math.Asin(relarot.y) * Math.Sign(relarot.w);
            reg3[3] /= Math.Max(limitAng, reg3[3]);
            Vec.scale(reg3, reg3, dMove);
            Vec.fromAxisCoordinates(reg4, reg3, engine.getAxisArray());
            engine.move(reg4);
        }
        if (rightMove)
        {
            Vec.unitVector(reg3, 3);
            for (int i = 0; i < 3; i++) reg2[i] = posRight[i] - fromPosRight[i];
            double t = Vec.norm(reg2);
            if (t != 0)
            {
                t = dRotate * Math.Min(limit, t) / limit;
                Vec.normalize(reg2, reg2);
                for (int i = 0; i < 3; i++) reg4[i] = reg2[i] * Math.Sin(t);
                reg4[3] = Math.Cos(t);
                Debug.Log("2: " + Vec.ToString(reg3) + " to " + Vec.ToString(reg4));
                target.rotateAngle(reg3, reg4);
            }

            Quaternion relarot = rotRight * Quaternion.Inverse(lastRotRight);
            float f;
            relarot.ToAngleAxis(out f, out reg0);
            //f = (float)dRotate * f * Mathf.PI / 180 / Mathf.Max((float)limitAng, f * Mathf.PI / 180);
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
            Debug.Log("2: " + Vec.ToString(reg3) + " to " + Vec.ToString(reg4));
            target.rotateAngle(reg3, reg4);
        }
        if (leftTrigger)
        {

        }
        if (rightTrigger)
        {

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

            //if (alignMode && !target.isAligned())
            //{
            //    alignMode = false;
            //}
        }
    }

    public OptionsAll getOptionsAll()
    {
        return oa;
    }

    public void setOptions(/*OptionsKeysConfig okc,*/ OptionsMotion ot)
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
        dRotate = Math.PI / 2.0 / (double)nRotate;
        dAlignMove = 1 / (double)nAlignMove;
        dAlignRotate = Math.PI / 2.0 / (double)nAlignRotate;
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
        opt.ot4.timeRotate = 1;
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

        opt.ov4.depth = 2;
        opt.ov4.texture[0] = false;
        opt.ov4.texture[1] = false;
        opt.ov4.texture[2] = false;
        opt.ov4.texture[3] = false;
        opt.ov4.texture[4] = false;
        opt.ov4.texture[5] = false;
        opt.ov4.texture[6] = false;
        opt.ov4.texture[7] = false;
        opt.ov4.texture[8] = true;
        opt.ov4.texture[9] = false;
        opt.ov4.retina = 1.8;
        opt.ov4.scale = 0.6;

        opt.od.transparency = 0.3;
        opt.od.border = 1;

        opt.oo.moveInputType = 0;
        opt.oo.rotateInputType = 1;
        opt.oo.toggleLeftAndRight = false;
        opt.oo.toggleForward = false;
        opt.oo.toggleAlignMode = false;

        dim = 4;
        gameDirectory = null;

        SaveData.SetClass("optDefault", opt);
        opt = SaveData.GetClass("opt", opt);
    }
}
