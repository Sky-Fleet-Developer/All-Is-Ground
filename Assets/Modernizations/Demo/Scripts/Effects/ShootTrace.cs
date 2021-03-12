using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShootTrace : MonoBehaviour
{
	public float FadingTimer = 2f;
	LineRenderer Line;
	RaycastHit ShootRay;
	public LayerMask ShootRayLayer;
	void Start ()
    {
		Line = GetComponent<LineRenderer> ();
		if (Physics.Raycast (transform.position, transform.forward, out ShootRay, Mathf.Infinity, ShootRayLayer))
        {
			Line.SetPosition (1, Vector3.forward * ShootRay.distance);
		}
        else
        {
			Line.SetPosition (1, Vector3.forward * 1000f);
		}
	}
	
	void Update ()
    {
		float A = Line.material.color.a;
		A = Mathf.MoveTowards (A, 0f, Time.deltaTime / FadingTimer);
		Line.material.color = new Color (Line.material.color.r, Line.material.color.g, Line.material.color.b, A);
		if (A == 0f)
            Destroy (gameObject);
	}
}
