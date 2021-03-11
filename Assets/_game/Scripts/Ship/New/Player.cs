using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : BattleMember
{
    public override void BaseInit()
    {
        base.BaseInit();
        ship.GetModule<Control>().SetGetControllFromInput();
    }

    protected override void InitCopy()
    {
        base.InitCopy();
    }

    protected override void StartFrame()
    {
        base.StartFrame();
    }

    protected override void StartReadPacket()
    {
        base.StartReadPacket();
        //Debug.LogError(ReadPacketBytes(false));
    }

    protected override void StartWritePacket()
    {
        base.StartWritePacket();
       // WritePacketBytes(GetHashCode() + " Лох");
    }
}
