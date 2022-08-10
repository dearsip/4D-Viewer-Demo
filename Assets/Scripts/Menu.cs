﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;
using System;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
    public Core core;
    public SteamVR_Action_Boolean interactUI, menu;
    public SteamVR_Input_Sources left, right;
    public Hand leftHand, rightHand;
    public GameObject leftLaser, rightLaser;
    private Options optDefault;
    private OptionsAll oaResult;
    private bool isActivating;

    public Transform parent;
    public Transform head;
    private Vector3 defaultPosition;
    private Quaternion defaultRotation;
    private Vector3 defaultScale;

    public Slider dimSlider, sizeSlider, densitySlider, twistProbabilitySlider, branchProbabilitySlider, loopCrossProbabilitySlider,
        dimSameParallelSlider, dimSamePerpendicularSlider, depthSlider, retinaSlider, scaleSlider, trainSpeedSlider,
        transparencySlider, borderSlider, baseTransparencySlider, sliceTransparencySlider, frameRateSlider, timeMoveSlider, timeRotateSlider,
        timeAlignMoveSlider, timeAlignRotateSlider;
    public InputField dimCurrent, dimNext, sizeCurrent, sizeNext, densityCurrent, densityNext, twistPobabilityCurrent, twistProbabilityNext,
        branchProbabilityCurrent, branchProbabilityNext, loopCrossProbabilityCurrent, loopCrossProbabilityNext, dimSameParallelField,
        dimSamePerpendicularField, mazeCurrent, mazeNext, colorCurrent, colorNext, depthField, retinaField, scaleField, trainSpeedField,
        transparencyField, borderField, baseTransparencyField, sliceTransparencyField, frameRateField, timeMoveField, timeRotateField, 
        timeAlignMoveField, timeAlignRotateField, width, flare, rainbowGap;
    public Toggle allowLoopsCurrent, allowLoopsNext, usePolygon, useEdgeColor, hideSel, invertNormals, separate, map, invertLeftAndRight, invertForward,
        invertYawAndPitch, invertRoll, alignMode, sliceMode, limit3D, fisheye, custom, rainbow;
    public Toggle[] enable, texture;
    public Dropdown colorMode, inputTypeLeftAndRight, inputTypeForward, inputTypeYawAndPitch, inputTypeRoll;

    private void Start()
    {

        SteamVR_Actions._default.Deactivate(left);
        SteamVR_Actions._default.Deactivate(right);
        interactUI.AddOnStateDownListener(LeftActivate, left);
        interactUI.AddOnStateDownListener(RightActivate, right);
        menu.AddOnStateUpListener(CloseMenu, left);
        menu.AddOnStateUpListener(CloseMenu, right);
        defaultPosition = transform.localPosition;
        defaultRotation = transform.localRotation;
        defaultScale = transform.localScale;
    }

    private void LeftActivate(SteamVR_Action_Boolean fromBoolean, SteamVR_Input_Sources fromSource)
    {
        if (gameObject.activeSelf)
        {
            rightLaser.SetActive(false);
            rightHand.useRaycastHover = false;
            leftLaser.SetActive(true);
            leftHand.useRaycastHover = true;
        }
    }

    private void RightActivate(SteamVR_Action_Boolean fromBoolean, SteamVR_Input_Sources fromSource)
    {
        if (gameObject.activeSelf)
        {
            leftLaser.SetActive(false);
            leftHand.useRaycastHover = false;
            rightLaser.SetActive(true);
            rightHand.useRaycastHover = true;
        }
    }

    private void CloseMenu(SteamVR_Action_Boolean fromBoolean, SteamVR_Input_Sources fromSource)
    {
        if (gameObject.activeSelf)
        {
            doCancel();
        }
    }

    public void OnDestroy()
    {
        interactUI.RemoveOnStateDownListener(LeftActivate, left);
        interactUI.RemoveOnStateDownListener(RightActivate, right);
        menu.RemoveOnStateUpListener(CloseMenu, left);
        menu.RemoveOnStateUpListener(CloseMenu, right);
    }

    public void Activate(OptionsAll oa)
    {
        isActivating = true;
        SteamVR_Actions._default.Activate(left);
        SteamVR_Actions._default.Activate(right);
        gameObject.SetActive(true);
        rightLaser.SetActive(true);
        rightHand.useRaycastHover = true;

        if (Input.GetKeyDown(KeyCode.Escape)) {
            GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        }
        else {
            GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            transform.localPosition = defaultPosition;
            transform.localRotation = defaultRotation;
            transform.localScale = defaultScale;
            parent.LookAt(head);
            parent.LookAt(new Vector3(parent.forward.x, 0, parent.forward.z) + parent.position);
        }

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
        put(usePolygon, oa.opt.od.usePolygon);
        put(borderField, borderSlider, oa.opt.od.border);
        put(useEdgeColor, oa.opt.od.useEdgeColor);
        put(hideSel, oa.opt.od.hidesel);
        put(invertNormals, oa.opt.od.invertNormals);
        put(separate, oa.opt.od.separate);
        put(map, oa.opt.od.map);
        put(trainSpeedField, trainSpeedSlider, oa.opt.od.trainSpeed);

        put(inputTypeLeftAndRight, oa.opt.oo.inputTypeLeftAndRight);
        put(inputTypeForward, oa.opt.oo.inputTypeForward);
        put(inputTypeYawAndPitch, oa.opt.oo.inputTypeYawAndPitch);
        put(inputTypeRoll, oa.opt.oo.inputTypeRoll);
        put(invertLeftAndRight, oa.opt.oo.invertLeftAndRight);
        put(invertForward, oa.opt.oo.invertForward);
        put(invertYawAndPitch, oa.opt.oo.invertYawAndPitch);
        put(invertRoll, oa.opt.oo.invertRoll);
        put(alignMode, core.alignMode);
        put(sliceMode, oa.opt.oo.sliceMode);
        put(baseTransparencyField, baseTransparencySlider, oa.opt.oo.baseTransparency);
        put(sliceTransparencyField, sliceTransparencySlider, oa.opt.oo.sliceTransparency);
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

        isActivating = false;
    }

    public void doUpdate()
    {
        if (isActivating) return;
        OptionsAll oa = core.getOptionsAll();
        oa.opt.oc4.colorMode = getInt(colorMode);
        getInt(ref oa.opt.oc4.dimSameParallel, dimSameParallelField, OptionsColor.DIM_SAME_MIN, OptionsColor.DIM_SAME_MAX);
        getInt(ref oa.opt.oc4.dimSamePerpendicular, dimSamePerpendicularField, OptionsColor.DIM_SAME_MIN, OptionsColor.DIM_SAME_MAX);
        getBool(enable, oa.opt.oc4.enable);

        getInt(ref oa.opt.ov4.depth, depthField, OptionsView.DEPTH_MIN, OptionsView.DEPTH_MAX);
        getBool(texture, oa.opt.ov4.texture);
        getDouble(ref oa.opt.ov4.retina, retinaField, false);
        getDouble(ref oa.opt.ov4.scale, scaleField, OptionsView.SCALE_MIN, OptionsView.SCALE_MAX, false);

        getDouble(ref oa.opt.od.transparency, transparencyField, OptionsDisplay.TRANSPARENCY_MIN, OptionsDisplay.TRANSPARENCY_MAX, true);
        oa.opt.od.usePolygon = getBool(usePolygon);
        getDouble(ref oa.opt.od.border, borderField, OptionsDisplay.BORDER_MIN, OptionsDisplay.BORDER_MAX, true);
        oa.opt.od.useEdgeColor = getBool(useEdgeColor);
        oa.opt.od.hidesel = getBool(hideSel);
        oa.opt.od.invertNormals = getBool(invertNormals);
        oa.opt.od.separate = getBool(separate);
        oa.opt.od.map = getBool(map);
        getInt(ref oa.opt.od.trainSpeed, trainSpeedField, OptionsDisplay.TRAINSPEED_MIN, OptionsDisplay.TRAINSPEED_MAX);

        oa.opt.oo.inputTypeLeftAndRight = getInt(inputTypeLeftAndRight);
        oa.opt.oo.inputTypeForward = getInt(inputTypeForward);
        oa.opt.oo.inputTypeYawAndPitch = getInt(inputTypeYawAndPitch);
        oa.opt.oo.inputTypeRoll = getInt(inputTypeRoll);
        oa.opt.oo.invertLeftAndRight = getBool(invertLeftAndRight);
        oa.opt.oo.invertForward = getBool(invertForward);
        oa.opt.oo.invertYawAndPitch = getBool(invertYawAndPitch);
        oa.opt.oo.invertRoll = getBool(invertRoll);
        oa.opt.oo.sliceMode = getBool(sliceMode);
        getFloat(ref oa.opt.oo.baseTransparency, baseTransparencyField, true);
        getFloat(ref oa.opt.oo.sliceTransparency, sliceTransparencyField, true);
        oa.opt.oo.limit3D = getBool(limit3D);

        OptionsFisheye ofTemp = new OptionsFisheye();
        ofTemp.fisheye = getBool(fisheye);
        ofTemp.adjust = getBool(custom);
        ofTemp.rainbow = getBool(rainbow);
        getDouble(ref ofTemp.width, width, 0, 1, false);
        getDouble(ref ofTemp.flare, flare, 0, 1, true);
        getDouble(ref ofTemp.rainbowGap, rainbowGap, 0, 1, true);
        OptionsFisheye.copy(OptionsFisheye.of, ofTemp);
        OptionsFisheye.recalculate();
        core.menuCommand = core.updateOptions;
    }

    public void doOK()
    {
        OptionsAll oa = core.getOptionsAll();
        getInt(ref oa.opt.om4.dimMap, dimNext, OptionsMap.DIM_MAP_MIN, OptionsMap.DIM_MAP_MAX);
        getDimMap(oa.opt.om4.size, sizeNext);
        getDouble(ref oa.opt.om4.density, densityNext, OptionsMap.DENSITY_MIN, OptionsMap.DIM_MAP_MAX, true);
        getDouble(ref oa.opt.om4.twistProbability, twistProbabilityNext, OptionsMap.PROBABILITY_MIN, OptionsMap.PROBABILITY_MAX, true);
        getDouble(ref oa.opt.om4.branchProbability, branchProbabilityNext, OptionsMap.PROBABILITY_MIN, OptionsMap.PROBABILITY_MAX, true);
        oa.opt.om4.allowLoops = getBool(allowLoopsNext);
        getDouble(ref oa.opt.om4.loopCrossProbability, loopCrossProbabilityNext, OptionsMap.PROBABILITY_MIN, OptionsMap.PROBABILITY_MAX, true);

        if (mazeNext.text.Length > 0)
        {
            oa.oeNext.mapSeedSpecified = true;
            getInt(ref oa.oeNext.mapSeed, mazeNext);
        }
        else oa.oeNext.mapSeedSpecified = false;
        if(colorCurrent.text.Length > 0)
        {
            getInt(ref oa.oeCurrent.colorSeed, colorCurrent);
            oa.oeCurrent.forceSpecified();
        }
        if(colorNext.text.Length > 0)
        {
            oa.oeNext.colorSeedSpecified = true;
            getInt(ref oa.oeNext.colorSeed, colorNext);
        }
        else oa.oeNext.colorSeedSpecified = false;

        oa.opt.oo.inputTypeLeftAndRight = getInt(inputTypeLeftAndRight);
        oa.opt.oo.inputTypeForward = getInt(inputTypeForward);
        oa.opt.oo.inputTypeYawAndPitch = getInt(inputTypeYawAndPitch);
        oa.opt.oo.inputTypeRoll = getInt(inputTypeRoll);
        oa.opt.oo.invertLeftAndRight = getBool(invertLeftAndRight);
        oa.opt.oo.invertForward = getBool(invertForward);
        oa.opt.oo.invertYawAndPitch = getBool(invertYawAndPitch);
        oa.opt.oo.invertRoll = getBool(invertRoll);

        getDouble(ref oa.opt.ot4.frameRate, frameRateField, false);
        getDouble(ref oa.opt.ot4.timeMove, timeMoveField, false);
        getDouble(ref oa.opt.ot4.timeRotate, timeRotateField, false);
        getDouble(ref oa.opt.ot4.timeAlignMove, timeAlignMoveField, false);
        getDouble(ref oa.opt.ot4.timeAlignRotate, timeAlignRotateField, false);

        oa.omCurrent = oa.opt.om4;
        oa.ocCurrent = oa.opt.oc4;
        oa.ovCurrent = oa.opt.ov4;

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
        doOK();
        core.menuCommand = core.newGame;
    }

    private void put(InputField inputField, Slider slider, float value)
    {
        slider.value = value;
        inputField.text = value.ToString();
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

    private void put(InputField inputField, float value)
    {
        inputField.text = value.ToString();
    }

    private void put(InputField inputField, double value)
    {
        inputField.text = value.ToString();
    }

    private void put(Slider slider, double value)
    {
        slider.value = (float)value;
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

    private void getInt(ref int i, InputField inputField)
    {
        try {
            i = int.Parse(inputField.text);
        } catch (Exception e) { Debug.LogException(e); }
    }

    private void getInt(ref int i, InputField inputField, bool allowZero)
    {
        try {
            i = int.Parse(inputField.text);
        if (i < 0 || (!allowZero && i == 0)) throw new Exception();
        } catch (Exception e) { Debug.LogException(e); }
    }

    private void getInt(ref int i, InputField inputField, int min, int max)
    {
        try {
            int i_ = int.Parse(inputField.text);
            if (i_ < min || i_ > max) throw new Exception();
            i = i_;
        } catch (Exception e) { Debug.LogException(e); }
    }

    private void getFloat(ref float f, InputField inputField, bool allowZero)
    {
        try {
            float f_ = float.Parse(inputField.text);
            if (f_ < 0 || (!allowZero && f_ == 0)) throw new Exception();
            f = f_;
        } catch (Exception e) { Debug.LogException(e); }
    }

    private void getFloat(ref float f, InputField inputField, float min, float max, bool allowMin)
    {
        try {
            float f_ = float.Parse(inputField.text);
            if (f_ > max || f_ < min || (!allowMin && f_ == min)) throw new Exception();
            f = f_;
        } catch (Exception e) { Debug.LogException(e); }
    }

    private void getDouble(ref double d, InputField inputField, bool allowZero)
    {
        try {
            double d_ = float.Parse(inputField.text);
            if (d_ < 0 || (!allowZero && d_ == 0)) throw new Exception();
            d = d_;
        } catch (Exception e) { Debug.LogException(e); }
    }

    private void getDouble(ref double d, InputField inputField, double min, double max, bool allowMin)
    {
        try {
            double d_ = float.Parse(inputField.text);
            if (d_ > max || d_ < min || (!allowMin && d_ == min)) throw new Exception();
            d = d_;
        } catch (Exception e) { Debug.LogException(e); }
    }

    readonly char[] separator = new char[] { ',' };
    private void getDimMap(int[] dest, InputField inputField)
    {
        try {
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
        } catch ( Exception e) { Debug.LogException(e); }
    }

    private int getInt(Dropdown dropdown) { return dropdown.value; }

    private bool getBool(Toggle toggle) { return toggle.isOn; }

    private void getBool(Toggle[] toggle, bool[] bools) { for (int i = 0; i < toggle.Length; i++) bools[i] = getBool(toggle[i]); }
}
