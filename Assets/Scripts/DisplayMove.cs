using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

// Display の位置を動かす。
public class DisplayMove : MonoBehaviour
{
    public Transform transform;
    public SteamVR_Input_Sources hand;
    public SteamVR_Action_Boolean grab;
    public SteamVR_Action_Pose pose;
    void Update()
    {
        if (grab.GetState(hand)) transform.position += pose.GetLocalPosition(hand) - pose.GetLastLocalPosition(hand);
    }
}
