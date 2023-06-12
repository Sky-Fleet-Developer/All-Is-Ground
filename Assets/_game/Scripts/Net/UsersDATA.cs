using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using Waiters;

public class UsersDATA : MonoBehaviour
{
    public static UsersDATA Instance;
    public static Account currentAccount;
    public static Storage storage;
    public UILink LogInWindow;
    public UILink RegisterWindow;
    public UILink OnlineWindow;
    public UILink UpperBar;

    public Text State;

    public int LevelToCreate;

    UILink SceneLoading;

    public const string ServerUri = "https://newships-storage.000webhostapp.com/";

    private static bool isPolygon;

    void Start()
    {
        Invoke("LateStart", Time.deltaTime);
        DontDestroyOnLoad(gameObject);
        Instance = this;
        var exit = UILink.MainCanvas.GetChildByName("Quit");
        UpperBar.GetChildByName("ExitGame").Button.onClick.AddListener(delegate { exit.gameObject.SetActive(true); });
        SceneLoading = UILink.MainCanvas.GetChildByName("LevelLoading");
        UILink.StartPolygon.Button.onClick.AddListener(StartPolygon);
        if (currentAccount == null || !PhotonNetwork.connected)
        {
            storage = Resources.Load<Storage>("Storage");
            GameValues.Init();
            PhotonNetwork.autoJoinLobby = true;
            PhotonNetwork.ConnectUsingSettings(PhotonNetwork.gameVersion);
            PhotonNetwork.automaticallySyncScene = true;
            PhotonNetwork.autoCleanUpPlayerObjects = false;
            Debug.Log("Start");
        }
        else
        {
            if (!PhotonNetwork.insideLobby)
                PhotonNetwork.JoinLobby();
        }
    }

    void LateStart()
    {
        State.text = "<color=green>Ожидание...</color>";
    }

    [ContextMenu("WriteItemsInDB")]
    public void WriteItemsInDB()
    {
        string send = string.Empty;
        foreach (var ship in Garage.Instance.Ships)
        {
            var item = Storage.GetItem(ship.PrefabName);
            send += ship.PrefabName + ":" + ship.Cost + ",";
            if (item == null) continue;
            foreach (var property in item.Modernizations)
            {
                if (property.GlobalResourceDependences.Count != 0) send += property.id + ":" + property.GlobalResourceDependences[0].Cost + ",";
            }
        }
    }

    #region Public
    public static void AddExperience(string PlayerName, int expToAdd)
    {
        Debug.Log("AddExperience method has no implementation");
    }
    #endregion

    #region Recived

    void OnJoinedLobby()
    {
        if (currentAccount == null || !PhotonNetwork.connected)
        {
            State.text = "<color=green>Соединён с сервером.</color>";
            LogInWindow.gameObject.SetActive(true);
            LogInWindow.GetChildByName("NicknameField").InputField.text = PlayerPrefs.GetString("LastLogin");
            LogInWindow.GetChildByName("PasswordField").InputField.text = PlayerPrefs.GetString("LastPassword");
        }
        else
        {
            SetAsSignedIn();
        }
        StartCoroutine(UpdateRoomList());
    }

    void OnJoinedRoom()
    {
        Debug.Log("Joined Room " + PhotonNetwork.room.Name);
        PhotonNetwork.automaticallySyncScene = true;
        if (PhotonNetwork.isMasterClient)
            PhotonNetwork.LoadLevel(GameValues.Levels[LevelToCreate].BuildID);
        //StartCoroutine(LoadSceneAsync());
    }

    private void OnLevelWasLoaded(int level)
    {
        if(level != 0)
        {
            this.Wait(1, Garage.Instance.LateStart);
        }
    }


    #endregion

    #region Buttons

    public void Authorization()
    {
        string LogIn;
        LogIn = LogInWindow.GetChildByName("NicknameField").InputField.text;
        if (string.IsNullOrEmpty(LogIn))
            return;
        SignIn(LogIn, 5000, int.MaxValue);
    }

