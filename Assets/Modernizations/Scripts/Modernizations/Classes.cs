using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static GUIEditor;
using static CustumGUILayout;

public interface INamedID
{
    string GetName();
    string GetID();
}

namespace Modernizations
{
    public interface IModifiable
    {
        int GetGroup();
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
        Vector3 = 3,
        Bool = 4,
        String = 5
    }

    public static class ModifiableUtility
    {
        public static readonly Type[] PropertyTypes = new Type[]
        {
            typeof(float),
            typeof(int),
            typeof(Vector2),
            typeof(Vector3),
            typeof(bool),
            typeof(string)
        };

        public static void GetBranch(IModifiable root, List<Tuple<string, IModifiable>> branch, string lastPath)
        {
            branch.Add(new Tuple<string, IModifiable>(lastPath, root));
            string dot = string.Empty;
            if (lastPath != string.Empty)
                dot = ".";
            var fields = root.GetType().GetFields();
            foreach (var field in fields)
            {
                var value = field.GetValue(root);
                if (value == null)
                    return;
                if (value.GetType().IsArray)
                {
                    IModifiable[] arr = value as IModifiable[];
                    foreach (var hit in arr)
                    {
                        GetBranch(hit, branch, lastPath + dot + field.Name);
                    }
                }
                else if (field.FieldType.GetInterfaces().Contains(typeof(IModifiable)))
                {
                    GetBranch(value as IModifiable, branch, lastPath + dot + field.Name);
                }
            }
        }
        public static Type GetFieldType(IModifiable mod, string name)
        {
            Type type = mod.GetType();
            var field = type.GetField(name);
            return field.GetType();
        }
        public static void SetField(IModifiable mod, string name, object value)
        {
            Type type = mod.GetType();

            var field = type.GetField(name);

            if (field != null)
            {
                field.SetValue(mod, value);
            }
        }
        public static object GetField(IModifiable mod, string name)
        {
            System.Type type = mod.GetType();
            var field = type.GetField(name);
            return field.GetValue(mod);
        }
    }

    [Serializable]
    public class SavedProperty : INamedID
    {
        public string Name;
        public string FieldName;
        public string Description;
        public PropertyTypes Type;

        public SavedProperty(string name, string description, Type type)
        {
            Name = name;
            Type = GetType(type);
            var spl = name.Split(new char[] { '.' });
            FieldName = spl[spl.Length - 1];
            Description = description;
        }

        public string GetID()
        {
            return GetHashCode().ToString();
        }

        public string GetName()
        {
            return Name;
        }

        PropertyTypes GetType(Type type)
        {
            for (int i = 0; i < ModifiableUtility.PropertyTypes.Length; i++)
            {
                if (type == ModifiableUtility.PropertyTypes[i])
                    return (PropertyTypes)Enum.ToObject(typeof(PropertyTypes), i);
            }
            return PropertyTypes.String;
        }
    }

    [Serializable]
    public class ItemSet
    {
        public string Name;
        public List<string> Stock;
        public List<PropertyBlock> Modernizations;
        public List<Resource> Resources;

        public void ApplyGrowth(GameObject target, bool ApplyToChildrens = false)
        {
            ApplyGrowth(target, Stock, ApplyToChildrens);
        }

        public List<PropertyBlock> GetModernizationRoots(PropertyBlock Modernization)
        {
            List<PropertyBlock> frame = new List<PropertyBlock>();
            List<PropertyBlock> lastFrame = new List<PropertyBlock>();
            List<PropertyBlock> result = new List<PropertyBlock>();

            frame.Add(Modernization);

            int newCount = frame.Count;
            int iterations = 0;
            while (newCount > 0 && iterations++ < 100)
            {
                if(iterations > 199)
                {
                    Debug.Log("Error. Iterations of \"GetModernizationRoots\" is more then must be");
                }
                newCount = 0;
                foreach (var hit in frame)
                {
                    List<PropertyBlock> add = GetModernizationParents(hit);
                    if (add.Count == 0)
                    {
                        result.Add(hit);
                    }
                    else
                    {
                        newCount += add.Count;
                        lastFrame.AddRange(add);
                    }
                }
                frame = lastFrame;
                lastFrame = new List<PropertyBlock>();
            }
            string list = string.Empty;
            foreach (var hit in result)
                list += hit.Name + ", ";

            return result;
        }

        public List<PropertyBlock> GetModernizationParents(PropertyBlock Modernization)
        {
            return Modernizations.Where(x => x.Connections.Contains(Modernization.GetID())).ToList();
        }

