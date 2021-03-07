using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageManager : MonoBehaviour
{
    public static readonly float STEELDENSITY = 7900;//kg/m3
    public static readonly float DamageMultiplyer = 5f;
    static int n;

    /*public static bool Ricochet(float ARC, float ChargeRicochetChance, float Speed, float Mass, float Caliber)
    {
        float b = Mathf.Pow(Speed / ARC, 1.43f) * (Mathf.Pow(Mass, 0.71f) / Mathf.Pow(Caliber, 1.07f));
        Debug.Log(b + " < " + 2.2f * ChargeRicochetChance);
        return b < 2.2f * ChargeRicochetChance;
    }
    */
    public static float ABPower(float Mass, float Caliber, float Velocity, float Angle, float ARC) //https://ru.wikipedia.org/wiki/%D0%91%D1%80%D0%BE%D0%BD%D0%B5%D0%BF%D1%80%D0%BE%D0%B1%D0%B8%D0%B2%D0%B0%D0%B5%D0%BC%D0%BE%D1%81%D1%82%D1%8C
    {
        return Mathf.Pow(Velocity / ARC, 1.43f) * (Mathf.Pow(Mass, 0.71f) / Mathf.Pow(Caliber, 1.07f)) * Mathf.Pow(Mathf.Cos(Angle * Mathf.Deg2Rad), 1.4f) * 100;
    }

    public static float ABPower(float ExplosiveWeight,float ArmorExplosionDist, float MaxDamageRadius, float ArmorResistanceCoefficient)
    {
        return ExplosiveWeight * 1000 * (1 - ArmorExplosionDist / MaxDamageRadius) / ArmorResistanceCoefficient * 50;
    }

    public static float CylinderVolume(float diametr, float height)
    {
        return diametr * diametr * 0.25f * height;
    }
}
