using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StepSlider : Slider
{
    [SerializeField]
    public float step = 0;
    protected override void Set(float input, bool sendCallback)
    {
        if (step > 0) base.Set(Mathf.Round(input / step) * step, sendCallback);
        else base.Set(input, sendCallback);
    }
}
