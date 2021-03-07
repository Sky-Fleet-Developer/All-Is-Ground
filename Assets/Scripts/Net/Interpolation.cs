using UnityEngine;
using System.Collections;

public class Interpolation : MonoBehaviourPlus
{
    PhotonView View;
    public float Delay;
    float Timer;
    Transform Tr;
    Rigidbody Rigid;
    void Start()
    {
        View = GetComponent<PhotonView>();
        Rigid = GetComponent<Rigidbody>();
        Tr = transform;
    }

    void Update()
    {
        if (PhotonNetwork.connected && View.isMine)
        {
            Timer -= Time.deltaTime;
            if (Timer < 0.0)
            {
                View.RPC("Scync", PhotonTargets.Others, Tr.position, Tr.rotation, Rigid.velocity, Rigid.angularVelocity, PhotonNetwork.time);
                Timer = Delay;
            }
        }
        if (PhotonNetwork.connected && !View.isMine)
        {
            float ping = Time.time - time;
            Tr.rotation = Quaternion.Lerp(Tr.rotation, Rot, Time.deltaTime * 5f);
            Vector3 tar = Pos + ClampDistance(Vel * ping * 2, 0, 20);
            Rigid.velocity = ClampDistance(tar - Tr.position, 0, 50) * 2;
        }
    }

    public Vector3 Pos;
    public Quaternion Rot;
    public Vector3 Vel;
    public float time;
    [PunRPC]
    void Scync(Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angVelocity, double t)
    {
        float ping = (float)(PhotonNetwork.time - t);
        //Debug.Log("p = " + ping);
        if (Vector3.Distance(Tr.position, position) > 10)
        {
            Tr.position = position;
            Tr.rotation = rotation;
            Pos = position;
            Vel = Vector3.zero;
            Rot = rotation;

        }
        else
        {
            Pos = position + ClampDistance(velocity * ping, 0, 20);
            Rot = rotation * Quaternion.Euler(angVelocity * 0.1f);
            Vel = velocity;
        }
        time = Time.time;
    }
}
