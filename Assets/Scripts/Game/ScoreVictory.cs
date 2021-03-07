using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
[RequireComponent(typeof(GameScore))]
public class ScoreVictory : MonoBehaviourPlus
{
    [System.NonSerialized]
    public GameScore score;
    public int ScoreToVictory;
    public float TimeForMatch;

    float MatchTimer;
    bool timeCheck = false;
    UILink VictoryUI;
    UILink FaildUI;
    UILink Timer;
    UILink TimerText;
    public static bool IsMatchContinue;

    void Start()
    {
        IsMatchContinue = true;
            score = GameScore.Instance;
        if (TimeForMatch != 0f)
        {
            timeCheck = true;
            MatchTimer = TimeForMatch * 60;
        }
        VictoryUI = UILink.MainCanvas.GetChildByName("BattleStateUI").GetChildByName("Victory");
        FaildUI = UILink.MainCanvas.GetChildByName("BattleStateUI").GetChildByName("Faild");
        Timer = UILink.MainCanvas.GetChildByName("BattleStateUI").GetChildByName("Timer");
        TimerText = Timer.GetChildByName("Text");
        if (timeCheck)
            Timer.gameObject.SetActive(true);
        else
            Timer.gameObject.SetActive(false);

    }

    void Update()
    {
        if (!IsMatchContinue)
            return;
        if (PhotonNetwork.isMasterClient)
        {
            if (timeCheck)
            {
                SetTimer();
                if (MatchTimer == 0)
                    Victory();

            }
            if (ScoreToVictory > 0)
            {
                if (score.BlueTeamScore >= ScoreToVictory)
                {
                    score.View.RPC("BlueTeamVictory", PhotonTargets.All);
                }
                if (score.RedTeamScore >= ScoreToVictory)
                {
                    score.View.RPC("RedTeamVictory", PhotonTargets.All);
                }
            }
        }
        else
        {
            if (timeCheck)
            {
                SetTimer();
            }
        }
    }

    void SetTimer()
    {
        MatchTimer = Mathf.MoveTowards(MatchTimer, 0f, Time.deltaTime);
        Timer.Image.color = GameValues.BattleTimerGradient.Evaluate(MatchTimer / TimeForMatch);
        TimerText.Text.text = Seconds2TimeMinuts(MatchTimer);
    }

    public void LoadStartScene()
    {
        GameManager.LoadLobby();
    }

    public void OnPhotonPlayerConnected(PhotonPlayer player)
    {
        if (PhotonNetwork.isMasterClient && timeCheck)
            Invoke("LateScyncTimer", 1f);
    }

    void LateScyncTimer()
    {
        score.View.RPC("ScyncBattleTime", PhotonTargets.Others, (float)PhotonNetwork.time + MatchTimer, TimeForMatch);
    }

    [PunRPC]
    public void BlueTeamVictory()
    {
        switch (PhotonNetwork.player.GetTeam())
        {
            case PunTeams.Team.red:
                FaildUI.gameObject.SetActive(true);
                break;
            case PunTeams.Team.blue:
                VictoryUI.gameObject.SetActive(true);
                break;
        }
        IsMatchContinue = false;
        Invoke("LoadStartScene", 5);
    }

    [PunRPC]
    public void RedTeamVictory()
    {
        switch (PhotonNetwork.player.GetTeam())
        {
            case PunTeams.Team.red:
                VictoryUI.gameObject.SetActive(true);
                break;
            case PunTeams.Team.blue:
                FaildUI.gameObject.SetActive(true);
                break;
        }
        IsMatchContinue = false;
        Invoke("LoadStartScene", 5);
    }

    [PunRPC]
    public void ScyncBattleTime(float time, float timeForMatch)
    {
        MatchTimer = time - (float)PhotonNetwork.time;
        TimeForMatch = timeForMatch;
        if (!Timer.gameObject.GetActive())
            Timer.gameObject.SetActive(true);
    }

    void Victory()
    {
        if(score.BlueTeamScore > score.RedTeamScore)
            score.View.RPC("BlueTeamVictory", PhotonTargets.All);
        if (score.BlueTeamScore < score.RedTeamScore)
            score.View.RPC("RedTeamVictory", PhotonTargets.All);
    }
}
