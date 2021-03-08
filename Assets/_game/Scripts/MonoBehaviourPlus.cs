using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Curves;

public interface IDestroyeble
{
    void Death();
    void Spawn();
}

public interface IDescription
{
    void GetDescription(ref List<string> Parameters, ref List<string> Values);
}

public interface IAccountEvents
{
    void OnEnterAccount(MonoBehaviourPlus.Account account);
}

public interface IComponent
{
    void SetField(string name, object value, object zero);
    object GetField(string name);
    System.Type GetFieldType(string name);
}

public class MonoBehaviourPlus : MonoBehaviour
{
    public static readonly float FORCE = 1000f;
    public static readonly float GRAVITY = 9.81f;
    public static readonly float MPS2KMPH = 3.6f;
    public static readonly float KMPH2MPM = 0.277f;
    public static readonly string Pool_Ricoshet = "RicoshetPool";

    public static readonly Color[] PunTeamsColors = new Color[]
    {
        Color.green,
        Color.red,
        Color.blue
    };
    public static readonly string[] PunTeamsColorsString = new string[]
    {
        "<color=green>",
        "<color=red>",
        "<color=blue>"
    };

    public float[] TowDToOneD(float[,] TowD, int size)
    {
        float[] OneD = new float[size * size];
        for (int h = 0; h < size; h++)
        {
            for (int w = 0; w < size; w++)
            {
                OneD[h * size + w] = TowD[h, w];
            }
        }
        return OneD;
    }

    public float[,] OneDToTowD(float[] OneD)
    {
        int width = (int)Mathf.Sqrt(OneD.Length);
        float[,] TowD = new float[width, width];
        for (int h = 0; h < width; h++)
        {
            for (int w = 0; w < width; w++)
            {
                TowD[h, w] = OneD[h * width + w];
            }
        }
        return TowD;
    }

    public enum LocomotorType
    {
        Space = 0,
        Forward = 1,
        SemiForward = 2
    }
    public enum ComponentAttachment
    {
        LocomotorEngine = 0,
        LocomotorSuspension = 1,
        WeaponBlocks = 2,
        WeaponTurels = 3,
        Protection = 4
    }
    public enum PropertyApplyTypes
    {
        Apply = 0,
        Add = 1,
        Multiply = 2
    }
    public enum PropertyTypes
    {
        Float = 0,
        Int = 1,
        Vector2 = 2,
        Vector3 = 3
    }

    [System.Serializable]
    public class PropertyBlock
    {
        [Header("---------------")]
        public string Name;
        [TextArea]
        public string Description;
        public int Cost;
        [Header("---------------")]
        public List<string> OpenNext;
        [Header("Properties")]
        public List<Property> Properties;
        [System.Serializable]
        public class Property
        {
            public string Name;
            public string groupName;
            public string Description;
            public PropertyApplyTypes ApplyType;
            public int Group;
            public int UnderGroup;
            public ComponentAttachment ComponentAttachment;
            public float FValue;
            public int IValue;
            public Vector3 V3Value;
            public Vector2 V2Value;

            public void SetProperty(IComponent component)
            {
                object Value = component.GetField(Name);
                Value = GetValue(Value);
                component.SetField(Name, Value, GetZero(component.GetFieldType(Name)));
            }

            public object GetValue(object oldValue)
            {
                object Value = GetValue(out PropertyTypes propertyType);

                if (Value == null)
                    return null;