        public void ApplyGrowth(GameObject target, List<string> configure, bool ApplyToChildrens = false)
        {
            List<string> Exist = new List<string>();
            var components = ApplyToChildrens ? target.GetComponentsInChildren<IModifiable>() : target.GetComponents<IModifiable>();

            var Special = Modernizations.Where(x => Modernizations.Count(n => n.Connections.Contains(x.GetID())) == 0 && configure.Contains(x.GetID())).ToList();

            foreach (var Hit in Modernizations)
            {
                if (Hit.IsDefault)
                {
                    Hit.ApplyBranch(components, null, Modernizations, configure, Exist);
                }
            }

            foreach (var Hit in Special)
            {
                Hit.ApplyBranch(components, null, Modernizations, configure, Exist);
            }
        }

        public ItemSet Clone()
        {
            var clone = new ItemSet();
            clone.Name = Name + "(clone)";
            clone.Stock = new List<string>();
            clone.Resources = new List<Resource>();
            foreach(var hit in Storage.Instance.SelfResources)
            {
                clone.Resources.Add(new Resource { Name = hit.Name, ResourceType = hit.ResourceType, Count = 0 });
            }
            foreach (var hit in Stock)
            {
                clone.Stock.Add(hit);
            }
            clone.Modernizations = Modernizations;
            return clone;
        }
    }

    [Serializable]
    public class PropertyBlock : INamedID
    {
        [Header("---------------")]
        public string Name;
        [TextArea]
        public string Description;
        public Sprite Sprite;
        public bool IsDefault;
        [HideInInspector]
        public Rect rect;
        [HideInInspector]
        public Vector2 position;
        float delta;
        bool mDown = false;
        bool drag = false;
        public bool AvailableBySingle; //TODO
        public string id;
        [Header("---------------")]
        public List<ResourceDependence> GlobalResourceDependences;
        public List<ResourceDependence> SelfResourceDependences;
        public List<string> Connections;
        [Header("Properties")]
        public List<Property> Properties;

        [Serializable]
        public class Property : INamedID
        {
            public SavedProperty property;
            public string groupName;
            [HideInInspector]
            public float id;
            public PropertyApplyTypes ApplyType;
            public int Group;
            public object Value
            {
                get
                {
                    return GetValue();
                }
                set
                {
                    SetValue(value);
                }
            }
#if UNITY_EDITOR
            [HideInInspector]
            public float fVal = 0f;
            [HideInInspector]
            public int iVal = 0;
            [HideInInspector]
            public Vector2 v2Val = Vector2.zero;
            [HideInInspector]
            public Vector3 v3Val = Vector3.zero;
            [HideInInspector]
            public bool bVal = false;
            [HideInInspector]
            public string sVal = "";
#endif

            object value;
            [SerializeField, HideInInspector]
            string _value;

#if UNITY_EDITOR
            void GetDefault()
            {
                switch (property.Type)
                {
                    case PropertyTypes.Float:
                        _value = fVal.ToString();
                        break;
                    case PropertyTypes.Int:
                        _value = iVal.ToString();
                        break;
                    case PropertyTypes.Vector2:
                        _value = JsonUtility.ToJson(v2Val);
                        break;
                    case PropertyTypes.Vector3:
                        _value = JsonUtility.ToJson(v3Val);
                        break;
                    case PropertyTypes.Bool:
                        _value = bVal.ToString();
                        break;
                    case PropertyTypes.String:
                        _value = sVal;
                        break;
                }
            }
            void SetDefault()
            {
                switch (property.Type)
                {
                    case PropertyTypes.Float:
                        fVal = float.Parse(_value);
                        break;
                    case PropertyTypes.Int:
                        iVal = int.Parse(_value);
                        break;
                    case PropertyTypes.Vector2:
                        v2Val = JsonUtility.FromJson<Vector2>(_value);
                        break;
                    case PropertyTypes.Vector3:
                        v3Val = JsonUtility.FromJson<Vector3>(_value);
                        break;
                    case PropertyTypes.Bool:
                        bVal = bool.Parse(_value);
                        break;
                    case PropertyTypes.String:
                        sVal = _value;
                        break;
                }
            }
#endif
            public string GetDescription()
            {
                string prefix = string.Empty;
                string postfix = string.Empty;
                string value = Value.ToString();
                switch (ApplyType)
                {
                    case PropertyApplyTypes.Apply:
                        prefix = " = ";
                        break;
                    case PropertyApplyTypes.Add:
                        prefix = " + ";
                        break;
                    case PropertyApplyTypes.Multiply:
                        prefix = " + ";
                        postfix = "%";
                        switch (property.Type)
                        {
                            case PropertyTypes.Float:
                                value = ((float)Value * 100 - 100).ToString();
                                break;
                            case PropertyTypes.Int:
                                value = ((int)Value * 100 - 100).ToString();
                                break;
                            case PropertyTypes.Vector2:
                                value = ((Vector2)Value * 100 - Vector2.one * 100).ToString();
                                break;
                            case PropertyTypes.Vector3:
                                value = ((Vector3)Value * 100 - Vector3.one * 100).ToString();
                                break;
                        }
                        break;
                }
                return prefix + value + postfix;
            }

