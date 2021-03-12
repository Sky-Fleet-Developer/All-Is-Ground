using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmoothFollow : MonoBehaviour
{

	public Transform target;
	public float MaxDistance;
	public float MinDistance;
	public float MaxHeight;
	public float MinHeight;
	public float View;
	public float distance = 10.0f;
	public float height = 5.0f;
	public float heightDamping = 2.0f;
	public float rotationDamping = 3.0f;

	[AddComponentMenu("Camera-Control/Smooth Follow")]

	void FixedUpdate ()
    {
        if (!target)
        {
            var p = GameObject.Find("Player");
            if(p)
                target = p.transform.Find("Turret");
            return;
        }

        float view = 0;
        if (Input.GetKey(KeyCode.E))
            view = 1;
        else
            if (Input.GetKey(KeyCode.Q))
            view = -1;

        View = Mathf.Clamp(View + view * Time.fixedDeltaTime, 0f, 1f);
		height = Mathf.Lerp (MinHeight, MaxHeight, View) - target.forward.y*3;
		distance = Mathf.Lerp (MinDistance, MaxDistance, View);
		float wantedRotationAngle = target.eulerAngles.y;
		float wantedHeight = target.position.y + height;

		float currentRotationAngle = transform.eulerAngles.y;
		float currentHeight = transform.position.y;

		currentRotationAngle = Mathf.LerpAngle(currentRotationAngle, wantedRotationAngle, rotationDamping * Time.fixedDeltaTime);

		currentHeight = Mathf.Lerp(currentHeight, wantedHeight, heightDamping * Time.fixedDeltaTime);

		var currentRotation = Quaternion.Euler(0, currentRotationAngle, 0);

		transform.position = target.position;
		transform.position -= currentRotation * Vector3.forward * distance;

		transform.position = new Vector3(transform.position.x,currentHeight,transform.position.z);

		transform.rotation = Quaternion.LookRotation (target.position + target.forward * 3 - transform.position);
	}
}