                switch (ApplyType)
                {
                    case PropertyApplyTypes.Add:
                        switch (propertyType)
                        {
                            case PropertyTypes.Float:
                                Value = (float)Value + (float)oldValue;
                                break;
                            case PropertyTypes.Int:
                                Value = (float)Value + (int)oldValue;
                                break;
                            case PropertyTypes.Vector2:
                                Value = (Vector2)Value + (Vector2)oldValue;
                                break;
                            case PropertyTypes.Vector3:
                                Value = (Vector3)Value + (Vector3)oldValue;
                                break;
                        }
                        break;
                    case PropertyApplyTypes.Multiply:
                        switch (propertyType)
                        {
                            case PropertyTypes.Float:
                                Value = (float)Value * (float)oldValue;
                                break;
                            case PropertyTypes.Int:
                                Value = (float)Value * (int)oldValue;
                                break;
                            case PropertyTypes.Vector2:
                                if (oldValue is Vector2)
                                {
                                    Vector2 val = (Vector2)Value;
                                    Vector2 mul = (Vector2)oldValue;
                                    Value = new Vector2(mul.x * val.x, mul.y * val.y);
                                }
                                break;
                            case PropertyTypes.Vector3:
                                if (oldValue is Vector3)
                                {
                                    Vector3 val = (Vector3)Value;
                                    Vector3 mul = (Vector3)oldValue;
                                    Value = new Vector3(mul.x * val.x, mul.y * val.y, mul.z * val.z);
                                }
                                break;
                        }
                        break;
                }
                return Value;
            }

            public object GetValue(out PropertyTypes type)
            {
                type = PropertyTypes.Float;
                object Value = null;
                if (FValue != 0)
                {
                    Value = FValue;
                }
                else if (IValue != 0)
                {
                    type = PropertyTypes.Int;
                    Value = IValue;
                }
                else if (V2Value != Vector2.zero)
                {
                    type = PropertyTypes.Vector2;
                    Value = V2Value;
                }
                else if (V3Value != Vector3.zero)
                {
                    type = PropertyTypes.Vector3;
                    Value = V3Value;
                }
                return Value;
            }

            public object GetZero(System.Type type)
            {
                if (type == typeof(float))
                    return 0f;
                if (type == typeof(int))
                    return 0;
                if (type == typeof(Vector2))
                    return Vector2.zero;
                if (type == typeof(Vector3))
                    return Vector3.zero;
                return null;
            }
        }

        public void Apply(ShipInstance instance, List<PropertyBlock> GrowthStock, List<string> BlocksInStock, List<string> Exist)
        {
            Exist.Add(Name);
            instance.ApplyPropertiesBlock(this);
            foreach (var hit in OpenNext)
            {
                if (BlocksInStock.Contains(hit))
                {
                    var nexts = GrowthStock.Where(x => x.Name == hit).ToList();
                    if (nexts.Count > 0)
                    {
                        var next = nexts.First();
                        next.Apply(instance, GrowthStock, BlocksInStock, Exist);
                    }
                }
            }
        }

        public void DrawGUI(Rect position, List<PropertyBlock> GrowthStock, List<string> BlocksInStock, Dictionary<Rect, PropertyBlock> dict, string selected, int column = 1)
        {
            /*foreach (var hit in OpposedTo)
            {
                if (Exist.Contains(hit))
                    return;
            }*/

            if(column == 1)
            {
                GUI.color = new Color(0.8f, 0.4f, 0);
                GUI.Box(new Rect(position.x - 1, position.y - 1, position.width + 2, position.height + 2), Name);
            }
            else if (BlocksInStock.Contains(Name))
            {
                GUI.color = Color.green;
                GUI.Box(new Rect(position.x - 1, position.y - 1, position.width + 2, position.height + 2), Name);
            }
            GUI.color = Color.white;
            if (selected == Name)
                GUI.color = Color.gray;
            GUI.Box(position, Name);

            GUI.color = Color.white;

            dict.Add(position, this);

            float cellSize = 100 / column;
            float verticalOffset = (OpenNext.Count - 1) * cellSize / 2;

            foreach (var hit in OpenNext)
            {
                var nexts = GrowthStock.Where(x => x.Name == hit).ToList();
                if (nexts.Count > 0)
                {
                    var next = nexts.First();
                    if (next == null)
                        continue;
                    var newPos = position.Add(Vector2.down * verticalOffset + Vector2.right * (position.width + 100));
                    float halfHeight = position.height * 0.5f;
                    GUIEditor.DrawLine(position.position + new Vector2(position.width, halfHeight), newPos.position + Vector2.up * halfHeight, Color.gray);
                    next.DrawGUI(newPos, GrowthStock, BlocksInStock, dict, selected, column + 1);
                    verticalOffset -= cellSize;
                }
            }
        }

