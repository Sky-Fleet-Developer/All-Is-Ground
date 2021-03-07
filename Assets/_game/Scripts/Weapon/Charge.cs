using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Charge : MonoBehaviourPlus, IDescription
{
    public string ExplosePoolName = "BlastPool";
    public float StartSpeed = 850f;
    public float Acceleration = 25f;
    public float Drag = 0.1f;
    public float RotationSpeed = 65;
    public float StabilizationForce = 0.5f;
    public float FuelCount = 8f;
    public float EffectiveDistance;
    [System.NonSerialized]
    public float StartFuelCount = 0f;
    public float Hardness = 1f;
    public float CollisionVelocityDumping = 1f;
    public bool EnableRicoshetEffect = true;
    public bool UseGravity = true;
    public ParticleSystem Afterburner;

    [System.NonSerialized]
    public Vector3 WantedPoint;
    Vector3 AvoidingVector;
    [System.NonSerialized]
    public Vector3 Velocity;
    Vector3 localVelocity;
    [System.NonSerialized]
    public Transform Tr;
    [System.NonSerialized]
    public Projectile projectile;
    [System.NonSerialized]
    public Quaternion Pather;
    [System.NonSerialized]
    public PoolObject PO;
    protected bool WasReflect;
    TrailRenderer trail;
    protected virtual void OnEnable()
    {
        Tr = transform;
        PO = GetComponent<PoolObject>();
    }

    private void OnDisable()
    {
        trail = GetComponentInChildren<TrailRenderer>();
        Tr.localPosition = Vector3.zero;
        if (trail)
            trail.emitting = false;
    }

    protected virtual void OnStart()
    {
        Tr = transform;
        trail = GetComponentInChildren<TrailRenderer>();
        if (trail)
        {
            trail.emitting = false;
        }
        if (StartFuelCount == 0f)
            StartFuelCount = FuelCount;
        FuelCount = StartFuelCount;
        WasReflect = false;
    }

    protected virtual LayerMask GetLeyerMask()
    {
        return GameValues.ChargeLayer;
    }

    public void CheckCast()
    {
        RaycastHit Hit;
        if (Physics.Raycast(Tr.position, Velocity, out Hit, Velocity.magnitude * 1.5f * Time.fixedDeltaTime, GetLeyerMask()))
        {
            Tr.position = Hit.point;
            if (trail)
                trail.AddPosition(Tr.position);

            float magnitude = Velocity.magnitude;
            float CollisionVelocity = Mathf.Cos(Vector3.Angle(Hit.normal, -Velocity) * Mathf.Deg2Rad) * magnitude;

            if (OnHit(Hit, CollisionVelocity, out bool explose))
            {
                Ricoshet(Hit.point, Hit.normal, magnitude, CollisionVelocity);
            }
            else
            {
                if (explose)
                {
                    Explose(Hit.point, Quaternion.LookRotation(Hit.normal));
                }
                else
                {
                    Deactivate();
                }
            }
        }

        if (Velocity.magnitude < 5f)
        {
            Deactivate();
        }
    }

    void FixedUpdate()
    {
        CheckCast();

        Tr.position += Velocity * Time.fixedDeltaTime;

        if (trail && !trail.emitting)
        {
            trail.Clear();
            trail.SetPositions(new Vector3[0]);
            trail.emitting = true;
        }

        Vector3 ttv = WantedPoint - Tr.position;
        Quaternion ToTarget = Quaternion.LookRotation(ttv);
        localVelocity = ToTarget.GetInverse() * Velocity;
        Debug.DrawRay(Tr.position, Velocity * 0.2f, Color.blue);

        Tr.rotation = Quaternion.RotateTowards(Tr.rotation, Quaternion.LookRotation(Velocity), StabilizationForce * Velocity.magnitude * Time.fixedDeltaTime);

        if (FuelCount > 0)
        {
            Debug.DrawLine(Tr.position, WantedPoint);
            float dist = ttv.magnitude;
            Vector3 patherLoc = Pather.GetInverse() * (Tr.position - projectile.Tr.position + Velocity * 0.4f) * 20;
            //Debug.Log("Right " + patherLoc.x + ". Up " + patherLoc.y);
            AvoidingVector = ttv - Vector3.ProjectOnPlane(Velocity * Mathf.Clamp(dist / 5, 1, 3), ToTarget.GetForward()) + Vector3.up * dist * 0.1f - Pather.GetRight() * patherLoc.x - Pather.GetUp() * patherLoc.y;
            Debug.DrawRay(Tr.position, AvoidingVector * 0.2f, Color.yellow);

            FuelCount = Mathf.MoveTowards(FuelCount, 0f, Time.fixedDeltaTime);
            Velocity += Tr.forward * Acceleration * Time.fixedDeltaTime;

            Tr.rotation = Quaternion.RotateTowards(Tr.rotation, Quaternion.LookRotation(AvoidingVector.normalized + Vector3.up * 0.15f), RotationSpeed * Time.fixedDeltaTime);
            if (FuelCount == 0 && Afterburner)
                Afterburner.Stop();
        }
        if(UseGravity)
            Velocity += Vector3.down * GRAVITY * 2 * Time.fixedDeltaTime;

        Velocity *= 1 - Drag * Time.fixedDeltaTime;

    }

    /// <param name="Explose">Did charge esplose?</param>
    /// <returns>Did charge has ricoshet?</returns>
    protected virtual bool OnHit(RaycastHit Hit, float CollisionVelocity, out bool Explose)
    {
        if (CollisionVelocity > Hardness * 200) // Достаточная ли сила удара для детонации?
        {
            Explose = true;
            return false;
        }

        Explose = false;
        float Angle = Vector3.Angle(-Tr.forward, Hit.normal);
        if (Angle > 75) // Достаточный ли угол для рикошета?
        {
            Pooling.InstancesDic[Pool_Ricoshet].Use(Hit.point, Quaternion.LookRotation(Vector3.ProjectOnPlane(Tr.forward, Hit.normal), Hit.normal), null);
            return true;
        }

        return false;
    }

    protected virtual void Ricoshet(Vector3 pos, Vector3 normal, float velocity, float CollisionVelocity)
    {
        Quaternion rot = Quaternion.LookRotation(Vector3.ProjectOnPlane(Tr.forward, normal), normal);
        Pooling.InstancesDic[Pool_Ricoshet].Use(pos, rot, null);
        Velocity = Vector3.Reflect(Velocity.normalized * (velocity - CollisionVelocity * CollisionVelocityDumping), normal);
        WasReflect = true;
        if (projectile.ScyncEffects && projectile.IsMine)
            projectile.View.RPC("ScyncWorldEffect", PhotonTargets.Others, pos, rot, Pool_Ricoshet);
    }

    void Explose(Vector3 pos, Quaternion rot)
    {
        Pooling.InstancesDic[ExplosePoolName].Use(pos, rot, null);
        if (projectile.ScyncEffects && projectile.IsMine)
            projectile.View.RPC("ScyncWorldEffect", PhotonTargets.Others, pos, rot, ExplosePoolName);
        Deactivate();
    }

    protected void Deactivate()
    {
        PO.Deactivate();
    }

    public virtual void GetDescription(ref List<string> Parameters, ref List<string> Values){ }
}