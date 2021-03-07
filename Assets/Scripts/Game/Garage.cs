﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Garage : MonoBehaviour, IAccountEvents
{
    public static Garage Instance;
    public UILink GarageWindow;
    public UILink ShipsScroll;
    public UILink SetVariantsMenu;
    public UILink UserShip;
    public UILink AIShip; 
    public ShipSetUI CurrentSet;
    public Transform UserPlace;
    public Transform AIPlace;
    public Transform UserInstance;
    public Transform AIInstance;
    public Transform[] MachinePrefabs;
    public Transform[] MachineInstances;

    UILink ModernizationsButton;
    UILink ToSelfButton;
    UILink ToBotButton;
    UILink ExploreButton;
    UILink SetWindow;
    UILink ExploreButtonText;

    StorageEditor Modernizations;

    public class ShipSetUI
    {
        [System.NonSerialized]
        public UILink SetName;
        [System.NonSerialized]
        public UILink MachineName;
        [System.NonSerialized]
        public UILink Parameters;
        [System.NonSerialized]
        public UILink Values;
        public void Set(UILink root, Storage.ShipSet Ship, string setName, Health machine)
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
                if(hit != string.Empty)
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
        Modernizations = GetComponent<StorageEditor>();
        CurrentSet = new ShipSetUI();
        MachinePrefabs = new Transform[storage.Ships.Count];
        MachineInstances = new Transform[storage.Ships.Count];
        for (int i = 0; i < MachinePrefabs.Length; i++)
        {
            MachinePrefabs[i] = Resources.Load<Transform>(storage.Ships[i].PrefabName);
            InstanceShip(ref MachineInstances[i], storage.Ships[i].PrefabName);
        }
        Invoke("LateStart", 0.1f);
    }

    void LateStart ()
    {
        SetWindow = GarageWindow.GetChildByName("SelectedSet", false);
        ToSelfButton = SetVariantsMenu.GetChildByName("ToSelf");
        ToBotButton = SetVariantsMenu.GetChildByName("ToBot");
        ExploreButton = SetVariantsMenu.GetChildByName("Explore");
        ModernizationsButton = SetVariantsMenu.GetChildByName("Modernizations");

        ToSelfButton.Button.onClick.AddListener(SetSelfMachine);
        ToBotButton.Button.onClick.AddListener(SetBotfMachine);
        ExploreButton.Button.onClick.AddListener(ExploreShip);
        ModernizationsButton.Button.onClick.AddListener(OpenModernizations);

        ExploreButtonText = ExploreButton.GetChildByName("Text");
        if (PhotonNetwork.connected && UsersDATA.currentAccount != null)
            Setup();
    }

    void OpenModernizations()
    {
        int shipN = ShipsScroll.ScrollRing.Value;
        StorageEditor.SelectedShip = storage.Ships[shipN];
        StorageEditor.Back.AddListener(CloseModernizations);
        UILink.MainCanvas.GetChildByName("Background").gameObject.SetActive(true);
        StartCoroutine(UsersDATA.Instance.GetItemsCosts(storage.Ships[shipN]));
    }

    void CloseModernizations()
    {
        UILink.MainCanvas.GetChildByName("Background").gameObject.SetActive(false);
    }

    void ExploreShip()
    {
        StartCoroutine(UsersDATA.Instance.Explore(storage.Ships[ShipsScroll.ScrollRing.Value].PrefabName, "ship"));
    }

    public void SetSelfMachine()
    {
        int shipN = ShipsScroll.ScrollRing.Value;
        if (storage.MyShips.Contains(storage.Ships[shipN].PrefabName))
        {
            UsersDATA.currentAccount.ShoosedMachine = storage.Ships[shipN];
            UserShip.Text.text = storage.Ships[shipN].FullName;
            SetShipInstance(ref UserInstance, shipN, UserPlace);
        }
    }

    public void SetBotfMachine()
    {
        int shipN = ShipsScroll.ScrollRing.Value;
        if (storage.MyShips.Contains(storage.Ships[shipN].PrefabName))
        {
            UsersDATA.currentAccount.AIMachine = storage.Ships[shipN];
            AIShip.Text.text = storage.Ships[shipN].FullName;
            SetShipInstance(ref AIInstance, shipN, AIPlace);
        }
    }

    public int GetShipID(string Name)
    {
        return storage.Ships.FindIndex(x => x.PrefabName == Name);
    }

    public void SetShipInstance(ref Transform ship, int N, Transform place)
    {
        if (ship)
            Destroy(ship.gameObject);
        ship = Instantiate(MachineInstances[N], place);
        ship.localPosition = Vector3.zero;
        ship.localRotation = Quaternion.identity;
        ship.gameObject.SetActive(true);
    }

    public void InstanceShip(ref Transform ship, string Name)
    {
        int i = GetShipID(Name);
        ship = Instantiate(MachinePrefabs[i]);
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
        storage.Ships[i].ApplyGrowth(MachineInstances[i].gameObject);
        CurrentSet.Set(SetWindow, storage.Ships[i], setName, MachineInstances[i].GetComponent<Health>());
        if(storage.MyShips.Contains(storage.Ships[i].PrefabName))
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
            ExploreButtonText.Text.text = "Исследовать: " + storage.Ships[i].Cost;
        }
    }

    void SetApply()
    {
        storage.Ships[ShipsScroll.ScrollRing.Value].ApplyGrowth(MachineInstances[ShipsScroll.ScrollRing.Value].gameObject);
        CurrentSet.Set(SetWindow, storage.Ships[ShipsScroll.ScrollRing.Value], "Машина", MachineInstances[ShipsScroll.ScrollRing.Value].GetComponent<Health>());
        if (storage.MyShips.Contains(storage.Ships[ShipsScroll.ScrollRing.Value].PrefabName))
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
            ExploreButtonText.Text.text = "Исследовать: " + storage.Ships[ShipsScroll.ScrollRing.Value].Cost;
        }
    }

    public void OnEnterAccount(MonoBehaviourPlus.Account account)
    {
        Invoke("Setup", 0.1f);
    }

    public void Setup()
    {
        GarageWindow.gameObject.SetActive(true);
        for (int i = 0; i < storage.Ships.Count; i++)
        {
            var hit = ShipsScroll.GetChildByName(string.Format("Item ({0})", i));
            hit.Text.text = storage.Ships[i].StorageName;
        }
        ShipsScroll.ScrollRing.OnValueChainge.AddListener(SetApply);
        SelectMachine(0, "Машина");
        SetShipInstance(ref UserInstance, 0, UserPlace);
        SetShipInstance(ref AIInstance, 0, AIPlace);
        UserShip.Text.text = UsersDATA.currentAccount.ShoosedMachine.FullName;
        AIShip.Text.text = UsersDATA.currentAccount.AIMachine.FullName;
    }
}