            object GetValue()
            {
#if UNITY_EDITOR
                if (_value == null)
                    GetDefault();
#endif

                switch (property.Type)
                {
                    case PropertyTypes.Float:
                        value = float.Parse(_value);
                        break;
                    case PropertyTypes.Int:
                        value = int.Parse(_value);
                        break;
                    case PropertyTypes.Vector2:
                        value = JsonUtility.FromJson<Vector2>(_value);
                        break;
                    case PropertyTypes.Vector3:
                        value = JsonUtility.FromJson<Vector3>(_value);
                        break;
                    case PropertyTypes.Bool:
                        value = bool.Parse(_value);
                        break;
                    case PropertyTypes.String:
                        value = _value;
                        break;
                }
                return value;
            }
            void SetValue(object value)
            {
#if UNITY_EDITOR
                if (value == null)
                {
                    GetDefault();
                    return;
                }
#endif

                string val = string.Empty;
                switch (property.Type)
                {
                    case PropertyTypes.Float:
                        val = ((float)value).ToString();
                        break;
                    case PropertyTypes.Int:
                        val = ((int)value).ToString();
                        break;
                    case PropertyTypes.Vector2:
                        val = JsonUtility.ToJson((Vector2)value);
                        break;
                    case PropertyTypes.Vector3:
                        val = JsonUtility.ToJson((Vector3)value);
                        break;
                    case PropertyTypes.Bool:
                        val = ((bool)value).ToString();
                        break;
                    case PropertyTypes.String:
                        val = (string)value;
                        break;
                }
                _value = val;
#if UNITY_EDITOR
                SetDefault();
#endif
            }

            public Property(SavedProperty property)
            {
                id = UnityEngine.Random.Range(0f, 1000f);
                this.property = property;
                Value = GetZero(ModifiableUtility.PropertyTypes[(int)property.Type]);
            }

            public void Apply(IModifiable component, Property root = null)
            {
                object value = ModifiableUtility.GetField(component, property.FieldName);
                object rootValue = null;
                if (root != null)
                {
                    rootValue = root.GetValue();
                }
                value = GetValue(value, rootValue);
                ModifiableUtility.SetField(component, property.FieldName, value);
            }

            public object GetValue(object oldValue, object rootValue)
            {
                object value = Value;
                object add = null;

                switch (ApplyType)
                {
                    case PropertyApplyTypes.Add:
                        switch (property.Type)
                        {
                            case PropertyTypes.Float:
                                value = (float)Value + (float)oldValue;
                                break;
                            case PropertyTypes.Int:
                                value = (float)Value + (int)oldValue;
                                break;
                            case PropertyTypes.Vector2:
                                value = (Vector2)Value + (Vector2)oldValue;
                                break;
                            case PropertyTypes.Vector3:
                                value = (Vector3)Value + (Vector3)oldValue;
                                break;
                            case PropertyTypes.Bool:
                                value = !(bool)oldValue;
                                break;
                            case PropertyTypes.String:
                                value = (string)oldValue + (string)Value;
                                break;
                        }
                        break;
                    case PropertyApplyTypes.Multiply:
                        switch (property.Type)
                        {
                            case PropertyTypes.Float:
                                add = ((float)Value - 1) * (float)oldValue;
                                if (rootValue != null)
                                {
                                    add = ((float)Value - 1) * (float)rootValue;
                                }
                                value = (float)oldValue + (float)add;
                                break;
                            case PropertyTypes.Int:
                                add = ((float)Value - 1) * (float)oldValue;
                                if (rootValue != null)
                                {
                                    add = ((float)Value - 1) * (float)rootValue;
                                }
                                value = (int)oldValue + (float)add;
                                break;
                            case PropertyTypes.Vector2:
                                Vector2 val = (Vector2)Value - Vector2.one;
                                Vector2 mul = (Vector2)oldValue;
                                add = new Vector2(mul.x * val.x, mul.y * val.y);
                                if (rootValue != null)
                                {
                                    mul = (Vector2)rootValue;
                                    add = new Vector2(mul.x * val.x, mul.y * val.y);
                                }
                                value = (Vector2)oldValue + (Vector2)add;
                                break;
                            case PropertyTypes.Vector3:
                                Vector3 _val = (Vector3)Value - Vector3.one;
                                Vector3 _mul = (Vector3)oldValue;
                                add = new Vector3(_mul.x * _val.x, _mul.y * _val.y, _mul.z * _val.z);
                                if (rootValue != null)
                                {
                                    _mul = (Vector3)rootValue;
                                    add = new Vector3(_mul.x * _val.x, _mul.y * _val.y, _mul.z * _val.z);
                                }
                                value = (Vector3)oldValue + (Vector3)add;
                                break;
                            case PropertyTypes.Bool:
                                value = !(bool)rootValue;
                                break;
                            case PropertyTypes.String:
                                value = (string)rootValue + (string)Value;
                                break;
                        }
                        break;
                }
                return value;
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
                if (type == typeof(bool))
                    return false;
                if (type == typeof(string))
                    return string.Empty;
                return null;
            }

