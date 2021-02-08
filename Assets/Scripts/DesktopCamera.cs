using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DesktopCamera : MonoBehaviour
{
    public Camera left, right;
    private int mode = 0;
    private Rect rLeft, rRight;
    // Start is called before the first frame update
    void Start()
    {
        left.enabled = false;
        right.enabled = false;
        rLeft = left.rect;
        rRight = right.rect;
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space))
            switch (mode++ % 3)
            {
                case 0:
                    left.enabled = true;
                    right.enabled = true;
                    rLeft.x = 0.5f;
                    rRight.x = 0f;
                    left.rect = rLeft;
                    right.rect = rRight;
                    break;
                case 1:
                    rLeft.x = 0f;
                    rRight.x = 0.5f;
                    left.rect = rLeft;
                    right.rect = rRight;
                    break;
                case 2:
                    left.enabled = false;
                    right.enabled = false;
                    break;
            }
    }
}
