using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;
using System;
using UnityEngine.UI;

public class Menu : MonoBehaviour
{
    public Core core;
    public SteamVR_Action_Boolean interactUI, menu;
    public SteamVR_Input_Sources left, right;
    public Hand leftHand, rightHand;
    public GameObject leftLaser, rightLaser;
    private Options optDefault;
    private OptionsAll oaResult;

    public Transform parent;
    public Transform head;

    public Slider dimSlider, sizeSlider, densitySlider, twistProbabilitySlider, branchProbabilitySlider, loopCrossProbabilitySlider,
        dimSameParallelSlider, dimSamePerpendicularSlider, depthSlider, retinaSlider, scaleSlider, trainSpeedSlider,
        transparencySlider, borderSlider, frameRateSlider, timeMoveSlider, timeRotateSlider, timeAlignMoveSlider, timeAlignRotateSlider;
    public InputField dimCurrent, dimNext, sizeCurrent, sizeNext, densityCurrent, densityNext, twistPobabilityCurrent, twistProbabilityNext,
        branchProbabilityCurrent, branchProbabilityNext, loopCrossProbabilityCurrent, loopCrossProbabilityNext, dimSameParallelField,
        dimSamePerpendicularField, mazeCurrent, mazeNext, colorCurrent, colorNext, depthField, retinaField, scaleField, trainSpeedField,
        transparencyField, borderField, frameRateField, timeMoveField, timeRotateField, timeAlignMoveField, timeAlignRotateField,
        width, flare, rainbowGap;
    public Toggle allowLoopsCurrent, allowLoopsNext, useEdgeColor, hideSel, invertNormals, separate, map, invertLeftAndRight, invertForward,
        alignMode, sliceMode, limit3D, fisheye, custom, rainbow;
    public Toggle[] enable, texture;
    public Dropdown colorMode, moveType, rotateType;

    private void Start()
    {

        SteamVR_Actions._default.Deactivate(left);
        SteamVR_Actions._default.Deactivate(right);
        interactUI.AddOnStateDownListener((SteamVR_Action_Boolean fromBoolean, SteamVR_Input_Sources fromSource) =>
        {
            if (gameObject.activeSelf)
            {
                rightLaser.SetActive(false);
                rightHand.useRaycastHover = false;
                leftLaser.SetActive(true);
                leftHand.useRaycastHover = true;
            }
        }, left);
        interactUI.AddOnStateDownListener((SteamVR_Action_Boolean fromBoolean, SteamVR_Input_Sources fromSource) =>
        {
            if (gameObject.activeSelf)
            {
                leftLaser.SetActive(false);
                leftHand.useRaycastHover = false;
                rightLaser.SetActive(true);
                rightHand.useRaycastHover = true;
            }
        }, right);
        menu.AddOnStateUpListener((SteamVR_Action_Boolean fromBoolean, SteamVR_Input_Sources fromSource) =>
        {
            if (gameObject.activeSelf)
            {
                doCancel();
            }
        }, left);
        menu.AddOnStateUpListener((SteamVR_Action_Boolean fromBoolean, SteamVR_Input_Sources fromSource) =>
        {
            if (gameObject.activeSelf)
            {
                doCancel();
            }
        }, right);
    }

