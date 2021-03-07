using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Health : MonoBehaviourPlus, IDestroyeble
{
    public float HitPoints = 100f;
    IDestroyeble[] Damageble;
    public Dictionary<PhotonPlayer, float> Damage;

    public bool IsMine { get { return PhotonNetwork.connected && View.isMine || !PhotonNetwork.connected; } }
    [System.NonSerialized]
    public PhotonView View;
    [System.NonSerialized]
    public Transform Tr;
    [System.NonSerialized]
    public bool IsAlive;
    [System.NonSerialized]
    public Rigidbody Rigid;
    UILink HPBar;
    [System.NonSerialized]
    public Control control;
    float DamageCollection;
    float ReleseDCTimer = 0.15f;
    [System.NonSerialized]
    public int DamageblePartsCount;

    void Awake()
    {
        Damageble = GetComponentsInChildren<IDestroyeble>();
        View = GetComponent<PhotonView>();
        Rigid = GetComponent<Rigidbody>();
        control = GetComponent<Control>();
        Tr = transform;
        IsAlive = true;
        Invoke("LateStart", 0.1f);
        Damage = new Dictionary<PhotonPlayer, float>();
        DamageblePartsCount = GetComponentsInChildren<Damageble>().Length;
    }
    void Update()
    {
        if(Tr.position.y < 0 && IsMine)
        {
            AddDamage(100, PhotonNetwork.player);
        }
        if (!View.isMine)
        {
            ReleseDCTimer = Mathf.MoveTowards(ReleseDCTimer, 0f, Time.deltaTime);
            if (DamageCollection > 5 && ReleseDCTimer == 0)
            {
                ReleseDCTimer = 0.15f;
                View.RPC("ScyncDamage", View.owner, DamageCollection, PhotonNetwork.player);
                DamageCollection = 0f;
            }
        }
    }

    bool destroy = false;
    public void Autodestroy()
    {
        destroy = !destroy;
        if(control.UserControl)
            UILink.MainCanvas.GetChildByName("Delete").gameObject.SetActive(destroy);
        if(destroy)
            Invoke("Destr", 5);
    }

    public void Destr()
    {
        if (destroy)
        {
            destroy = false;
            UILink.MainCanvas.GetChildByName("Delete").gameObject.SetActive(destroy);
            AddDamage(100, PhotonNetwork.player);
        }
    }


    void LateStart()
    {
        if (IsMine && control.UserControl && gameObject.GetActive())
        {
            InputEvents.Instance.OnButtonDown("Destroy").AddListener(Autodestroy);
            HPBar = UILink.MainCanvas.GetChildByName("HPBar").GetChildByName("Bar");
            HPBar.Image.color = PunTeamsColors[(int)PhotonNetwork.player.GetTeam()];
        }
    }

    void WriteDamage(PhotonPlayer from, float damage)
    {
        if (!Damage.ContainsKey(from))
            Damage.Add(from, 0f);
        Damage[from] += damage;
    }
    void ClearDamage()
    {
        Damage = new Dictionary<PhotonPlayer, float>();
    }

    /*void Update()
    {
        if (IsMine)
        {
            if (float.IsNaN(HitPoints))
            {
                HitPoints = 0f;
                View.RPC("ScyncHealth", PhotonTargets.Others, 0, 0, null);
            }
        }
    }*/

    public void AddDamage(float damage, PhotonPlayer from)
    {
        if (!IsAlive)
            return;

        if (IsMine)
        {
            var ft = from.GetTeam();
            var mt = PhotonNetwork.player.GetTeam();

            float hp = Mathf.MoveTowards(HitPoints, 0f, damage * DamageManager.DamageMultiplyer);
            if (PhotonNetwork.connected)
            {
                View.RPC("ScyncHealth", PhotonTargets.Others, hp, HitPoints - hp, from);
                if (ft != mt)
                    WriteDamage(from, HitPoints - hp);
            }
            HitPoints = hp;
            if (HPBar)
                HPBar.Image.fillAmount = HitPoints / 100;
            if (hp == 0f || float.IsNaN(HitPoints))
            {
                GameManager.WaitForSpawn(this);

                string c1 = PunTeamsColorsString[(int)PhotonNetwork.player.GetTeam()];

                if (from == PhotonNetwork.player)
                    GameLog.Write(c1 + PhotonNetwork.player.NickName + "</color>", GameLog.LogType.KillSelf);
                else
                {
                    string c2 = PunTeamsColorsString[(int)from.GetTeam()];
                    GameLog.Write(c1 + PhotonNetwork.player.NickName + "</color>" + "|" + c2 + from.NickName + "</color>", GameLog.LogType.Kill);
                }

                Kill();
                if (ft != mt)
                {
                    switch (ft)
                    {
                        case PunTeams.Team.red:
                            GameScore.Instance.AddRedScore(20);
                            break;
                        case PunTeams.Team.blue:
                            GameScore.Instance.AddBlueScore(20);
                            break;
                    }
                }
                foreach (var hit in Damage)
                {
                    hit.Key.AddScore((int)(hit.Value / 100f * 20f));
                    UsersDATA.AddExperience(hit.Key.NickName, (int)(hit.Value / 100f * 20f));
                }

                ClearDamage();
            }
        }
        else
        {
            DamageCollection += damage;
        }
    }

    public void Respawn()
    {
        foreach (var hit in Damageble)
            hit.Spawn();
        HitPoints = 100;
        if (IsMine)
        {
            if (HPBar)
                HPBar.Image.fillAmount = 1;
            View.RPC("Respawn", PhotonTargets.Others, HitPoints, Tr.position, Tr.rotation);
        }
    }

    public void Kill()
    {
        foreach (var hit in Damageble)
            hit.Death();
    }
    

    [PunRPC]
    public void ScyncDamage(float damage, PhotonPlayer from)
    {
        AddDamage(damage, from);
    }
    [PunRPC]
    public void ScyncHealth(float health, float damage, PhotonPlayer from)
    {
        if(PhotonNetwork.player == from)
        {
            PoollingStringLine.Instances["DamageMesseges"].Write(Mathf.Ceil(damage * 10).ToString(), health == 0 ? Color.cyan : Color.Lerp(Color.red, Color.green, damage / 75));
        }
        HitPoints = health;
        if (float.IsNaN(HitPoints))
            HitPoints = 0f;

        if (HitPoints == 0f)
            Kill();
    }
    [PunRPC]
    public void Respawn(float health, Vector3 position, Quaternion rotation)
    {
        Tr.position = position;
        Tr.rotation = rotation;
        Rigid.velocity = Vector3.zero;
        Rigid.angularVelocity = Vector3.zero;
        foreach (var hit in Damageble)
            hit.Spawn();
        HitPoints = health;
    }
    [PunRPC]
    public void ScyncLocalEffect(Vector3 localPosition, Quaternion localRotation, string EffectPoolName)
    {
        Pooling.InstancesDic[EffectPoolName].Use(Tr.TransformPoint(localPosition), Tr.rotation * localRotation, null);
    }
    [PunRPC]
    public void ScyncWorldEffect(Vector3 pos, Quaternion rot, string ExplosePoolName)
    {
        Pooling.InstancesDic[ExplosePoolName].Use(pos, rot, null);
    }

    public void Death()
    {
        IsAlive = false;
    }

    public void Spawn()
    {
        IsAlive = true;
    }

    public void GetDescription(ref List<string> Parameters, ref List<string> Values)
    {
        foreach(var hit in GetComponentsInChildren<IDescription>())
        {
            hit.GetDescription(ref Parameters, ref Values);
            Parameters.Add("");
            Values.Add("");
        }
    }
}
