using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using UnityEngine.UI;

// Display の位置を動かす。
public class DisplayMove : MonoBehaviour
{
    public SteamVR_Input_Sources hand;
    public SteamVR_Action_Boolean grab;
    public SteamVR_Action_Pose pose;
    public Slider size;
    private Quaternion relarot;
    void Update()
    {
        if (grab.GetState(hand)) {
            transform.position += (pose.GetLocalPosition(hand) - pose.GetLastLocalPosition(hand))*size.value;
            relarot = pose.GetLocalRotation(hand) * Quaternion.Inverse(pose.GetLastLocalRotation(hand));
            relarot.x = 0; relarot.z = 0;
            transform.rotation *= relarot;
        }
    }
}