    public void Activate(OptionsAll oa)
    {
        SteamVR_Actions._default.Activate(left);
        SteamVR_Actions._default.Activate(right);
        gameObject.SetActive(true);
        rightLaser.SetActive(true);
        rightHand.useRaycastHover = true;

        parent.LookAt(head);
        parent.LookAt(new Vector3(parent.forward.x, 0, parent.forward.z) + parent.position);

        put(dimCurrent, oa.omCurrent.dimMap);
        put(dimNext, dimSlider, oa.opt.om4.dimMap);
        put(sizeCurrent, oa.omCurrent.size[0]);
        put(sizeNext, sizeSlider, oa.opt.om4.size);
        put(densityCurrent, oa.omCurrent.density);
        put(densityNext, densitySlider, oa.opt.om4.density);
        put(twistPobabilityCurrent, oa.omCurrent.twistProbability);
        put(twistProbabilityNext, twistProbabilitySlider, oa.opt.om4.twistProbability);
        put(branchProbabilityCurrent, oa.omCurrent.branchProbability);
        put(branchProbabilityNext, branchProbabilitySlider, oa.opt.om4.branchProbability);
        put(allowLoopsCurrent, oa.omCurrent.allowLoops);
        put(allowLoopsNext, oa.opt.om4.allowLoops);
        put(loopCrossProbabilityCurrent, oa.omCurrent.loopCrossProbability);
        put(loopCrossProbabilityNext, loopCrossProbabilitySlider, oa.opt.om4.loopCrossProbability);

        put(colorMode, oa.opt.oc4.colorMode);
        put(dimSameParallelField, dimSameParallelSlider, oa.opt.oc4.dimSameParallel);
        put(dimSamePerpendicularField, dimSamePerpendicularSlider, oa.opt.oc4.dimSamePerpendicular);
        put(enable, oa.opt.oc4.enable);

        put(mazeCurrent, oa.oeCurrent.mapSeed);
        put(colorCurrent, oa.oeCurrent.colorSeed);

        put(depthField, depthSlider, oa.opt.ov4.depth);
        put(texture, oa.opt.ov4.texture);
        put(retinaField, retinaSlider, oa.opt.ov4.retina);
        put(scaleField, scaleSlider, oa.opt.ov4.scale);

        put(transparencyField, transparencySlider, oa.opt.od.transparency);
        put(borderField, borderSlider, oa.opt.od.border);
        put(useEdgeColor, oa.opt.od.useEdgeColor);
        put(hideSel, oa.opt.od.hidesel);
        put(invertNormals, oa.opt.od.invertNormals);
        put(separate, oa.opt.od.separate);
        put(map, oa.opt.od.map);
        put(trainSpeedField, trainSpeedSlider, oa.opt.od.trainSpeed);

        put(moveType, oa.opt.oo.moveInputType);
        put(rotateType, oa.opt.oo.rotateInputType);
        put(invertLeftAndRight, oa.opt.oo.invertLeftAndRight);
        put(invertForward, oa.opt.oo.invertForward);
        put(alignMode, core.alignMode);
        put(sliceMode, oa.opt.oo.sliceMode);
        put(limit3D, oa.opt.oo.limit3D);

        put(frameRateField, frameRateSlider, oa.opt.ot4.frameRate);
        put(timeMoveField, timeMoveSlider, oa.opt.ot4.timeMove);
        put(timeRotateField, timeRotateSlider, oa.opt.ot4.timeRotate);
        put(timeAlignMoveField, timeAlignMoveSlider, oa.opt.ot4.timeAlignMove);
        put(timeAlignRotateField, timeAlignRotateSlider, oa.opt.ot4.timeAlignRotate);

        put(fisheye, OptionsFisheye.of.fisheye);
        put(custom, OptionsFisheye.of.adjust);
        put(rainbow, OptionsFisheye.of.rainbow);
        put(width, OptionsFisheye.of.width);
        put(flare, OptionsFisheye.of.flare);
        put(rainbowGap, OptionsFisheye.of.rainbowGap);
    }

