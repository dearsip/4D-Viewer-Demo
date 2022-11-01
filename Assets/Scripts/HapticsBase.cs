using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HapticsBase : MonoBehaviour
{
    public virtual void GetPosition(double[] pos) {}
    public virtual void SetHaptics(double[] haptics) {}
    public virtual bool Button1Pressed() { return false; }
    public virtual bool Button2Pressed() { return false; }
}