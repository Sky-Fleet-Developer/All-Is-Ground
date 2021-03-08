using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Damageble : MonoBehaviourPlus, IDestroyeble
{
    [System.NonSerialized]
    public bool IsAlive;
    public float Armor = 20;
    public float ArmorResistanceCoefficient = 2200;
    public float DamageMP = 1f;
    Health health;
    Transform Root;
    [System.NonSerialized]
    public Transform Tr;
    [System.NonSerialized]
    Collider collider;
    public bool IsMine => health.IsMine;

    void Awake()
    {
        Tr = transform;
        Root = Tr.root;
        health = GetComponentInParent<Health>();
        collider = GetComponent<Collider>();
    }

    public void SoundWaweHit(SoundWawe wave, bool ChargeIsMine)
    {
        Vector3 closestPoint = collider.ClosestPoint(wave.Tr.position);
        float CentrDist = Vector3.ProjectOnPlane(closestPoint - wave.Tr.position, wave.Tr.forward).magnitude / wave.RealSize;

        if (ChargeIsMine)
        {
            health.AddDamage(9f * DamageMP * wave.Force / health.DamageblePartsCount / Armor * (1 - CentrDist) / Mathf.Pow(wave.RealSize, 0.75f), PhotonNetwork.player);
        }
    }

    public bool CumulativeHit(CumulativeCharge charge, RaycastHit Hit)
    {
        float Angle = Vector3.Angle(-charge.Tr.forward, Hit.normal);

        // Debug.Log("No ricochet. Angle = " + Angle);
        float armor = Armor / Mathf.Cos(Angle * Mathf.Deg2Rad);

        //Effect(Hit.point, Quaternion.LookRotation(Hit.normal), charge.ExplosePoolName);

        if (armor > charge.PenetrationDistance)
        {
            //Debug.Log("No break. power = " + charge.PenetrationDistance + ". armor = " + armor);
            return false;
        }


        AddCumulativeDamage(charge.PenetrationDistance - armor);
        return false;
    }

    public bool KineticHit(KineticCharge charge, RaycastHit Hit, out bool Explose)
    {
        float Angle = Vector3.Angle(-charge.Tr.forward, Hit.normal);
        float Power = DamageManager.ABPower(charge.Mass, charge.Caliber / 100, charge.Velocity.magnitude * charge.Hardness, Vector3.Angle(-charge.Tr.forward, Hit.normal), ArmorResistanceCoefficient);
        Explose = false;

        if (Power < Armor * charge.RicochetChance.Evaluate(Angle) * 1.2f)
        {
            //Debug.Log("Ricoshet. Angle = " + Angle);
            return true;
        }
        Explose = true;

        //Debug.Log("No ricochet. Angle = " + Angle);

        if (Power < Armor)
        {
            //Debug.Log("No break. power = " + Power + ". armor = " + Armor);
            return false;
        }
        //Debug.Log("Break. power = " + Power + ". armor = " + Armor);

        charge.Velocity *= Mathf.Clamp01(1 - Armor / Power);

        AddBreakDamage(charge.Caliber / 100, charge.Velocity.magnitude);
        return false;
    }

    public void ExploseHit(ExplosiveCharge charge, Vector3 HitPosition)
    {
        float ArmorExplosionDist = Vector3.Distance(collider.ClosestPoint(HitPosition), HitPosition);
        float ABPower = DamageManager.ABPower(charge.ExplosiveWeight, ArmorExplosionDist, charge.MaxDamageRadius, ArmorResistanceCoefficient);
        //Debug.Log("ABPower = " + ABPower + ". Armor = " + Armor);
        health.Rigid.AddExplosionForce(charge.ExplosiveWeight * 10 * FORCE, HitPosition, charge.MaxDamageRadius);
        if (ABPower > Armor)
        {
            float damage = (ABPower - Armor * 0.2f) / health.DamageblePartsCount * DamageMP * 0.6f;
            //Debug.Log("Damage = " + damage);
            health.AddDamage(damage, PhotonNetwork.player);
        }
    }


   /* public void Effect(Vector3 Position, Quaternion Rotation, string EffectPoolName)
    {
        Pooling.InstancesDic[EffectPoolName].Use(Position, Rotation, null);
        //Pooling.InstancesDic["BlastDecals"].Use(Root.TransformPoint(LocalPosition), Quaternion.LookRotation(-SurfaceLocalNormal), Root);

        if(PhotonNetwork.connected)
            health.View.RPC("ScyncLocalEffect", PhotonTargets.Others, Root.InverseTransformPoint(Position), Root.rotation.GetInverse() * Rotation, EffectPoolName);
    }*/

    void AddBreakDamage(float Size, float Speed)
    {
        health.AddDamage(Size * Size * Mathf.Pow(Speed, 0.75f) * DamageMP, PhotonNetwork.player);
    }

    void AddCumulativeDamage(float RemaindLength)
    {
        health.AddDamage(Mathf.Sqrt(RemaindLength) * DamageMP, PhotonNetwork.player);
    }


    public void Death()
    {
        IsAlive = false;
    }

    public void Spawn()
    {
        IsAlive = true;
    }
}
