using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnPosition : MonoBehaviour
{
    public static Dictionary<string, SpawnPosition> Instances;
    public float Radius = 30;
    [System.NonSerialized]
    public Transform Tr;
    void Awake()
    {
        if (Instances == null)
            Instances = new Dictionary<string, SpawnPosition>();
        if (Instances.ContainsKey(gameObject.name))
            Instances[gameObject.name] = this;
        else
            Instances.Add(gameObject.name, this);
        Tr = transform;
    }

    public static SpawnPosition Spawn(PunTeams.Team team)
    {
        return Instances[team.ToString()];
    }

    public Vector3 GetSpawnPosition()
    {
        RaycastHit Hit;
        if (Physics.Raycast(Tr.position + new Vector3(Random.Range(-Radius, Radius), 1000, Random.Range(-Radius, Radius)), Vector3.down, out Hit))
        {
            return Hit.point + Hit.normal * 3f;
        }
        return Tr.position + Vector3.up * 3f;
    }
}
