using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LobbyMouseOrbit : MonoBehaviourPlus
{
	public Transform target;
    public AnimationCurve Distance;
    public float d = 10.0f;
    public AnimationCurve Height;

    public float xSpeed= 250.0f;
	public float ySpeed= 120.0f;
	
	public float yMinLimit= -20f;
	public float yMaxLimit= 80f;
    public float Pose;
    float Zoom;
    float SmoothZoom;
    public Vector2 MinMaxZoom;
    public float ZoommingSpeed;

    public Vector3 Offset;

	private float x= 0.0f;
	private float y= 0.0f;

    private float mx = 0.0f;
    private float my = 0.0f;
    Transform Tr;

    void  Start (){
		Tr = transform;
        Vector3 angles = Tr.eulerAngles;
		x = angles.y;
		y = angles.x;

		// Make the rigid body not change rotation
		if (GetComponent<Rigidbody>())
			GetComponent<Rigidbody>().freezeRotation = true;
    }

	void LateUpdate (){
		if (!target)
			return;
        Pose = Mathf.MoveTowards(Pose, Mathf.Clamp(Pose + Input.GetAxis("Mouse ScrollWheel") * ZoommingSpeed, 0f, 1f), Time.fixedDeltaTime * 3f);

        Vector3 tp = target.position + Tr.up * Height.Evaluate(1 - Pose);

		d = Distance.Evaluate(1 - Pose);
		if (target) {
            if (Input.GetButton("Fire2"))
            {
                mx = Mathf.Lerp(mx, Input.GetAxis("Mouse X"), Time.fixedDeltaTime * 3f);
                my = Mathf.Lerp(my, Input.GetAxis("Mouse Y"), Time.fixedDeltaTime * 3f);
            }
            else
            {
                mx = Mathf.Lerp(mx, 0f, Time.fixedDeltaTime * 5f);
                my = Mathf.Lerp(my, 0f, Time.fixedDeltaTime * 5f);
            }

            x += mx * xSpeed * Mathf.Clamp(Time.fixedDeltaTime, 0, 0.07f);
            y -= my * ySpeed * Mathf.Clamp(Time.fixedDeltaTime, 0, 0.07f);
            y = ClampAngle(y, yMinLimit, yMaxLimit);
			
			Quaternion rotation = Quaternion.Euler(y, x, 0f);
			Vector3 position = Quaternion.Euler(y, x, 0f) * new Vector3(0.0f, 0.0f, -d) + tp;

            Tr.rotation = rotation;
			Tr.position = position + Tr.TransformDirection(Offset);

        }
	}
	
	static float  ClampAngle ( float angle ,   float min ,   float max  ){
		if (angle < -360)
			angle += 360;
		if (angle > 360)
			angle -= 360;
		return Mathf.Clamp (angle, min, max);
	}
}