        public PropertyBlock()
        {
            Name = "newBlock";
            Description = string.Empty;
            OpenNext = new List<string>();
            Properties = new List<Property>();
        }
    }

    public class ShipInstance
    {
        public Locomotor Locomotor;
        public Projectile[] Weapon;
        public ProtectionManager Protection;
        public ShipInstance(GameObject Instance)
        {
            Locomotor = Instance.GetComponent<Locomotor>();
            Weapon = Instance.GetComponents<Projectile>();
            Protection = Instance.GetComponent<ProtectionManager>();
        }
        public void ApplyPropertiesBlock(PropertyBlock Block)
        {
            foreach(var hit in Block.Properties)
            {
                switch (hit.ComponentAttachment)
                {
                    case ComponentAttachment.LocomotorEngine:
                        hit.SetProperty(Locomotor as IComponent);
                        break;
                    case ComponentAttachment.LocomotorSuspension:
                        foreach(var supp in Locomotor.Supports)
                            hit.SetProperty(supp as IComponent);
                        break;
                    case ComponentAttachment.WeaponBlocks:
                        if (hit.Group != -1)
                        {
                            foreach (var block in Weapon.Where(x => x.Group == hit.Group))
                            {
                                if (hit.UnderGroup != -1)
                                {
                                    foreach (var Hit in block.Blocks.Where(x => x.Group == hit.UnderGroup))
                                    {
                                        hit.SetProperty(Hit as IComponent);
                                    }
                                }
                                else
                                {
                                    foreach (var Hit in block.Blocks)
                                    {
                                        hit.SetProperty(Hit as IComponent);
                                    }
                                }
                            }
                        }
                        else
                        {
                            foreach (var block in Weapon)
                            {
                                if (hit.UnderGroup != -1)
                                {
                                    foreach (var Hit in block.Blocks.Where(x => x.Group == hit.UnderGroup))
                                    {
                                        hit.SetProperty(Hit as IComponent);
                                    }
                                }
                                else
                                {
                                    foreach (var Hit in block.Blocks)
                                    {
                                        hit.SetProperty(Hit as IComponent);
                                    }
                                }
                            }
                        }
                        break;
                    case ComponentAttachment.WeaponTurels:
                        if (hit.Group != -1)
                        {
                            foreach (var block in Weapon.Where(x => x.Group == hit.Group))
                            {
                                if (hit.UnderGroup != -1)
                                {
                                    foreach (var Hit in block.Turels.Where(x => x.Group == hit.UnderGroup))
                                    {
                                        hit.SetProperty(Hit as IComponent);
                                    }
                                }
                                else
                                {
                                    foreach (var Hit in block.Turels)
                                    {
                                        hit.SetProperty(Hit as IComponent);
                                    }
                                }
                            }
                        }
                        else
                        {
                            foreach (var block in Weapon)
                            {
                                if (hit.UnderGroup != -1)
                                {
                                    foreach (var Hit in block.Turels.Where(x => x.Group == hit.UnderGroup))
                                    {
                                        hit.SetProperty(Hit as IComponent);
                                    }
                                }
                                else
                                {
                                    foreach (var Hit in block.Turels)
                                    {
                                        hit.SetProperty(Hit as IComponent);
                                    }
                                }
                            }
                        }
                        break;
                    case ComponentAttachment.Protection:
                        if (hit.Group != -1)
                        {
                            hit.SetProperty(Protection.ArmorGroups[hit.Group] as IComponent);
                        }
                        else
                        {
                            foreach(var Hit in Protection.ArmorGroups)
                            {
                                hit.SetProperty(Hit as IComponent);
                            }
                        }
                        break;
                }
            }

            /*switch (Block.ComponentAttachment)
            {
                case ComponentAttachment.LocomotorEngine:
                    Locomotor.SetProperties(Block);
                    break;
                case ComponentAttachment.LocomotorSuspension:
                    foreach (var hit in Locomotor.Supports.Where(x => x.Group == Block.Group))
                    {
                        hit.SetProperties(Block);
                    }
                    break;
                case ComponentAttachment.WeaponBlocks:
                    foreach (var hit in Weapon.Where(x => x.Group == Block.Group))
                    {
                        foreach (var Hit in hit.Blocks.Where(x => x.Group == Block.Group))
                        {
                            Hit.SetProperties(Block);
                        }
                    }
                    break;
                case ComponentAttachment.WeaponTurels:
                    foreach (var hit in Weapon.Where(x => x.Group == Block.Group))
                    {
                        foreach (var Hit in hit.Turels.Where(x => x.Group == Block.Group))
                        {
                            Hit.SetProperties(Block);
                        }
                    }
                    break;
                case ComponentAttachment.Protection:
                    Protection.ArmorGroups[Block.Group].SetProperties(Block);
                    break;
            }*/
        }
    }