            public string GetName()
            {
                return property.Name;
            }

            public string GetID()
            {
                return id.ToString();
            }

            public Property Clone()
            {
                Property clone = (Property)this.MemberwiseClone();
                clone.id = UnityEngine.Random.Range(0f, 1000f);
                return clone;
            }
        }

        [Serializable]
        public class ResourceDependence
        {
            public string ResourceName;
            public int Cost;
            public ResourceDependence Clone()
            {
                return (ResourceDependence)this.MemberwiseClone();
            }
        }

        public void ApplyBranch(IModifiable[] components, PropertyBlock root, List<PropertyBlock> Modernizations, List<string> configure, List<string> Exist)
        {
            string ID = GetID();

            if (!configure.Contains(ID) && !IsDefault)
                return;

            if (!Exist.Contains(ID))
            {
                Apply(components, root);
                Exist.Add(ID);
            }

            if (root == null)
            {
                root = this;
            }

            foreach (var next in Connections)
            {
                if (configure.Contains(next))
                {
                    var Next = Modernizations.FirstOrDefault(x => x.GetID() == next);
                    Next.ApplyBranch(components, root, Modernizations, configure, Exist);
                }
            }
        }

        public void Apply(IModifiable[] components, PropertyBlock root = null)
        {
            foreach (var property in Properties)
            {
                ApplyProperty(components, property, root);
            }
        }

        public void ApplyProperty(IModifiable[] components, Property property, PropertyBlock root = null)
        {
            List<string> path = property.property.Name.Split(new char[] { '.' }).ToList();
            var filtred = components.Where(x => x.GetType().Name == path[0]).ToArray();
            string clampedPath = string.Empty;

            if (path.Count > 2)
            {
                for (int i = 1; i < path.Count - 2; i++)
                {
                    clampedPath += path[i] + ".";
                }
                clampedPath += path[path.Count - 2];

            }
            foreach (var hit in filtred)
            {
                    //Debug.Log(property.GetName() + " group is " + property.Group + ". " + hit + " group is " + hit.GetGroup() + ". ");
                if (property.Group == hit.GetGroup() || property.Group == -1)
                {
                    List<Tuple<string, IModifiable>> Branch = new List<Tuple<string, IModifiable>>();
                    ModifiableUtility.GetBranch(hit, Branch, string.Empty);

                    foreach (var mod in Branch.Where(x => x.Item1 == clampedPath))
                    {
                        property.Apply(mod.Item2, root == null ? null : root.Properties.FirstOrDefault(x => x.property.Name == property.property.Name && x.Group == property.Group || x.Group == -1));
                    }
                }
            }
        }

        public void DrawGUI(Rect Rect, List<string> BlocksInStock, List<Tuple<Rect, PropertyBlock>> poses, string selected, bool CanDrag)
        {
            Vector2 clampedPos = new Vector2(Mathf.Ceil(position.x / 20) * 20, Mathf.Ceil(position.y / 20) * 20);
            rect = new Rect(clampedPos + Rect.position, Rect.size).Add(-Rect.size * 0.5f);
            Draw(rect, Name, Sprite ? Sprite.texture : null, IsDefault ? new Color(0.8f, 0.4f, 0) : (BlocksInStock.Contains(GetID()) ? Color.green : Color.clear), selected == GetID() ? Color.white * 0.8f : Color.white);
            poses.Add(new Tuple<Rect, PropertyBlock>(rect, this));

            if (!mDown && Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                if (CanDrag)
                {
                    mDown = true;
                }
            }

            if (mDown)
            {
                if (Event.current.type == EventType.MouseDrag)
                {
                    delta += Event.current.delta.magnitude;
                    if (delta > 5)
                        drag = true;
                }
                if (drag)
                {
                    if (Event.current.button == 1)
                    {
                        position += Event.current.delta;
                    }
                    else if (Event.current.button == 0)
                    {
                        DrawLine(rect.center, Event.current.mousePosition, Color.white, true, true);
                    }
                }
                if (Event.current.type == EventType.MouseUp)
                {
                    if (Event.current.button == 0)
                    {
                        StorageEditor.CreateNewConnection(this, Event.current.mousePosition);
                    }
                    drag = false;
                    mDown = false;
                }
            }
        }

