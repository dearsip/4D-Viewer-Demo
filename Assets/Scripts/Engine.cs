using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/**
 * The game graphics engine.
 */

public class Engine : IMove
{

    // --- fields ---

    //private IDisplay displayInterface;
    private IModel model;

    private double[] origin;
    private double[][] axis;
    private bool win;

    private Clip.Result clipResult;
    private double fall;
    private double height;
    private double gravity;
    private const double gdef = 18;
    private const double hdef = 7.5;

    private double[][] sraxis;
    private double nonFisheyeRetina; // have to cache this up here for fisheye

    private PolygonBuffer bufAbsolute;
    private PolygonBuffer bufRelative;
    //private PolygonBuffer[] bufDisplay;

    private RenderRelative renderRelative;

    private double[][] objRetina, objCross, objWin, objDead;

    //private Display[] display;

    private int dimSpaceCache; // display-related cache of engine-level fields
    private bool enableCache;
    private int edgeCache;

    private int[] reg1; // temporary registers
    private int[] reg2;
    private double[] reg3;
    private double[] reg4;

    //private MainTransform mt;
    //private SideTransform st;
    //private CrossTransform ct;

    private double[] reg5;
    private List<Vector3> verts;
    private List<int> tris;
    private List<Color> cols;
    private Mesh mesh;

    // --- construction ---

    /**
     * Construct a new engine object.
     * After construction, you must call newGame before anything else.
     */
    public Engine(/*IDisplay displayInterface*/ Mesh mesh)
    {
        //this.displayInterface = displayInterface;

        // dimSpaceCache starts at zero, that will force rebuild ...
        //  ... so enableCache is irrelevant
        // edgeCache starts at zero
        this.mesh = mesh;
    }

    // --- helpers ---

    // caller must not change these!

    public double[] getOrigin()
    {
        return origin;
    }

    public double[] getViewAxis()
    {
        return axis[axis.Length - 1];
    }

    public double[][] getAxisArray()
    {
        return axis;
    }

    public IModel retrieveModel()
    {
        return model;
    }

    // --- games ---

    public void newGame(int dimSpace, IModel model, OptionsView ov, /*OptionsStereo os,*/ OptionsMotion ot, bool render)
    {

        this.model = model;

        origin = new double[dimSpace];
        axis = new double[dimSpace][];
        for (int i = 0; i < axis.Length; i++) axis[i] = new double[dimSpace];
        initPlayer();

        //if (getSaveType() == IModel.SAVE_ACTION
        // || getSaveType() == IModel.SAVE_BLOCK
        // || getSaveType() == IModel.SAVE_SHOOT) ((ActionModel)model).setEngine(this);

        sraxis = new double[dimSpace][];
        for (int i = 0; i < sraxis.Length; i++) sraxis[i] = new double[dimSpace];
        nonFisheyeRetina = ov.retina;

        bufAbsolute = new PolygonBuffer(dimSpace);
        bufRelative = new PolygonBuffer(dimSpace - 1);
        //bufDisplay = new PolygonBuffer[2];
        //bufDisplay[0] = new PolygonBuffer(2);
        //bufDisplay[1] = new PolygonBuffer(2); // may not be used

        model.setBuffer(bufAbsolute);
        renderRelative = new RenderRelative(bufAbsolute, bufRelative, dimSpace, getRetina());

        //if (dimSpace == 3)
        //{
        //    objRetina = objRetina2;
        //    objCross = objCross2;
        //    objWin = objWin2;
        //    objDead = objDead2;
        //}
        //else
        {
            //objRetina = objRetina3;
            objCross = objCross3;
            objWin = objWin3;
            //objDead = objDead3;
        }

        //setDisplay(dimSpace, ov.scale, /*os,*/ true);

        reg1 = new int[dimSpace];
        reg2 = new int[dimSpace];
        reg3 = new double[dimSpace];
        reg4 = new double[dimSpace];

        //mt = new MainTransform(reg3);
        //st = new SideTransform(reg3);
        //ct = new CrossTransform(reg3);

        reg5 = new double[3];
        verts = new List<Vector3>();
        tris = new List<int>();
        cols = new List<Color>();

        fall = 0;
        gravity = gdef / ot.frameRate / ot.frameRate;
        height = hdef / ot.frameRate;

        //if (render) renderAbsolute();
        // else we are loading a saved game, and will render later
    }