    public class SorteblePlayers : System.IComparable<SorteblePlayers>
    {
        public PhotonPlayer player;

        public SorteblePlayers(PhotonPlayer player)
        {
            this.player = player;
        }

        public float score { get => player.GetScore(); }

        int System.IComparable<SorteblePlayers>.CompareTo(SorteblePlayers other)
        {
            return score.CompareTo(other.score);
        }
    }

    [System.Serializable]
    public class Account
    {
        public string Name;
        public int Experience;
        public int FreeExperience;
        public Storage.ShipSet ShoosedMachine;
        public Storage.ShipSet AIMachine;

        public Account(string name, int experience, int freeExp, Storage storage)
        {
            Name = name;
            Experience = experience;
            FreeExperience = freeExp;
            ShoosedMachine = storage.Ships.Where(x => x.PrefabName == PlayerPrefs.GetString("PlayerMachine", "MMZ")).SingleOrDefault();
            AIMachine = storage.Ships.Where(x => x.PrefabName == PlayerPrefs.GetString("AIMachine", "MMZ")).SingleOrDefault();

            foreach (var hit in FindObjectsOfType<MonoBehaviourPlus>())
            {
                var ae = hit.GetComponent<IAccountEvents>();
                if (ae != null)
                    ae.OnEnterAccount(this);
            }
        }
    }

    public class SortableObject<T> : System.IComparable<SortableObject<T>>
    {
        public T Value;
        public float parametr;

        public SortableObject(T value, float parametr)
        {
            this.Value = value;
            this.parametr = parametr;
        }

        public int CompareTo(SortableObject<T> obj)
        {
            return parametr.CompareTo(obj.parametr);
        }
    }

    public void ApplyRect(ref List<Rect> List, Rect rect, int ID)
    {
        if (List == null)
            List = new List<Rect>();
        while (List.Count < ID + 1)
                List.Add(rect);

        List[ID] = rect;
    }
    public struct AttantibleFloat
    {
        public float Value;
        float LastValue;
        public UnityEvent OnChainge;

        public void Set(float value)
        {
            Value = value;
            if (LastValue != value)
            {
                OnChainge.Invoke();
                LastValue = value;
            }
        }
    }

    public Vector3 DistantPoint(Vector3 from, Vector3 to, float distance)
    {
        Vector3 dir = to - from;
        dir.Normalize();
        return to - dir * distance;
    }

    public Vector3 WorldPointToTerrainVertex(Vector3 point, Terrain terrain, bool floor = true)
    {
        Vector3 rp = terrain.transform.InverseTransformPoint(point);
        int coordX = (int)(rp.x / (terrain.terrainData.bounds.size.x / terrain.terrainData.heightmapWidth / terrain.transform.localScale.x) + (floor ? 0f : 0.5f));
        int coordZ = (int)(rp.z / (terrain.terrainData.bounds.size.z / terrain.terrainData.heightmapWidth / terrain.transform.localScale.z) + (floor ? 0f : 0.5f));
        rp = new Vector3(coordX, point.y, coordZ);
        return rp;
    }

