using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Demo
{
    public class EnamyWawes : MonoBehaviour
    {
        public Spawn[] EnamySpawns;
        bool Wawe = true;
        float WaweTimer = 45;
        public static int enamysCount;

        void Update()
        {
            WaweTimer -= Time.deltaTime;

            if (WaweTimer < 0)
            {
                if (Wawe)
                {
                    if (enamysCount == 0)
                    {
                        WaweTimer = 10;
                        Wawe = false;
                    }
                }
                else
                {
                    foreach (var hit in EnamySpawns)
                    {
                        hit.spawnCount = 3;
                    }
                    WaweTimer = 45;
                    Wawe = true;
                }
            }
        }
    }

}