        public void DrawLines(List<PropertyBlock> Modernizations)
        {
            Vector2 outPoint = GetPointFromAlignment(rect, Storage.GetInPointAlignment());
            foreach (var hit in Connections)
            {
                var nexts = Modernizations.Where(x => x.GetID() == hit).ToList();
                if (nexts.Count > 0)
                {
                    var next = nexts.First();
                    if (next == null)
                        continue;
                    Vector2 inPoint = GetPointFromAlignment(next.rect, Storage.GetInPointAlignment(), true);
                    DrawLine(outPoint, inPoint, Color.white, true, true);
                }
            }
        }

        public void Draw(Rect rect, string text, Texture2D image, Color border, Color fill)
        {
            GUI.color = border;
            GUI.Box(new Rect(rect.x - 1, rect.y - 1, rect.width + 2, rect.height + 2), "");
            GUI.color = fill;
            GUI.Box(rect, "");
            if (image != null)
                GUI.Label(rect, image, Storage.Instance.Skin.GetStyle("TextureLabel"));
            if (text != string.Empty)
                GUI.Label(rect, text);
            GUI.color = Color.white;
        }

        public void DrawEditor(Rect rect, ref float PropertiesScroll, ItemSet Item)// display selected modernization if enable editor mode
        {
            if (Event.current.type == EventType.ScrollWheel)
            {
                if (rect.Contains(Event.current.mousePosition))
                {
                    PropertiesScroll += Event.current.delta.y * 9;
                    PropertiesScroll = Mathf.Clamp(PropertiesScroll, 0, 800);
                }
            }

            if (StorageEditor.SelectedBlock != this)
            {
                StorageEditor.SelectedBlock = this;
                StorageEditor.SelectedBlockRoots = Item.GetModernizationRoots(this);
                StorageEditor.ResetBlocksList();
                StorageEditor.CheckCanExplore();
            }
            if(StorageEditor.BlocksIDs == null)
                StorageEditor.ResetBlocksList();

            List<INamedID> BlocksIDs = StorageEditor.BlocksIDs;

            GUIStyle EditorLabelStyle = Storage.Instance.Skin.GetStyle("EditorLabel");
            if (EditorLabelStyle == null)
                EditorLabelStyle = GUI.skin.label;

            GUI.Box(rect, "Block Setup");
            GUI.BeginClip(new Rect(rect.x, rect.y + 20, rect.width, rect.height - 20));
            SetDefault(new Rect(5, -PropertiesScroll, 0, 0));
            GUI.color = Color.white;
            GUI.backgroundColor = Color.white;
            GUI.Label(GetNextWithOffset(0, 15, 85, 22), "Block:", EditorLabelStyle);
            string _name = Name;
            _name = GUI.TextField(GetNextWithoutOffset(85, 0, 300, 22), _name);
            if (_name != Name)
            {
                foreach (var item in Item.Modernizations)
                {
                    for (int n = 0; n < item.Connections.Count; n++)
                    {
                        if (item.Connections[n] == Name)
                        {
                            item.Connections[n] = _name;
                        }
                    }
                }
                Name = _name;
            }

            GUI.Label(GetNextWithOffset(0, 22, 85, 22), "Description:", EditorLabelStyle);
            Description = GUI.TextArea(GetNextWithoutOffset(85, 0, 300, 44), Description);
            GetNextWithOffset(0, 22, 0, 0);
#if UNITY_EDITOR

            GUI.Label(GetNextWithOffset(0, 22, 85, 22), "Sprite:", EditorLabelStyle);
            GUISkin skin = GUI.skin;
            GUI.skin = null;
            Sprite = (Sprite)EditorGUI.ObjectField(GetNextWithoutOffset(85, 0, 100, 22), Sprite, typeof(Sprite), true);
            GUI.skin = skin;
#endif

            GUI.Label(GetNextWithOffset(0, 22, 150, 22), "IsDefault:", EditorLabelStyle);
            IsDefault = GUI.Toggle(GetNextWithoutOffset(150, 0, 280, 22), IsDefault, "");

            GUI.Label(GetNextWithOffset(0, 22, 150, 22), "AvailableBySingle:", EditorLabelStyle);
            AvailableBySingle = GUI.Toggle(GetNextWithoutOffset(150, 0, 280, 22), AvailableBySingle, "");

            if (!IsDefault)
            {
                GUI.Label(GetNextWithOffset(0, 22, 300, 22), "Global resources dependence:", EditorLabelStyle);
                if (Storage.Instance.GlobalResources.Count == 0)
                {
                    GUI.Label(GetNextWithOffset(0, 22, 350, 22), "<color=red>First add the resource to Storage.GlobalResources.</color>", EditorLabelStyle);
                }
                else
                {
                    List<string> resNames = new List<string>();
                    foreach (var hit in Storage.Instance.GlobalResources)
                        resNames.Add(hit.Name);

                    foreach (var res in GlobalResourceDependences)
                    {
                        res.ResourceName = PopUp(GetNextWithOffset(0, 25, 170, 22), res.ResourceName, resNames, Item.Name + id + res.GetHashCode() + "Global");
                        GUI.Label(GetNextWithoutOffset(175, 0, 40, 22), "Cost:", EditorLabelStyle);
                        res.Cost = (int)FloatField(GetNextWithoutOffset(215, 0, 150, 22), res.Cost, Item.Name + id + res.GetHashCode() + "GlobalCost");
                        if (GUI.Button(GetNextWithoutOffset(365, 0, 20, 22), "-"))
                        {
                            GlobalResourceDependences.Remove(res);
                            break;
                        }
                    }
                    if (GlobalResourceDependences.Count < Storage.Instance.GlobalResources.Count)
                    {
                        if (GUI.Button(GetNextWithOffset(0, 22, 80, 22), "++Add++"))
                        {
                            GlobalResourceDependences.Add(new PropertyBlock.ResourceDependence());
                        }
                    }
                }
                GUI.Label(GetNextWithOffset(0, 33, 300, 22), "Self resources dependence:", EditorLabelStyle);
                if (Item.Resources.Count == 0)
                {
                    GUI.Label(GetNextWithOffset(0, 22, 350, 22), "<color=red>First add the resource to Storage.SelfResources.</color>", EditorLabelStyle);
                }
                else
                {
                    List<string> resNames = new List<string>();
                    foreach (var hit in Item.Resources)
                        resNames.Add(hit.Name);

                    foreach (var res in SelfResourceDependences)
                    {
                        res.ResourceName = PopUp(GetNextWithOffset(0, 25, 170, 22), res.ResourceName, resNames, Item.Name + id + res.GetHashCode() + "Self");
                        GUI.Label(GetNextWithoutOffset(175, 0, 40, 22), "Cost:", EditorLabelStyle);
                        res.Cost = (int)FloatField(GetNextWithoutOffset(215, 0, 150, 22), res.Cost, Item.Name + id + res.GetHashCode() + "SelfCost");
                        if (GUI.Button(GetNextWithoutOffset(365, 0, 20, 22), "-"))
                        {
                            SelfResourceDependences.Remove(res);
                            break;
                        }
                    }
                    if (SelfResourceDependences.Count < Storage.Instance.GlobalResources.Count)
                    {
                        if (GUI.Button(GetNextWithOffset(0, 22, 80, 22), "++Add++"))
                        {
                            SelfResourceDependences.Add(new PropertyBlock.ResourceDependence());
                        }
                    }
                }
            }
            else
            {
                GlobalResourceDependences = new List<PropertyBlock.ResourceDependence>();
            }

            GUI.Label(GetNextWithOffset(0, 33, 80, 22), "Connect:", EditorLabelStyle);
            int i = 0;
            for (i = 0; i < Connections.Count; i++)
            {
                GUI.Label(GetNextWithOffset(0, 22, 15, 22), (i + 1) + ":");
                Connections[i] = PopUp(GetNextWithoutOffset(15, 0, 200, 22), Connections[i], BlocksIDs, Item.Name + id + i);
                if (GUI.Button(GetNextWithoutOffset(215, 0, 15, 22), "-"))
                {
                    Connections.RemoveRange(i--, 1);
#if UNITY_EDITOR
                    EditorUtility.SetDirty(Storage.Instance);
#endif
                    break;
                }
            }
            if (GUI.Button(GetNextWithOffset(20, 22, 100, 22), "++Connection++"))
            {
                Connections.Add("empty");
            }
            GetNextWithOffset(-20, 22, 0, 0);
            i = 0;
            if (StorageEditor.dropdown == null)
                StorageEditor.dropdown = new List<bool>();
            GUI.Label(GetNextWithOffset(0, 22, 90, 22), "Properties:", EditorLabelStyle);
            foreach (var hit in Properties)
            {
                if (StorageEditor.dropdown.Count >= i)
                    StorageEditor.dropdown.Add(false);
                if (StorageEditor.dropdown[i])
                {
                    StorageEditor.dropdown[i] = GUI.Toggle(GetNextWithOffset(0, 22, 380, 22), StorageEditor.dropdown[i], hit.property.FieldName + hit.groupName);
                    GUI.Label(GetNextWithOffset(20, 22, 65, 22), "Property:", EditorLabelStyle);

                    var t = hit.property.Type;
                    List<INamedID> props = new List<INamedID>();
                    if (StorageEditor.SelectedBlockRoots.Contains(this))
                    {
                        foreach (var p in Storage.Instance.SavedProperties)
                        {
                            props.Add(p);
                        }
                    }
                    else
                    {
                        foreach (var root in StorageEditor.SelectedBlockRoots)
                        {
                            foreach (var p in root.Properties)
                                if (!props.Contains(p.property))
                                    props.Add(p.property);
                        }
                    }

                    hit.property = (SavedProperty)PopUp(GetNextWithoutOffset(65, 0, 300, 22), hit.property, props, Item.Name + id + hit.id + "Property" + i);
                    if (t != hit.property.Type)
                    {
                        StorageEditor.ResetBlocksList();
                        hit.Value = null;
                        break;
                    }

                    GUI.Label(GetNextWithOffset(0, 22, 300, 22), "Description: " + hit.property.Description, EditorLabelStyle);

                    GUI.Label(GetNextWithOffset(0, 22, 80, 22), "Group:", EditorLabelStyle);
                    hit.Group = (int)FloatField(GetNextWithoutOffset(80, 0, 80, 22), hit.Group, Item.Name + id + hit.id + "Group" + i);

                    GUI.Label(GetNextWithOffset(0, 22, 80, 22), "Apply type:", EditorLabelStyle);
                    if (IsDefault)
                    {
                        GUI.Box(GetNextWithoutOffset(80, 0, 150, 22), hit.ApplyType.ToString());
                        hit.ApplyType = PropertyApplyTypes.Apply;
                    }
                    else
                    {
                        hit.ApplyType = (PropertyApplyTypes)PopUp(GetNextWithoutOffset(80, 0, 150, 22), hit.ApplyType, Item.Name + id + hit.id + "AT" + i);
                    }
                    if (hit.Value != null)
                        switch (hit.property.Type)
                        {
                            case PropertyTypes.Float:
                                GUI.Label(GetNextWithOffset(0, 22, 55, 22), "Float:", EditorLabelStyle);
                                hit.Value = FloatField(GetNextWithoutOffset(55, 0, 100, 22), (float)hit.Value, Item.Name + id + hit.id + "FVAL" + i);
                                break;
                            case PropertyTypes.Int:
                                GUI.Label(GetNextWithOffset(0, 22, 55, 22), "Int:", EditorLabelStyle);
                                hit.Value = (int)FloatField(GetNextWithoutOffset(55, 0, 100, 22), (int)hit.Value, Item.Name + id + hit.id + "IVAL" + i);
                                break;
                            case PropertyTypes.Vector2:
                                GUI.Label(GetNextWithOffset(0, 22, 55, 22), "Vector:", EditorLabelStyle);
                                hit.Value = Vector2InputField(GetNextWithoutOffset(55, 0, 150, 22), (Vector2)hit.Value, Item.Name + id + hit.id + "V2Val" + i);
                                break;
                            case PropertyTypes.Vector3:
                                GUI.Label(GetNextWithOffset(0, 22, 55, 22), "Vector:", EditorLabelStyle);
                                hit.Value = Vector3InputField(GetNextWithoutOffset(55, 0, 150, 22), (Vector3)hit.Value, Item.Name + id + hit.id + "V3Val" + i);
                                break;
                            case PropertyTypes.Bool:
                                GUI.Label(GetNextWithOffset(0, 22, 55, 22), "Bool:", EditorLabelStyle);
                                hit.Value = GUI.Toggle(GetNextWithoutOffset(55, 0, 150, 22), (bool)hit.Value, "");
                                break;
                            case PropertyTypes.String:
                                GUI.Label(GetNextWithOffset(0, 22, 55, 22), "String:", EditorLabelStyle);
                                hit.Value = GUI.TextField(GetNextWithoutOffset(55, 0, 150, 22), (string)hit.Value);
                                break;
                        }

                    if (GUI.Button(GetNextWithOffset(0, 22, 80, 22), "--Delete--"))
                    {
                        Properties.Remove(hit);
#if UNITY_EDITOR
                        EditorUtility.SetDirty(Storage.Instance);
#endif
                        break;
                    }
                    GetNextWithOffset(-20, 10, 0, 0);
                }
                else
                {
                    StorageEditor.dropdown[i] = GUI.Toggle(GetNextWithOffset(0, 22, 380, 22), StorageEditor.dropdown[i], hit.property.FieldName + hit.groupName);
                }
                i++;
            }
            if (Storage.Instance.SavedProperties.Count > 0)
                if (GUI.Button(GetNextWithOffset(0, 22, 100, 22), "++Property++"))
                {
                    Properties.Add(new PropertyBlock.Property(StorageEditor.SelectedBlockRoots.Contains(this) ? Storage.Instance.SavedProperties[0] : StorageEditor.SelectedBlockRoots[0].Properties[0].property));
#if UNITY_EDITOR
                    EditorUtility.SetDirty(Storage.Instance);
#endif
                }
            GetNextWithOffset(0, 40, 0, 0);
            GUI.Label(GetNextWithoutOffset(0, 22, 340, 22), "------------------------------------------------------------", EditorLabelStyle);
            if (GUI.Button(GetNextWithoutOffset(120, 0, 100, 22), "Delete block"))
            {
                Item.Modernizations.Remove(this);
                StorageEditor.SelectedBlock = null;
                StorageEditor.SelectedBlockRoots = null;
#if UNITY_EDITOR
                EditorUtility.SetDirty(Storage.Instance);
#endif
            }
            GUIEditor.Draw();
            GUI.EndClip();
        }