    public Vector3 WorldPointToTerrainPoint(Vector3 point, Terrain terrain)
    {
        Vector3 rp = terrain.transform.InverseTransformPoint(point);
        float coordX = (rp.x / (terrain.terrainData.bounds.size.x / terrain.terrainData.heightmapWidth)) / terrain.transform.localScale.x;
        float coordZ = (rp.z / (terrain.terrainData.bounds.size.z / terrain.terrainData.heightmapWidth)) / terrain.transform.localScale.z;
        rp = new Vector3(coordX, point.y, coordZ);
        return rp;
    }

    public float RGBToHeight(Color color)
    {
        float add = 0.0039f * (color.r == 1 ? 1 : 0 + color.g == 1 ? 1 : 0);
        return (color.r + color.g + color.b + add) / 3f;
    }


    public static Rect RightUpCorner(Rect rect)
    {
        return new Rect(Screen.width - rect.x - rect.width, rect.y, rect.width, rect.height);
    }
    public static Rect RightDownCorner(Rect rect)
    {
        return new Rect(Screen.width - rect.x - rect.width, Screen.height - rect.y - rect.height, rect.width, rect.height);
    }
    public static Rect LeftDownCorner(Rect rect)
    {
        return new Rect(rect.x, Screen.height - rect.y - rect.height, rect.width, rect.height);
    }
    public static Rect MiddleCenter(Vector2 Size)
    {
        return new Rect((Screen.width - Size.x) / 2, (Screen.height - Size.y) / 2, Size.x, Size.y);
    }

    static public Texture2D LoadIcon(string name)
    {
        return Resources.Load<Texture2D>(name);
    }

    public static int TypeID(object en)
    {
        return (int)System.Convert.ChangeType(en, System.Enum.GetUnderlyingType(en.GetType())); ;
    }
    public static string Seconds2TimeHours(float seconds, int precision = 0)
    {
        string strTime = "";
        int hr = ((int)(seconds / 3600) % 24);
        int mm = ((int)(seconds / 60) % 60);
        float ss = seconds % 60;
        switch (precision)
        {
            case -1:
                strTime = string.Format("{0:00}:{1:00}", hr, mm);
                break;
            case 0:
                strTime = string.Format("{0:00}:{1:00}:{2:00}", hr, mm, ss);
                break;
            case 1:
                strTime = string.Format("{0:00}:{1:00}:{2:00.0}", hr, mm, ss);
                break;
            case 2:
                strTime = string.Format("{0:00}:{1:00}:{2:00.00}", hr, mm, ss);
                break;
            case 3:
                strTime = string.Format("{0:00}:{1:00}:{2:00.000}", hr, mm, ss);
                break;
            default:
                strTime = string.Format("{0:00}:{1:00}:{2:00}", hr, mm, ss);
                break;
        }
        return strTime;
    }
    public static string Seconds2TimeMinuts(float seconds)
    {
        string strTime = "";
        int mm = ((int)(seconds / 60) % 60);
        float ss = seconds % 60;
        strTime = string.Format("{0:00}:{1:00}", mm, ss);
        return strTime;
    }

    // Создание листа ВСЕХ детей текущего трансформа 
    public List<GameObject> ChildrensAll(Transform T)
    {
        List<GameObject> retval = new List<GameObject>();
        foreach (Transform child in T.GetComponentsInChildren<Transform>(true))
        {
            if (child != this)
            {
                retval.Add(child.gameObject);
                //	if (isDebug) Debug.Log (this + string.Format (" Childrens ---> [{0}]", child));
            }
        }
        return retval;
    }

    // Создания листа ВСЕХ детей трансформа T isActive=true - только активные, isActive=False - все 
    public List<GameObject> ChildrensAll(Transform T, bool isActive)
    {
        List<GameObject> retval = new List<GameObject>();
        foreach (Transform child in T.GetComponentsInChildren<Transform>(true))
        {
            if (child != this && (child.gameObject.activeSelf || !isActive))
            {
                retval.Add(child.gameObject);
                //if (isDebug) Debug.Log (this + string.Format (" Childrens ---> [{0}]", child));
            }
        }
        return retval;
    }