    public void ExitGame()
    {
        Application.Quit();
    }

    public void SelectCreationLevel(int n)
    {
        LevelToCreate = n;
        UILink.MainCanvas.GetChildByName("LevelSelector").gameObject.SetActive(false);
        OnlineWindow.GetChildByName("Image").Image.sprite = GameValues.Levels[LevelToCreate].Image;
    }

    public void CreateRoom()
    {
        string N = OnlineWindow.GetChildByName("BattleName").InputField.text;
        if (string.IsNullOrEmpty(N))
            N += string.Format("{0:0000}", UnityEngine.Random.Range(0, 1000));
        PhotonNetwork.CreateRoom(GameValues.Levels[LevelToCreate].Name + ": " + N);
    }

    private void StartPolygon() {
        isPolygon = true;
        SignIn("Empty name", 0, int.MaxValue);
    }

    #endregion

    #region Internal

    public IEnumerator LoadSceneAsync(int scene)
    {
        float timer = Time.time;

        UILink.MainCanvas.GetChildByName("Background").gameObject.SetActive(true);

        SceneLoading.gameObject.SetActive(true);
        var bar = SceneLoading.GetChildByName("LoadBar").Scrollbar;
        var tooltip = SceneLoading.GetChildByName("Tooltip").Text;
        var image = SceneLoading.GetChildByName("Image").Image;
        tooltip.text = GameValues.GetRandomTooltip();
        image.sprite = GameValues.Levels.Where(x => x.BuildID == scene).SingleOrDefault().Image;

        while (Time.time < timer + 0.5f)
        {
            timer += Time.deltaTime;
            yield return new WaitForSeconds(Time.deltaTime);
        }
        
        AsyncOperation asyncLoad = null;

        asyncLoad = SceneManager.LoadSceneAsync(scene);

        asyncLoad.allowSceneActivation = false;

        while (!asyncLoad.isDone || Time.time < timer + 4)
        {
            if (Time.time >= timer + 4)
                asyncLoad.allowSceneActivation = true;

            bar.size = asyncLoad.progress;
            yield return new WaitForSeconds(Time.deltaTime);
        }
    }

    void SignIn(string Name, int Experience, int freeExp)
    {
        currentAccount = new Account(Name, Experience, freeExp, storage);
        PhotonNetwork.player.NickName = Name;
        RegisterWindow.gameObject.SetActive(false);
        LogInWindow.gameObject.SetActive(false);
        SetAsSignedIn();
    }

    void SetAsSignedIn()
    {
        OnlineWindow.gameObject.SetActive(true);
        var uset = OnlineWindow.GetChildByName("UserSetup");
        uset.GetChildByName("NickName").Text.text = currentAccount.Name;
        uset.GetChildByName("UserExperience").Text.text = currentAccount.FreeExperience.ToString();
        //var InpF = OnlineWindow.GetChildByName("BattleName").InputField;
        //OnlineWindow.GetChildByName("Create").Button.onClick.AddListener(delegate { CreateRoom(InpF.text); });
    }

    void EnterRoom(string Name)
    {
        PhotonNetwork.JoinRoom(Name);
    }

    IEnumerator UpdateRoomList()
    {
        while (Application.isPlaying)
        {
            if (OnlineWindow && OnlineWindow.gameObject.GetActive() && PhotonNetwork.connected && PhotonNetwork.insideLobby)
            {
                GetRoomList();
            }
            yield return new WaitForSeconds(1);
        }
    }

    void GetRoomList()
    {
        var rooms = PhotonNetwork.GetRoomList();
        foreach (var hit in rooms)
        {
            var obj = OnlineWindow.GetChildByName("RoomsList", false).Pooling.Use().GetComponent<UILink>();
            obj.GetChildByName("Name").Text.text = hit.Name;
            obj.GetChildByName("Entry").Button.onClick.RemoveAllListeners();
            string n = hit.Name;
            obj.GetChildByName("Entry").Button.onClick.AddListener(delegate { EnterRoom(n); });
        }
    }
    
    #endregion
}
