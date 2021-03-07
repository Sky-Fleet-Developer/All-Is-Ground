using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KineticCharge : Charge
{
    public float Mass;
    public float Caliber;
    public AnimationCurve RicochetChance;

    protected override bool OnHit(RaycastHit Hit, float CollisionVelocity, out bool Explose)
    {
        var rigid = Hit.rigidbody;
        if (rigid)
        {
            rigid.AddForceAtPosition(-Hit.normal * CollisionVelocity * Mass * 100, Hit.point);
        }

        Damageble dam = null;
        if(projectile.IsMine)
            dam = Hit.collider.GetComponent<Damageble>();
        if (dam) // Если попал по броне, отдаём контроль ей
        {
            return dam.KineticHit(this, Hit, out Explose);
        }
        else //Если по окруженеию, то задаём параметры сами
        {

            float Angle = Vector3.Angle(-Tr.forward, Hit.normal);
            if(CollisionVelocity > Hardness * 200) // Взрыв 
            {
                Explose = true;
                return false;
            }
            else //Не взрыв. Рикошет ли?
            {
                Explose = false;
                if (RicochetChance.Evaluate(Angle) > Random.Range(0f, 0.5f))
                {
                    return true;
                }
                return false;
            }

        }
    }

    public override void GetDescription(ref List<string> Parameters, ref List<string> Values)
    {
        if (FuelCount > 0)
        {
            Parameters.Add("Кинетический\nреактивный снаряд");
            Values.Add("");
        }
        else
            Parameters.Add("Кинетический снаряд");
        Values.Add("");
        Parameters.Add("Калибр");
        Values.Add(Caliber + " mm");
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
        Values.Add(string.Empty);
        float distance = 0f;
        float velocity = StartSpeed;
        int iterations = 20000;
        int Zero, Half, One;
        Zero = (int)DamageManager.ABPower(Mass, Caliber / 100, velocity, 0f, 2200);
        while(distance < 500 && iterations-- > 0)
        {
            distance += velocity * Time.fixedDeltaTime;
            velocity *= 1 - Drag * Time.fixedDeltaTime;
        }
        Half = (int)DamageManager.ABPower(Mass, Caliber / 100, velocity, 0f, 2200);
        while (distance < 1000 && iterations-- > 0)
        {
            distance += velocity * Time.fixedDeltaTime;
            velocity *= 1 - Drag * Time.fixedDeltaTime;
        }
        One = (int)DamageManager.ABPower(Mass, Caliber / 100, velocity, 0f, 2200);
        Parameters.Add("0м");
        Values.Add(Mathf.Ceil(Zero) + " mm");
        if (distance > 499)
        {
            Parameters.Add("500м");
            Values.Add(Mathf.Ceil(Half) + " mm");
            if (distance > 999)
            {
                Parameters.Add("1000м");
                Values.Add(Mathf.Ceil(One) + " mm");
            }
        }
    }
}
