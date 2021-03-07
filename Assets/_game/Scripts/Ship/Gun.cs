using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gun : Projectile
{
    protected override void OnUpdate()
    {
        for (int i = 0; i < Turels.Count; i++)
        {
            Turels[i].Rotate(Control.AimPoint);
        }
        if (Control.Fire1 && !Control.Fire2)
        {
            Discharge();
        }
    }

    void LateUpdate()
    {
        for (int i = 0; i < Turels.Count; i++)
        {
            if (Control.UserControl)
            {
                Aiming[i].RectTransform.anchoredPosition = Turels[i].GetRaycastScreenPose();
            }
        }
    }

    protected override void OnDischarge(Charge charge, int block, int ID)
    {
        /*if (PhotonNetwork.connected)
        {
            View.RPC("ScyncGunDischarge", PhotonTargets.Others, charge.Tr.position, charge.Tr.rotation, block, ID);
        }*/
    }



    [PunRPC]
    void ScyncGunDischarge(Vector3 pos, Quaternion rot, int Block, int ID)
    {
        var charge = ChargePool.Use(ID).GetComponent<Charge>();
        charge.transform.position = pos;
        charge.transform.rotation = rot;
        charge.Velocity = charge.transform.forward * charge.StartSpeed;
        charge.projectile = this;
        if(!flyCharges.ContainsKey(ID))
            flyCharges.Add(ID, charge);
    }
}
