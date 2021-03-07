using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Curves;
[RequireComponent(typeof(Curve))]
public class Curve_ObjectPlacement : MonoBehaviourPlus
{
    public Transform Source;
    public float ObjectLength;
    public Quaternion ObjectRotation;
    public Vector3 RandomRotation;
    public Vector3 ObjectOffset;
    public int FirstAnchor;
    public bool PlaceForRay;
    [HideInInspector]
    public List<Transform> instances;
    Curve curve;
    Transform Tr;
#if UNITY_EDITOR
    [ContextMenu("Place")]
    public void Place()
    {
        if(instances != null && instances.Count > 0)
        {
            foreach(var hit in instances)
            {
                if(hit)
                    DestroyImmediate(hit.gameObject);
            }
        }

        Tr = transform;
        instances = new List<Transform>();
        curve = GetComponent<Curve>();

        float length = GetCurveLength(curve);
        Vector3 lastPos = GetCurvePosition(curve, 0);
        for (float i = ObjectLength; i < length + ObjectLength; i += ObjectLength)
        {
            Vector3 pos = GetCurvePosition(curve, i);
            Quaternion rot = Quaternion.LookRotation(Vector3.ProjectOnPlane(pos - lastPos, Vector3.up)) * ObjectRotation * Quaternion.Euler(new Vector3(Random.Range(-RandomRotation.x, RandomRotation.x), Random.Range(-RandomRotation.y, RandomRotation.y), Random.Range(-RandomRotation.z, RandomRotation.z)));
            var obj = PrefabUtility.InstantiatePrefab(Source, Tr) as Transform;
            obj.localPosition = (pos + lastPos) / 2;
            obj.localRotation = rot;
            instances.Add(obj);
            lastPos = pos;
        }
        if (PlaceForRay)
        {
            foreach (var hit in instances)
            {
                hit.transform.position += Vector3.up * 100;
                RaycastHit Hit;
                if(Physics.Raycast(hit.transform.position + Vector3.down * 10, Vector3.down, out Hit))
                {
                    hit.transform.position = Hit.point + Vector3.up * ObjectOffset.y;
                }
            }
        }
    }
#endif
}
