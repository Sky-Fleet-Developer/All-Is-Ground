using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using UnityEditor;
using Modernizations;

[RequireComponent(typeof(Rigidbody))]
public class Locomotor : MonoBehaviourPlus, IDestroyeble, IDescription, IModifiable
{
    public bool IsAlive { get; set; } = true;
    [System.NonSerialized]
    public Control Control;
    [Header("Suspension")]
    public List<Support> Supports;
    [Header("Engine")]
    public bool MultiplyEngineSoundOnGear;
    public bool InverceBacktreck;
        [Modifiable("Максимальная скорость")]
    public Vector2 Speed;
    public float VibroengineVelocity;
    public Vector2 VibroengineSide;
    public Vector3 EngineTopOffset;
    public Vector3 EngineBottomOffset;
        [Modifiable("Вертикальное сопротивление")]
    public float YDrag = 0.5f;
        [Modifiable("Мощность")]
    public float VibroengineForce;
    public float VibroengineMass;
        [Modifiable("Сцепление")]
    public float SpaceEngineForce;
    public float Traction;
    public float GrowingGearTurns = 0.9f;
    public float LowingGearTurns = 0.45f;
    public AnimationCurve GearTurns;
    public int Gear;
    public List<float> Gears;
    [Header("Hyroscope")]
    public Vector3 RotationVelocty;
        [Modifiable("Мощность гироскопов")]
    public Vector3 GyroscopeForce;
    public float AngularPredictionValue;
    public float DragForce;

    EngineSoundManager sound;

    [System.Serializable]
    public class Support : IModifiable
    {
        public int Group;
        public Vector3 localPosition;
        public Vector3 position
        {
            get
            {
                return parent.TransformPoint(localPosition);
            }
        }
        public Vector3 localDirection = Vector3.down;
        public Vector3 direction
        {
            get
            {
                return parent.TransformDirection(localDirection).normalized;
            }
        }
        [Modifiable("Длина")]
        public float length = 1f;
        [Modifiable("Высота подвески")]
        public float targetDistance;
        [Modifiable("Сила пружины")]
        public float Force;
        [Modifiable("Жесткость пружины")]
        public float Dumping;
        [HideInInspector]
        public bool IsGrounded;
        [HideInInspector]
        public Transform parent;
        [HideInInspector]
        public float groundDistance;
        [HideInInspector]
        public float lastGroundDistance;

        public bool Work(Locomotor locomotor)
        {
            RaycastHit Ray;
            Vector3 pos = position;
            Vector3 dir = direction;
            float rayLength = length - GameValues.SupportRayRadius;
            if (Physics.SphereCast(pos - dir * GameValues.SupportRayRadius, GameValues.SupportRayRadius, dir, out Ray, rayLength, GameValues.SupportsGround))
            {
                IsGrounded = true;
                groundDistance = Mathf.MoveTowards(groundDistance, Ray.distance, Time.fixedDeltaTime * 20);
            }
            else
            {
                Ray = new RaycastHit();
                IsGrounded = false;
                groundDistance = Mathf.MoveTowards(groundDistance, rayLength, Time.fixedDeltaTime * 20);
            }

            float springValue = (targetDistance + GameValues.SupportRayRadius - Ray.distance) / length;
            float dumperValue = Mathf.Clamp(lastGroundDistance - groundDistance, -0.1f, 0.1f) * 10;
            lastGroundDistance = groundDistance;
            float springForce = springValue * Force;
            float dumperForce = dumperValue * Dumping;
            float abs = Mathf.Abs(springForce);
            springForce = Mathf.Clamp(springForce, -abs * (1 - locomotor.Control.ClampUp), abs);
            abs = Mathf.Abs(dumperForce);
            dumperForce = Mathf.Clamp(dumperForce, -abs * (1 - locomotor.Control.ClampUp), abs);

            Vector3 fDir;
            if(IsGrounded)
                fDir = (Ray.normal - dir) * 0.5f * (springForce + dumperForce);
            else
                fDir = -dir * dumperForce;

            Vector3 force = fDir * FORCE * Time.fixedDeltaTime;
            locomotor.Rigid.AddForceAtPosition(force, pos);

            if (IsGrounded)
            {
                Debug.DrawRay(Ray.point, fDir / (Force + Dumping) * 20, Color.red);
                var rig = Ray.rigidbody;
                if (rig)
                    rig.AddForceAtPosition(-force, pos);
            }
            return IsGrounded;
        }

