using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControl : MonoBehaviour
{
    public Transform center;
    private Vector3 defaultPosition;
    private Quaternion defaultRotation;
    private Vector3 lastMousePosition;
    private Vector3 deltaMousePosition;
    // Start is called before the first frame update
    void Start()
    {
        defaultPosition = transform.localPosition;
        defaultRotation = transform.localRotation;
        lastMousePosition = Input.mousePosition;
    }

    // Update is called once per frame
    void Update()
    {
        deltaMousePosition = Input.mousePosition - lastMousePosition;
        lastMousePosition = Input.mousePosition;
        if (Input.GetMouseButton(1)) {
            transform.RotateAround(center.position,Vector3.up,-deltaMousePosition.x/3);
            transform.RotateAround(center.position,transform.right,deltaMousePosition.y/3);
        }
        if (Input.GetMouseButton(2))
            transform.Translate(deltaMousePosition / 200);
        transform.Translate(0,0,Input.mouseScrollDelta.y / 10);
        if (Input.GetKeyDown(KeyCode.Return)) {
            transform.position = center.TransformPoint(defaultPosition);
            transform.rotation = center.rotation*defaultRotation;
        }
        
    }
}
