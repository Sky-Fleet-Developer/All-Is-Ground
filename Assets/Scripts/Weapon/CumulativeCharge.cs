using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CumulativeCharge : Charge
{
    public float ExplosionForce;
    public float PenetrationDistance; //mm

    protected override bool OnHit(RaycastHit Hit, float CollisionVelocity, out bool Explose)
    {
        Explose = true;
        List<Rigidbody> f = new List<Rigidbody>();
        foreach (var hit in Physics.OverlapSphere(Hit.point, 10))
        {
            var rigid = hit.attachedRigidbody;
            if (rigid && !f.Contains(rigid))
            {
                f.Add(rigid);
                rigid.AddExplosionForce(ExplosionForce * FORCE, Hit.point, 10);
            }
        }

        if (!projectile.IsMine)
            return false;

        Damageble dam = Hit.collider.GetComponent<Damageble>();
        if (dam)
        {
            return dam.CumulativeHit(this, Hit);
        }

        return false;

    }
    protected override LayerMask GetLeyerMask()
    {
        return GameValues.CumulativeChargeLayer;
    }
    public override void GetDescription(ref List<string> Parameters, ref List<string> Values)
    {
        if (FuelCount > 0)
        {
            Parameters.Add("Кумулятивный\nреактивный снаряд");
            Values.Add("");
        }
        else
            Parameters.Add("Кумулятивный снаряд");
        Values.Add("");
        Parameters.Add("Начальная скорость");
        Values.Add(StartSpeed + " м/с");
        if(RotationSpeed > 0)
        {
            Parameters.Add("Маневренность");
            Values.Add(RotationSpeed + " град/с");
        }
        if(FuelCount > 0)
        {
            Parameters.Add("Ускорение");
            Values.Add(Acceleration + " м/с2");
            Parameters.Add("Топливо");
            Values.Add(FuelCount + " сек");
        }
        Parameters.Add("Пробитие");
        Values.Add(string.Empty);
        Parameters.Add("0°");
        Values.Add(PenetrationDistance + " mm");
        Parameters.Add("30°");
        Values.Add(Mathf.Ceil(PenetrationDistance * Mathf.Cos(3.14f / 6)) + " mm");
        Parameters.Add("60°");
        Values.Add(Mathf.Ceil(PenetrationDistance * Mathf.Cos(3.14f / 3)) + " mm");
    }
}