    private void initPlayer()
    {
        model.initPlayer(origin, axis);
        win = false;
    }

    public void resetWin()
    {
        if (win && !atFinish())
        {
            win = false;
            RenderRelative();
        }
    }

    public void restartGame()
    {
        initPlayer();
        //renderAbsolute();
    }

    public void toggleFisheye()
    {
        //OptionsFisheye.of.fisheye = !OptionsFisheye.of.fisheye;
        updateRetina();
    }

    private double getRetina()
    {
        return /*OptionsFisheye.of.fisheye ? 1 : */nonFisheyeRetina;
    }

    private void updateRetina()
    {
        renderRelative.setRetina(getRetina());
    }

    // --- implementation of IStorable ---

    private const string KEY_ORIGIN = "origin";
    private const string KEY_AXIS = "axis";
    private const string KEY_WIN = "win";

    //public void load(IStore store, bool alignMode)
    //{
    //    try
    //    {

    //        store.getObject(KEY_ORIGIN, origin);
    //        store.getObject(KEY_AXIS, axis);
    //        win = store.getBoolean(KEY_WIN);

    //        model.testOrigin(origin, reg1, reg2);

    //        // check that axes are orthonormal, more or less
    //        const double EPSILON = 0.001;
    //        for (int i = 0; i < axis.Length; i++)
    //        {
    //            for (int j = 0; j < axis.Length; j++)
    //            {
    //                double dotExpected = (i == j) ? 1 : 0; // delta_ij
    //                double dot = Vec.dot(axis[i], axis[j]);
    //                if (Math.Abs(dot - dotExpected) > EPSILON) throw App.getEmptyException();
    //            }
    //        }

    //        if (alignMode) align().snap();
    //        //
    //        // pseudo-validation to prevent being in align mode without being aligned.
    //        // this can only happen if someone modifies a file by hand
    //        //
    //        // a real validation would compare the current position to the align goal,
    //        // and if they were different, would snap to the goal and throw an exception
    //        // carrying a message similar to Engine.e1

    //    }
    //    catch (ValidationException e)
    //    {
    //        initPlayer();
    //        throw App.getException("Engine.e1"); // slight misuse of protocol to report information
    //    }
    //    finally
    //    {
    //        renderAbsolute();
    //    }
    //}

    public int getSaveType()
    {
        return model.getSaveType();
    }

    //public void save(IStore store)
    //{

    //    store.putObject(KEY_ORIGIN, origin);
    //    store.putObject(KEY_AXIS, axis);
    //    store.putBoolean(KEY_WIN, win);
    //}

    // --- options ---

    public void setColorMode(int colorMode)
    {
        model.setColorMode(colorMode);
    }

    public void setDepth(int depth)
    {
        model.setDepth(depth);
    }

    public void setTexture(bool[] texture)
    {
        model.setTexture(texture);
    }

    public void setRetina(double retina)
    {
        nonFisheyeRetina = retina;
        updateRetina();
    }

    //public void setScale(double scale)
    //{
    //    for (int i = 0; i < display.Length; i++) display[i].setScale(scale);
    //}

    //public void setScreenWidth(double screenWidth)
    //{
    //    for (int i = 0; i < display.Length; i++) display[i].setScreenWidth(screenWidth);
    //}

    //public void setScreenDistance(double screenDistance)
    //{
    //    for (int i = 0; i < display.Length; i++) display[i].setScreenDistance(screenDistance);
    //}

    //public void setEyeSpacing(double eyeSpacing)
    //{
    //    for (int i = 0; i < display.Length; i++) display[i].setEyeSpacing(eyeSpacing);
    //}

    //public void setTiltVertical(double tiltVertical)
    //{
    //    for (int i = 0; i < display.Length; i++) display[i].setTiltVertical(tiltVertical);
    //}

    //public void setTiltHorizontal(double tiltHorizontal)
    //{
    //    for (int i = 0; i < display.Length; i++) display[i].setTiltHorizontal(tiltHorizontal);
    //}

    // these last two option-setting functions are not invoked via the controller,
    // so they need to end with an explicit re-render call

