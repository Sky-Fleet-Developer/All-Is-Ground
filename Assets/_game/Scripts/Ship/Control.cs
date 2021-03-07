using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Control : MonoBehaviourPlus, IDestroyeble
{
    public bool UserControl;

    public LocomotorType LocomotionType;

    public Vector2 InputAxis;
    public Vector3 Forward;
    public Vector3 AimPoint;
    public float ClampUp;
    public bool Fire1;
    public bool Fire2;
    public bool Fire3;
    bool lFire1;
    bool lFire2;
    bool lFire3;
    bool illumination = true;

    public List<Light> Illuminations;

    [System.NonSerialized]
    public bool IsAlive;
    [System.NonSerialized]
    public PhotonView View;
    [System.NonSerialized]
    AIBot Bot;
    float ScyncTimer = 0.1f;
    void Start()
    {
        Forward = transform.forward;
        View = GetComponent<PhotonView>();
        Bot = GetComponent<AIBot>();
        IsAlive = true;
        TurnIllumination();
        if (PhotonNetwork.connected)
        {
            UserControl = View.isMine && !Bot;
        }
        if (UserControl)
        {
            foreach (var hit in GetComponents<Projectile>())
                InputEvents.Instance.OnButtonDown("Reload").AddListener(delegate { if (!Chat.Active) hit.Reload(); });
            InputEvents.Instance.OnButtonDown("Illumination").AddListener(delegate { if (!Chat.Active) TurnIllumination(); });
        }
    }

    public void TurnIllumination()
    {
        illumination = !illumination;
        foreach (var Hit in Illuminations)
            Hit.enabled = illumination;
        if(PhotonNetwork.connected && View.isMine)
            View.RPC("ScyncIllumination", PhotonTargets.Others, illumination);
    }


    void Update()
    {
        if (UserControl && !Chat.Active)
        {
            bool UnlockCursor = Input.GetButton("UnlockCursor");
            InputAxis = new Vector2(Input.GetAxis("Vertical"), Input.GetAxis("Horizontal"));
            Forward = MouseOrbit.Instance.Rotation * Vector3.forward;//Vector3.ProjectOnPlane(Vector3.ProjectOnPlane(MouseOrbit.Instance.Rotation * (Vector3.forward + Vector3.up), Tr.up), MouseOrbit.Instance.Tr.right).normalized;
            AimPoint = MouseOrbit.Instance.AimingHit.point;
            Debug.DrawRay(transform.position, Forward * 5);
            ClampUp = Input.GetAxis("Jump");
            Fire1 = Input.GetButton("Fire1") && !UnlockCursor;
            Fire2 = Input.GetButton("Fire2") && !UnlockCursor;
            Fire3 = Input.GetButton("Fire3") && !UnlockCursor;

        }
        if (Fire1)
            lFire1 = true;
        if (Fire2)
            lFire2 = true;
        if (Fire3)
            lFire3 = true;
        if (PhotonNetwork.connected && View.isMine)
        {
            ScyncTimer = Mathf.MoveTowards(ScyncTimer, 0f, Time.deltaTime);

            if (ScyncTimer == 0f)
            {
                ScyncTimer = 0.2f;

                View.RPC("Scync", PhotonTargets.Others, InputAxis, Forward, AimPoint, lFire1, lFire2, lFire3, ClampUp);
                lFire1 = false;
                lFire2 = false;
                lFire3 = false;
            }
        }

    }
    [PunRPC]
    public void ScyncIllumination(bool value)
    {
        TurnIllumination();
    }

    [PunRPC]
    public void Scync(Vector2 inputAxis, Vector3 forward, Vector3 aimPoint, bool fire1, bool fire2, bool fire3, float clampUp)
    {
        InputAxis = inputAxis;
        Forward = forward;
        AimPoint = aimPoint;
        ClampUp = clampUp;
        Fire1 = fire1;
        Fire2 = fire2;
        Fire3 = fire3;
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