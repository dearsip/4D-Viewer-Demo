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
    public SteamVR_Action_Boolean interactUI;
    public Hand leftHand, rightHand;
    public GameObject leftLaser, rightLaser;
    public Slider dimSlider, sizeSlider, densitySlider, twistProbabilitySlider, branchProbabilitySlider, loopCrossProbabilitySlider,
        dimSameParallelSlider, dimSamePerpendicularSlider, depthSlider, retinaSlider, scaleSlider, trainSpeedSlider,
        transparencySlider, borderSlider, frameRateSlider, timeMoveSlider, timeRotateSlider, timeAlignMoveSlider, timeAlignRotateSlider;
    public InputField dimCurrent, dimNext, sizeCurrent, sizeNext, densityCurrent, densityNext, twistPobabilityCurrent, twistProbabilityNext,
        branchProbabilityCurrent, branchProbabilityNext, loopCrossProbabilityCurrent, loopCrossProbabilityNext, dimSameParallelField,
        dimSamePerpendicularField, mazeCurrent, mazeNext, colorCurrent, colorNext, depthField, retinaField, scaleField, trainSpeedField,
        transparencyField, borderField, frameRateField, timeMoveField, timeRotateField, timeAlignMoveField, timeAlignRotateField,
        width, flare, rainbowGap;
    public Toggle allowLoopsCurrent, allowLoopsNext, toggleEdgeColor, hideSel, toggleNormals, separate, toggleLeftAndRight, toggleForward,
        alignMode, fisheye, custom, rainbow;
    public Toggle[] enable, texture;
    public Dropdown colorMode, moveType, rotateType;

    public void Activate(OptionsAll oa)
    {
        gameObject.SetActive(true);

        put(dimCurrent, oa.omCurrent.dimMap);
        put(dimNext, dimSlider, oa.opt.om4.dimMap);
        put(sizeCurrent, sizeSlider, oa.omCurrent.size);
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
        put(mazeNext, oa.oeNext.mapSeed);
        put(colorCurrent, oa.oeCurrent.colorSeed);
        put(colorNext, oa.oeNext.colorSeed);

        put(depthField, depthSlider, oa.opt.ov4.depth);
        put(texture, oa.opt.ov4.texture);
        put(retinaField, retinaSlider, oa.opt.ov4.retina);
        put(scaleField, scaleSlider, oa.opt.ov4.retina);

        put(transparencyField, transparencySlider, oa.opt.od.transparency);
        put(borderField, borderSlider, oa.opt.od.border);
        //put(toggleEdgeColor, oa.opt.od.toggleEdgeColor);, hidesel, normal, separate, trainspeed

        put(moveType, oa.opt.oo.moveInputType);
        put(rotateType, oa.opt.oo.rotateInputType);
        put(toggleLeftAndRight, oa.opt.oo.toggleLeftAndRight);
        put(toggleForward, oa.opt.oo.toggleForward);
        //put(aligmMode,

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
        // すぐいじらないやつ　Map, Seed, Motion, aligmmode
    }

    private void doUpdate()
    {
        OptionsAll oa = core.getOptionsAll();
        try
        {
            oa.opt.oc4.colorMode = getInt(colorMode);
            put(dimSameParallelField, dimSameParallelSlider, oa.opt.oc4.dimSameParallel);
            put(dimSamePerpendicularField, dimSamePerpendicularSlider, oa.opt.oc4.dimSamePerpendicular);
            put(enable, oa.opt.oc4.enable);

        }
        catch (Exception e) { }
    }

    private void doOK()
    {

    }

    private void doCancel()
    {

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

    private int getInt(InputField inputField, int min, int max)
    {
        int i = int.Parse(inputField.text);
        if (i < min || i > max) throw new Exception();
        return i;
    }

    private double getDouble(InputField inputField, double min, double max, bool allowMin)
    {
        double d = float.Parse(inputField.text);
        if (d > max || d < min || (d == min && !allowMin)) throw new Exception();
        return d;
    }

    private int getInt(Dropdown dropdown) { return dropdown.value; }

    private bool getBool(Toggle toggle) { return toggle.isOn; }
}
