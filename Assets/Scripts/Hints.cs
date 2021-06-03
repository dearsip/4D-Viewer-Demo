using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

public class Hints : MonoBehaviour
{
    public Hand rightHand;
    public Hand leftHand;
    public SteamVR_Input_Sources hand;
    public SteamVR_Action_Boolean trigger, move, menu, grab;
    private bool visibleHints;

    // Start is called before the first frame update
    void Start()
    {
        visibleHints = true;
        StartCoroutine(InitHints());
        grab.AddOnStateUpListener(ToggleHints, hand);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private IEnumerator InitHints() {
        yield return new WaitForSeconds(1.0f);
        if (visibleHints) showHints();
        yield break;
    }

    private void showHints()
    {
        ControllerButtonHints.ShowTextHint(rightHand, move, "rotate");
        ControllerButtonHints.ShowTextHint(leftHand, move, "move");
        ControllerButtonHints.ShowTextHint(rightHand, menu, "menu");
        ControllerButtonHints.ShowTextHint(leftHand, menu, "menu");
        ControllerButtonHints.ShowTextHint(rightHand, trigger, "select");
        ControllerButtonHints.ShowTextHint(leftHand, trigger, "select");
        ControllerButtonHints.ShowTextHint(rightHand, grab, "move display");
        ControllerButtonHints.ShowTextHint(leftHand, grab, "show/hide hints");
    }

    private void ToggleHints(SteamVR_Action_Boolean fromBoolean, SteamVR_Input_Sources fromSource)
    {
        if (visibleHints)
        {
            ControllerButtonHints.HideAllTextHints(rightHand);
            ControllerButtonHints.HideAllTextHints(leftHand);
        }
        else
        {
            showHints();
        }
        visibleHints = !visibleHints;
    }

}
