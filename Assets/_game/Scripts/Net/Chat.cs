using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(PhotonView))]
public class Chat : Photon.MonoBehaviour
{
    public InputField InputField;
    public UILink Text;
    public UILink TextRect;
    public UILink Window;
    public Scrollbar Scrollbar;

    public List<string> messages = new List<string>();
    [System.NonSerialized]
    public PhotonView View;
    public static bool Active = false;

    void Start()
    {
        View = GetComponent<PhotonView>();
        //InputField.onEndEdit.AddListener(delegate { AddMessege(); });
        InputEvents.Instance.OnButtonDown("Enter").AddListener(delegate
        {
            if (!Active)
            {
                InputField.interactable = true;
                InputField.ActivateInputField();
                Window.RectTransform.sizeDelta = new Vector2(300, Screen.height - 100);
                Active = true;
            }
            else
            {
                AddMessege();
                Window.RectTransform.sizeDelta = new Vector2(200, 100);
            }
        });
        InputEvents.Instance.OnButtonDown("Cancel").AddListener(delegate
        {
            if (Active)
            {
                InputField.text = string.Empty;
                InputField.interactable = false;
                Active = false;
                Window.RectTransform.sizeDelta = new Vector2(200, 100);
            }
        });
    }


    void AddMessege()
    {
        if (InputField.text != string.Empty && PhotonNetwork.connected)
        {
            View.RPC("Messege", PhotonTargets.Others, InputField.text);
            AddLine("<color=cyan>Вы:</color> <color=gray>" + InputField.text + "</color>");
            InputField.text = string.Empty;
            Scrollbar.value = 0;
        }
        InputField.interactable = false;
        Active = false;
    }

    [PunRPC]
    public void Messege(string newLine, PhotonMessageInfo mi)
    {
        string senderName = "anonymous";

        if (mi.sender != null)
        {
            if (!string.IsNullOrEmpty(mi.sender.NickName))
            {
                senderName = mi.sender.NickName;
            }
            else
            {
                senderName = "player " + mi.sender.ID;
            }
        }
        switch (mi.sender.GetTeam())
        {
            case PunTeams.Team.none:
            AddLine("<color=green>" + senderName + ":</color>  <color=gray>" + newLine + "</color>");
                break;
            case PunTeams.Team.red:
            AddLine("<color=red>" + senderName + ":</color>  <color=gray>" + newLine + "</color>");
                break;
            case PunTeams.Team.blue:
            AddLine("<color=blue>" + senderName + ":</color>  <color=gray>" + newLine + "</color>");
                break;
        }
    }

    public void AddLine(string newLine)
    {
        messages.Add(newLine);
        Text.Text.text = string.Empty;
        foreach (string hit in messages)
        {
            Text.Text.text += "\n" + hit;
        }
        TextRect.RectTransform.anchoredPosition = new Vector2(1.5f, 12.5f + messages.Count * 12.5f);
        TextRect.RectTransform.sizeDelta = new Vector2(-7f, 25 + messages.Count * 25);
    }
}