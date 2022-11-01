using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhantomDriver : HapticsBase
{
    public string configNameL = "Left Device";
    public string configNameR = "Right Device";

    HapticPlugin deviceL;
    HapticPlugin deviceR;
    int FXID_RU, FXID_RD, FXID_LH, FXID_RH;
    bool inTheZone, startHaptics;
    // Start is called before the first frame update
    void Start()
    {
        HapticPlugin[] devices = (HapticPlugin[])Object.FindObjectsOfType(typeof(HapticPlugin));
        for (int ii = 0; ii < devices.Length; ii++)
        {
            if (devices[ii].configName == configNameL) deviceL = devices[ii];
            if (devices[ii].configName == configNameR) deviceR = devices[ii];
        }
        if (deviceL == null || deviceR == null) { Debug.Log("Unable to initialize the haptic device."); Destroy(this.gameObject); }
        inTheZone = false;

        FXID_LH = HapticPlugin.effects_assignEffect(deviceL.configName);
        FXID_RH = HapticPlugin.effects_assignEffect(deviceR.configName);
        AssignRestrict();
    }

    // Update is called once per frame
    void Update()
    {
        RightRestrict();
    }

    private void RightRestrict() {
        if (FXID_RU == -1 || FXID_RD == -1) AssignRestrict();
        bool oldInTheZone = inTheZone;
        inTheZone = Mathf.Abs(deviceR.stylusPositionRaw.x) + Mathf.Abs(deviceR.stylusPositionRaw.z) < 50;
        if (oldInTheZone != inTheZone)
        {
            if (inTheZone)
            {
                Debug.Log("start");
                HapticPlugin.effects_startEffect(deviceR.configName, FXID_RU);
                HapticPlugin.effects_startEffect(deviceR.configName, FXID_RD);
            } else
            {
                Debug.Log("stop");
                HapticPlugin.effects_stopEffect(deviceR.configName, FXID_RU);
                HapticPlugin.effects_stopEffect(deviceR.configName, FXID_RD);
            }
		}
    }

    private void AssignRestrict() {
        FXID_RD = HapticPlugin.effects_assignEffect(deviceR.configName);
        FXID_RU = HapticPlugin.effects_assignEffect(deviceR.configName);
        if (FXID_RU == -1 || FXID_RD == -1) return;
        HapticPlugin.effects_settings(
            deviceR.configName,
            FXID_RU,
            0.75, // Gain
            0.9, // Magnitude
            1, // Frequency
            new double[] {0,150,0}, // Position
            new double[] {0,0,0}); // Direction
        HapticPlugin.effects_type(
            deviceR.configName,
            FXID_RU,
            (int)HapticEffect.EFFECT_TYPE.SPRING);
        HapticPlugin.effects_settings(
            deviceR.configName,
            FXID_RD,
            0.75,
            0.75,
            1,
            new double[] {0,-50,0},
            new double[] {0,0,0});
        HapticPlugin.effects_type(
            deviceR.configName,
            FXID_RD,
            (int)HapticEffect.EFFECT_TYPE.SPRING);
    }

    public override double[] GetPosition() {
        double x = deviceL.stylusPositionRaw.x*.01;
        double y = (deviceL.stylusPositionRaw.y-50)*.01;
        double z = deviceL.stylusPositionRaw.z*.01;
        double w = -(deviceR.stylusPositionRaw.y-50)*.01+1.1;
        return new double[] {x,y,z,w};
    }

    public override void SetHaptics(double[] haptics){
        if (FXID_LH == -1 || FXID_RH == -1) {
            FXID_LH = HapticPlugin.effects_assignEffect(deviceL.configName);
            FXID_RH = HapticPlugin.effects_assignEffect(deviceR.configName);
            if (FXID_LH == -1 || FXID_RH == -1) return;
        }
        HapticPlugin.effects_settings(
            deviceL.configName,
            FXID_LH,
            0, // Gain
            1.5*System.Math.Sqrt(haptics[0]*haptics[0]+haptics[1]*haptics[1]+haptics[2]*haptics[2]), // Magnitude
            1, // Frequency
            new double[] {0,0,0}, // Position
            new double[] {haptics[0],haptics[1],haptics[2]}); // Direction
        HapticPlugin.effects_type(
            deviceL.configName,
            FXID_LH,
            (int)HapticEffect.EFFECT_TYPE.CONSTANT);
        HapticPlugin.effects_settings(
            deviceR.configName,
            FXID_RH,
            0,
            1.5*System.Math.Abs(haptics[3]),
            1,
            new double[] {0,0,0},
            new double[] {0,-haptics[3],0});
        HapticPlugin.effects_type(
            deviceR.configName,
            FXID_RH,
            (int)HapticEffect.EFFECT_TYPE.CONSTANT);
        if (!startHaptics) {
            HapticPlugin.effects_startEffect(deviceL.configName, FXID_LH);
            HapticPlugin.effects_startEffect(deviceR.configName, FXID_RH);
            startHaptics = true;
        }
    }
}
