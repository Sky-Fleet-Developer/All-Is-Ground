using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Demo
{
    public class AI : MonoBehaviour
    {
        Transform Enamy;
        TankControl Movement;
        TurelControl Weapon;
        Vector3 EnamyDirection;
        Vector3 TurretDirection;
        float BodyAngle;
        Transform Tr;
        float lifetime;
        void Start()
        {
            EnamyWawes.enamysCount += 1;
            Tr = transform;
            Movement = GetComponent<TankControl>();
            Weapon = GetComponentInChildren<TurelControl>();
            lifetime = Random.Range(0, 5);
        }

        private void OnDestroy()
        {
            EnamyWawes.enamysCount -= 1;
        }

        void Update()
        {
            lifetime += Time.deltaTime;
            if (!Enamy)
            {
                var e = GameObject.Find("Player");
                if (e)
                    Enamy = e.transform;
                return;
            }
            TurretDirection = Weapon.Tr.InverseTransformPoint(Enamy.position);
            EnamyDirection = Tr.InverseTransformPoint(Enamy.position);
            BodyAngle = Mathf.Atan2(EnamyDirection.x, EnamyDirection.z);
            Movement.Vertical = 1;
            if (EnamyDirection.magnitude < 20)
            {
                Movement.Horizontal = Mathf.Clamp((BodyAngle + Mathf.Pow(Mathf.Sin(lifetime / 10), 2) * 0.4f) * 2, -1, 1);
            }
            else
            {
                Movement.Horizontal = Mathf.Clamp((BodyAngle + Mathf.Pow(Mathf.Sin(lifetime / 10), 2) * 1.2f) * 2, -1, 1);
            }
            Weapon.Turel = Mathf.Clamp(TurretDirection.x * 5, -1, 1);
            Weapon.Fire = Mathf.Abs(TurretDirection.x) < 0.05f;
        }
    }
}