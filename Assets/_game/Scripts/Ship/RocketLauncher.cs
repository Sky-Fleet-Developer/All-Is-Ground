using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RocketLauncher : Projectile
{
    public List<Charge> Followers;
    public bool FollowRocket;

    float ScyncTimer = 0.1f;
    bool Fire1WasUp;
    bool Fire2WasUp;

    /*Vector3[] positions;
    Quaternion[] rotations;
    Vector3[] velocitys;
    void SetFollowers()
    {
        for (int i = 0; i < Followers.Count; i++)
        {
            if (i < Followers.Count)
            {
                if (i < positions.Length)
                    Followers[i].Tr.position = positions[i];
                if (i < rotations.Length)
                    Followers[i].Tr.rotation = rotations[i];
                if (i < velocitys.Length)
                    Followers[i].Velocity = velocitys[i];
            }
        }
    }
    void GetFollowers()
    {
        positions = new Vector3[Followers.Count];
        rotations = new Quaternion[Followers.Count];
        velocitys = new Vector3[Followers.Count];
        for (int i = 0; i < Followers.Count; i++)
        {
            positions[i] = Followers[i].Tr.position;
            rotations[i] = Followers[i].Tr.rotation;
            velocitys[i] = Followers[i].Velocity;
        }
    }*/


    protected override void OnStart()
    {
        base.OnStart();
        Followers = new List<Charge>();
    }

    void FollowRockets()
    {
        for (int i = 0; i < Followers.Count; i++)
            if (!Followers[i].gameObject.GetActive())
            {
                Followers.RemoveRange(i, 1);
                i--;
            }
        if (Followers.Count == 0)
            FollowRocket = false;

        foreach (var Hit in Followers)
        {
            Hit.WantedPoint = Control.AimPoint;
            Hit.Pather = Quaternion.LookRotation(Tr.position - Control.AimPoint);
        }
        /*if (IsMine)
        {
            ScyncTimer = Mathf.MoveTowards(ScyncTimer, 0f, Time.deltaTime);

            if (ScyncTimer == 0f)
            {
                ScyncTimer = 0.1f;
                GetFollowers();
                View.RPC("ScyncFollowers", PhotonTargets.Others, JsonHelper.ToJson(positions), JsonHelper.ToJson(rotations), JsonHelper.ToJson(velocitys));
            }
        }*/
    }

    protected override void OnDischarge(Charge charge, int block, int ID)
    {
        FollowRocket = true;
        Followers.Add(charge);
        charge.Pather = Quaternion.LookRotation(Tr.position - Control.AimPoint);

        /*if (PhotonNetwork.connected)
        {
            View.RPC("ScyncRLDischarge", PhotonTargets.Others, charge.Tr.position, charge.Tr.rotation, block, ID);
        }*/
    }

    protected override void OnUpdate()
    {
        if (Control.Fire2)
        {
            if (Fire2WasUp) 
                Discharge();
            else if (Followers.Count > 0)
                FollowRockets();

            if (Control.Fire1)
            {
                Discharge();
            }
        }
        else
        {
            if (FollowRocket)
            {
                FollowRocket = false;
                Followers = new List<Charge>();
            }
        }

        Fire2WasUp = !Control.Fire2;
        Fire1WasUp = !Control.Fire1;
    }

    /*[PunRPC]
    void ScyncFollowers(string pos, string rot, string vel)
    {
        positions = JsonHelper.FromJson<Vector3>(pos);
        rotations = JsonHelper.FromJson<Quaternion>(rot);
        velocitys = JsonHelper.FromJson<Vector3>(vel);
        SetFollowers();
        FollowRockets();
    }*/

    [PunRPC]
    void ScyncRLDischarge(Vector3 pos, Quaternion rot, int Block, int ID)
    {
        var charge = ChargePool.Use(ID).GetComponent<Charge>();
        charge.transform.position = pos;
        charge.transform.rotation = rot;
        FollowRocket = true;
        Followers.Add(charge);
        charge.Velocity = charge.transform.forward * charge.StartSpeed;
        charge.projectile = this;
        flyCharges.Add(ID, charge);
    }
}
