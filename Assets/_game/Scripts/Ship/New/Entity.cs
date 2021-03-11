using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon;
using System.Linq;

public delegate void RPCSend(byte[] bts);

public enum TypeRPCEntitySend : byte
{
    ShipInit = 0,
    Player = 1,
    Bot = 2
}

public abstract class Entity : UnityEngine.MonoBehaviour, IPunObservable
{
    public bool IsMine => view.isMine;

    public byte CurrentPlayerID => (byte)PhotonNetwork.player.ID;

    public PhotonPlayer Owner => view.owner;

    [SerializeField] private PhotonView view;

    protected event RPCSend callRpc;

    protected PhotonStream stream;

    private TypeWorkFromPacket workFromPacketT = TypeWorkFromPacket.None;

    private void Awake()
    {
        if (!IsMine)
        {
            InitCopy();
        }
    }

    public abstract void BaseInit();


    private void Start()
    {
        StartFrame();
    }

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
        //Debug.Log("Игрок: " + CurrentPlayerID + "Отправляеться сообщение!! " + bts[1]);
        view.RpcSecure("CallRPC", targets, false, bts);
    }

    protected void SendRPC(PhotonPlayer player, params byte[] bts)
    {
        //Debug.Log("Игрок: " + CurrentPlayerID + "Отправляеться сообщение!! " + bts[1]);
        view.RpcSecure("CallRPC", player, false, bts);
    }


    [PunRPC]
    public void CallRPC(params byte[] bts)// Byte type system
    {
        //Debug.LogError("Пришло сообщение блять");
        if (callRpc != null)
        {
            //Debug.Log("Событие у когото вызвалось");
            callRpc(bts);
        }
    }
    #endregion
    #endregion
}
