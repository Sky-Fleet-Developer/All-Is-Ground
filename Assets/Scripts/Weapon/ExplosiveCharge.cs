using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplosiveCharge : Charge
{
    public float Mass;
    public float ExplosiveWeight;
    public float MaxDamageRadius = 20f;
    public AnimationCurve RicochetChance;

    protected override bool OnHit(RaycastHit Hit, float CollisionVelocity, out bool Explose)
    {
        float Angle = Vector3.Angle(-Tr.forward, Hit.normal);
        if (CollisionVelocity > Hardness * 200) // Взрыв 
        {
            Explose = true;
            foreach (var hit in Physics.OverlapSphere(Hit.point, MaxDamageRadius, GameValues.ChargeLayer))
            {
                var rigid = hit.GetComponent<Rigidbody>();
                if (rigid)
                {
                    rigid.AddExplosionForce(ExplosiveWeight * 10 * FORCE, Hit.point, MaxDamageRadius);
                }
                if (projectile.IsMine)
                {
                    var dam = hit.GetComponent<Damageble>();
                    if (dam)
                    {
                        dam.ExploseHit(this, Hit.point);
                    }
                }
            }
            return false;
        }
        else //Не взрыв. Рикошет ли?
        {
            Explose = false;
            if (RicochetChance.Evaluate(Angle) > Random.Range(0f, 0.5f))
            {
                Pooling.InstancesDic[Pool_Ricoshet].Use(Hit.point, Quaternion.LookRotation(Vector3.ProjectOnPlane(Tr.forward, Hit.normal), Hit.normal), null);
                return true;
            }
            return false;
        }
    }

    public override void GetDescription(ref List<string> Parameters, ref List<string> Values)
    {
        if (FuelCount > 0)
        {
            Parameters.Add("Осколочно-фугасный\nреактивный снаряд");
        }
        else
            Parameters.Add("Осколочно-фугасный\nснаряд");
        Values.Add("");
        Values.Add("");
        Parameters.Add("Масса взрыв. вещ-ва");
        Values.Add(ExplosiveWeight * 1000 + " гр");
        Parameters.Add("Масса");
        Values.Add(Mass * 1000 + " гр");
        Parameters.Add("Начальная скорость");
        Values.Add(StartSpeed + " м/с");
        if (RotationSpeed > 0)
        {
            Parameters.Add("Маневренность");
            Values.Add(RotationSpeed + " град/с");
        }
        if (FuelCount > 0)
        {
            Parameters.Add("Ускорение");
            Values.Add(Acceleration + " м/с2");
            Parameters.Add("Топливо");
            Values.Add(FuelCount + " сек");
        }
        Parameters.Add("Пробитие");
        Values.Add((int)DamageManager.ABPower(ExplosiveWeight, 0, MaxDamageRadius, 2200) + " mm");
    }
}