    public void setOptions(OptionsColor oc, OptionsView ov, /*OptionsStereo os,*/ OptionsSeed oe, OptionsMotion ot, OptionsDisplay od)
    {

        model.setOptions(oc, oe.colorSeed, ov.depth, ov.texture, od);

        setRetina(ov.retina);

        //setDisplay(dimSpaceCache, ov.scale, /*os,*/ false);

        gravity = gdef / ot.frameRate / ot.frameRate;
        height = hdef / ot.frameRate;

        //renderAbsolute(); // not always necessary, but who cares, it's fast enough
    }

    //public void setEdge(int edge)
    //{
    //    edgeCache = edge;
    //    for (int i = 0; i < display.Length; i++) display[i].setEdge(edge);
    //    renderDisplay();
    //}

    // --- display ---

    private const int DISPLAY_MODE_NONE = 0;
    private const int DISPLAY_MODE_3D = 1;
    private const int DISPLAY_MODE_4D_MONO = 2;
    private const int DISPLAY_MODE_4D_STEREO = 3;

    private int getDisplayMode(int dimSpace, bool enable)
    {
        switch (dimSpace)
        {
            case 3: return DISPLAY_MODE_3D;
            case 4: return enable ? DISPLAY_MODE_4D_STEREO : DISPLAY_MODE_4D_MONO;
            default: return DISPLAY_MODE_NONE;
        }
    }

    private int getPanels(int mode)
    {
        return (mode == DISPLAY_MODE_4D_STEREO) ? 2 : 1;
    }

    //private void setDisplay(int dimSpace, double scale, /*OptionsStereo os,*/ bool force)
    //{

    //    int modeNew = getDisplayMode(dimSpace, os.enable);
    //    int modeOld = getDisplayMode(dimSpaceCache, enableCache);

    //    // here we are embedding the knowledge that the edge changes (and setEdge gets called)
    //    // if and only if the number of visible panels changes.
    //    // so, that's the condition we should use to decide when to clear the cache.
    //    // the goal is not to draw stereo displays until after re-layout occurs
    //    if (getPanels(modeNew) != getPanels(modeOld)) edgeCache = 0;

    //    if (modeNew != modeOld || force)
    //    { // must rebuild on new game, because buffers are changing
    //        dimSpaceCache = dimSpace;
    //        enableCache = os.enable;
    //        rebuildDisplay(scale, os);
    //    }
    //    else
    //    {
    //        for (int i = 0; i < display.Length; i++) display[i].setOptions(scale, os);
    //    }
    //}

    /**
     * A function that rebuilds the display objects, for when the stereo mode has changed.
     */
    //private void rebuildDisplay(double scale, OptionsStereo os)
    //{
    //    switch (getDisplayMode(dimSpaceCache, os.enable))
    //    {

    //        case DISPLAY_MODE_3D:

    //            display = new Display[1];
    //            display[0] = new DisplayScaled(bufRelative, bufDisplay[0], scale);

    //            displayInterface.setMode3D(bufDisplay[0]);
    //            break;

    //        case DISPLAY_MODE_4D_MONO:

    //            display = new Display[1];
    //            display[0] = new DisplayStereo(bufRelative, bufDisplay[0], 0, scale, os, edgeCache);

    //            displayInterface.setMode4DMono(bufDisplay[0]);
    //            break;

    //        case DISPLAY_MODE_4D_STEREO:

    //            display = new Display[2];
    //            display[0] = new DisplayStereo(bufRelative, bufDisplay[0], -1, scale, os, edgeCache);
    //            display[1] = new DisplayStereo(bufRelative, bufDisplay[1], +1, scale, os, edgeCache);

    //            displayInterface.setMode4DStereo(bufDisplay[0], bufDisplay[1]);
    //            break;
    //    }
    //}

    // --- motion ---

    public bool canMove(int a, double d)
    {

        if (getSaveType() != IModel.SAVE_ACTION
         && getSaveType() != IModel.SAVE_BLOCK
         && getSaveType() != IModel.SAVE_SHOOT)
        {
            Vec.addScaled(reg3, origin, axis[a], d);
            if (!model.canMove(origin, reg3, reg1, reg4)) return false;
        }

        return true;
    }

    private bool atFinish()
    {
        return model.atFinish(origin, reg1, reg2);
    }