    // Создание словаря детей трансформа T
    public Dictionary<string, GameObject> DictChildrensAll(Transform T)
    {
        Dictionary<string, GameObject> retval = new Dictionary<string, GameObject>();
        foreach (Transform child in T.GetComponentsInChildren<Transform>(true))
        {
            if (child != this)
            {
                string key = child.name;
                // Избавляемся от дублирования, добавляя в случае дублей парент
                if (retval.ContainsKey(key))
                    key = child.parent.name + "." + key;

                if (!retval.ContainsKey(key))
                    retval.Add(key, child.gameObject);
                //	if (isDebug) Debug.Log (this + string.Format (" Childrens ---> [{0}]", child));
            }
        }
        return retval;
    }

    public static float Side(float value)
    {
        if (value == 0f)
            return 0f;
        return value / Mathf.Abs(value);
    }


    public Vector2 ClampDistance(Vector2 anchor, Vector2 position, float Min, float Max)
    {
        Vector2 vector = position - anchor;
        return anchor + Mathf.Clamp(vector.magnitude, Min, Max) * vector.normalized;
    }

    public Vector3 ClampDistance(Vector3 position, float Min, float Max)
    {
        return Mathf.Clamp(position.magnitude, Min, Max) * position.normalized;
    }

    public Vector2 ClampDistance(Vector2 position, float Min, float Max)
    {
        return Mathf.Clamp(position.magnitude, Min, Max) * position.normalized;
    }

    public static Vector3 Multiply(Vector3 left, Vector3 right)
    {
        return new Vector3(left.x * right.x, left.y * right.y, left.z * right.z);
    }

    public static bool RotateTuel(Transform turret, Transform turel, Transform gunpoint, float MinHorizontal, float MaxHorizontal, float MinVertical, float MaxVertical, Vector3 point, float horizontalSpeed, float verticalSpeed, bool Radial, float Angle = 1f)
    {
        if (Side(turret.InverseTransformPoint(point).x) == Side(turret.InverseTransformDirection(turel.forward).x) && !Radial || Radial)
        {
            turel.localRotation = Quaternion.RotateTowards(turel.localRotation, Quaternion.LookRotation(Vector3.ProjectOnPlane(turel.parent.InverseTransformDirection(point - turel.position), Vector3.up)), Time.deltaTime * horizontalSpeed);
        }
        else
        {
            if (turel.parent.InverseTransformDirection(turel.forward).x > 0f)
            {
                turel.localRotation = Quaternion.RotateTowards(turel.localRotation, Quaternion.LookRotation(Vector3.left * 0.1f + Vector3.forward * 4f), Time.deltaTime * horizontalSpeed);
            }
            else
            {
                turel.localRotation = Quaternion.RotateTowards(turel.localRotation, Quaternion.LookRotation(Vector3.right * 0.1f + Vector3.forward * 4f), Time.deltaTime * horizontalSpeed);
            }
        }
        if (!Radial)
        {
            float leay = turel.localRotation.eulerAngles.y;
            if (leay >= 180f)
                leay -= 360f;
            turel.localEulerAngles = Vector3.up * Mathf.Clamp(leay, MinHorizontal, MaxHorizontal);
        }
        gunpoint.localRotation = Quaternion.RotateTowards(gunpoint.localRotation, Quaternion.LookRotation(Vector3.ProjectOnPlane(turel.InverseTransformDirection(point - gunpoint.position), Vector3.right)), Time.deltaTime * verticalSpeed);

        float leax = gunpoint.localEulerAngles.x;
        if (leax >= 180f)
            leax -= 360f;
        gunpoint.localEulerAngles = Vector3.right * Mathf.Clamp(leax, -MaxVertical, -MinVertical);

        return Vector3.Angle(gunpoint.forward, point - gunpoint.position) < Angle;
    }



    public bool PointInZone(Transform root, Vector3 point, float minAngle, float maxAngle, float distance = Mathf.Infinity)
    {
        Vector3 ProjectedPoint = Vector3.ProjectOnPlane(root.InverseTransformPoint(point), Vector3.up);
        return (Vector3.Angle(Vector3.forward, ProjectedPoint) < (ProjectedPoint.x < 0 ? Mathf.Abs(minAngle) : maxAngle)) && (point - root.position).magnitude < distance;
    }
    public bool PointInZone(Transform root, Vector3 point, float distance)
    {
        return (point - root.position).magnitude < distance;
    }

