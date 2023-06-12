using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Waiters;
using UnityEngine.SceneManagement;
using Modernizations;
using System.Linq;

public class Garage : MonoBehaviourPlus, IAccountEvents
{
    public List<string> MyShips;
    public List<ShipSet> Ships;

    [System.Serializable]
    public class ShipSet
    {
        public string PrefabName;
        public string StorageName;
        public string FullName;
        public int Cost;

        public void ApplyGrowth(GameObject Instance)
        {
            Storage.ApplyGrowth(PrefabName, Instance);
        }
    }

    public ShipSet GetShip(string PrefabName)
    {
        foreach (var ship in Ships)
        {
            if (ship.PrefabName == PrefabName)
                return ship;
        }

        return null;
    }


    public static Garage Instance;
    public ShipSetUI CurrentSet;
    public Transform UserPlace;
    public Transform AIPlace;
    public Transform UserInstance;
    public Transform AIInstance;
    public Transform[] MachinePrefabs;
    public Transform[] MachineInstances;

    UILink _Garage;
    UILink ModernizationsButton;
    UILink ToSelfButton;
    UILink ToBotButton;
    UILink ExploreButton;
    UILink SetWindow;
    UILink ExploreButtonText;
    UILink ShipsScroll;
    UILink UserShip;
    UILink AIShip;

    public static bool selfShipDirty = false;
    public static bool aiShipDirty = false;

    public class ShipSetUI
    {
        [System.NonSerialized] public UILink SetName;
        [System.NonSerialized] public UILink MachineName;
        [System.NonSerialized] public UILink Parameters;
        [System.NonSerialized] public UILink Values;

        public void Set(UILink root, ShipSet Ship, string setName, Health machine)
        {
            SetName = root.GetChildByName("SetName");
            var mr = root.GetChildByName("MachineSet");
            MachineName = mr.GetChildByName("MachineName");
            Parameters = mr.GetChildByName("Parameters");
            Values = mr.GetChildByName("Values");

            SetName.Text.text = setName;
            MachineName.Text.text = Ship.FullName;
            string Params = string.Empty;
            string Vals = string.Empty;
            GetParameters(machine, out Params, out Vals);
            Parameters.Text.text = Params;
            Values.Text.text = Vals;
        }

        public static void GetParameters(Health machine, out string Params, out string Vals)
        {
            Params = string.Empty;
            Vals = string.Empty;
            List<string> Parameters = new List<string>();
            List<string> Values = new List<string>();
            machine.GetDescription(ref Parameters, ref Values);
            foreach (var hit in Parameters)
            {
                if (hit != string.Empty)
                    Params += hit + ":\n";
                else
                    Params += hit + "\n";
            }

            foreach (var hit in Values)
            {
                Vals += hit + "\n";
            }
        }
    }


    Storage storage;

    void Awake()
    {
        Instance = this;
        storage = Resources.Load<Storage>("Storage");
        CurrentSet = new ShipSetUI();
        MachinePrefabs = new Transform[Ships.Count];
        MachineInstances = new Transform[Ships.Count];

        this.Wait(0.1f, LateStart);
    }

    void GetUI()
    {
        SetWindow = _Garage.GetChildByName("SelectedSet", false);
        ToSelfButton = _Garage.GetChildByName("ToSelf", false);
        ToBotButton = _Garage.GetChildByName("ToBot", false);
        ExploreButton = _Garage.GetChildByName("Explore", false);
        ModernizationsButton = _Garage.GetChildByName("Modernizations", false);
        ShipsScroll = _Garage.GetChildByName("Ring");
        UserShip = _Garage.GetChildByName("PlayerShipValue");
        AIShip = _Garage.GetChildByName("AIShipValue");

        if (isGame)
        {
            UILink.ToggleGarage.Button.onClick.AddListener(ToggleActive);
            InputEvents.Instance.OnButtonDown("Cancel").AddListener(() => ToggleActive(false));
        }

        ToSelfButton.Button.onClick.AddListener(SetSelfMachine);
        ToBotButton.Button.onClick.AddListener(SetBotfMachine);
        ExploreButton.Button.onClick.AddListener(ExploreShip);
        ModernizationsButton.Button.onClick.AddListener(OpenModernizations);

        ExploreButtonText = ExploreButton.GetChildByName("Text");
    }

    public void ToggleActive()
    {
        _Garage.gameObject.SetActive(!_Garage.gameObject.activeSelf);
        MouseOrbit.ForceUnlockCursor = _Garage.gameObject.activeSelf;
    }

    public void ToggleActive(bool value)
    {
        _Garage.gameObject.SetActive(value);
        MouseOrbit.ForceUnlockCursor = value;
    }

    public void LateStart()
    {
        for (int i = 0; i < MachinePrefabs.Length; i++)
        {
            MachinePrefabs[i] = Resources.Load<Transform>(Ships[i].PrefabName);
            InstanceShip(ref MachineInstances[i], Ships[i].PrefabName);
        }


        _Garage = isGame ? UILink.BattleGarage : UILink.Garage;
        GetUI();

        if (PhotonNetwork.connected && UsersDATA.currentAccount != null) Setup();
    }

    public void OpenModernizations()
    {
        int shipN = ShipsScroll.ScrollRing.Value;
        //SelectedShip = Ships[shipN];
        //StorageEditor.Back.AddListener(CloseModernizations);
        var bg = UILink.MainCanvas.GetChildByName("Background");
        if (bg) bg.gameObject.SetActive(true);

        var items = (from x in Storage.GetItem(Ships[shipN].PrefabName).Modernizations
            where x.IsDefault == false
            select x.id).ToArray();

        StorageEditor.SelectedItem = Storage.GetItem(Ships[shipN].PrefabName);
        StorageEditor.OnCloseWindow += CloseModernizations;
    }

