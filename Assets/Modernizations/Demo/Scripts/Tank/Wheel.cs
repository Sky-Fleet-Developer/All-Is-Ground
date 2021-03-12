using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wheel : MonoBehaviour
{
    public Vector3 Offset;
    public Vector3 AngleOffset;

    Transform suspension;
    Transform wheel;
    WheelCollider wheelCollider;
    private void Start()
    {
        wheelCollider = GetComponent<WheelCollider>();
        suspension = transform.GetChild(0);
        wheel = suspension.GetChild(0);
    }

    void Update ()
    {
        Vector3 pos;
        Quaternion rot;
        wheelCollider.GetWorldPose(out pos, out rot);
        suspension.position = pos + transform.TransformDirection(Offset);
        wheel.rotation = rot * Quaternion.Euler(transform.TransformDirection(AngleOffset));
    }
}