    public void move(double[] d)
    {
        //const double epsilon = 0.00001;
        //if (getSaveType() != IModel.SAVE_ACTION
        // && getSaveType() != IModel.SAVE_BLOCK
        // && getSaveType() != IModel.SAVE_SHOOT)
        Vec.fromAxisCoordinates(reg3, d, axis);
        Vec.add(origin, origin, reg3);
        //else
        //{
        //    if (a == 1) return;
        //    Vec.unitVector(reg3, 1);
        //    double e = Vec.dot(reg3, axis[a]);
        //    Vec.addScaled(reg3, axis[a], reg3, -e);
        //    Vec.normalize(reg3, reg3);
        //    Vec.scale(reg3, reg3, d);
        //    Vec.add(reg3, origin, reg3);
        //    if (model.canMove(origin, reg3, reg1, reg4))
        //    {
        //        Vec.copy(origin, reg3);
        //    }
        //    else
        //    { // not functioning (climing)
        //        Clip.Result clipResult = ((ActionModel)model).getResult();
        //        int ib = clipResult.ib;
        //        Vec.unitVector(reg3, 1);
        //        Vec.addScaled(reg3, origin, reg3, (d > 0) ? d : -d);
        //        if ((Clip.clip(origin, reg3, ((GeomModel)model).retrieveShapes()[ib], clipResult) & Clip.KEEP_B) != 0)
        //        {
        //            Vec.unitVector(reg3, 1);
        //            Vec.addScaled(origin, origin, reg3, fall * clipResult.b + epsilon);
        //        }
        //    }
        //}
    }

    public void rotateAngle(double[] from, double[] to)
    {
        //if (getSaveType() != IModel.SAVE_ACTION
        // && getSaveType() != IModel.SAVE_BLOCK
        // && getSaveType() != IModel.SAVE_SHOOT)
        Vec.fromAxisCoordinates(reg3, from, axis);
        Vec.fromAxisCoordinates(reg4, to, axis);
        Vec.normalize(reg3, reg3);
        Vec.normalize(reg4, reg4);
        for (int i = 0; i < axis.Length; i++) Vec.rotate(axis[i], axis[i], reg3, reg4, from, to);
        //else
        //{
        //    const double epsilon = 0.000001;
        //    int dim = origin.Length;
        //    if (a2 == 1)
        //    {
        //        if (a1 != dim - 1) return;
        //        Vec.copy(reg3, axis[a1]);
        //        Vec.copy(reg4, axis[a2]);
        //        Vec.rotateAngle(axis[a1], axis[a2], axis[a1], axis[a2], theta);
        //        if (axis[a2][1] < epsilon)
        //        {
        //            Vec.copy(axis[a1], reg3);
        //            Vec.copy(axis[a2], reg4);
        //        }
        //    }
        //    else
        //    {
        //        double asin = Math.Asin(axis[a1][1]) * 180 / Math.PI;
        //        if (a1 == dim - 1)
        //        {
        //            Vec.rotateAngle(axis[1], axis[a1], axis[1], axis[a1], asin);
        //        }
        //        Vec.rotateAngle(axis[a1], axis[a2], axis[a1], axis[a2], theta);
        //        if (a1 == dim - 1)
        //        {
        //            Vec.rotateAngle(axis[a1], axis[1], axis[a1], axis[1], asin);
        //        }
        //    }
        //}
    }

    public Align align()
    {
        if (getSaveType() != IModel.SAVE_ACTION
         && getSaveType() != IModel.SAVE_BLOCK
         && getSaveType() != IModel.SAVE_SHOOT)
        {
            return new Align(origin, axis);
        }
        return null;
    }

    public bool isAligned()
    {
        return Align.isAligned(origin, axis);
    }

    public bool update(double[] saveOrigin, double[][] saveAxis, double[] viewOrigin)
    {
        if (model.canMove(saveOrigin, origin, reg1, reg4))
        {
            if (atFinish()) win = true;
            return true;
        }
        else
        {
            return false;
        }
    }

    public void save(double[] saveOrigin, double[][] saveAxis)
    {
        Vec.copy(saveOrigin, origin);
        Vec.copyMatrix(saveAxis, axis);
    }

    public void restore(double[] saveOrigin, double[][] saveAxis)
    {
        Vec.copy(origin, saveOrigin);
        Vec.copyMatrix(axis, saveAxis);
    }

