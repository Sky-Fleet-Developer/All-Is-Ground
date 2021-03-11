using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon;
using System.Linq;

public class BattleMember : Entity
{
    protected Ship ship;

    public override void BaseInit()
    {
        callRpc += HearRPC;
        LoadShip(UsersDATA.currentAccount.ShoosedMachine.ShipPrefab);
    }

    protected override void StartFrame()
    {
    }

    protected override void StartReadPacket()
    {
         ReadPacketFromShip();
    }

    protected override void StartWritePacket()
    {
         WritePacketFromShip();
    }


    /*
        SendShipLoadType == ShipInit, GetTypeShip, CurrentPlayerNum 
        LoadShip == ShipInit, LoadThisShip, IdShip
    */
    enum RPCSend : byte
    {
        GetTypeShip = 0,
        LoadThisShip = 1,
        NumShip = 2
    }


    public T GetModule<T>() where T : Module
    {
        return ship.GetModule<T>();
    }

    public void RespawnShip(Vector3 pos, Quaternion rot)
    {
        ship.transform.localPosition = pos;
        ship.transform.localRotation = rot;
        ship.PhysShip.velocity = Vector3.zero;
        ship.PhysShip.angularVelocity = Vector3.zero;
    }

    public void SpawnShip(Vector3 pos, Quaternion rot)
    {
        ship.transform.localPosition = pos;
        ship.transform.localRotation = rot;
        ship.PhysShip.velocity = Vector3.zero;
        ship.PhysShip.angularVelocity = Vector3.zero;
    }

    private void HearRPC(byte[] bts)//Resolution rpc functions
    {
        //Debug.Log("Запрос на корабль " + (byte)TypeRPCEntitySend.ShipInit + "  " + (byte)RPCSend.GetTypeShip);
        if (bts[0] == (byte)TypeRPCEntitySend.ShipInit)
        {
            if (bts[1] == (byte)RPCSend.GetTypeShip)
            {
                SendShipLoadType(PhotonNetwork.playerList.Where((x) => x.ID == bts[2]).FirstOrDefault());
            }
            else if (bts[1] == (byte)RPCSend.LoadThisShip)
            {
                //Тут будет загрузка корабля CallAnswerTypeShip(Ship ship)
                CallAnswerTypeShip(Garage.Instance.ShipData.GetShipID(bts[2]).ShipPrefab);
            }
        }
    }

    public void LoadShip(Ship ship)
    {
        Ship s = Instantiate(ship, transform);
        this.ship = s;
        s.transform.localPosition = Vector3.zero;
        s.transform.localRotation = Quaternion.identity;
        s.Init(IsMine);
        if (IsMine)
        {
            SendRPC(PhotonTargets.Others, (byte)TypeRPCEntitySend.ShipInit, (byte)RPCSend.LoadThisShip, (byte)s.ID);//Нужно поменять на aйди корабля
        }
    }

    private void SendShipLoadType(PhotonPlayer player)//Request to the player master to get the type ship
    {
        //Debug.Log("Я дал ответ");
        if (ship)
        {
            SendRPC(player, (byte)TypeRPCEntitySend.ShipInit, (byte)RPCSend.LoadThisShip, (byte)ship.ID);
        }
    }

    #region InitCopy

    protected override void InitCopy()
    {
        callRpc += HearRPC;
        //Debug.LogError("Загрузилась копия");
        SendRPC(PhotonTargets.MasterClient, (byte)TypeRPCEntitySend.ShipInit, (byte)RPCSend.GetTypeShip, CurrentPlayerID);
    }

    private void CallAnswerTypeShip(Ship ship)//LoadShip which was sent player master
    {
        LoadShip(ship);
    }
    #endregion


    #region WorkFromObserve
    public void WritePacketFromShip()
    {
        if (stream != null)
        {
            if (ship)
            {
                List<object[]> packets = new List<object[]>();
                Module[] modules = ship.GetModules();

                int countParam = 0;

                for (int i = 0; i < modules.Length; i++)
                {
                    countParam++;
                    packets.Add(modules[i].GetPacketPhoton());
                    countParam += packets[packets.Count - 1].Length;
                }

                WritePacketBytes(countParam);
                WritePacketBytes(ship.ID);

                for (int i = 0; i < packets.Count; i++)
                {
                    WritePacketBytes(packets[i].Length);
                    for (int i2 = 0; i2 < packets[i].Length; i2++)
                    {
                        WritePacketBytes(packets[i][i2]);
                    }
                }
            }
            else {
                WritePacketBytes(0);
                WritePacketBytes(0);
            }
        }
    }

    public void ReadPacketFromShip()
    {
        if (stream != null)
        {
            int countParam = (int)ReadPacketBytes(false);
            int idShip = (int)ReadPacketBytes(false);
            if (ship && idShip == ship.ID)
            {
                int getParams = 0;
                int numModul = 0;
                while (countParam != getParams)
                {
                    int count = (int)ReadPacketBytes(false);
                    object[] obj = new object[count];
                    for (int i = 0; i < count; i++)
                    {
                        obj[i] = ReadPacketBytes(false);
                    }
                    getParams += 1 + count;

                    ship.GetModules()[numModul].SetPacketPhoton(obj);
                    numModul++;
                }
            }
            else
            {
                for (int i = 0; i < countParam; i++)
                {
                    ReadPacketBytes(false);
                }
                //event: The load info no this ship
            }
        }
    }
    #endregion
}
