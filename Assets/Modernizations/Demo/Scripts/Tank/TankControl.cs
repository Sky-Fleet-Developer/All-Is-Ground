using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Modernizations;
namespace Demo 
{

public class TankControl : MonoBehaviour, IModifiable
{
    public bool IsPlayer;
    [HideInInspector]
    public float Horizontal;
    [HideInInspector]
    public float Vertical;

    public List<WheelCollider> WheelsLeft;
    public List<WheelCollider> WheelsRight;
    public float StayEngineRPM;
    [Modifiable("Maximal RPM")]
    public float MaxEngineRPM;
    public float EngineRPM;
    public float EngineMass;
    [Modifiable("Engine power")]
    public float EnginePower;
    public float WheelsForce;
    public float RPMMul;
    public float breaktorque;

    float Clutch;
    float LeftTrackWantedSpeed;
    float RightTrackWantedSpeed;
    float LeftTrackRPM;
    float RightTrackRPM;

    public Renderer RightTrack;
    public Renderer LeftTrack;
    public Vector2 TracksTextureSpeed;
    public ParticleSystem RBTrackParticle;
    public ParticleSystem LBTrackParticle;
    public float TrackParticleSpeedMP;

    public float COM;

    float LeftAddForce;
    float RightAddForce;

    Rigidbody Rigid;

    void Start()
    {
        Rigid = GetComponent<Rigidbody>();
        Rigid.centerOfMass -= transform.up * COM;
        if (IsPlayer)
            gameObject.name = "Player";
        else
            gameObject.name = "Enamy";

        foreach (WheelCollider wh in WheelsLeft)
        {
            wh.ConfigureVehicleSubsteps(5, 12, 15);
        }
        foreach (WheelCollider wh in WheelsRight)
        {
            wh.ConfigureVehicleSubsteps(5, 12, 15);
        }

    }

    void FixedUpdate()
    {
        if (IsPlayer)
        {
            Horizontal = Mathf.MoveTowards(Horizontal, Input.GetAxis("Horizontal"), Time.fixedDeltaTime * 2);
            Vertical = Mathf.MoveTowards(Vertical, Input.GetAxis("Vertical"), Time.fixedDeltaTime * 2);
        }

        Clutch = Mathf.Max(Mathf.Abs(Vertical), Mathf.Abs(Horizontal));

        if (Vertical != 0f || Horizontal != 0f)
            EngineRPM = Mathf.MoveTowards(EngineRPM, MaxEngineRPM, Time.fixedDeltaTime * EnginePower);
        else
            EngineRPM = Mathf.MoveTowards(EngineRPM, StayEngineRPM, Time.fixedDeltaTime * EnginePower);

        LeftTrackWantedSpeed = (Vertical + (Horizontal * (1f - Mathf.Abs(Vertical * 0.3f)))) * EngineRPM;
        RightTrackWantedSpeed = (Vertical - (Horizontal * (1f - Mathf.Abs(Vertical * 0.3f)))) * EngineRPM;

        float lastRRpm = RightTrackRPM;
        float lastLRpm = LeftTrackRPM;
        RightTrackRPM = 0f;
        LeftTrackRPM = 0f;
        foreach (WheelCollider wh in WheelsLeft)
        {
            if (Mathf.Abs(LeftTrackRPM) < Mathf.Abs(wh.rpm))
                LeftTrackRPM = wh.rpm;
            wh.brakeTorque = wh.rpm != 0 ? breaktorque : 0;
            LeftAddForce = (LeftTrackWantedSpeed - wh.rpm * RPMMul) * Clutch;
            Debug.DrawRay(wh.transform.position, transform.forward * LeftTrackRPM * Time.fixedDeltaTime);
            wh.motorTorque = LeftAddForce * WheelsForce;
        }
        foreach (WheelCollider wh in WheelsRight)
        {
            if (Mathf.Abs(RightTrackRPM) < Mathf.Abs(wh.rpm))
                RightTrackRPM = wh.rpm;
            wh.brakeTorque = wh.rpm != 0 ? breaktorque : 0;
            Debug.DrawRay(wh.transform.position, transform.forward * RightTrackRPM * Time.fixedDeltaTime);
            RightAddForce = (RightTrackWantedSpeed - wh.rpm * RPMMul) * Clutch;
            wh.motorTorque = RightAddForce * WheelsForce;
        }
        LeftTrackRPM = Mathf.Lerp(lastLRpm, LeftTrackRPM, Time.fixedDeltaTime * 3);
        RightTrackRPM = Mathf.Lerp(lastRRpm, RightTrackRPM, Time.fixedDeltaTime * 3);

        EngineRPM -= (LeftAddForce * (Mathf.Abs(LeftTrackWantedSpeed) / (LeftTrackWantedSpeed + 0.01f)) + RightAddForce * (Mathf.Abs(RightTrackWantedSpeed) / (RightTrackWantedSpeed + 0.01f))) * 0.5f / EngineMass * Time.fixedDeltaTime;
        EngineRPM = Mathf.Clamp(EngineRPM, 0, MaxEngineRPM * 2);

        LeftTrack.material.SetTextureOffset("_MainTex", LeftTrack.material.GetTextureOffset("_MainTex") + TracksTextureSpeed * LeftTrackRPM);
        RightTrack.material.SetTextureOffset("_MainTex", RightTrack.material.GetTextureOffset("_MainTex") + TracksTextureSpeed * RightTrackRPM);

        WheelCollider LW = WheelsLeft.Find(x => x.name.Contains("WheelDummy_LB2"));
        WheelCollider RW = WheelsRight.Find(x => x.name.Contains("WheelDummy_RB2"));
        RBTrackParticle.startSpeed = Mathf.Clamp(RW.rpm * TrackParticleSpeedMP, 0f, 10f);
        LBTrackParticle.startSpeed = Mathf.Clamp(LW.rpm * TrackParticleSpeedMP, 0f, 10f);
        RBTrackParticle.transform.rotation = Quaternion.LookRotation(WheelsRight.Find(x => x.name.Contains("WheelDummy_RB")).transform.position - RW.transform.position, transform.up);
        RBTrackParticle.transform.Rotate(75, 180, 0);
        LBTrackParticle.transform.rotation = Quaternion.LookRotation(WheelsRight.Find(x => x.name.Contains("WheelDummy_RB")).transform.position - RW.transform.position, transform.up);
        LBTrackParticle.transform.Rotate(75, 180, 0);
        if (RW.isGrounded)
            RBTrackParticle.emissionRate = Mathf.Clamp(RightTrackRPM * 2, 0f, 250f);
        else
            RBTrackParticle.emissionRate = 0f;
        if (LW.isGrounded)
            LBTrackParticle.emissionRate = Mathf.Clamp(LeftTrackRPM * 2, 0f, 250f);
        else
            LBTrackParticle.emissionRate = 0f;
        Vector3 pos;
        Quaternion rot;
        LW.GetWorldPose(out pos, out rot);
        LBTrackParticle.transform.position = pos - LW.transform.up * RW.radius;
        RW.GetWorldPose(out pos, out rot);
        RBTrackParticle.transform.position = pos - RW.transform.up * RW.radius;
    }

    public int GetGroup()
    {
        return 0;
    }
}
}