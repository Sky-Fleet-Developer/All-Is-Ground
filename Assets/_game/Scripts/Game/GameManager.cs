using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Waiters;

public class GameManager : MonoBehaviourPlus
{
    public static GameManager Instance;
    [System.NonSerialized]
    public Health CurrentShip;
    [System.NonSerialized]
    public AIBot AIShip;
    [System.NonSerialized]
    public PunTeams.Team playerTeam;

    public PhotonPlayer[] PlayersList;
    public static List<SorteblePlayers> RedTeam;
    public static List<SorteblePlayers> BlueTeam;

    public static PunTeams.Team PlayerTeam { get => Instance.playerTeam; }
    void Start()
    {
        Instance = this;
        this.Wait(1f,delegate { InitPlayer(); });
        StartCoroutine(GetPlayers());
        UILink.MainCanvas.GetChildByName("Exit").Button.onClick.AddListener(LeveBattle);
    }

    public static void LoadLobby()
    {
        PhotonNetwork.LeaveRoom();
        Destroy(UsersDATA.Instance.gameObject);
        SceneManager.LoadScene(0);
    }

    public void LeveBattle()
    {
        LoadLobby();
    }

    public static void WaitForSpawn(Health ship)
    {

        var RespMessege = UILink.MainCanvas.GetChildByName("RespawnMessege");
        if (ship.control.UserControl)
        {
            RespMessege.gameObject.SetActive(true);
            RespMessege.GetChildByName("Messege").Text.text = "You dead!\nHahaha.\nWait for spawn.";
        }
        Instance.StartCoroutine(Instance.WaitForSpawnRoutine(RespMessege, ship));
    }

    IEnumerator WaitForSpawnRoutine(UILink RespMessege, Health ship)
    {
        float timer = 10f;
        while (timer > 0f)
        {
            yield return new WaitForSeconds(0.1f);
            timer -= 0.1f;
            if (ship.control.UserControl)
            {
                RespMessege.GetChildByName("State").Text.text = string.Format("{0:0.0}", timer);
                RespMessege.GetChildByName("Bar").Image.fillAmount = timer / 10;
            }
        }
        if (ship.control.UserControl)
            RespMessege.gameObject.SetActive(false);
        Respawn(ship);
    }

    void Respawn(Health ship)
    {
        bool selfDirty = ship == CurrentShip && Garage.selfShipDirty;
        bool aiDirty = ship.gameObject == AIShip.gameObject && Garage.aiShipDirty;

        if (selfDirty)
        {
            Garage.selfShipDirty = false;
            SpawnSelf();
        }
        else if (aiDirty)
        {
            Garage.aiShipDirty = false;
            SpawnAI();
        }
        else
        {
            ship.Respawn();
            ship.transform.position = SpawnPosition.Spawn(PhotonNetwork.player.GetTeam()).GetSpawnPosition();
            ship.transform.rotation = SpawnPosition.Spawn(PhotonNetwork.player.GetTeam()).Tr.rotation;
            var rb = ship.Rigid;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            StartCoroutine(Unfreeze(ship));
        }
    }

    IEnumerator Unfreeze(Health ship)
    {
        ship.Rigid.isKinematic = true;
        yield return new WaitForSeconds(0.2f);
        ship.Rigid.isKinematic = false;
    }

    public static int GetEnamysCount()
    {
        if (GameManager.PlayerTeam == PunTeams.Team.blue)
            return RedTeam.Count;
        if (GameManager.PlayerTeam == PunTeams.Team.red)
            return BlueTeam.Count;
        return 0;
    }

    IEnumerator GetPlayers()
    {
        while (Application.isPlaying)
        {
            if (PhotonNetwork.connected)
            {
                ResetPlayersList();
            }
            yield return new WaitForSeconds(1);
        }
    }

    void ResetPlayersList()
    {
        PlayersList = PhotonNetwork.playerList;
        RedTeam = new List<SorteblePlayers>();
        BlueTeam = new List<SorteblePlayers>();
        foreach (var hit in PlayersList)
        {
            switch (hit.GetTeam())
            {
                case PunTeams.Team.red:
                    RedTeam.Add(new SorteblePlayers(hit));
                    break;
                case PunTeams.Team.blue:
                    BlueTeam.Add(new SorteblePlayers(hit));
                    break;
            }
        }
        RedTeam.Sort();
        BlueTeam.Sort();
    }

    private void InitPlayer()
    {
        int red = 0;
        int blue = 0;
        foreach (var hit in PhotonNetwork.playerList)
        {
            switch (hit.GetTeam())
            {
                case PunTeams.Team.none:
                    break;
                case PunTeams.Team.red:
                    red++;
                    break;
                case PunTeams.Team.blue:
                    blue++;
                    break;
            }
        }

        if (red > blue)
        {
            playerTeam = PunTeams.Team.blue;
        }
        else
        {
            playerTeam = PunTeams.Team.red;
        }

        PhotonNetwork.player.SetTeam(playerTeam);

        switch (playerTeam)
        {
            case PunTeams.Team.none:
                break;
            case PunTeams.Team.red:
                PoollingStringLine.Instances["BattleMesseges"].Write("Вы играете за красную команду", Color.red);
                break;
            case PunTeams.Team.blue:
                PoollingStringLine.Instances["BattleMesseges"].Write("Вы играете за синюю команду", Color.blue);
                break;
        }

        SpawnSelf();
        SpawnAI();
    }

    void SpawnSelf()
    {
        var ship = PhotonNetwork.Instantiate(UsersDATA.currentAccount.ShoosedMachine.PrefabName, SpawnPosition.Spawn(PhotonNetwork.player.GetTeam()).GetSpawnPosition(), SpawnPosition.Spawn(PhotonNetwork.player.GetTeam()).Tr.rotation, 0);
        UsersDATA.currentAccount.ShoosedMachine.ApplyGrowth(ship);
        ship.AddComponent<ShowFriendUI>();


        if (CurrentShip != null) PhotonNetwork.Destroy(CurrentShip.View);
        CurrentShip = ship.GetComponent<Health>();

        MouseOrbit.Instance.target = ship.transform;
    }

    void SpawnAI()
    {
        var ship = PhotonNetwork.Instantiate(UsersDATA.currentAccount.AIMachine.PrefabName, SpawnPosition.Spawn(PhotonNetwork.player.GetTeam()).GetSpawnPosition(), SpawnPosition.Spawn(PhotonNetwork.player.GetTeam()).Tr.rotation, 0);
        UsersDATA.currentAccount.AIMachine.ApplyGrowth(ship);
        switch (ship.GetComponent<Control>().LocomotionType)
        {
            case MonoBehaviourPlus.LocomotorType.Space:
                ship.AddComponent<AIBot>();
                break;
            case MonoBehaviourPlus.LocomotorType.Forward:
                ship.AddComponent<FLAIBot>();
                break;
            case MonoBehaviourPlus.LocomotorType.SemiForward:
                ship.AddComponent<FLAIBot>();
                break;
        }

        if (AIShip != null) PhotonNetwork.Destroy(AIShip.GetComponent<PhotonView>());
        AIShip = ship.GetComponent<AIBot>();
    }
}