    public void CloseModernizations()
    {
        StorageEditor.OnCloseWindow -= CloseModernizations;
        var bg = UILink.MainCanvas.GetChildByName("Background");
        if (bg) bg.gameObject.SetActive(false);
    }

    void ExploreShip()
    {
        string item = Ships[ShipsScroll.ScrollRing.Value].PrefabName;
        MyShips.Add(item);
        SelectMachine(GetShipID(item), "Машина");
    }

    public void SetSelfMachine()
    {
        if (isGame) selfShipDirty = true;

        int shipN = ShipsScroll.ScrollRing.Value;
        if (MyShips.Contains(Ships[shipN].PrefabName))
        {
            UsersDATA.currentAccount.ShoosedMachine = Ships[shipN];
            UserShip.Text.text = Ships[shipN].FullName;
            SetShipInstance(ref UserInstance, shipN, UserPlace);
        }
    }

    public void SetBotfMachine()
    {
        if (isGame) aiShipDirty = true;

        int shipN = ShipsScroll.ScrollRing.Value;
        if (MyShips.Contains(Ships[shipN].PrefabName))
        {
            UsersDATA.currentAccount.AIMachine = Ships[shipN];
            AIShip.Text.text = Ships[shipN].FullName;
            SetShipInstance(ref AIInstance, shipN, AIPlace);
        }
    }

    public int GetShipID(string Name)
    {
        return Ships.FindIndex(x => x.PrefabName == Name);
    }

    public void SetShipInstance(ref Transform ship, int N, Transform place)
    {
        if (!isGame)
        {
            if (ship) Destroy(ship.gameObject);
            ship = Instantiate(MachineInstances[N], place);
            ship.localPosition = Vector3.zero;
            ship.localRotation = Quaternion.identity;
            ship.gameObject.SetActive(true);
        }
    }

    public void InstanceShip(ref Transform ship, string Name)
    {
        int i = GetShipID(Name);
        ship = Instantiate(MachinePrefabs[i]);
        Destroy(ship.GetComponent<Control>());
        foreach (var hit in ship.GetComponentsInChildren<MonoBehaviour>())
            hit.enabled = false;
        foreach (var hit in ship.GetComponentsInChildren<AudioSource>())
            hit.enabled = false;
        ship.GetComponent<Rigidbody>().isKinematic = true;
        ship.gameObject.SetActive(false);
    }

    public void SelectMachine(int i, string setName)
    {
        ShipsScroll.ScrollRing.Value = i;
        Ships[i].ApplyGrowth(MachineInstances[i].gameObject);
        CurrentSet.Set(SetWindow, Ships[i], setName, MachineInstances[i].GetComponent<Health>());
        if (MyShips.Contains(Ships[i].PrefabName))
        {
            ExploreButton.gameObject.SetActive(false);
            ToBotButton.gameObject.SetActive(true);
            ToSelfButton.gameObject.SetActive(true);
        }
        else
        {
            ExploreButton.gameObject.SetActive(true);
            ToBotButton.gameObject.SetActive(false);
            ToSelfButton.gameObject.SetActive(false);
            ExploreButtonText.Text.text = "Исследовать: " + Ships[i].Cost;
        }
    }

    void SetApply()
    {
        Ships[ShipsScroll.ScrollRing.Value].ApplyGrowth(MachineInstances[ShipsScroll.ScrollRing.Value].gameObject);
        CurrentSet.Set(SetWindow, Ships[ShipsScroll.ScrollRing.Value], "Машина",
            MachineInstances[ShipsScroll.ScrollRing.Value].GetComponent<Health>());
        if (MyShips.Contains(Ships[ShipsScroll.ScrollRing.Value].PrefabName))
        {
            ExploreButton.gameObject.SetActive(false);
            ToBotButton.gameObject.SetActive(true);
            ToSelfButton.gameObject.SetActive(true);
        }
        else
        {
            ExploreButton.gameObject.SetActive(true);
            ToBotButton.gameObject.SetActive(false);
            ToSelfButton.gameObject.SetActive(false);
            ExploreButtonText.Text.text = "Исследовать: " + Ships[ShipsScroll.ScrollRing.Value].Cost;
        }
    }

    public void OnEnterAccount(Account account)
    {
        this.Wait(0.1f, Setup);
    }

    bool isGame => SceneManager.GetActiveScene().buildIndex != 0;

    public void Setup()
    {
        _Garage.gameObject.SetActive(true);

        for (int i = 0; i < Ships.Count; i++)
        {
            var hit = ShipsScroll.GetChildByName(string.Format("Item ({0})", i));
            hit.Text.text = Ships[i].StorageName;
        }

        ShipsScroll.ScrollRing.OnValueChainge.AddListener(SetApply);
        if (!isGame) SelectMachine(0, "Машина");
        SetShipInstance(ref UserInstance, 0, UserPlace);
        SetShipInstance(ref AIInstance, 0, AIPlace);
        UserShip.Text.text = UsersDATA.currentAccount.ShoosedMachine.FullName;
        AIShip.Text.text = UsersDATA.currentAccount.AIMachine.FullName;
        if (isGame) _Garage.gameObject.SetActive(false);
    }
}