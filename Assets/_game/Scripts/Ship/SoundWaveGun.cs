using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundWaveGun : Projectile
{
    float Hold = 0f;

    protected override void OnUpdate()
    {
        for (int i = 0; i < Turels.Count; i++)
        {
            Turels[i].Rotate(Control.AimPoint);
        }
        if (Control.Fire1 && !Control.Fire2)
        {
            Hold = Mathf.MoveTowards(Hold, 1, Time.deltaTime);
            if (Control.UserControl)
            {
                for (int i = 0; i < Blocks.Count; i++)
                {
                    if (Blocks[i].Charged)
                    {
                        Reloading[i].Image.fillAmount = Hold;
                    }
                }
            }
        }
        else if (Hold > 0)
        {
            Discharge();
            if (Control.UserControl)
            {
                for (int i = 0; i < Blocks.Count; i++)
                {
                    Reloading[i].Image.fillAmount = 0;
                }
            }
            Hold = 0f;
        }
    }

    void LateUpdate()
    {
        if (Control.UserControl)
        {
            for (int i = 0; i < Turels.Count; i++)
            {
                Aiming[i].RectTransform.anchoredPosition = Turels[i].GetRaycastScreenPose();
            }
        }
    }

    protected override void OnDischarge(Charge charge, int block, int ID)
    {
        var ch = charge as SoundWawe;
        ch.SizeMultiplyer = Mathf.Lerp(1.5f, 0.7f, Hold);
        ch.Velocity /= ch.SizeMultiplyer;
    }

    public override void Death()
    {
        base.Death();
        Hold = 0;
    }
}
