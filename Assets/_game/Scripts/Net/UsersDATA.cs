using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class UsersDATA : MonoBehaviourPlus
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

    void Start()
    {
        Invoke("LateStart", Time.deltaTime);
        DontDestroyOnLoad(gameObject);
        Instance = this;
        var exit = UILink.MainCanvas.GetChildByName("Quit");
        UpperBar.GetChildByName("ExitGame").Button.onClick.AddListener(delegate { exit.gameObject.SetActive(true); });
        SceneLoading = UILink.MainCanvas.GetChildByName("LevelLoading");
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
            StartCoroutine(GetExp());
        }
    }

    void LateStart()
    {
        State.text = "<color=green>Ожидание...</color>";
    }

    #region Public
    public static void AddExperience(string PlayerName, int expToAdd)
    {
        Instance.StartCoroutine(Instance.AddExp(PlayerName, expToAdd));
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


    #endregion

    #region Buttons

    public void AddNewUser()
    {
        string LogIn, Pass, ConfPass;
        LogIn = RegisterWindow.GetChildByName("NicknameField").InputField.text;
        Pass = RegisterWindow.GetChildByName("PasswordField").InputField.text;
        ConfPass = RegisterWindow.GetChildByName("ConfirmPasswordField").InputField.text;
        if (string.IsNullOrEmpty(LogIn) || string.IsNullOrEmpty(Pass))
            return;
        if (Pass != ConfPass)
        {
            State.text = "<color=red>Пароли не совпадают.</color>";
        }
        StartCoroutine(Registration(LogIn, Pass));
    }

    public void Authorization()
    {
        string LogIn, Pass;
        LogIn = LogInWindow.GetChildByName("NicknameField").InputField.text;
        Pass = LogInWindow.GetChildByName("PasswordField").InputField.text;
        if (string.IsNullOrEmpty(LogIn) || string.IsNullOrEmpty(Pass))
            return;
        StartCoroutine(Auth(LogIn, Pass));
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

    void BuildGarage(string String)
    {
        var setup = String.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

        storage.MyShips = new List<string>();

        foreach (var set in setup)
        {
            if (set.Length > 2)
            {
                var vals = set.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                Storage.ShipSet ship = storage.Ships.Where(x => x.PrefabName == vals[0]).SingleOrDefault();
                storage.MyShips.Add(vals[0]);
                if (vals[1] == "standart")
                {
                    Storage.SetStandart(ship);
                }
                else
                {
                    var moderns = vals[1].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    ship.BlocksInStock.AddRange(moderns);
                }
            }
        }
    }
    #endregion

    #region HostRequests

    IEnumerator Auth(string login, string password) // Авторизация
    {
        string uri = string.Format("{0}?method=Auth&name={1}&password={2}", ServerUri, login, password);
        State.text = "Вход...";

        using (UnityWebRequest www = UnityWebRequest.Get(uri))
        {

            yield return www.SendWebRequest();

            if (!string.IsNullOrEmpty(www.error))
            {
                Debug.Log("NetworkApi.Login error: " + www.error);
                yield break;
            }

            Debug.Log(SelectString(www.downloadHandler.text));

            var split = SelectString(www.downloadHandler.text).Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries);

            var dic = split.Select(n => n.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries)).ToDictionary(k => k[0].ToString(), v => v[1].ToString());


            switch (dic["auth"])
            {
                case "error":
                    Debug.Log("Error");
                    State.text = "<color=red>" + dic["error"] + "</color>";
                    break;
                case "correct":
                    PlayerPrefs.SetString("LastLogin", login);
                    PlayerPrefs.SetString("LastPassword", password);

                    BuildGarage(dic["set"]);

                    List<string> ask = new List<string>();

                    for(int i = 1; i < storage.Ships.Count; i++)
                        ask.Add(storage.Ships[i].PrefabName);

                    yield return StartCoroutine(GetShipsCosts(ask));

                    SignIn(dic["name"], int.Parse(dic["experience"]), int.Parse(dic["free_exp"]));
                    State.text = "";
                    break;
            }
        }
    }

    IEnumerator Registration(string login, string password) // Регистрация
    {
        string uri = string.Format("{0}?method=Registration&id={1}&name={2}&password={3}", ServerUri, string.Format("{0:00000000}", UnityEngine.Random.Range(0, 99999999)), login, password);
        using (UnityWebRequest www = UnityWebRequest.Get(uri))
        {

            yield return www.SendWebRequest();

            if (!string.IsNullOrEmpty(www.error))
            {
                Debug.Log("NetworkApi.Login error: " + www.error);
                yield break;
            }

            string answer = SelectString(www.downloadHandler.text);

            Debug.Log(answer);

            var pars = answer.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries);
            var dic = pars.Select(n => n.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries)).ToDictionary(k => k[0].ToString(), v => v[1].ToString());

            switch (dic["auth"])
            {
                case "error":
                    Debug.Log("Error");
                    State.text = "<color=red>" + dic["error"] + "</color>";
                    break;
                case "correct":
                    PlayerPrefs.SetString("LastLogin", login);
                    PlayerPrefs.SetString("LastPassword", password);

                    List<string> ask = new List<string>();
                    for (int i = 1; i < storage.Ships.Count; i++)
                        ask.Add(storage.Ships[i].PrefabName);
                    yield return StartCoroutine(GetShipsCosts(ask));

                    SignIn(dic["name"], int.Parse(dic["experience"]), 0);
                    State.text = "";
                    break;
            }

            //var DetechModuls = SelectString(www.downloadHandler.text).Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);

        }
    }

    IEnumerator AddExp(string login, int exp)
    {
        string uri = string.Format("{0}?method=AddExperience&name={1}&exp={2}", ServerUri, login, exp);
        using (UnityWebRequest www = UnityWebRequest.Get(uri))
        {
            yield return www.SendWebRequest();
            string answer = SelectString(www.downloadHandler.text);
            Debug.Log(answer);
        }
    }

    IEnumerator GetExp()
    {
        string uri = string.Format("{0}?method=GetExperience&name={1}", ServerUri, currentAccount.Name);
        using (UnityWebRequest www = UnityWebRequest.Get(uri))
        {
            yield return www.SendWebRequest();
            string answer = SelectString(www.downloadHandler.text);
            var dic = answer.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries).ToDictionary(k => k[0].ToString(), v => v[1].ToString());
            int exp = 0;
            if (int.TryParse(dic["experience"], out exp))
                currentAccount.Experience = exp;
            if (int.TryParse(dic["free_experience"], out exp))
                currentAccount.FreeExperience = exp;
            SetAsSignedIn();
        }
    }

    public IEnumerator GetItemsCosts(Storage.ShipSet ship)
    {
        string uri = string.Format("{0}?method=GetItemsCosts&ask={1}", ServerUri, ship.PrefabName);
        using (UnityWebRequest www = UnityWebRequest.Get(uri))
        {
            yield return www.SendWebRequest();
            string answer = SelectString(www.downloadHandler.text);

            Debug.Log(answer);

            var split = answer.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            var dic = split.Select(x => x.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries)).ToDictionary(k => k[0].ToString(), v => v[1].ToString());

            foreach (var hit in dic)
            {
                string key = hit.Key.Replace(ship.PrefabName + ".", "");
                ship.GrowthStock.Where(x => x.Name == key).SingleOrDefault().Cost = int.Parse(hit.Value);
            }
        }
    }

    public IEnumerator SetItemsCosts(string send)
    {
        string uri = string.Format("{0}?method=SetItemsCosts&items={1}", ServerUri, send);
        using (UnityWebRequest www = UnityWebRequest.Get(uri))
        {
            yield return www.SendWebRequest();
            string answer = SelectString(www.downloadHandler.text);

            Debug.Log(answer);


        }
    }

    IEnumerator GetShipsCosts(List<string> items)
    {
        string ask = string.Empty;
        foreach(var hit in items)
        {
            ask += hit + ",";
        }
        string uri = string.Format("{0}?method=GetShipsCosts&ask={1}", ServerUri, ask);
        using (UnityWebRequest www = UnityWebRequest.Get(uri))
        {
            yield return www.SendWebRequest();
            string answer = SelectString(www.downloadHandler.text);

            var split = answer.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

            var dic = split.Select(x => x.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries)).ToDictionary(k => k[0].ToString(), v => v[1].ToString());
            foreach (var hit in dic)
            {
                storage.Ships.Where(x => x.PrefabName == hit.Key).SingleOrDefault().Cost = int.Parse(hit.Value);
            }
        }
    }

    bool exploreInProgress = false;
    public IEnumerator Explore(string item, string type)
    {
        int Try = 0;
        while (exploreInProgress && Try++ < 10)
            yield return new WaitForSeconds(1);

        string uri = string.Format("{0}?method=Explore&name={1}&item={2}&type={3}", ServerUri, currentAccount.Name, item, type);
       // Debug.Log(string.Format("method=Explore&name={0}&item={1}&type={2}", currentAccount.Name, item, type));
        using (UnityWebRequest www = UnityWebRequest.Get(uri))
        {
            yield return www.SendWebRequest();
            string answer = SelectString(www.downloadHandler.text);

            Debug.Log(answer);

            var split = answer.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries);

            var dic = split.Select(x => x.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries)).ToDictionary(k => k[0].ToString(), v => v[1].ToString());

            switch (dic["result"])
            {
                case "error":
                    Debug.Log("Error");
                    State.text = "<color=red>" + dic["error"] + "</color>";
                    break;
                case "correct":
                    if (type == "ship")
                    {
                        storage.MyShips.Add(item);
                        Garage.Instance.SelectMachine(Garage.Instance.GetShipID(item), "Машина");
                    }
                    if(type == "item")
                    {
                        BuildGarage(dic["set"]);
                    }
                    currentAccount.FreeExperience = int.Parse(dic["free_experience"]);
                    SetAsSignedIn();
                    break;
            }
        }
    }


    public static string SelectString(string text)
    {
        int StartIndex = text.IndexOf("<start>") + 7;
        int EndIndex = text.IndexOf("</start>");
        string Text = string.Empty;
        for (int i = StartIndex; i < EndIndex; i++)
            Text += text[i].ToString();
        return Text;
    }

    #endregion
}