        public int GetGroup()
        {
            return Group;
        }
    }
    
    protected Vector3 Velocity;
    protected Vector3 AngularVelocity;
    protected Rigidbody Rigid;
    protected Vector3 InputAxis;
    protected float InputAxisMagnitude;
    protected Vector3 EngineForce;
    protected float suppMP;
    protected Vector2 TargetSpeed;
    protected float GearTurnTimer;

    void Start()
    {
        Rigid = GetComponent<Rigidbody>();
        Control = GetComponent<Control>();
        foreach (var Hit in Supports)
        {
            Hit.parent = transform;
        }
        if (!GameValues.Instance)
            GameValues.Instance = Resources.Load<GameValues>("GameValues");

        sound = EngineSoundManager.Instances[gameObject];
        /*if(EngineEntity)
            SetProperties(EngineEntity);
        if (SupportsEntity)
        {
            foreach(var hit in Supports)
            {
                hit.SetProperties(SupportsEntity);
            }
        }*/
    }

    private void OnDrawGizmosSelected()
    {
        if (Application.isPlaying)
        {
            foreach (var Hit in Supports)
            {
                Debug.DrawRay(Hit.position, Hit.direction * Hit.groundDistance, Color.green);
                Debug.DrawLine(Hit.position + Hit.direction * Hit.targetDistance + transform.forward * 0.1f, Hit.position + Hit.direction * Hit.targetDistance - transform.forward * 0.1f, Color.yellow);
                if (GameValues.EnableDebug)
                {
                    UltiDraw.Begin();

                    UltiDraw.DrawWiredSphere(Hit.position - Hit.direction * GameValues.SupportRayRadius, transform.rotation, GameValues.SupportRayRadius * 2, new Color(1, 1, 1, 0.1f), Color.white);

                    UltiDraw.DrawWiredSphere(Hit.position + Hit.direction * (Hit.groundDistance - GameValues.SupportRayRadius), transform.rotation, GameValues.SupportRayRadius * 2, new Color(1, 1, 1, 0.1f), Color.green);

                    UltiDraw.End();
                }
            }
        }
        else
        {
            if (!GameValues.Instance)
                GameValues.Instance = Resources.Load<GameValues>("GameValues");
            foreach (var Hit in Supports)
            {
                Vector3 pos = transform.TransformPoint(Hit.localPosition);
                Vector3 dir = transform.TransformDirection(Hit.localDirection);
                Debug.DrawRay(pos, dir * Hit.length, Color.green);
                Debug.DrawLine(pos + dir * Hit.targetDistance + transform.forward * 0.1f, pos + dir * Hit.targetDistance - transform.forward * 0.1f, Color.yellow);
                if (GameValues.EnableDebug)
                {
                    UltiDraw.Begin();
                    UltiDraw.DrawWiredSphere(pos - dir * GameValues.SupportRayRadius, transform.rotation, GameValues.SupportRayRadius * 2, new Color(1, 1, 1, 0.1f), Color.white);
                    UltiDraw.End();
                }
            }
        }
    }

    public virtual Vector3 GetInputAxis()
    {
        Quaternion forwRot = Quaternion.LookRotation(Control.Forward, transform.up);
        return transform.InverseTransformDirection(Vector3.ProjectOnPlane(forwRot * new Vector3(Control.InputAxis.y, 0f, Control.InputAxis.x), transform.up).normalized);
    }
    