        public void DrawPlaymode(Rect rect, ItemSet Item)// display selected modernization if disable editor mode
        {
            Color color = GUI.color;
            if (StorageEditor.SelectedBlock != this)
            {
                StorageEditor.SelectedBlock = this;
                StorageEditor.SelectedBlockRoots = Item.GetModernizationRoots(this);
                StorageEditor.ResetBlocksList();
                StorageEditor.CheckCanExplore();
            }

            SetDefault(new Rect(rect.x + 15, rect.y + 20, 0, 0));
            GUI.Box(rect, Description + ":");

            foreach (var hit in Properties)
            {
                GUI.Label(GetNextWithOffset(0, 24, 380, 24), hit.property.Description + hit.GetDescription());
            }

            if (IsDefault || Item.Stock.Contains(GetID()))
            {
                GUI.Label(GetNextWithOffset(0, 30, 380, 24), "<color=green>Explored</color>");
            }
            else
            {
                int n;
                string c = GetCost(out n);
                n++;
                if (n > 1)
                    GUI.Box(GetNextWithOffset(0, 30, 380, 24 * n), "Cost:\n" + c);

                if (StorageEditor.CanExplore)
                {
                    GUI.color = Storage.AvailableWithResources(this, Item) ? new Color(0.2f, 0.7f, 0.2f, 1) : new Color(0.7f, 0.2f, 0.2f, 1);
                    if (GUI.Button(GetNextWithOffset(0, 24 * n, 380, 24), "Explore"))
                    {

                        UsersDATA.Instance.StartCoroutine(UsersDATA.Instance.Explore(id, (v) => 
                        {
                            if (v)
                            {
                                Storage.Explore(Item.Name, this);
                            }
                        }));
                    }
                }
                else
                {
                    GUI.Label(GetNextWithOffset(0, 24 * n, 380, 48), "<color=red>To explore this modernization,\nexplore the previous one.</color>");
                }
            }
            GUI.color = color;
        }

