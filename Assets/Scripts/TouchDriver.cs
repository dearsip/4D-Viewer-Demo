using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HapticGUI;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class TouchDriver : HapticsBase
{
    public string configNameL = "Left Device";
    public string configNameR = "Right Device";
    public GameObject leftRestrict;
    public double scale = 1;
    public double wOffset = 0;

    HapticPlugin deviceL;
    HapticPlugin deviceR;
    bool inTheZone, inTheZoneLeft, startHaptics, limit3D, lastButton4Pressed;
    int inkwell;
    double[] matrix16, reg1, reg2;
    bool idle = false, button1 = false, button2 = false, button3 = false, button4 = false;
    // Start is called before the first frame update
    void Start()
    {
        HapticPlugin[] devices = (HapticPlugin[])FindObjectsOfType(typeof(HapticPlugin));
        for (int ii = 0; ii < devices.Length; ii++)
        {
            if (devices[ii].DeviceIdentifier == configNameL) deviceL = devices[ii];
            if (devices[ii].DeviceIdentifier == configNameR) deviceR = devices[ii];
        }
        if (deviceL == null || deviceR == null) { Debug.Log("Unable to initialize the haptic device."); Destroy(gameObject); }
        inTheZone = false;
        matrix16 = new double[16];
        reg1 = new double[3];
        reg2 = new double[3];
    }

    float clock = 1;
    bool debug, done;
    // Update is called once per frame
    void Update()
    {
        //clock -= Time.deltaTime;
        //if (done) debug = done = false;
        //if (clock < 0)
        //{
            //debug = true;
            //clock++;
        //}
        RightRestrict();
    }

    public void OnButton1Pressed() { button1 = true; }
    public void OnButton1Released() { button1 = false; }
    public void OnButton2Pressed() { button2 = true; }
    public void OnButton2Released() { button2 = false; }
    public void OnButton3Pressed() { button3 = true; }
    public void OnButton3Released() { button3 = false; }
    public void OnButton4Pressed() { button4 = true; idle = !idle; }
    public void OnButton4Released() { button4 = false; }

    private void RightRestrict() {
        bool oldInTheZone = inTheZone;
        inTheZone = Mathf.Abs(deviceR.CurrentPosition.x) + Mathf.Abs(deviceR.CurrentPosition.z) < 50;
        if (oldInTheZone != inTheZone)
        {
            if (inTheZone)
            {
                Debug.Log("inthezone");
            } else
            {
                Debug.Log("outthezone");
            }
		}
    }

    public override void GetPosition(double[] pos) {
        pos[0] = -deviceL.CurrentPosition.x*.02*scale;
        pos[1] = deviceL.CurrentPosition.y*.02*scale;
        pos[2] = limit3D ? 0 : -deviceL.CurrentPosition.z*.02*scale;
        pos[3] = (-(deviceR.CurrentPosition.y-150)*.02+wOffset-2.1)*scale;
        if (debug) Debug.Log(deviceR.CurrentPosition.y);
        if (debug) Debug.Log("position: " + Vec.ToString(pos));
        done = true;
    }

    public override void GetAbsolutePosition(double[] pos) {
        pos[0] = -deviceL.CurrentPosition.x*.02*scale;
        pos[1] = deviceL.CurrentPosition.y*.02*scale;
        pos[2] = limit3D ? 0 : -deviceL.CurrentPosition.z*.02*scale;
        pos[3] = (-(deviceR.CurrentPosition.y-150)*.02-2.1)*scale;
        if (debug) Debug.Log(deviceR.CurrentPosition.y);
        if (debug) Debug.Log("position: " + Vec.ToString(pos));
        done = true;
    }

    public override Quaternion GetRotation()
    {
        Matrix4x4 mat = deviceL.GetDeviceTransformationRaw();
        mat.SetColumn(0, -mat.GetColumn(0));
        mat.SetColumn(2, -mat.GetColumn(2));
        mat.SetRow(0, -mat.GetRow(0));
        mat.SetRow(2, -mat.GetRow(2));
        return mat.ExtractRotation();
    }

    public override void SetHaptics(double[] haptics){
        if (idle) Vec.zero(haptics);
        Vec.scale(haptics, haptics, 1/scale);
        reg1[0] = -3 * haptics[0]; reg1[1] = 3 * haptics[1]; reg1[2] = -3 * haptics[2]; reg2[1] = -3 * haptics[3];
        HapticPlugin.setConstantForceValues(configNameL, reg1, Vec.norm(reg1));
        HapticPlugin.setConstantForceValues(configNameR, reg2, Math.Abs(reg2[1]));
        if (debug) Debug.Log("applyforce: " + Vec.ToString(haptics));
        done = true;
    }

    public override bool Button1Pressed()
    {
        return button1;
    }

    public override bool Button2Pressed()
    {
        return button2;
    }

    public override bool Button3Pressed()
    {
        return button3;
    }

    public override bool Button4Pressed()
    {
        return button4;
    }

    public override void ToggleLimit3D(bool limit3D)
    {
        this.limit3D = limit3D;
    }
}
