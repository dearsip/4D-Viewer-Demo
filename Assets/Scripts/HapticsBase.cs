using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HapticsBase : MonoBehaviour
{
    public virtual double[] GetPosition() { return new double[] {0,0,0,0}; }
    public virtual void SetHaptics(double[] haptics) {}
}