using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PhotonView))]
public class CaptureblePoint : MonoBehaviour
{
    public ParticleSystem Letter;
    public string UILetter = "A";
    public float Radius;
    [System.NonSerialized]
    public List<Control> PlayersInside;
    [System.NonSerialized]
    public float RedTeamOwenship;
    [System.NonSerialized]
    public float BlueTeamOwenship;
    [System.NonSerialized]
    public PunTeams.Team Owner = PunTeams.Team.none;

    [System.NonSerialized]
    public Transform transform;
    [System.NonSerialized]
    public PhotonView View;
    UILink Red;
    UILink Blue;
    UILink Background;
    void Awake()
    {
        transform = base.transform;
        View = GetComponent<PhotonView>();
        PlayersInside = new List<Control>();
        Owner = PunTeams.Team.none;
    }

    void Start()
    {
        StartCoroutine(OwenshipCounterRoutine());
        var PointsUI = UILink.MainCanvas.GetChildByName("BattleStateUI").GetChildByName("PointsUI").Pooling.Use().GetComponent<UILink>();
        Red = PointsUI.GetChildByName("Red");
        Blue = PointsUI.GetChildByName("Blue");
        Background = PointsUI.GetChildByName("Background");
    }

    IEnumerator OwenshipCounterRoutine()
    {
        yield return new WaitForSeconds(1f);
        while (Application.isPlaying)
        {
            yield return new WaitForSeconds(1f);
            if (PhotonNetwork.isMasterClient)
            {
                PlayersInside = new List<Control>();

                foreach(var hit in FindObjectsOfType<Control>())
                {
                    if (Vector3.SqrMagnitude(hit.transform.position - transform.position) < Radius * Radius)
                        PlayersInside.Add(hit);
                }

                List<int> owenship = new List<int>();
                owenship.Add(0);
                owenship.Add(0);
                foreach (var hit in PlayersInside)
                {
                    if (hit.IsAlive)
                    {
                        owenship[(int)hit.View.owner.GetTeam() - 1] += 1;
                    }
                }
                if (owenship[0] != owenship[1])
                {

                    bool redMore = owenship[1] < owenship[0];
                    int more = Mathf.Max(owenship[0], owenship[1]);

                    BlueTeamOwenship += redMore ? -more : more;
                    RedTeamOwenship += redMore ? more : -more;

                    RedTeamOwenship = Mathf.Clamp(RedTeamOwenship, 0, 15);
                    BlueTeamOwenship = Mathf.Clamp(BlueTeamOwenship, 0, 15);

                    float count = RedTeamOwenship + BlueTeamOwenship;

                    count /= 2;
                    if (count > 15)
                    {
                        RedTeamOwenship /= count;
                        BlueTeamOwenship /= count;
                    }
                }

                var lastOwner = Owner;

                SolveOwner();

                if (lastOwner != Owner)
                {
                    switch (Owner)
                    {
                        case PunTeams.Team.red:
                            GameLog.Write("<color=red>Красная команда захватила точку " + UILetter + "</color>", GameLog.LogType.GameEvent);
                            break;
                        case PunTeams.Team.blue:
                            GameLog.Write("<color=blue>Синяя команда захватила точку " + UILetter + "</color>", GameLog.LogType.GameEvent);
                            break;
                    }

                    int add = (int)Mathf.Pow(GameManager.GetEnamysCount(), 0.5f) * 15;
                    if (add > 0)
                    {
                        foreach (var hit in PlayersInside)
                        {
                            if (hit.View.owner.GetTeam() == Owner)
                            {
                                hit.View.owner.AddScore(add);
                                UsersDATA.AddExperience(hit.View.owner.NickName, add);
                            }

                        }
                    }
                    else
                    {
                        Debug.Log(add);
/*#if UNITY_EDITOR
                        UnityEditor.EditorApplication.isPaused = true;
#endif*/
                    }
                }

                if (Owner == PunTeams.Team.none)
                {
                    if (PlayersInside.Count == 0)
                    {
                        RedTeamOwenship--;
                        BlueTeamOwenship--;
                    }
                }

                View.RPC("ScyncOwenship", PhotonTargets.Others, RedTeamOwenship, BlueTeamOwenship);
            }
        }
    }

    public void SolveOwner()
    {
        Owner = PunTeams.Team.none;
        Background.Text.color = Color.white;
        Background.Text.text = UILetter;
        var main = Letter.main;
        main.startColor = Color.white;
        if (BlueTeamOwenship == 15)
        {
            Owner = PunTeams.Team.blue;
            Background.Text.color = Color.blue;
            main.startColor = Color.blue;
        }
        else if (RedTeamOwenship == 15)
        {
            Owner = PunTeams.Team.red;
            Background.Text.color = Color.red;
            main.startColor = Color.red;
        }
        Red.Image.fillAmount = RedTeamOwenship / 15;
        Blue.Image.fillAmount = BlueTeamOwenship / 15;
    }


    [PunRPC]
    public void ScyncOwenship(float RedO, float BlueO)
    {
        RedTeamOwenship = RedO;
        BlueTeamOwenship = BlueO;
        SolveOwner();
    }
}
