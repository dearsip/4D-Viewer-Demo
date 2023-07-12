using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HapticsBase : MonoBehaviour
{
    public virtual void GetPosition(double[] pos) {}
    public virtual Quaternion GetRotation() { return Quaternion.identity; }
    public virtual void SetHaptics(double[] haptics) {}
    public virtual bool Button1Pressed() { return false; }
    public virtual bool Button2Pressed() { return false; }
    public virtual bool Button3Pressed() { return false; }
    public virtual bool Button4Pressed() { return false; }
    public virtual void ToggleLimit3D(bool limit3D) {}
}