using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DesktopCamera : MonoBehaviour
{
    public Camera left, right, fix, fLeft, fRight;
    private int mode = 0;
    private Rect rLeft, rRight, frLeft, frRight;
    // Start is called before the first frame update
    void Start()
    {
        left.enabled = false;
        right.enabled = false;
        fix.enabled = false;
        fLeft.enabled = false;
        fRight.enabled = false;
        rLeft = left.rect;
        rRight = right.rect;
        frLeft = fLeft.rect;
        frRight = fRight.rect;
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space))
            switch (mode++ % 6)
            {
                case 0:
                    left.enabled = true;
                    right.enabled = true;
                    left.rect = rRight;
                    right.rect = rLeft;
                    break;
                case 1:
                    left.rect = rLeft;
                    right.rect = rRight;
                    break;
                case 2:
                    left.enabled = false;
                    right.enabled = false;
                    fix.enabled = true;
                    break;
                case 3:
                    fix.enabled = false;
                    fLeft.enabled = true;
                    fRight.enabled = true;
                    fLeft.rect = frRight;
                    fRight.rect = frLeft;
                    break;
                case 4:
                    fLeft.rect = frLeft;
                    fRight.rect = frRight;
                    break;
                case 5:
                    fLeft.enabled = false;
                    fRight.enabled = false;
                    break;
            }
    }
}
