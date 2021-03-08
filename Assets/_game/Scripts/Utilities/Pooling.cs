using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("Poolling")]
public class Pooling : MonoBehaviour
{
    public static List<Pooling> Instances = new List<Pooling>();
    public static Dictionary<string, Pooling>  InstancesDic = new Dictionary<string, Pooling>();

    public GameObject SourcePrefab;
    public int MassiveSize;
    public float DisableDelay;
    public string Messege;
    public bool SendOnStart;
    [HideInInspector]
    public Charge charge;
    [Space(10)]
    public bool InitOnStart = true;
    public int FirstIndex;
    public bool DeteachActive = true;
    bool inited = false;

    public List<PoolObject> DisabledObject;
    public List<PoolObject> EnabledObject;
    [System.NonSerialized]
    public Transform Tr;
    void Awake()
    {
        Tr = transform;
        if (InitOnStart)
            Initialize();

        if (!GetComponentInParent<PhotonView>())
        {
            Instances.Add(this);
            if (!InstancesDic.ContainsKey(name))
                InstancesDic.Add(name, this);
            else
                InstancesDic[name] = this;
        }
    }

    public void Initialize()
    {
        if (inited)
            return;
        inited = true;
        Tr = transform;
        charge = SourcePrefab.GetComponent<Charge>();
        DisabledObject = new List<PoolObject>();
        EnabledObject = new List<PoolObject>();
        PoolObject obj;
        for (int i = 0; i < MassiveSize; i++)
        {
            obj = Instantiate(SourcePrefab, Tr).AddComponent<PoolObject>();
            obj.name = SourcePrefab.name + "_" + (i + 1);
            if (SendOnStart)
            obj.SendMessage("OnStart");
            if (obj.GetComponent<UILink>())
                obj.GetComponent<UILink>().Init();
            obj.gameObject.SetActive(false);
            obj.ID = FirstIndex + i;
            DisabledObject.Add(obj);
            obj = null;
        }
    }

    int lastID;
    public GameObject Use(Vector3 Position, Quaternion Rotation, Transform parent, out int ID)
    {

        Transform Obj = Use().transform;
        Obj.position = Position;
        Obj.rotation = Rotation;
        Obj.parent = parent;
        ID = lastID;
        return Obj.gameObject;
    }
    public GameObject Use(out int ID)
    {

        Transform Obj = Use().transform;
        ID = lastID;
        return Obj.gameObject;
    }

    public GameObject Use(Vector3 Position, Quaternion Rotation, Transform parent, int ID = -1)
    {
        Transform Obj = Use(ID).transform;
        Obj.position = Position;
        Obj.rotation = Rotation;
        Obj.parent = parent;
        
        return Obj.gameObject;
    }

    public PoolObject GetEnabledChildrenWithID(int ID)
    {
        foreach (PoolObject Hit in EnabledObject)
            if (Hit.ID == ID)
            {
                return Hit;
            }
                return null;
    }

    public PoolObject GetChildrenWithID(int ID)
    {
        foreach (PoolObject Hit in DisabledObject)
            if (Hit.ID == ID)
            {
                return Hit;
            }
        foreach (PoolObject Hit in EnabledObject)
            if (Hit.ID == ID)
            {
                return Hit;
            }
        return null;
    }

    public PoolObject Use(int ID = -1)
    {
        PoolObject Obj = null;
        switch (DisabledObject.Count > 0)
        {
            case true:

                if (ID == -1)
                    Obj = DisabledObject[0];
                else
                    Obj = GetChildrenWithID(ID);
                DisabledObject.Remove(Obj);
                Obj.gameObject.SetActive(true);
                if (DisableDelay > 0f)
                Obj.StartCoroutine(Obj.Deactive(DisableDelay, this));
                EnabledObject.Add(Obj);
                if (!string.IsNullOrEmpty(Messege))
                    Obj.SendMessage(Messege);
                break;
            case false:
                Obj = EnabledObject[0];
                EnabledObject.Remove(Obj);
                Obj.StopAllCoroutines();
                if (DisableDelay > 0f)
                Obj.StartCoroutine(Obj.Deactive(DisableDelay, this));
                EnabledObject.Add(Obj);
                if (!string.IsNullOrEmpty(Messege))
                    Obj.SendMessage(Messege);
                break;
        }
        lastID = Obj.ID;
        if (DeteachActive)
            Obj.transform.SetParent(null);

        Obj.Use(this);
        return Obj;
    }

    public void DeactiveAll()
    {
        for (int i = EnabledObject.Count-1; i >= 0; i--)
        {
            Deactive(EnabledObject[i]);
        }
    }

    public void Deactive(GameObject Obj)
    {
        var po = Obj.GetComponent<PoolObject>();
        if (po) Deactive(po);
    }
    public void Deactive(PoolObject Obj)
    {
        Obj.StopAllCoroutines();
        Obj.transform.parent = Tr;
        Obj.transform.localPosition = Vector3.zero;
        Obj.gameObject.SetActive(false);

        if (DisabledObject.Contains(Obj))
        {
            return;
        }
        DisabledObject.Add(Obj);
        EnabledObject.Remove(Obj);
    }
}