        public string GetCost(out int strings)
        {
            string Cost = string.Empty;
            strings = 0;
            foreach (var cost in GlobalResourceDependences)
            {
                strings++;
                Cost += cost.ResourceName + ": " + cost.Cost + "\n";
            }
            foreach (var cost in SelfResourceDependences)
            {
                strings++;
                Cost += cost.ResourceName + ": " + cost.Cost + "\n";
            }
            return Cost;
        }

        public PropertyBlock()
        {
        }

        public PropertyBlock(bool setup)
        {
            if (setup)
            {
                Name = "newBlock";
                Description = string.Empty;
                Connections = new List<string>();
                Properties = new List<Property>();
                AvailableBySingle = false;
                position = new Vector2(UnityEngine.Random.Range(-100, 100), UnityEngine.Random.Range(-100, 100));
                id = System.Guid.NewGuid().ToString();
                GlobalResourceDependences = new List<ResourceDependence>();
                SelfResourceDependences = new List<ResourceDependence>();
            }
        }
        public string GetName()
        {
            return Name;
        }

        public string GetID()
        {
            return id.ToString();
        }

        public PropertyBlock GetClone()
        {
            PropertyBlock clone = new PropertyBlock(false);
            clone.Name += Name + " (clone)";
            clone.position = position + Vector2.one * 40;
            clone.id = System.Guid.NewGuid().ToString();
            clone.GlobalResourceDependences = new List<ResourceDependence>();
            foreach (var hit in GlobalResourceDependences)
                clone.GlobalResourceDependences.Add(hit.Clone());
            clone.SelfResourceDependences = new List<ResourceDependence>();
            foreach (var hit in SelfResourceDependences)
                clone.SelfResourceDependences.Add(hit.Clone());
            clone.Connections = new List<string>();
            clone.Properties = new List<Property>();
            foreach (var hit in Properties)
                clone.Properties.Add(hit.Clone());
            return clone;
        }
    }

    [Serializable]
    public class Resource
    {
        public string Name;
        public int Count;
        public ResourceTypes ResourceType;
    }

    public enum ResourceTypes
    {
        Absolute = 0,
        Material = 1
    }

    public class Modifiable : Attribute
    {
        public string Description;

        public Modifiable(string description)
        {
            Description = description;
        }
    }

}