    public void doUpdate()
    {
        OptionsAll oa = core.getOptionsAll();
        try
        {
            oa.opt.oc4.colorMode = getInt(colorMode);
            oa.opt.oc4.dimSameParallel = getInt(dimSameParallelField, OptionsColor.DIM_SAME_MIN, OptionsColor.DIM_SAME_MAX);
            oa.opt.oc4.dimSamePerpendicular = getInt(dimSamePerpendicularField, OptionsColor.DIM_SAME_MIN, OptionsColor.DIM_SAME_MAX);
            getBool(enable, oa.opt.oc4.enable);

            oa.opt.ov4.depth = getInt(depthField, OptionsView.DEPTH_MIN, OptionsView.DEPTH_MAX);
            getBool(texture, oa.opt.ov4.texture);
            oa.opt.ov4.retina = getDouble(retinaField, false);
            oa.opt.ov4.scale = getDouble(scaleField, OptionsView.SCALE_MIN, OptionsView.SCALE_MAX, false);

            oa.opt.od.transparency = getDouble(transparencyField, OptionsDisplay.TRANSPARENCY_MIN, OptionsDisplay.TRANSPARENCY_MAX, true);
            oa.opt.od.border = getDouble(borderField, OptionsDisplay.BORDER_MIN, OptionsDisplay.BORDER_MAX, true);
            oa.opt.od.useEdgeColor = getBool(useEdgeColor);
            oa.opt.od.hidesel = getBool(hideSel);
            oa.opt.od.invertNormals = getBool(invertNormals);
            oa.opt.od.separate = getBool(separate);
            oa.opt.od.map = getBool(map);
            oa.opt.od.trainSpeed = getInt(trainSpeedField, OptionsDisplay.TRAINSPEED_MIN, OptionsDisplay.TRAINSPEED_MAX);

            oa.opt.oo.sliceMode = getBool(sliceMode);
            oa.opt.oo.limit3D = getBool(limit3D);

            OptionsFisheye ofTemp = new OptionsFisheye();
            ofTemp.fisheye = getBool(fisheye);
            ofTemp.adjust = getBool(custom);
            ofTemp.rainbow = getBool(rainbow);
            ofTemp.width = getDouble(width, 0, 1, false);
            ofTemp.flare = getDouble(flare, 0, 1, true);
            ofTemp.rainbowGap = getDouble(rainbowGap, 0, 1, true);
            OptionsFisheye.copy(OptionsFisheye.of, ofTemp);
            OptionsFisheye.recalculate();
        }
        catch (Exception e) { Debug.Log(e); }
        core.menuCommand = core.updateOptions;
    }

    public void doOK()
    {
        OptionsAll oa = core.getOptionsAll();
        try
        {
            oa.opt.om4.dimMap = getInt(dimNext, OptionsMap.DIM_MAP_MIN, OptionsMap.DIM_MAP_MAX);
            getDimMap(oa.opt.om4.size, sizeNext);
            oa.opt.om4.density = getDouble(densityNext, OptionsMap.DENSITY_MIN, OptionsMap.DIM_MAP_MAX, true);
            oa.opt.om4.twistProbability = getDouble(twistProbabilityNext, OptionsMap.PROBABILITY_MIN, OptionsMap.PROBABILITY_MAX, true);
            oa.opt.om4.branchProbability = getDouble(branchProbabilityNext, OptionsMap.PROBABILITY_MIN, OptionsMap.PROBABILITY_MAX, true);
            oa.opt.om4.allowLoops = getBool(allowLoopsNext);
            oa.opt.om4.loopCrossProbability = getDouble(loopCrossProbabilityNext, OptionsMap.PROBABILITY_MIN, OptionsMap.PROBABILITY_MAX, true);

            if (mazeNext.text.Length > 0)
            {
                oa.oeNext.mapSeedSpecified = true;
                oa.oeNext.mapSeed = getInt(mazeNext);
            }
            else oa.oeNext.mapSeedSpecified = false;
            if(colorCurrent.text.Length > 0)
            {
                oa.oeCurrent.colorSeed = getInt(colorCurrent);
                oa.oeCurrent.forceSpecified();
            }
            if(colorNext.text.Length > 0)
            {
                oa.oeNext.colorSeedSpecified = true;
                oa.oeNext.colorSeed = getInt(colorNext);
            }
            else oa.oeNext.colorSeedSpecified = false;

            oa.opt.oo.moveInputType = getInt(moveType);
            oa.opt.oo.rotateInputType = getInt(rotateType);
            oa.opt.oo.invertLeftAndRight = getBool(invertLeftAndRight);
            oa.opt.oo.invertForward = getBool(invertForward);

            oa.opt.ot4.frameRate = getInt(frameRateField, false);
            oa.opt.ot4.timeMove = getDouble(timeMoveField, false);
            oa.opt.ot4.timeRotate = getDouble(timeRotateField, false);
            oa.opt.ot4.timeAlignMove = getDouble(timeAlignMoveField, false);
            oa.opt.ot4.timeAlignRotate = getDouble(timeAlignRotateField, false);
        }
        catch (Exception e) { Debug.Log(e); }

        // command
        core.menuCommand = core.setOptions;
        SteamVR_Actions._default.Deactivate(left);
        SteamVR_Actions._default.Deactivate(right);
        doCancel();
    }

