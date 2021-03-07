using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundWawe : Charge
{
    public AnimationCurve SizeByTime = AnimationCurve.Linear(0, 1, 1, 3);
    public float Force = 4000;
    public float SizeMultiplyer = 1;
    public float Lifetime = 2.5f;

    float startTime;
    public float RealSize;
    public List<Damageble> damaged;
    public Gradient WaveColor;
    public AnimationCurve DistortionScale;
    Renderer[] rends;

    protected override void OnEnable()
    {
        base.OnEnable();
        startTime = Time.time;
        damaged = new List<Damageble>();
    }

    protected override void OnStart()
    {
        base.OnStart();
        rends = GetComponentsInChildren<Renderer>();
    }

    protected override bool OnHit(RaycastHit Hit, float CollisionVelocity, out bool Explose)
    {
        Explose = false;
        return !Hit.collider.GetComponent<Damageble>();
    }

    private void Update()
    {
        float old = (Time.time - startTime) / Lifetime;
        RealSize = SizeMultiplyer * SizeByTime.Evaluate(old);
        Tr.localScale = Vector3.one * RealSize;
        foreach(var hit in rends)
        {
            hit.material.SetColor("_UnlitColor", WaveColor.Evaluate(old));
            hit.material.SetFloat("_DistortionScale", DistortionScale.Evaluate(old)); 
        }
        if(old >= 1)
        {
            Deactivate();
        }
    }

    protected override void Ricoshet(Vector3 pos, Vector3 normal, float velocity, float CollisionVelocity)
    {
        Velocity = Vector3.Reflect(Velocity, normal);
        WasReflect = true;
    }

    void OnTriggerEnter(Collider other)
    {

        var dam = other.GetComponent<Damageble>();
        if (other.transform.root == projectile.Tr.root)
            return;

        if (dam && (other.transform.root != projectile.Tr.root || WasReflect))
        {
            if (other.attachedRigidbody && damaged.Contains(dam))
                return;
            damaged.Add(dam);
            dam.SoundWaweHit(this, projectile.IsMine);
            if (!dam.IsMine)
                return;
        }

        if (other.attachedRigidbody)
        {
            Vector3 closestPoint = other.ClosestPoint(Tr.position);
            float CentrDist = Vector3.ProjectOnPlane(closestPoint - Tr.position, Tr.forward).magnitude / RealSize;
            float force = Force / (1 - CentrDist) / Mathf.Pow(RealSize, 0.75f);
            force = Mathf.Min(force, Force * 2);
            other.attachedRigidbody.AddForceAtPosition(Tr.forward * force * FORCE, Tr.position);
        }
    }

    public override void GetDescription(ref List<string> Parameters, ref List<string> Values)
    {

        Parameters.Add("Звуковая волна");
        Values.Add("");
        Parameters.Add("Начальная скорость");
        Values.Add(Mathf.Ceil(StartSpeed / 0.07f) / 10  + " / " + Mathf.Ceil(StartSpeed / 0.15f) / 10 + " м/с");
        Parameters.Add("Дистанция поражения");
        Values.Add(Mathf.Ceil(StartSpeed / 0.07f * Lifetime) / 10  + " / " + Mathf.Ceil(StartSpeed / 0.15f * Lifetime) / 10 + " м");

    }
}