    //public void jump()
    //{
    //    const double epsilon = 0.001;
    //    Vec.unitVector(reg3, 1);
    //    Vec.addScaled(reg3, origin, reg3, -epsilon);
    //    if (!model.canMove(origin, reg3, reg1, reg4) || reg3[1] < 0) fall = height;
    //}

    //public void Fall()
    //{
    //    const double epsilon = 0.00001;
    //    fall -= gravity;
    //    Vec.unitVector(reg3, 1);
    //    Vec.addScaled(reg3, origin, reg3, fall);
    //    if (reg3[1] < epsilon)
    //    {
    //        fall = 0;
    //        origin[1] = epsilon;
    //    }
    //    else if (model.canMove(origin, reg3, reg1, reg4))
    //    {
    //        Vec.copy(origin, reg3);
    //    }
    //    else
    //    {
    //        clipResult = ((ActionModel)model).getResult();
    //        Vec.unitVector(reg3, 1);
    //        Vec.addScaled(origin, origin, reg3, fall * clipResult.a + ((fall > 0) ? -epsilon : epsilon));
    //        fall = 0;
    //    }
    //    if (atFinish()) win = true;
    //}

    // --- rendering ---

    public void renderAbsolute(double[] eyeVector)
    {
        model.animate();
        model.render(origin);
        RenderRelative();
        bufRelative.sort(eyeVector);
    }

    private void renderObject(PolygonBuffer buf, double[][] obj)
    {
        renderObject(buf, obj, Color.white);
    }

    private void renderObject(PolygonBuffer buf, double[][] obj, Color color)
    {
        for (int i = 0; i < obj.Length; i += 3)
        {
            Polygon poly = buf.getNext();

            poly.vertex = new double[3][];
            for (int j = 0; j < 3; j++)
            {
                poly.vertex[j] = new double[3];
                for (int k = 0; k < 3; k++) poly.vertex[j][k] = obj[i + j][k];
            }
            poly.color = color;
        }
    }

    private void RenderRelative()
    {
        //if (OptionsFisheye.of.fisheye)
        //{
        //    renderPrepare();
        //    if (OptionsFisheye.of.rainbow && dimSpaceCache == 4)
        //    {
        //        renderRainbow();
        //    }
        //    else
        //    {
        //        renderFisheye();
        //    }
        //}
        //else
        {
            renderRelative.run(axis);
            //renderObject(bufRelative, objRetina);
            renderObject(bufRelative, objCross);
        }

        if (win) renderObject(bufRelative, objWin);
        //if (model.dead()) renderObject(bufRelative, objDead, Color.red);

        //renderDisplay();
        convert();
    }

    private void renderPrepare()
    {
        int f = sraxis.Length - 1; // forward

        Vec.zero(reg3);

        // no need to set first or last now,
        // they'll get set in the next loop
        for (int i = 1; i < f; i++)
        {
            Vec.copy(sraxis[i], axis[i]);
        }
    }

    //private void renderFisheye()
    //{
    //    int f = sraxis.Length - 1;

    //    renderRelative.run(axis, true, mt);
    //    renderRelative.runObject(objCross, -1, ct);

    //    for (int i = 0; i < f; i++)
    //    {
    //        renderPair(f, i, i);
    //    }
    //}

    //private void renderRainbow()
    //{
    //    int f = sraxis.Length - 1;

    //    reg3[1] = -OptionsFisheye.of.rdist;
    //    renderRelative.run(axis, true, mt);
    //    renderRelative.runObject(objRetina, r, mt);
    //    renderRelative.runObject(objCross, -1, ct);
    //    renderPair(f, 0, 0); // x pair offset in x

    //    Vec.copy(sraxis[3], axis[3]); // renderPair doesn't really put everything back

    //    // for the rest of them we have to rotate everything.
    //    // positive z goes to positive x, that's the
    //    // natural way because of the retina horizontal tilt.
    //    //
    //    Vec.scale(sraxis[2], axis[0], -1);
    //    Vec.copy(sraxis[0], axis[2]);

    //    reg3[1] = OptionsFisheye.of.rdist;
    //    renderRelative.run(sraxis, false, mt);
    //    renderRelative.runObject(objRetina, r, mt);
    //    renderRelative.runObject(objCross, -1, ct);
    //    renderPair(f, 2, 0); // z pair offset in x

    //    // no need to put back, we're done
    //}

