using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviourPlus, IDestroyeble, IDescription
{
    public int Group;
    [Header("Description")]
    public string WeaponName;
    [Header("Setup")]
    public Pooling ChargePool;

    public Color ReloadRingColor;

    public UIReloadingBarTypes ReloadUIBatType;

    public bool ScyncEffects = false;

    public enum UIReloadingBarTypes
    {
        ReloadIsClip = 0,
        ReloadIsGun = 1,
        ReloadIsCharges = 2,
        ReloadIsClips = 3
    }

    public List<ChargeBlock> Blocks;

    protected Dictionary<int, Charge> flyCharges;

    public List<Turel> Turels;

    protected List<UILink> Aiming;
    protected List<UILink> Reloading;

    [System.Serializable]
    public class Turel : IComponent
    {
        public int Group;
        public Transform Turret;
        public float HorizontalRotationSpeed;
        public Vector2 HorizontalRotationAngle;
        public bool Radial;
        public Transform Gunpoint;
        public float VerticalRotationSpeed;
        public Vector2 VerticalRotationAngle;
        Vector2 screenPos;

        public bool Rotate(Vector3 point)
        {
            return RotateTuel(Turret.parent, Turret, Gunpoint, HorizontalRotationAngle.x, HorizontalRotationAngle.y, VerticalRotationAngle.x, VerticalRotationAngle.y, point, HorizontalRotationSpeed, VerticalRotationSpeed, Radial);
        }

        public Vector2 GetRaycastScreenPose()
        {
            RaycastHit hit;
            Vector3 pos;
            if (Physics.Raycast(Gunpoint.position, Gunpoint.forward, out hit, 2000, GameValues.AimingLayer))
                pos = hit.point;
            else
                pos = Gunpoint.position + Gunpoint.forward * 2000;
            var p = MouseOrbit.MainCamera.WorldToScreenPoint(pos);
            if (p.z < 0)
                p.y -= Screen.height * 2;
            var r = new Vector2(p.x, Screen.height - p.y);
            screenPos = Vector2.Lerp(screenPos, r, Time.deltaTime * 5);
            if (Vector2.SqrMagnitude(screenPos - r) > 400)
                screenPos = Vector2.MoveTowards(screenPos, r, 20);
            return screenPos;
        }

        public System.Type GetFieldType(string name)
        {
            System.Type type = GetType();
            var field = type.GetField(name);
            return field.GetType();
        }
        public object GetField(string name)
        {
            System.Type type = GetType();
            var field = type.GetField(name);
            return field.GetValue(this);
        }
        public void SetField(string name, object value, object zero)
        {
            System.Type type = GetType();

            var field = type.GetField(name);

            object Val = 0f;
            if (field != null)
            {
                if (value == null)
                    field.SetValue(this, zero);
                field.SetValue(this, value);
            }
        }
    }

    [System.Serializable]
    public class ChargeBlock : IComponent
    {
        public string name;
        public int Group;
        public Transform Launcher;
        public float ReloadDelay = 5;
        //[System.NonSerialized]
        public bool Charged;
        [System.NonSerialized]
        public float ReloadTimer;
        public int ChargesCount = 15;
        [System.NonSerialized]
        public AudioSource audio;
        [System.NonSerialized]
        public int StartChargesCount;
        public int Clips = 1;
        public float ClipReloadDelay = 15;
        [System.NonSerialized]
        public float ClipReloadTimer;
        [System.NonSerialized]
        public int StartClipsCount;
        //[System.NonSerialized]
        public bool ClipCharged;

        public System.Type GetFieldType(string name)
        {
            System.Type type = GetType();
            var field = type.GetField(name);
            return field.GetType();
        }
        public object GetField(string name)
        {
            System.Type type = GetType();
            var field = type.GetField(name);
            return field.GetValue(this);
        }
        public void SetField(string name, object value, object zero)
        {
            System.Type type = GetType();

            var field = type.GetField(name);

            object Val = 0f;
            if (field != null)
            {
                if (value == null)
                    field.SetValue(this, zero);
                field.SetValue(this, value);
            }
        }
    }

    float DischargeTimer;
    [System.NonSerialized]
    public Transform Tr;
    [System.NonSerialized]
    public Control Control;
    [System.NonSerialized]
    Rigidbody Rigid;
    int lastShoot = 0;
    [System.NonSerialized]
    public PhotonView View;
    public bool IsAlive { get; set; } = true;
    public bool IsMine { get { return PhotonNetwork.connected && View.isMine || !PhotonNetwork.connected; } }

    void Start()
    {
        Tr = transform;
        Control = GetComponent<Control>();
        View = GetComponent<PhotonView>();
        Rigid = GetComponent<Rigidbody>();
        OnStart();
        flyCharges = new Dictionary<int, Charge>();
        foreach (var hit in Blocks)
        {
            hit.StartChargesCount = hit.ChargesCount;
            hit.StartClipsCount = hit.Clips;
            hit.ChargesCount = 0;
            hit.Charged = false;
            hit.ClipCharged = false;
            hit.audio = hit.Launcher.GetComponent<AudioSource>();
        }
        Aiming = new List<UILink>();
        Reloading = new List<UILink>();
        if (Control.UserControl)
        {
            var pooling = UILink.MainCanvas.GetChildByName("Weapon").GetChildByName("Aiming").Pooling;
            foreach (var hit in Turels)
            {
                Aiming.Add(pooling.Use().GetComponent<UILink>());
            }
            pooling = UILink.MainCanvas.GetChildByName("Weapon").GetChildByName("Crosshair").GetChildByName("Reloading").Pooling;
            foreach (var hit in Blocks)
            {
                var rui = pooling.Use().GetComponent<UILink>();
                rui.Image.color = ReloadRingColor;
                rui.Image.fillAmount = 0;
                rui.RectTransform.localScale = Vector3.one * (1 + rui.GetComponent<PoolObject>().ID * 0.12f);
                Reloading.Add(rui);
            }
        }
        ChargePool.Tr.parent = null;
        int groups = 0;
        for (int i = 0; i < Blocks.Count; i++)
        {
            groups = Mathf.Max(groups, Blocks[i].Group);
        }
        groups += 1;


        /*if (WeaponEntities.Length < groups)
        {
#if UNITY_EDITOR
            Debug.Log("<color=red>WeaponEntities in " + name + " is wrong setup!</color>");
            UnityEditor.EditorApplication.isPaused = true;
#endif
            return;
        }

        for (int i = 0; i < groups; i++)
        {
            foreach (var hit in Blocks.Where(x => x.Group == i))
            {
                hit.SetProperties(WeaponEntities[i]);
            }
            foreach (var hit in Turels.Where(x => x.Group == i))
            {
                hit.SetProperties(WeaponEntities[i]);
            }
        }*/
    }

    public void ReturnUI()
    {
        var pooling = UILink.MainCanvas.GetChildByName("Weapon").GetChildByName("Aiming").Pooling;
        foreach (var hit in Aiming)
        {
            pooling.Deactive(hit.gameObject);
        }
        pooling = UILink.MainCanvas.GetChildByName("Weapon").GetChildByName("Crosshair").GetChildByName("Reloading").Pooling;
        foreach (var hit in Reloading)
        {
            pooling.Deactive(hit.gameObject);
        }
    }

    protected virtual void OnStart() { }

    protected virtual void Discharge()
    {
        if (DischargeTimer != 0f)
            return;

        for (int I = lastShoot; I < Blocks.Count + lastShoot; I++)
        {
            int i = (int)Mathf.Repeat(lastShoot + 1, Blocks.Count);
            if (Blocks[i].Charged)
            {
                if (Blocks[i].ChargesCount == 0)
                {
                    Blocks[i].ClipReloadTimer = Blocks[i].ClipReloadDelay;
                    Blocks[i].ClipCharged = false;
                }
                Blocks[i].Charged = false;
                Blocks[i].ReloadTimer = Blocks[i].ReloadDelay;
                int ID;
                var charge = ChargePool.Use(out ID).GetComponent<Charge>();
                charge.transform.position = Blocks[i].Launcher.position;
                charge.transform.rotation = Blocks[i].Launcher.rotation;
                charge.Velocity = charge.transform.forward * charge.StartSpeed + Rigid.velocity;
                OnDischarge(charge, i, ID);
                if (!flyCharges.ContainsKey(ID))
                    flyCharges.Add(ID, charge);
                charge.projectile = this;
                if (Blocks[i].audio)
                    Blocks[i].audio.Play();
                DischargeTimer = GameValues.DischargeDelay;
                lastShoot = i;
                if (Control.UserControl)
                {
                    if (ReloadUIBatType == UIReloadingBarTypes.ReloadIsCharges)
                        Reloading[i].Image.fillAmount = 1 - (float)Blocks[i].ChargesCount / Blocks[i].StartChargesCount;
                    if (ReloadUIBatType == UIReloadingBarTypes.ReloadIsClips)
                        Reloading[i].Image.fillAmount = 1 - (float)Blocks[i].Clips / Blocks[i].StartClipsCount;
                }
                break;
            }
        }
    }

    protected virtual void OnDischarge(Charge charge, int block, int ID) { }

    public void Reload()
    {
        for (int i = 0; i < Blocks.Count; i++)
        {
            if (Blocks[i].ChargesCount < Blocks[i].StartChargesCount && Blocks[i].Clips > 0)
            {
                Blocks[i].ClipCharged = false;
                if (Blocks[i].Charged)
                {
                    Blocks[i].Charged = false;
                    Blocks[i].Clips += 1;
                }
                Blocks[i].ReloadTimer = Blocks[i].ReloadDelay;
                Blocks[i].ClipReloadTimer = Blocks[i].ClipReloadDelay;
            }
        }
    }

    void Update()
    {
        if (!IsAlive)
            return;

        DischargeTimer = Mathf.MoveTowards(DischargeTimer, 0f, Time.deltaTime);

        for (int i = 0; i < Blocks.Count; i++)
        {
            Blocks[i].ClipReloadTimer = Mathf.MoveTowards(Blocks[i].ClipReloadTimer, 0f, Time.deltaTime);

            if (Control.UserControl && ReloadUIBatType == UIReloadingBarTypes.ReloadIsClip) Reloading[i].Image.fillAmount = Blocks[i].ClipReloadTimer / Blocks[i].ClipReloadDelay;

            if (!Blocks[i].ClipCharged)
            {
                if (Blocks[i].ClipReloadTimer == 0 && Blocks[i].Clips > 0)
                {
                    Blocks[i].ClipCharged = true;
                    Blocks[i].Clips -= Blocks[i].StartChargesCount - Blocks[i].ChargesCount;
                    Blocks[i].ChargesCount = Blocks[i].StartChargesCount;
                }
            }
            else
            {
                Blocks[i].ReloadTimer = Mathf.MoveTowards(Blocks[i].ReloadTimer, 0f, Time.deltaTime);

                if (Control.UserControl && ReloadUIBatType == UIReloadingBarTypes.ReloadIsGun) Reloading[i].Image.fillAmount = Blocks[i].ReloadTimer / Blocks[i].ReloadDelay;

                if (!Blocks[i].Charged && Blocks[i].ReloadTimer == 0f)
                {
                    if (Blocks[i].ChargesCount > 0)
                    {
                        Blocks[i].Charged = true;
                        Blocks[i].ChargesCount--;
                    }
                }
            }
        }
        OnUpdate();
    }

    protected virtual void OnUpdate() { }

    public virtual void Death()
    {
        IsAlive = false;
    }

    public virtual void Spawn()
    {
        IsAlive = true;
        foreach (var hit in Blocks)
        {
            hit.ChargesCount = hit.StartChargesCount;
            hit.Clips = hit.StartClipsCount - hit.StartChargesCount;
            hit.ClipReloadTimer = 0;
            hit.ReloadTimer = 0;
            hit.Charged = false;
            hit.ClipCharged = true;
        }
    }

    public int GetFireTemp(int chargetInClip, float ClipReload, float GunReload)
    {
        float time = 0f;
        int shoots = 0;
        int charges = chargetInClip;
        while (time < 60)
        {
            if (charges-- > 0)
            {
                time += GunReload;
                shoots++;
            }
            else
            {
                charges = chargetInClip;
                time += ClipReload;
            }
        }
        return shoots;
    }

    public void GetDescription(ref List<string> Parameters, ref List<string> Values)
    {
        Parameters.Add(WeaponName);
        Values.Add("");
        int groups = 0;
        Blocks.ForEach(x => groups = Mathf.Max(groups, x.Group));
        groups++;
        var charge = ChargePool.SourcePrefab.GetComponent<Charge>();
        charge.GetDescription(ref Parameters, ref Values);
        for (int i = 0; i < groups; i++)
        {
            var gr = Blocks.Where(x => x.Group == i).ToList();
            Parameters.Add("Боезапас");
            Values.Add((gr[0].Clips * gr.Count + gr[0].ChargesCount).ToString());
            Parameters.Add("Темп стрельбы");
            Values.Add(GetFireTemp(gr[0].ChargesCount, gr[0].ClipReloadDelay, gr[0].ReloadDelay).ToString() + "/мин");
            if (gr[0].Clips > 1)
            {
                Parameters.Add("Перезарядка");
                float reload = gr[0].ClipReloadDelay;
                if (reload == 0)
                    reload = gr[0].ReloadDelay;
                Values.Add(reload.ToString() + "сек");
            }
        }
    }

    void OnDestroy()
    {
        if (Control && Control.UserControl) ReturnUI();
    }
}