    public virtual Vector3 GetGyroscopeForce()
    {
        Vector3 localForward = transform.InverseTransformDirection(Control.Forward);
        float YAngle = Mathf.Atan2(localForward.x, localForward.z) * Mathf.Rad2Deg;
        float TarY = Mathf.Clamp((YAngle - AngularVelocity.y * AngularPredictionValue) / 5, -1, 1) * RotationVelocty.y;
        return new Vector3(Mathf.Clamp(InputAxis.y * RotationVelocty.x - AngularVelocity.x * DragForce, -1, 1) * GyroscopeForce.x, Mathf.Clamp((TarY - AngularVelocity.y * DragForce), -1, 1) * GyroscopeForce.y, Mathf.Clamp(InputAxis.x * RotationVelocty.z - AngularVelocity.z * DragForce, -1, 1) * GyroscopeForce.z);
    }

    public virtual Vector3 GetEngineForce()
    {
        TargetSpeed = new Vector2(InputAxis.z * VibroengineVelocity, InputAxis.x * VibroengineVelocity / Speed.x * Speed.y) * Gears[Gear];
        Vector3 TVDifference = new Vector3(Mathf.Clamp(TargetSpeed.y - Velocity.x, -Speed.x / 4, Speed.x / 4), 0, Mathf.Clamp(TargetSpeed.x - Velocity.z, -Speed.x / 4, Speed.x / 4));
        return TVDifference * SpaceEngineForce * Traction;
    }

    public virtual void SetGear()
    {
        int max = 10;
        Vector3 TargetSpaceVelocity = new Vector3(TargetSpeed.y, 0, TargetSpeed.x);
        if (TargetSpaceVelocity == Vector3.zero)
            TargetSpaceVelocity = Velocity;

        Quaternion TV = Quaternion.LookRotation(TargetSpaceVelocity);
        Vector3 tvVel = TV.GetInverse() * Velocity;
        max = (int)GearTurns.Evaluate(tvVel.z / Speed.x);
            
        if (GearTurnTimer <= 0)
        {
            if (VibroengineVelocity > Speed.x * GrowingGearTurns)
            {
                int next = Mathf.Min(max, Mathf.Clamp(Gear + 1, 0, Gears.Count - 1));
                if (next != Gear)
                {
                    Gear = next;
                    Traction = 0;
                }
                GearTurnTimer = 0.35f;
            }
            if (VibroengineVelocity < Speed.x * LowingGearTurns)
            {
                int next = Mathf.Clamp(Gear - 1, 0, Gears.Count - 1);
                if (next != Gear)
                {
                    Gear = next;
                    Traction = 0;
                }
                GearTurnTimer = 0.35f;
            }
        }
        GearTurnTimer -= Time.fixedDeltaTime;
    }

    public virtual void SetVibroengineDrag()
    {
        VibroengineVelocity -= (EngineForce.z * VibroengineSide.x + EngineForce.x * VibroengineSide.y) / VibroengineMass * Gears[Gear] * Time.fixedDeltaTime;
    }

    public virtual float GetEngineTenson()
    {
        return EngineForce.magnitude;
    }