    public void doCancel()
    {
        leftLaser.SetActive(false);
        leftHand.useRaycastHover = false;
        rightLaser.SetActive(false);
        rightHand.useRaycastHover = false;
        SteamVR_Actions._default.Deactivate(left);
        SteamVR_Actions._default.Deactivate(right);
        core.closeMenu();
    }

    public void doToggleAlignMode()
    {
        int n = core.getSaveType();
        if (n == IModel.SAVE_MAZE || n == IModel.SAVE_GEOM || n == IModel.SAVE_NONE)
        {
            core.alignMode = alignMode.isOn;
            if (alignMode.isOn) core.command = core.align;
        }
    }

    public void doAlign()
    {
        core.command = core.align;
    }

    public void doNewGame()
    {
        core.menuCommand = core.newGame;
    }

    private void put(InputField inputField, Slider slider, double value)
    {
        slider.value = (float)value;
        inputField.text = value.ToString();
    }

    private void put(InputField inputField, Slider slider, int[] value)
    {
        slider.value = value[0];
        inputField.text = Vec.ToString(value);
    }
    private void put(InputField inputField, double value)
    {
        inputField.text = value.ToString();
    }

    private void put(InputField inputField, Slider slider, int value)
    {
        slider.value = value;
        inputField.text = value.ToString();
    }

    private void put(Toggle toggle, bool value)
    {
        toggle.isOn = value;
    }

    private void put(Toggle[] toggle, bool[] value)
    {
        for (int i = 0; i < toggle.Length; i++) toggle[i].isOn = value[i];
    }

    private void put(Dropdown dropdown, int value)
    {
        dropdown.value = value;
    }

    private int getInt(InputField inputField)
    {
        int i = int.Parse(inputField.text);
        return i;
    }

    private int getInt(InputField inputField, bool allowZero)
    {
        int i = int.Parse(inputField.text);
        if (i < 0 || (!allowZero && i == 0)) throw new Exception();
        return i;
    }

    private int getInt(InputField inputField, int min, int max)
    {
        int i = int.Parse(inputField.text);
        if (i < min || i > max) throw new Exception();
        return i;
    }

    private double getDouble(InputField inputField, bool allowZero)
    {
        double d = float.Parse(inputField.text);
        if (d < 0 || (!allowZero && d == 0)) throw new Exception();
        return d;
    }

    private double getDouble(InputField inputField, double min, double max, bool allowMin)
    {
        double d = float.Parse(inputField.text);
        if (d > max || d < min || (!allowMin && d == min)) throw new Exception();
        return d;
    }

    readonly char[] separator = new char[] { ',' };
    private void getDimMap(int[] dest, InputField inputField)
    {
        string[] reg = inputField.text.Split(separator);
        if (reg.Length == 1)
        {
            int j = int.Parse(reg[0].Trim());
            for (int i = 0; i < dest.Length; i++) dest[i] = j;
            return;
        }
        int[] n = new int[dest.Length];
        for (int i = 0; i < n.Length; i++) n[i] = int.Parse(reg[i].Trim());
        for (int i = 0; i < n.Length; i++) dest[i] = n[i];
    }

    private int getInt(Dropdown dropdown) { return dropdown.value; }

    private bool getBool(Toggle toggle) { return toggle.isOn; }

    private void getBool(Toggle[] toggle, bool[] bools) { for (int i = 0; i < toggle.Length; i++) bools[i] = getBool(toggle[i]); }
}
