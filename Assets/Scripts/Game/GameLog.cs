using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameLog : MonoBehaviour
{
    public static GameLog Instance;
    PhotonView View;
    PoollingStringLine Log;

    public enum LogType
    {
        RoomEvent = 0,
        Kill = 1,
        KillSelf = 2,
        GameEvent = 3
    }

    public static Color[] TypeColors;

    void Start()
    {
        Instance = this;
        View = GetComponent<PhotonView>();
        Log = PoollingStringLine.Instances["GameLog"];
        TypeColors = new Color[]
        {
            Color.green,
            new Color(0.75f, 0.5f, 0.25f, 1),
            new Color(0.75f, 0.5f, 0.25f, 1),
            Color.yellow
        };
    }

    void OnPhotonPlayerConnected(PhotonPlayer player)
    {
        AddLine(player.NickName + " вошёл в бой", LogType.RoomEvent);
    }

    void OnPhotonPlayerDisconnected(PhotonPlayer player)
    {
        AddLine(player.NickName + " вышел из боя", LogType.RoomEvent);
    }

    public static void Write(string text, LogType type)
    {
        Instance.AddLine(text, type);
        Instance.View.RPC("AddLine", PhotonTargets.Others, text, type);
    }

    [PunRPC]
    public void AddLine(string text, LogType type)
    {
        switch (type)
        {
            case LogType.RoomEvent:
                Log.Write(text, TypeColors[(int)type]);
                break;
            case LogType.Kill:
                string[] split = text.Split(new char[] { '|' });
                if(split.Length == 2)
                    Log.Write(split[1] + " уничтожил " + split[0], TypeColors[(int)type]);
                break;
            case LogType.KillSelf:
                    Log.Write(text + " самоуничтожился", TypeColors[(int)type]);
                break;
            case LogType.GameEvent:
                Log.Write(text, TypeColors[(int)type]);
                break;
        }
    }
}