    void FixedUpdate()
    {
        if (!IsAlive)
            return;
        
        Velocity = transform.InverseTransformDirection(Rigid.velocity);
        AngularVelocity = transform.InverseTransformDirection(Rigid.angularVelocity);
        int work = 0;
        foreach (var Hit in Supports)
        {
            if (Hit.Work(this)) work++;
        }
        Vector3 engineOffset = Vector3.Lerp(EngineTopOffset, EngineBottomOffset, work);
        suppMP = Mathf.Lerp((float)work / Supports.Count, 1f, 0.35f);

        InputAxis = GetInputAxis();
        InputAxisMagnitude = new Vector3(Mathf.Abs(InputAxis.x), Mathf.Abs(InputAxis.y), Mathf.Abs(InputAxis.z)).magnitude;
        Traction = Mathf.Min(InputAxisMagnitude, Mathf.MoveTowards(Traction, InputAxisMagnitude, Time.fixedDeltaTime));
        //Power = Mathf.MoveTowards(Power, Mathf.Lerp(MidPower, MaxPower, InputAxis.magnitude), Time.fixedDeltaTime * PowerAcceleration / EngineInertia);

        VibroengineSide = new Vector2(Side(InputAxis.z + 0.01f), Side(InputAxis.x + 0.01f));

        VibroengineVelocity = Mathf.MoveTowards(VibroengineVelocity, Speed.x * (Traction * 0.9f + 0.4f), VibroengineForce / VibroengineMass * Time.fixedDeltaTime);

        EngineForce = GetEngineForce();

        SetGear();


        SetVibroengineDrag();

        float EngineTenson = Mathf.Abs(GetEngineTenson());

        Vector3 GiroscopeForce = GetGyroscopeForce();

        sound.Set(Mathf.Abs(VibroengineVelocity) * (MultiplyEngineSoundOnGear ? Gears[Gear] : 1), EngineTenson, GiroscopeForce.magnitude);

        EngineForce -= Vector3.up * Velocity.y * YDrag;

        //Power = Mathf.MoveTowards(Power, 0f, EngineTenson / EngineInertia * Time.fixedDeltaTime);

        Vector3 f = transform.rotation * EngineForce * suppMP * FORCE * Time.fixedDeltaTime;
        f = ClampDistance(f, 0, 500000);
        if (float.IsNaN(f.x) || float.IsNaN(f.y) || float.IsNaN(f.z))
            f = Vector3.zero;
        Rigid.AddForceAtPosition(f, transform.TransformPoint(engineOffset + Rigid.centerOfMass));
        
        Vector3 gyroscopeForce = GiroscopeForce * FORCE;
        Vector3 g = gyroscopeForce * suppMP * Time.fixedDeltaTime;
        g = ClampDistance(g, 0, 50000);
        if (float.IsNaN(g.x) || float.IsNaN(g.y) || float.IsNaN(g.z))
            g = Vector3.zero;
        Rigid.AddRelativeTorque(g);
    }

    public void Death()
    {
        IsAlive = false;
        sound.Set(0, 0, 0);
    }

    public void Spawn()
    {
        IsAlive = true;
    }

    public virtual void GetDescription(ref List<string> Parameters, ref List<string> Values)
    {
        Parameters.Add("Движение");
        Values.Add("Пространств.");
        Parameters.Add("Масса");
        Values.Add(GetComponent<Rigidbody>().mass + " кг");
        Parameters.Add("Скорость");
        Values.Add((Mathf.Ceil(Speed.x * 3.6f)).ToString() + " / " + (Speed.y * 3.6f).ToString() + " км/ч");
        Parameters.Add("Количество опор");
        Values.Add((Supports.Count).ToString());
        Parameters.Add("Мощность опор");
        Values.Add((Supports[0].Force * FORCE / 1000000) + " Т");
        Parameters.Add("Мощность \nвибродвигателя");
        Values.Add("");
        Values.Add((VibroengineForce) + " м/сек2");
        Parameters.Add("Мощность плоскостей \n сцепления");
        Values.Add("");
        Values.Add((SpaceEngineForce) + " КН");
        Parameters.Add("Мощность гироскопа");
        Values.Add(GyroscopeForce.ToString());
    }

    public System.Type GetFieldType(string name)
    {
        System.Type type = GetType();
        var field = type.GetField(name);
        return field.GetType();
    }

    public void SetField(string name, object value, object zero)
    {
        System.Type type = GetType();

        var field = type.GetField(name);

        object Val = 0f;
        if (field != null)
        {
            if(value == null)
                field.SetValue(this, zero);
            field.SetValue(this, value);
        }
    }

    public object GetField(string name)
    {
        System.Type type = GetType();
        var field = type.GetField(name);
        return field.GetValue(this);
    }

    public int GetGroup()
    {
        return 0;
    }
}
