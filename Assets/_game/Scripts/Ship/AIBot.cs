using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AIBot : MonoBehaviourPlus, IDestroyeble
{
    [System.NonSerialized]
    public Control control;
    [System.NonSerialized]
    public RocketLauncher rockets;
    [System.NonSerialized]
    public Projectile gun;
    [System.NonSerialized]
    public NavMeshAgent agent;
    [System.NonSerialized]
    public Locomotor locomotor;
    [System.NonSerialized]
    public CaptureblePoint[] Points;
    [System.NonSerialized]
    public CaptureblePoint SelectedPoint;
    protected Vector3 PathWaypoint;
    protected Vector3 NextWaypoint;
    protected Vector3 TargetWaypoint;
    protected Vector3 LastTargetWaypoint;
    protected Vector3 HoldWaypoint;
    protected Vector3 Velocity;
    protected Vector3 AngularVelocity;

    Health[] AllShips;
    List<Health> Enamys;

    protected Health MainEnamy;

    protected Vector3[] Path;
    protected bool IsEndPoint;
    protected int PathItem;
    protected Transform Tr;
    protected Rigidbody Rigid;
    protected Transform TWP;
    float PCTimer;
    protected NavMeshPath NMPath;
    protected bool RocketShoot = false;
    protected bool CanRocketShoot = false;
    protected bool CanGunShoot = false;
    protected PunTeams.Team team;
    protected Charge gunCharge;
    protected SoundWawe sw;
    protected float chargeLifetime;
    protected MovingPrioritets MovingPriority;
    protected float CantCalculatePath;
    protected bool WantToJump;
    public enum MovingPrioritets
    {
        Standart = 0,
        Partner = 1,
        CapPoint = 2,
        ProtectPoint = 3,
        HoldPosition = 4
    }
    float hold;
    void Start()
    {
        Tr = transform;
        Rigid = GetComponent<Rigidbody>();
        control = GetComponent<Control>();
        Points = FindObjectsOfType<CaptureblePoint>();
        rockets = GetComponent<RocketLauncher>();
        gun = GetComponent<Gun>();
        locomotor = GetComponent<Locomotor>();
        if (!gun)
            gun = GetComponent<SoundWaveGun>();
        TargetWaypoint = GetCloserPoint();
        NMPath = new NavMeshPath();
        TWP = new GameObject().transform;
        team = PhotonNetwork.player.GetTeam();
        agent = new GameObject().AddComponent<NavMeshAgent>();
        agent.transform.parent = Tr;
        agent.transform.localPosition = Vector3.down;
        agent.transform.localRotation = Quaternion.identity;
        agent.height = 2;
        agent.radius = 2.5f;
        agent.baseOffset = -1.2f;
        agent.enabled = false;
        gunCharge = gun.ChargePool.charge;
        sw = gunCharge as SoundWawe;
        if (sw)
        {
            chargeLifetime = sw.Lifetime * sw.StartSpeed;
            hold = 0f;
        }
    }

    #region Retargeteble
    protected virtual void SetTargetWaypointFromPath()
    {
        TWP.position = TargetWaypoint;
        float z = Tr.InverseTransformPoint(PathWaypoint).z;
        float d = Vector3.Distance(Tr.position, PathWaypoint);
        if (d < 2.5f || (d < 10 && z < 2.5 && z > -10))
        {
            if (Path.Length > PathItem + 1)
            {
                PathItem++;
                PathWaypoint = Path[PathItem];
                GetNextPoint();
            }
        }
        Debug.DrawLine(Tr.position, NextWaypoint);
        /*if (Path.Length > PathItem + 2)
        {
            if (Tr.InverseTransformPoint(Path[PathItem]).z < 2.5f && Tr.InverseTransformPoint(Path[PathItem + 1]).z > 2.5f)
            {
                PathItem++;
                PathWaypoint = Path[PathItem];
            }
        }*/
        if (Path != null)
            IsEndPoint = PathItem == (Path.Length - 1);
    }
    protected virtual void SetControlInputAxis()
    {
        Quaternion rot = Quaternion.LookRotation(control.Forward).GetInverse();
        var ia = rot * (PathWaypoint - Tr.position);
        ia = new Vector3(ia.x / locomotor.Speed.x, 0, ia.z / locomotor.Speed.y).normalized;
        control.InputAxis = new Vector2(ia.z + Tr.forward.y, ia.x + Tr.right.y);
    }
    protected virtual void SetCombatControlInputAxis()
    {
        SetControlInputAxis();
    }
    protected virtual void SetFlyInputAxis()
    {
        Vector3 itf = Tr.InverseTransformDirection(control.Forward).normalized;
        control.InputAxis = new Vector2(Tr.forward.y * 2 - Mathf.Clamp(itf.y * 0.7f, -0.7f, -0.1f) + 0.1f, Tr.right.y * 2 - Velocity.x * 0.15f);
    }
    protected virtual void SetCombatForward()
    {
        control.Forward = MainEnamy.transform.position - Tr.position;
    }
    protected virtual void SetForward()
    {
        control.Forward = PathWaypoint - Tr.position;
    }
    protected virtual void SetGunAimPoint()
    {
        control.AimPoint = MainEnamy.transform.position + MainEnamy.transform.up + MainEnamy.Rigid.velocity * GetGunPreemption(MainEnamy.transform.position);
    }
    protected virtual void SetRocketAimPoint()
    {
        float RTEDist = (rockets.Followers[0].Tr.position - MainEnamy.transform.position).magnitude;
        float Up = RTEDist / 120 - 0.5f;
        Up = Mathf.Clamp(Up, -0.5f, 1);
        control.AimPoint = MainEnamy.transform.position + MainEnamy.Rigid.velocity * RTEDist / rockets.Followers[0].Velocity.magnitude + MainEnamy.transform.up + Vector3.up * Up * 6;
    }
    #endregion
    protected IEnumerator ShootRockets()
    {
        RocketShoot = true;
        control.Fire2 = true;
        for (int i = 0; i < rockets.Blocks.Count; i++)
        {
            control.Fire1 = true;
            yield return new WaitForSeconds(0.1f);
            control.Fire1 = false;
            yield return new WaitForSeconds(0.2f);
        }
        while (rockets.Followers.Count > 0)
        {
            SetRocketAimPoint();
            yield return new WaitForSeconds(0.1f);
        }
        control.Fire2 = false;
        RocketShoot = false;
    }

    public void FollowMe()
    {
        MovingPriority = MovingPrioritets.Partner;
        CalculatePath();
    }

    public void CapPoint()
    {
        Vector3[] points = new Vector3[Points.Length];
        for (int i = 0; i < Points.Length; i++)
            points[i] = Points[i].transform.position;
        Vector3 point;
        int nomber;
        if (PointClosedToRay(MouseOrbit.AimRay, points, out point, out nomber))
        {
            MovingPriority = MovingPrioritets.CapPoint;
            SelectedPoint = Points[nomber];
            HoldWaypoint = point;
            CalculatePath();
        }
    }
    public void ProtectPoint()
    {
        Vector3[] points = new Vector3[Points.Length];
        for (int i = 0; i < Points.Length; i++)
            points[i] = Points[i].transform.position;
        Vector3 point;
        int nomber;
        if (PointClosedToRay(MouseOrbit.AimRay, points, out point, out nomber))
        {
            MovingPriority = MovingPrioritets.ProtectPoint;
            SelectedPoint = Points[nomber];
            HoldWaypoint = point;
            CalculatePath();
        }
    }

    public void HoldPosition()
    {
        Vector3 point = MouseOrbit.Instance.AimingHit.point;
        HoldWaypoint = point;
        MovingPriority = MovingPrioritets.HoldPosition;
        CalculatePath();
    }

    protected void NeedToJump()
    {
        var dist = Vector3.Distance(Tr.position, PathWaypoint);
        if (dist < 20 && !WantToJump && NextWaypoint != Vector3.zero && !isFly)
        {
            Vector3 dir = NextWaypoint - Tr.position;
            if (dir.y < -7 && Tr.up.y > 0.7f)
            {
                StartCoroutine(wantToJump());
            }
        }
    }

    IEnumerator wantToJump()
    {
        WantToJump = true;
        control.ClampUp = 1;
        yield return new WaitForSeconds(5);
        WantToJump = false;
        control.ClampUp = 0;
    }

    void GetNextPoint()
    {
        NextWaypoint = Vector3.zero;
        float dist = 0;
        int i = PathItem;
        while (dist < 20)
        {
            if (Path.Length > i + 1)
            {
                dist += Vector3.Distance(Path[i], Path[i + 1]);
                NextWaypoint = Path[i + 1];
                i++;
            }
            else
                break;
        }
    }

    bool PointClosedToRay(Ray ray, Vector3[] points, out Vector3 point, out int nomber)
    {
        float angle = 30;
        bool val = false;
        point = Vector3.zero;
        nomber = -1;
        for (int i = 0; i < points.Length; i++)
        {
            float a = Vector3.Angle(ray.direction, points[i] - ray.origin);
            if(a < angle)
            {
                angle = a;
                point = points[i];
                nomber = i;
                val = true;
            }
        }
        return val;
    }
    void NonCombatControl()
    {
        NeedToJump();
        control.AimPoint = Tr.position + control.Forward * 20;
        SetControlInputAxis();
        RocketShoot = false;
        control.Fire1 = false;
        control.Fire2 = false;
        SetForward();
    }

    void CombatControl()
    {
        SetCombatForward();
        NeedToJump();
        SetCombatControlInputAxis();
        if (!RocketShoot)
        {
            SetGunAimPoint();
            control.Fire1 = false;
            float dist = Vector3.Distance(Tr.position, MainEnamy.transform.position);
            float time = dist / gun.ChargePool.charge.StartSpeed;
            Vector3 grav = Vector3.up * GRAVITY * time;
            if (gun.ChargePool.charge.UseGravity)
                grav = Vector3.zero;
            Vector3 GunPreemption = (MainEnamy.Rigid.velocity - Rigid.velocity + grav) * time;
            
            if (RocketsCanShoot(MainEnamy.transform.position))
            {
                StartCoroutine(ShootRockets());
            }
            else if (GunCanShoot(MainEnamy.transform.position + GunPreemption))
            {
                MainGunShoot(MainEnamy.transform.position + GunPreemption);
            }
        }
    }

    void FlyControl()
    {
        SetFlyInputAxis();
        control.AimPoint = Tr.position + control.Forward * 20;
        SetForward();
        RocketShoot = false;
        control.Fire1 = false;
        control.Fire2 = false;
    }

    public bool IsFly()
    {
        int gr = 0;
        for(int i = 0; i < locomotor.Supports.Count; i++)
        {
            if (locomotor.Supports[i].IsGrounded)
                gr++;
        }
        return gr == 0;
    }

    void CalculatePath()
    {
        agent.enabled = true;
        agent.transform.localPosition = Vector3.down;
        agent.transform.localRotation = Quaternion.identity;
        if (agent.CalculatePath(TWP.position, NMPath))
        {
            agent.enabled = false;
            Path = NMPath.corners;
            if (Vector3.Distance(Path[Path.Length - 1], TWP.position) > 100)
            {
                CantCalculatePath += 2;
                Path = new Vector3[2];
                Path[0] = Tr.position;
                Path[1] = TWP.position;
            }
            else
                CantCalculatePath = 0;
            for (int i = 1; i < Path.Length; i++)
            {
                Debug.DrawLine(Path[i - 1], Path[i], Color.cyan, 3f);
            }
            PathItem = 1;
            PathWaypoint = Path[PathItem];
            GetNextPoint();

        }
        else
        {
            CantCalculatePath += 2;
             //Debug.Log("<color=red>Блядская хуйня!!!</color>");
        }
        if(CantCalculatePath > 12)
        {
            CantCalculatePath = 0;
            GetComponent<Health>().Autodestroy();
            GetComponent<Health>().Destr();
        }
        agent.enabled = false;
    }
    bool isFly;
    void Update()
    {
        isFly = IsFly();
        if (GameManager.GetEnamysCount() == 0 && MovingPriority == MovingPrioritets.Standart)
            return;
        Velocity = Tr.InverseTransformDirection(Rigid.velocity);
        AngularVelocity = Tr.InverseTransformDirection(Rigid.angularVelocity);

        SetTargetWaypointFromPath();

        Debug.DrawLine(Tr.position, PathWaypoint, Color.blue);
        Debug.DrawLine(Tr.position, TargetWaypoint, Color.red);

        if (isFly)
        {
            FlyControl();
            PCTimer = 1;
        }
        else
        {

            if (MainEnamy && MainEnamy.IsAlive)
            {
                CombatControl();
            }
            else
            {
                NonCombatControl();
            }
        }


        if (PCTimer < 0 && !isFly)
        {
            if (!WantToJump || PCTimer < -7)
            {
                PCTimer = 2;
                CalculatePath();
                LastTargetWaypoint = TargetWaypoint;
                ResearchShips();
                MainEnamy = GetCloserEnamy();
                switch (MovingPriority)
                {
                    case MovingPrioritets.Standart:
                        TargetWaypoint = GetCloserPoint();
                        break;
                    case MovingPrioritets.Partner:
                        var tr = GameManager.Instance.CurrentShip.transform;
                        TargetWaypoint = Vector3.ProjectOnPlane(DistantPoint(Tr.position, tr.position, 10f) - tr.position, tr.up) + tr.position;
                        break;
                    case MovingPrioritets.CapPoint:
                        TargetWaypoint = Vector3.ProjectOnPlane(DistantPoint(Tr.position, HoldWaypoint, Random.Range(SelectedPoint.Radius * 0.7f, SelectedPoint.Radius * 0.3f)) - SelectedPoint.transform.position, SelectedPoint.transform.up) + SelectedPoint.transform.position;
                        if (SelectedPoint.Owner == team)
                            MovingPriority = MovingPrioritets.Standart;
                        break;
                    case MovingPrioritets.ProtectPoint:
                        TargetWaypoint = Vector3.ProjectOnPlane(DistantPoint(Tr.position, HoldWaypoint, Random.Range(SelectedPoint.Radius * 0.7f, SelectedPoint.Radius * 0.3f)) - SelectedPoint.transform.position, SelectedPoint.transform.up) + SelectedPoint.transform.position;
                        break;
                    case MovingPrioritets.HoldPosition:
                        TargetWaypoint = HoldWaypoint;
                        break;
                }
            }
        }
        PCTimer -= Time.deltaTime;
    }

    public void MainGunShoot(Vector3 Target)
    {
        float dist = Vector3.Distance(Target, Tr.position);
        if (sw)
        {
            //Debug.Log("sw");
            float nearD = chargeLifetime / 1.5f;
            float farD = chargeLifetime / 0.7f;
            if (farD > dist)
            {
                float wantedHold = (dist - nearD) / (farD - nearD);
                if (hold < wantedHold)
                {
                    //Debug.Log("hold");
                    control.Fire1 = true;
                    hold += Time.deltaTime;
                }
                else
                {
                    //Debug.Log("В самый раз. ближ: " + nearD + ". дальн: " + farD + ". дист: " + dist + ". знач: " + hold + ". нужн: " + wantedHold);
                    control.Fire1 = false;
                    hold = 0;
                }
            }
            else
            {
                control.Fire1 = false;
                hold = 0;
            }
        }
        else
        {
            control.Fire1 = true;
        }
    }

    protected float GetGunPreemption(Vector3 target)
    {
        if (gun)
            return Vector3.Distance(Tr.position, target) / gun.ChargePool.charge.StartSpeed;
        else
            return 0f;
    }

    public void ResearchShips()
    {
        Enamys = new List<Health>();
        AllShips = FindObjectsOfType<Health>();
        foreach (var hit in AllShips)
        {
            if (hit.View.owner.GetTeam() != team)
                Enamys.Add(hit);
        }
    }

    public bool RocketsIsAimed(Vector3 Target)
    {
        int all = 0, redy = 0;
        foreach (var hit in rockets.Blocks)
        {
            all++;
            if (hit.Charged && Vector3.Angle(hit.Launcher.forward, Target - Tr.position) < 8)
                redy++;
        }
        if(rockets.ChargePool.charge.EffectiveDistance != 0)
        {
            if (Vector3.Distance(Tr.position, Target) > rockets.ChargePool.charge.EffectiveDistance)
                return false;
        }
        CanRocketShoot = redy == all;
        return CanRocketShoot;
    }

    public bool RocketsCanShoot(Vector3 Target)
    {
        if (!rockets)
            return false;

        if (Vector3.Distance(Tr.position, MainEnamy.transform.position) < 15)
            return false;

        return RocketsIsAimed(Target);
    }

    public bool GunCanShoot(Vector3 Target)
    {
        int all = 0, redy = 0;
        foreach (var hit in gun.Blocks)
        {
            all++;
            if (!sw || (sw && chargeLifetime / 0.7f > Vector3.Distance(Target, Tr.position)))
            {
                Vector3 direction = Target - Tr.position;
                Quaternion rot = Quaternion.LookRotation(direction).GetInverse();
                Vector3 angle = rot * hit.Launcher.forward * direction.magnitude;
                float XY = Mathf.Abs(angle.x) + Mathf.Abs(angle.y);
                if (hit.Charged && XY < 1.2f)
                    redy++;

            }
        }
        if (gun.ChargePool.charge.EffectiveDistance != 0)
        {
            if (Vector3.Distance(Tr.position, Target) > gun.ChargePool.charge.EffectiveDistance)
                return false;
        }
        CanGunShoot = redy == all;
        return CanGunShoot;
    }

    public Vector3 GetCloserPoint()
    {
        float dist = 10000;
        int point = -1;
        for (int i = 0; i < Points.Length; i++)
        {
            if (Points[i].Owner != team)
            {
                float d = Vector3.Distance(Points[i].transform.position, Tr.position);
                if (d < dist)
                {
                    dist = d;
                    point = i;
                }
            }
        }
        if (point == -1)
        {
            for (int i = 0; i < Points.Length; i++)
            {
                float d = Vector3.Distance(Points[i].transform.position, Tr.position);
                if (d < dist)
                {
                    dist = d;
                    point = i;
                }
            }
        }
        SelectedPoint = Points[point];
        Vector3 res = DistantPoint(Tr.position, Points[point].transform.position, Random.Range(20, 10));
        return new Vector3(res.x, Points[point].transform.position.y, res.z);
    }
    public Health GetCloserEnamy()
    {
        float dist = 10000;
        int en = 0;
        bool found = false;
        for (int i = 0; i < Enamys.Count; i++)
        {
            float d = Vector3.Distance(Enamys[i].transform.position, Tr.position);
            RaycastHit Hit;
            Vector3 self = Tr.position + Vector3.up;
            Vector3 enamy = Enamys[i].transform.position + Vector3.up;
            if (Enamys[i].IsAlive)
            {
                if (!Physics.Raycast(self, enamy - self, out Hit, d, GameValues.BotAimingLayer))
                {
                    if (d < dist)
                    {
                        dist = d;
                        en = i;
                        found = true;
                    }
                }
            }
        }
        if (found)
            return Enamys[en];
        else
            return null;
    }

    public void Death()
    {
    }

    public void Spawn()
    {
        MovingPriority = MovingPrioritets.Standart;
    }
}
