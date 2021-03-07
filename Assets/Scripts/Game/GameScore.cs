using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[RequireComponent(typeof(PhotonView))]
public class GameScore : MonoBehaviour
{
    public static GameScore Instance;
    public CaptureblePoint[] Points;

    public int RedTeamScore;
    public int BlueTeamScore;
    [System.NonSerialized]
    public PhotonView View;
    UILink RedScore;
    UILink BlueScore;
    void Awake()
    {
        View = GetComponent<PhotonView>();
        Points = FindObjectsOfType<CaptureblePoint>();
        Instance = this;
    }

    void Start()
    {
        var score = UILink.MainCanvas.GetChildByName("BattleStateUI").GetChildByName("Score");
        RedScore = score.GetChildByName("Red");
        BlueScore = score.GetChildByName("Blue");
        StartCoroutine(ScoreCounterRoutine());
    }

    IEnumerator ScoreCounterRoutine()
    {
        while (Application.isPlaying)
        {
            yield return new WaitForSeconds(1f);
            if (PhotonNetwork.isMasterClient)
            {
                for(int i = 0; i < Points.Length; i++)
                {
                    if(Points[i].Owner != PunTeams.Team.none)
                    {
                        if (Points[i].Owner == PunTeams.Team.blue)
                            BlueTeamScore += 1;
                        else
                            RedTeamScore += 1;
                    }
                }
                DrawScore();
                View.RPC("ScyncScore", PhotonTargets.Others, BlueTeamScore, RedTeamScore);
            }
        }
    }

    public void AddBlueScore(int scoreToAdd)
    {

        View.RPC("AddScore", PhotonTargets.MasterClient, scoreToAdd, 0);
    }

    public void AddRedScore(int scoreToAdd)
    {

        View.RPC("AddScore", PhotonTargets.MasterClient, 0, scoreToAdd);
    }

    public void DrawScore()
    {
        BlueScore.Text.text = BlueTeamScore.ToString();
        RedScore.Text.text = RedTeamScore.ToString();
    }

    /*public void OnPhotonPlayerConnected(PhotonPlayer player)
    {
        if (PhotonNetwork.isMasterClient)
        {
            View.RPC("ScyncScore", player, Score);
        }
    }*/
    [PunRPC]
    public void AddScore(int blue, int red)
    {
        BlueTeamScore += blue;
        RedTeamScore += red;
        View.RPC("ScyncScore", PhotonTargets.Others, BlueTeamScore, RedTeamScore);
    }

    [PunRPC]
    public void ScyncScore(int blue, int red)
    {
        BlueTeamScore = blue;
        RedTeamScore = red;
        DrawScore();
    }
}
