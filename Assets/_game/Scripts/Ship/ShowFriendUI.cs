using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShowFriendUI : MonoBehaviourPlus
{
    public Health[] All;
    public List<Health> Friends;
    Camera mainCam;
    Transform CamTr;
    Texture2D bg;
    Texture2D fill;
    Color teamColor;
    GUISkin skin;
    Font font;

    public void ResearchShips()
    {
        Friends = new List<Health>();
        All = FindObjectsOfType<Health>();
        var team = PhotonNetwork.player.GetTeam();
        foreach (var hit in All)
        {
            if (hit.View.owner.GetTeam() == team && hit.gameObject != gameObject)
                Friends.Add(hit);
        }
    }

    private void Start()
    {
        mainCam = Camera.main;
        CamTr = mainCam.transform;
        StartCoroutine(UpdateList());
        bg = Resources.Load<Texture2D>("UI/HPBar1");
        fill = Resources.Load<Texture2D>("UI/HPBar2");
        font = Resources.Load<Font>("SITKAZ");
        teamColor = PunTeamsColors[(int)PhotonNetwork.player.GetTeam()];
    }

    IEnumerator UpdateList()
    {
        while (Application.isPlaying)
        {
            ResearchShips();
            yield return new WaitForSeconds(1);
        }
    }

    private void OnGUI()
    {
        if (!skin)
        {
            skin = Instantiate(GUI.skin);
            skin.label.alignment = TextAnchor.MiddleCenter;
            skin.label.font = font;
            skin.label.fontSize = 15;
            skin.box.normal.background = bg;
            skin.box.border = new RectOffset(2, 2, 0, 2);
        }
        Vector3 scrPos;
        string label = "";
        GUI.color = teamColor;
        GUI.skin = skin;
        foreach (var Hit in Friends)
        {
            if (CamTr.InverseTransformPoint(Hit.transform.position).z > 0)
            {
                scrPos = mainCam.WorldToScreenPoint(Hit.transform.position + Hit.transform.up + Vector3.up * 5f);
                Vector2 pos = new Vector2(scrPos.x - 100, Screen.height - scrPos.y - 35);
                if (Hit.IsMine)
                    label = "Bot";
                else
                    label = Hit.View.owner.NickName;
                GUI.Label(new Rect(pos, new Vector2(200, 35)), label);
                pos = new Vector2(scrPos.x - 30, Screen.height - scrPos.y);
                Vector2 scale = new Vector2(60, 8);
                GUI.Box(new Rect(pos, scale), "");
                GUI.DrawTexture(new Rect(pos, new Vector2(scale.x * Hit.HitPoints / 100f, scale.y)), fill);
            }
        }
    }
}
