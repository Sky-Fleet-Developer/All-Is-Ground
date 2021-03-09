using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon;
using System.Linq;

public delegate void RPCSend(byte[] bts);

public enum TypeRPCEntitySend
{
    ShipInit = 0,
    Player = 1,
    Bot = 2
}

public abstract class Entity : UnityEngine.MonoBehaviour, IPunObservable
{
    public bool IsMine => view.isMine;

    public byte CurrentPlayer => (byte)PhotonNetwork.player.ID;

    public WorkFromShip ShipWork { get { return workFromShip; } }

    [SerializeField] private PhotonView view;

    protected event RPCSend callRpc;

    protected Ship ship;

    private WorkFromShip workFromShip;

    private PhotonStream stream;

    private TypeWorkFromPacket workFromPacketT = TypeWorkFromPacket.None;

    private void Awake()
    {
        if (!IsMine)
        {
            workFromShip = new WorkFromShip(this);
            InitCopy();
        }
    }

    public void BaseInit()
    {
        workFromShip = new WorkFromShip(this);
        Init();
    }

    private void Start()
    {
        StartFrame();
    }

    protected abstract void Init();

    protected abstract void InitCopy();

    protected abstract void StartFrame();


    #region PhotonWork

    enum TypeWorkFromPacket
    {
        Read = 0,
        Write = 1,
        None = 2
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)//Call photonView
    {
        this.stream = stream;
        if (stream.isReading)
        {
            workFromPacketT = TypeWorkFromPacket.Read;
            StartReadPacket();
        }
        else
        {
            workFromPacketT = TypeWorkFromPacket.Write;
            StartWritePacket();
        }
        this.stream = null;

        workFromPacketT = TypeWorkFromPacket.None;
    }

    protected abstract void StartWritePacket();

    protected void WritePacketBytes(object obj)
    {
        if (workFromPacketT != TypeWorkFromPacket.Write) return;
        stream.SendNext(obj);

    }

    protected abstract void StartReadPacket();

    protected object ReadPacketBytes(bool isPeek)
    {
        if (workFromPacketT != TypeWorkFromPacket.Read) return null;

        if (isPeek)
        {
            return stream.PeekNext();
        }
        else
        {
            return stream.ReceiveNext();
        }
    }

    #region RPC
    protected void SendRPC(PhotonTargets targets, params byte[] bts)
    {
        view.RpcSecure("CallRPC", targets, false, bts);
    }

    protected void SendRPC(PhotonPlayer player, params byte[] bts)
    {
        view.RpcSecure("CallRPC", player, false, bts);
    }

    private void CallRPC(params byte[] bts)// Byte type system
    {
        if (callRpc != null)
        {
            callRpc(bts);
        }
    }


    #endregion
    #endregion

    public class WorkFromShip
    {
        /*
            SendShipLoadType == ShipInit, GetTypeShip, CurrentPlayerNum 
            LoadShip == ShipInit, LoadThisShip, IdShip
        */
        enum RPCSend
        {
            GetTypeShip = 0,
            LoadThisShip = 1,
            NumShip = 2
        }


        private Entity entity;
        public WorkFromShip(Entity entity)
        {
            this.entity = entity;
            entity.callRpc += HearRPC;
            if (!entity.IsMine)
            {
                InitCopy();
            }
        }

        private void HearRPC(byte[] bts)//Resolution rpc functions
        {
            if (bts[0] == (byte)TypeRPCEntitySend.ShipInit)
            {
                if (bts[1] == (byte)RPCSend.GetTypeShip)
                {
                    SendShipLoadType(PhotonNetwork.playerList.Where((x) => x.ID == bts[2]).FirstOrDefault());
                }
                else if (bts[1] == (byte)RPCSend.LoadThisShip)
                {
                    //Тут будет загрузка корабля CallAnswerTypeShip(Ship ship)
                }
            }
        }

        public void LoadShip(Ship ship)
        {
            entity.ship = Instantiate(ship, entity.transform);
            entity.ship.Init();
            if (entity.IsMine)
            {
                entity.SendRPC(PhotonTargets.Others, (byte)TypeRPCEntitySend.ShipInit, (byte)RPCSend.LoadThisShip, 0);//Нужно поменять на йди корабля
            }
        }

        private void SendShipLoadType(PhotonPlayer player)//Request to the player master to get the type ship
        {
            entity.SendRPC(player, (byte)TypeRPCEntitySend.ShipInit, (byte)RPCSend.LoadThisShip, 0);//Нужно поменять на йди корабля
        }




        #region InitCopy

        private void InitCopy()
        {
            entity.SendRPC(PhotonTargets.MasterClient, (byte)TypeRPCEntitySend.ShipInit, (byte)RPCSend.GetTypeShip, entity.CurrentPlayer);
        }

        private void CallAnswerTypeShip(Ship ship)//LoadShip which was sent player master
        {
            LoadShip(ship);
        }
        #endregion

        #region WorkFromObserve
        public void WritePacketFromShip()
        {

        }

        public void ReadPacketFromShip()
        {

        }
        #endregion
    }
}
