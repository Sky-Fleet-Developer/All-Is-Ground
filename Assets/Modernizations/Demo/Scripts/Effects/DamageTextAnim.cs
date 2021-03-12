using UnityEngine;
using System.Collections;

public class DamageTextAnim : MonoBehaviour {
	Vector3 Velocity = Vector3.up*2;
	Vector3 Accel;
	float Timer = 1.5f;
	// Use this for initialization
	void Start () {
		Accel = new Vector3 ( Random.Range (-2f, 2f), -1, Random.Range (-2f, 2f));
	}
	
	// Update is called once per frame
	void Update () {
		Timer -= Time.deltaTime;
		Velocity += Accel*Time.deltaTime;
		transform.position += Velocity * Time.deltaTime * transform.localScale.x;
		transform.rotation = Quaternion.LookRotation (transform.position - Camera.main.transform.position);
		Color c = GetComponent<TextMesh>().color;
		c.a = Timer;
		GetComponent<TextMesh>().color = c;
		if (Timer < 0f)
			Destroy (gameObject);
	}
}