    private static int[] rmask = new int[] { 0x7, 0xD, 0xE, 0xB, 0x677, 0x9DD, 0xCEE, 0x3BB, 0xFF0, 0xF0F };
    private const int r = 0x055; // rainbow mode main retina mask

    //private void renderPair(int f, int i, int j)
    //{
    //    int n = ((dimSpaceCache == 4) ? 4 : 0) + 2 * j;

    //    reg3[j] = OptionsFisheye.of.offset;
    //    Vec.scale(sraxis[j], axis[f], -1);
    //    Vec.copy(sraxis[f], axis[i]);
    //    st.configure(j, 1);
    //    renderRelative.run(sraxis, false, st);
    //    renderRelative.runObject(objRetina, rmask[n], st);

    //    reg3[j] = -OptionsFisheye.of.offset;
    //    Vec.copy(sraxis[j], axis[f]);
    //    Vec.scale(sraxis[f], axis[i], -1);
    //    st.configure(j, -1);
    //    renderRelative.run(sraxis, false, st);
    //    renderRelative.runObject(objRetina, rmask[n + 1], st);

    //    // now put everything back
    //    reg3[j] = 0;
    //    Vec.copy(sraxis[j], axis[i]); // incorrect in j != i case but it's the last thing we do
    //}

    //private void renderDisplay()
    //{
    //    for (int i = 0; i < display.Length; i++)
    //    {
    //        display[i].run();
    //    }
    //    displayInterface.nextFrame();
    //}

    private void convert()
    {
        int count = 0;
        Polygon p;
        verts.Clear();
        tris.Clear();
        cols.Clear();
        for (int i = 0; i < bufRelative.getSize(); i++)
        {
            p = bufRelative.get(i);
            int v = p.vertex.Length;
            for (int j = 0; j < v; j++)
            {
                reg5 = p.vertex[j];
                verts.Add(new Vector3((float)reg5[0], (float)reg5[1], (float)reg5[2]));
                cols.Add(p.color);
            }
            for (int j = 0; j < v - 2; j++)
            {
                tris.Add(count);
                tris.Add(count + j + 1);
                tris.Add(count + j + 2);
            }
            count += v;
        }
        if (verts.Count < mesh.vertices.Length) // triangles の参照する項が vertices から消えるとエラーを吐くため注意する
        {
            mesh.triangles = tris.ToArray();
            mesh.vertices = verts.ToArray();
        }
        else
        {
            mesh.vertices = verts.ToArray();
            mesh.triangles = tris.ToArray();
        }
        mesh.colors = cols.ToArray();
    }

    // --- fixed objects ---

    // private static readonly double[][] objRetina2 = new double[][] {
    //   new double[] {-1,-1}, new double[] { 1,-1},
    //      new double[] { 1,-1}, new double[] { 1, 1},
    //      new double[] { 1, 1}, new double[] {-1, 1},
    //      new double[] {-1, 1}, new double[] {-1,-1}
    //};

    // private static readonly double[][] objRetina3 = new double[][] {
    //   new double[] {-1,-1,-1}, new double[] { 1,-1,-1},
    //      new double[] { 1,-1,-1}, new double[] { 1, 1,-1},
    //      new double[] { 1, 1,-1}, new double[] {-1, 1,-1},
    //      new double[] {-1, 1,-1}, new double[] {-1,-1,-1},

    //      new double[] {-1,-1, 1}, new double[] { 1,-1, 1},
    //      new double[] { 1,-1, 1}, new double[] { 1, 1, 1},
    //      new double[] { 1, 1, 1}, new double[] {-1, 1, 1},
    //      new double[] {-1, 1, 1}, new double[] {-1,-1, 1},

    //      new double[] {-1,-1,-1}, new double[] {-1,-1, 1},
    //      new double[] { 1,-1,-1}, new double[] { 1,-1, 1},
    //      new double[] { 1, 1,-1}, new double[] { 1, 1, 1},
    //      new double[] {-1, 1,-1}, new double[] {-1, 1, 1}
    //};

    private const double B = 0.04;
    // private static readonly double[][] objCross2 = new double[][] {
    //   new double[] {-B, 0}, new double[] { B, 0},
    //      new double[] { 0,-B}, new double[] { 0, B}
    //};

