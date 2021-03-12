using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Demo
{
    public class Spawn : MonoBehaviour
    {
        [HideInInspector]
        public bool IsPlayer;
        public Transform Prefab;
        public int spawnCount = 1;
        Transform Instance;
        float timer = 0;

        public static GameObject Player;

        void Update()
        {
            if (spawnCount > 0 && (!Instance || !IsPlayer))
            {
                timer -= Time.deltaTime;
                if (timer < 0)
                {
                    timer = IsPlayer ? 5 : 10;
                    Instance = Instantiate(Prefab, transform.position, transform.rotation).transform;
                    Instance.GetComponent<TankControl>().IsPlayer = IsPlayer;
                    Instance.GetComponent<Health>().IsPlayer = IsPlayer;
                    Instance.GetComponentInChildren<TurelControl>().IsPlayer = IsPlayer;
                    if (!IsPlayer)
                    {
                        spawnCount--;
                        Instance.gameObject.AddComponent<AI>();
                        Instance.gameObject.AddComponent<AddResources>();
                    }
                    else
                    {
                        Player = Instance.gameObject;
                    }
                }
            }
        }
    }
}