    public Vector3 SetDefaultPreemption(Vector3 EnamyCentr, Vector3 EnamyVelocity, Vector3 MyCentr, Vector3 MyVelocity, float ChSp)
    {
        return EnamyCentr + (EnamyVelocity - MyVelocity) * Vector3.Distance(MyCentr, EnamyCentr) / ChSp;
    }

    public static Vector3 RotateAround(Vector3 point, Vector3 Angle)
    {
        point = Quaternion.Euler(0f, Angle.x, 0f) * point;
        Vector3 projection = Vector3.ProjectOnPlane(point.normalized, Vector3.up);
        float angleUp = Mathf.Atan2(point.normalized.y, projection.magnitude);
        Vector3 plane = projection.normalized * point.magnitude;
        Vector3 wanted = plane * Mathf.Cos(angleUp + Angle.y * Mathf.Deg2Rad) + Vector3.up * point.magnitude * Mathf.Sin(angleUp + Angle.y * Mathf.Deg2Rad);
        return wanted;
    }

    public static float ColorToFloat(Color32 value, bool r = true, bool g = true, bool b = true)
    {
        int devider = (r ? 1 : 0) + (g ? 1 : 0) + (b ? 1 : 0);
        return (float)((r ? value.r : 0) + (g ? value.g : 0) + (b ? value.b : 0)) / (255 * devider);
    }
    public static Vector3 SplineLerp(Vector3 start, Vector3 corner, Vector3 end, float value)
    {
        Vector3 c = Vector3.Lerp(start, corner, value);
        Vector3 d = Vector3.Lerp(corner, end, value);
        Vector3 Value = Vector3.Lerp(c, d, value);
        return Value;
    }

    public static Vector3 SplineLerp4(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float value)
    {
        Vector3 x = Vector3.Lerp(a, b, value);
        Vector3 y = Vector3.Lerp(b, c, value);
        Vector3 z = Vector3.Lerp(c, d, value);

        Vector3 n = Vector3.Lerp(x, y, value);
        Vector3 m = Vector3.Lerp(y, z, value);
        return Vector3.Lerp(n, m, value);
    }

    public static Vector3 CornerLerp(Vector3 start, Vector3 corner, Vector3 end, float distance)
    {
        float AB = Vector3.Distance(start, corner);
        float BC = Vector3.Distance(end, corner);
        if (distance < AB)
            return start + (corner - start).normalized * distance;
        else
            return corner + (end - corner).normalized * (distance - AB);
    }

    public static float GetCurveLength(Curve curve)
    {
        List<Vector3> path = new List<Vector3>();
        int A = 0, B, C;
        curve.GetConnections(A, out B, out C);
        List<int> p = curve.GetInterpolatedLine(A, B, false);

        for (int i = 0; i < p.Count; i++)
        {
            path.Add(curve.InterpolatedAnchors[p[i]]);
        }

        Vector3 point = path[0];
        float d = 0f;
        for (int i = 1; i < path.Count; i++)
        {
            d += Vector3.Distance(point, path[i]);
            point = path[i];
        }
        return d;
    }

    public static Vector3 GetCurvePosition(Curve curve, float distance, bool worldDistance = true)
    {
        List<Vector3> path = new List<Vector3>();
        int A = 0, B, C;
        curve.GetConnections(A, out B, out C);
        List<int> p = curve.GetInterpolatedLine(A, B, false);

        for (int i = 0; i < p.Count; i++)
        {
            path.Add(curve.InterpolatedAnchors[p[i]]);
        }

        Vector3 point = path[0];
        if (!worldDistance)
        {
            float d = 0f;
            for (int i = 1; i < path.Count; i++)
            {
                d += Vector3.Distance(point, path[i]);
                point = path[i];
            }
            distance *= d;
            point = path[0];
        }

        for (int i = 1; i < path.Count; i++)
        {
            Vector3 last = point;
            point = Vector3.MoveTowards(point, path[i], distance);
            distance -= (point - last).magnitude;
            if (distance <= 0.001f)
                break;
        }
        return point;
    }