    private const double C = 0.1;
    private static readonly double[][] objCross3 = new double[][] {
        new double[] {-C, 0, 0 }, new double[] { 0, B, 0 }, new double[] { 0, 0, 0 },
        new double[] {-C, 0, 0 }, new double[] { 0,-B, 0 }, new double[] { 0, 0, 0 },
        new double[] { C, 0, 0 }, new double[] { 0, B, 0 }, new double[] { 0, 0, 0 },
        new double[] { C, 0, 0 }, new double[] { 0,-B, 0 }, new double[] { 0, 0, 0 },
        new double[] { 0,-C, 0 }, new double[] { 0, 0, B }, new double[] { 0, 0, 0 },
        new double[] { 0,-C, 0 }, new double[] { 0, 0,-B }, new double[] { 0, 0, 0 },
        new double[] { 0, C, 0 }, new double[] { 0, 0, B }, new double[] { 0, 0, 0 },
        new double[] { 0, C, 0 }, new double[] { 0, 0,-B }, new double[] { 0, 0, 0 },
        new double[] { 0, 0,-C }, new double[] { B, 0, 0 }, new double[] { 0, 0, 0 },
        new double[] { 0, 0,-C }, new double[] {-B, 0, 0 }, new double[] { 0, 0, 0 },
        new double[] { 0, 0, C }, new double[] { B, 0, 0 }, new double[] { 0, 0, 0 },
        new double[] { 0, 0, C }, new double[] {-B, 0, 0 }, new double[] { 0, 0, 0 },
    };

   // private static readonly double[][] objWin2 = new double[][] {
   //   new double[] {-0.8, 0.4}, new double[] {-0.8,-0.4},
   //      new double[] {-0.8,-0.4}, new double[] {-0.6, 0  },
   //      new double[] {-0.6, 0  }, new double[] {-0.4,-0.4},
   //      new double[] {-0.4,-0.4}, new double[] {-0.4, 0.4},

   //      new double[] {-0.1, 0.4}, new double[] { 0.1, 0.4},
   //      new double[] { 0,   0.4}, new double[] { 0,  -0.4},
   //      new double[] {-0.1,-0.4}, new double[] { 0.1,-0.4},

   //      new double[] { 0.4,-0.4}, new double[] { 0.4, 0.4},
   //      new double[] { 0.4, 0.4}, new double[] { 0.8,-0.4},
   //      new double[] { 0.8,-0.4}, new double[] { 0.8, 0.4}
   //};

    private static readonly double[][] objWin3 = new double[][] {
        new double[] {-0.8, 0.4,-1}, new double[] {-0.7,-0.4,-1}, new double[] {-0.9, 0.4,-1},
        new double[] {-0.7,-0.4,-1}, new double[] {-0.6, 0.1,-1}, new double[] {-0.6, 0.2,-1},
        new double[] {-0.6, 0.1,-1}, new double[] {-0.5,-0.4,-1}, new double[] {-0.6, 0.2,-1},
        new double[] {-0.5,-0.4,-1}, new double[] {-0.4, 0.4,-1}, new double[] {-0.3, 0.4,-1},

        new double[] {-0.05, 0.4,-1}, new double[] { 0.05, 0.4,-1}, new double[] { 0,   0,  -1},
        new double[] {-0.05,-0.4,-1}, new double[] { 0.05,-0.4,-1}, new double[] { 0,   0,  -1},

        new double[] { 0.4,-0.4,-1}, new double[] { 0.4, 0.4,-1}, new double[] { 0.5,-0.4,-1},
        new double[] { 0.4, 0.4,-1}, new double[] { 0.8,-0.4,-1}, new double[] { 0.8,-0.3,-0.999},
        new double[] { 0.8,-0.4,-1}, new double[] { 0.8, 0.4,-1}, new double[] { 0.7, 0.4,-1}
   };

   // private static readonly double[][] objDead2 = new double[][] {
   //   new double[] {-1,-1}, new double[] { 1, 1}, new double[] { 1,-1}, new double[] {-1, 1}
   //};

   // private static readonly double[][] objDead3 = new double[][] {
   //   new double[] {-1,-1,-1}, new double[] { 1, 1, 1}, new double[] {-1,-1, 1}, new double[] { 1, 1,-1},
   //     new double[] {-1, 1,-1}, new double[] { 1,-1, 1}, new double[] { 1,-1,-1}, new double[] {-1, 1, 1}
   //};

}

