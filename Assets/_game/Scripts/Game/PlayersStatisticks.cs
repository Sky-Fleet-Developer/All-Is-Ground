using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayersStatisticks : MonoBehaviourPlus
{
    public static PlayersStatisticks Instance;

    [NonSerialized]
    public UILink StatistickWindow;
    [NonSerialized]
    public UILink RedTeamPool;
    [NonSerialized]
    public UILink BlueTeamPool;



    void Start()
    {
        Instance = this;
        InputEvents.Instance.OnButtonDown("Statistick").AddListener(delegate { ShowStatistick(); });
        InputEvents.Instance.OnButtonUp("Statistick").AddListener(delegate { HideStatistick(); });
        StatistickWindow = UILink.MainCanvas.GetChildByName("Statisticks");
        RedTeamPool = StatistickWindow.GetChildByName("RedTeam");
        BlueTeamPool = StatistickWindow.GetChildByName("BlueTeam");
    }

    public void ShowStatistick()
    {
        StatistickWindow.gameObject.SetActive(true);
        UILink item;
        for (int i = 0; i < 11; i++)
        {
            item = RedTeamPool.GetChildByName(string.Format("PlayerInfoItem ({0})", i));
            item.gameObject.SetActive(false);
            item = BlueTeamPool.GetChildByName(string.Format("PlayerInfoItem ({0})", i));
            item.gameObject.SetActive(false);
        }
        for (int i = 0; i < GameManager.RedTeam.Count; i++)
        {
            item = RedTeamPool.GetChildByName(string.Format("PlayerInfoItem ({0})", i));
            item.gameObject.SetActive(true);
            item.GetChildByName("Name").Text.text = GameManager.RedTeam[i].player.NickName;
            item.GetChildByName("Score").Text.text = GameManager.RedTeam[i].player.GetScore().ToString();
        }
        for (int i = 0; i < GameManager.BlueTeam.Count; i++)
        {
            item = BlueTeamPool.GetChildByName(string.Format("PlayerInfoItem ({0})", i));
            item.gameObject.SetActive(true);
            item.GetChildByName("Name").Text.text = GameManager.BlueTeam[i].player.NickName;
            item.GetChildByName("Score").Text.text = GameManager.BlueTeam[i].player.GetScore().ToString();
        }
    }

    public void HideStatistick()
    {
        StatistickWindow.gameObject.SetActive(false);
    }

    public void OnPhotonPlayerConnected(PhotonPlayer player)
    {
        Debug.Log("Connected: " + player.NickName);
    }
    public void OnPhotonPlayerDisconnected(PhotonPlayer player)
    {
        Debug.Log("Disconnected: " + player.NickName);
    }
}