    public static Vector3 GetCurveDirection(Curve curve, float distance, bool worldDistance = true)
    {
        List<Vector3> path = new List<Vector3>();
        int A = 0, B, C;
        curve.GetConnections(A, out B, out C);
        List<int> p = curve.GetInterpolatedLine(A, B, false);

        for (int i = 0; i < p.Count; i++)
        {
            path.Add(curve.InterpolatedAnchors[p[i]]);
        }

        Vector3 point = path[0];
        if (!worldDistance)
        {
            float d = 0f;
            for (int i = 1; i < path.Count; i++)
            {
                d += Vector3.Distance(point, path[i]);
                point = path[i];
            }
            distance *= d;
            point = path[0];
        }

        for (int i = 1; i < path.Count; i++)
        {
            Vector3 last = point;
            point = Vector3.MoveTowards(point, path[i], distance);
            distance -= (point - last).magnitude;
            if (distance <= 0.001f)
            {
                point = (point - last).normalized;
                break;
            }
        }
        return point;
    }

    public static Vector3 FollowSpline(Vector3 start, Vector3 direction, Vector3 end, Vector3 endDirection, float distance, int precision, bool worldDistance = true)
    {
        List<Vector3> interpolated = new List<Vector3>();
        int i;
        float dis = 0f;
        for (i = 0; i <= precision; i++)
        {
            interpolated.Add(SplineLerp4(start, start + direction, end - endDirection, end, (float)i / precision));
            if (i > 0)
                dis += (interpolated[i] - interpolated[i - 1]).magnitude;
        }
        if (!worldDistance)
            distance *= dis;
        if (dis < distance)
            return end;
        i = 1;
        Vector3 point = start;
        while (distance > 0.01f)
        {
            Vector3 last = point;
            point = Vector3.MoveTowards(point, interpolated[i++], distance);
            distance -= (point - last).magnitude;
            if (i == precision - 1)
                break;
        }
        return point;
    }

    public static bool MeshRaycast(Transform Tr, Mesh mesh, Vector3 position, Vector3 direction, float radius, out RaycastHit Hit)
    {
        Hit = new RaycastHit();
        Vector3[] points = new Vector3[3];
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        float rsqr = radius * radius;
        bool hit = false;
        for (int i = 0; i < mesh.triangles.Length - 2; i += 3)
        {
            points[0] = Tr.localToWorldMatrix.MultiplyPoint(vertices[triangles[i]]);
            points[1] = Tr.localToWorldMatrix.MultiplyPoint(vertices[triangles[i + 1]]);
            points[2] = Tr.localToWorldMatrix.MultiplyPoint(vertices[triangles[i + 2]]);

            if (rsqr > Vector3.SqrMagnitude(points[0] - position) || rsqr > Vector3.SqrMagnitude(points[1] - position) || rsqr > Vector3.SqrMagnitude(points[2] - position))
            {
                Plane p = new Plane(points[0], points[1], points[2]);
                float dist = 0f;
                p.Raycast(new Ray(position, direction), out dist);
                if (dist > 0)
                {
                    Vector3 point = position + direction * dist;

                    Matrix4x4 ed1 = Matrix4x4.Inverse(Matrix4x4.LookAt(points[0], points[1], points[2] - points[0]));
                    Matrix4x4 ed2 = Matrix4x4.Inverse(Matrix4x4.LookAt(points[1], points[2], points[0] - points[1]));
                    Matrix4x4 ed3 = Matrix4x4.Inverse(Matrix4x4.LookAt(points[2], points[0], points[1] - points[2]));

                    if (ed1.MultiplyPoint(point).y > 0 && ed2.MultiplyPoint(point).y > 0 && ed3.MultiplyPoint(point).y > 0)
                    {
                        if (Hit.distance > dist || Hit.distance == 0)
                        {
                            Hit.point = point;
                            Hit.normal = p.normal;
                            Hit.distance = dist;
                        }
                        hit = true;
                    }
                }
            }
        }
        return hit;
    }
}