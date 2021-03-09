using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Modernizations;
using UnityEditor;
using System.Reflection;

[CreateAssetMenu(fileName = "Storage", menuName = "Storage")]
public class Storage : ScriptableObject
{
    public List<Resource> GlobalResources;
    public List<Resource> SelfResources;
    public static List<string> GarageSet;
    public static System.Action OnResourcesChainge
    {
        get => instance.onResourcesChainge;
        set => instance.onResourcesChainge = value;
    }
    public static System.Action<string, string> OnExploreSome
    {
        get => instance.onExploreSome;
        set => instance.onExploreSome = value;
    }

    #region Variables
    public static Storage Instance
    {
        get
        {
            if (!instance)
                instance = Resources.Load<Storage>("Storage");
            return instance;
        }
    }
    public System.Action onResourcesChainge;
    public System.Action<string, string> onExploreSome;
    static Storage instance;
    public List<ItemSet> Items;
    public GUISkin Skin;
    public List<SavedProperty> SavedProperties;
    public Texture2D BackgroundMesh;
    public Vector2 BlockGUISize = new Vector2(130, 30);
    public static SpriteAlignment GetInPointAlignment()
    {
        if (!IPAisapplyed)
        {
            _InPointAlignment = Instance.inPointAlignment;
            IPAisapplyed = true;
        }
        return _InPointAlignment;
    }
    public static bool IPAisapplyed = false;
    static SpriteAlignment _InPointAlignment;
    public SpriteAlignment inPointAlignment;
    public bool IsEditor = false;
    #endregion
    #region User functions  
    /// <summary>
    /// Apply ItemSet to target gameObject
    /// </summary>
    /// <param name="ItemName">name if item</param>
    /// <param name="target">target gameObject</param>
    /// <param name="ApplyToChildrens">search for child objects?</param>
    public static void ApplyGrowth(string ItemName, GameObject target, bool ApplyToChildrens = false)
    {
        ItemSet Item = GetItem(ItemName);
        if (Item == null)
        {
            Debug.Log("<color=red>Has no item with name \"" + ItemName + "\"</color>");
            return;
        }
        Item.ApplyGrowth(target, ApplyToChildrens);
    }
    /// <summary>
    /// Apply configure to target instance
    /// </summary>
    /// <param name="target">target gameObject</param>
    /// <param name="configure">custum configuration</param>
    /// <param name="ApplyToChildrens">search for child objects?</param>
    public static void ApplyConfigure(GameObject target, string configure, bool ApplyToChildrens = false)
    {
        string[] split = configure.Split(new char[] { ':' });
        var Item = GetItem(split[0]);
        if (Item == null)
        {
            Debug.Log("<color=red>Has no item with name \"" + split[0] + "\"</color>");
            return;
        }
        var newStock = split.ToList();
        newStock.RemoveRange(0, 1);
        Item.ApplyGrowth(target, newStock, ApplyToChildrens);
    }
    /// <summary>
    /// Get list of current available modernizations in stock
    /// </summary>
    public static List<PropertyBlock> GetAvailableModernizations(string ItemName)
    {
        ItemSet Item = GetItem(ItemName);
        if (Item == null)
        {
            Debug.Log("<color=red>Has no item with name \"" + ItemName + "\"</color>");
            return null;
        }
        return GetAvailableModernizations(Item);
    }
    /// <summary>
    /// Get list of current available modernizations in stock
    /// </summary>
    public static List<PropertyBlock> GetAvailableModernizations(ItemSet Item)
    {
        return GetAvailableModernizations(Item, Item.Stock);
    }
    /// <summary>
    /// Get list of current available modernizations in confugure
    /// </summary>
    public static List<PropertyBlock> GetAvailableModernizations(string ItemName, List<string> configure)
    {
        ItemSet Item = GetItem(ItemName);
        if (Item == null)
        {
            Debug.Log("<color=red>Has no item with name \"" + ItemName + "\"</color>");
            return null;
        }
        return GetAvailableModernizations(Item, configure);
    }
    /// <summary>
    /// Get list of current available modernizations in confugure
    /// </summary>
    public static List<PropertyBlock> GetAvailableModernizations(ItemSet Item, List<string> configure)
    {
        List<PropertyBlock> resut = new List<PropertyBlock>();
        foreach (var block in Item.Modernizations)
        {
            if (!configure.Contains(block.GetID()) && !Item.GetModernizationRoots(block).Contains(block) && !block.IsDefault && AvailableWithResources(block, Item) && AvailableWithParents(block, Item, configure))
            {
                resut.Add(block);
            }
        }
        return resut;
    }

    /// <summary>
    /// Have previous modernizations has been explored?
    /// </summary>
    public static bool AvailableWithParents(PropertyBlock Modernization, ItemSet Item)
    {
        return AvailableWithParents(Modernization, Item, Item.Stock);
    }

    /// <summary>
    /// Have previous modernizations in configure has been explored?
    /// </summary>
    public static bool AvailableWithParents(PropertyBlock Modernization, ItemSet Item, List<string> configure)
    {
        if(Modernization.AvailableBySingle)
            return Item.GetModernizationParents(Modernization).Count(x => configure.Contains(x.GetID()) || x.IsDefault) > 0;
        else
            return Item.GetModernizationParents(Modernization).All(x => configure.Contains(x.GetID()) || x.IsDefault);
    }
    /// <summary>
    /// Do you have enough resources to explore the modernization?
    /// </summary>
    public static bool AvailableWithResources(PropertyBlock Modernization, ItemSet Item)
    {
        bool Availeble = true;

        foreach (var hit in Modernization.GlobalResourceDependences)
        {
            var res = Instance.GlobalResources.FirstOrDefault(x => x.Name == hit.ResourceName);
            if (res.Count < hit.Cost)
                Availeble = false;
        }
        foreach (var hit in Modernization.SelfResourceDependences)
        {
            var res = Item.Resources.FirstOrDefault(x => x.Name == hit.ResourceName);
            if (res.Count < hit.Cost)
                Availeble = false;
        }
        return Availeble;
    }

    /// <summary>
    /// Adds a new modernization to the configure if it is opened. Stock of the item was not chainged.
    /// </summary>
    /// <returns>Chainged configure</returns>
    public static string ExploreToConfigure(string configure, string ModName)
    {
        string[] split = configure.Split(new char[] { ':' });
        var Item = GetItem(split[0]);
        if (Item == null)
        {
            Debug.Log("<color=red>Has no item with name \"" + split[0] + "\"</color>");
            return configure;
        }
        PropertyBlock Modernization = Item.Modernizations.FirstOrDefault(x => x.GetName() == ModName);
        if (Modernization == null)
        {
            Debug.Log("<color=red>Has no modernization with name \"" + ModName + "\" in item \"" + split[0] + "\" </color>");
            return configure;
        }
        return ExploreToConfigure(configure, Modernization);
    }
    /// <summary>
    /// Adds a new modernization to the configure if it is opened. Stock of the item was not chainged.
    /// </summary>
    /// <returns>Chainged configure</returns>
    public static string ExploreToConfigure(string configure, PropertyBlock Modernization)
    {

        string[] split = configure.Split(new char[] { ':' });
        if (split.Contains(Modernization.GetID()))
        {
            Debug.Log("<color=yellow>modernization alredy exist</color>");
            return configure;
        }

        var Item = GetItem(split[0]);
        if (Item == null)
        {
            Debug.Log("<color=red>Has no item with name \"" + split[0] + "\"</color>");
            return configure;
        }
        if (!Item.Modernizations.Contains(Modernization))
        {
            Debug.Log("<color=red>Has no match modernization in item \"" + split[0] + "\" </color>");
            return configure;
        }

        string id = Modernization.GetID();
        if (Item.Modernizations.Count(x => split.Contains(x.GetID()) && x.Connections.Contains(id)) > 0)
        {
            bool CanExplore = true;
            foreach (var hit in Modernization.GlobalResourceDependences)
            {
                var res = Instance.GlobalResources.FirstOrDefault(x => x.Name == hit.ResourceName);
                if (res.Count < hit.Cost)
                    CanExplore = false;
            }
            foreach (var hit in Modernization.SelfResourceDependences)
            {
                var res = Item.Resources.FirstOrDefault(x => x.Name == hit.ResourceName);
                if (res.Count < hit.Cost)
                    CanExplore = false;
            }

            if (CanExplore)
            {
                foreach (var hit in Modernization.GlobalResourceDependences)
                {
                    var res = Instance.GlobalResources.FirstOrDefault(x => x.Name == hit.ResourceName);

                    if (res.ResourceType == ResourceTypes.Material)
                    {
                        res.Count -= hit.Cost;
                    }
                }
                foreach (var hit in Modernization.SelfResourceDependences)
                {
                    var res = Item.Resources.FirstOrDefault(x => x.Name == hit.ResourceName);
                    if (res.ResourceType == ResourceTypes.Material)
                    {
                        res.Count -= hit.Cost;
                    }
                }
                configure = configure + "," + Modernization.GetID();
            }
            else
            {
                Debug.Log("<color=red>shortage of currency</color>");
            }
        }
        else
            Debug.Log("<color=red>Modernization \"" + Modernization.Name + "\" is not open yet.\nCall \"ExploreExtra()\" to explore modernization with ignoring modernization tree</color>");

        return configure;
    }
    /// <summary>
    /// Adds a new modernization to the stock if it is opened
    /// </summary>
    public static void Explore(string ItemName, string ModName)
    {
        ItemSet Item = GetItem(ItemName);
        if (Item == null)
        {
            Debug.Log("<color=red>Has no item with name \"" + ItemName + "\"</color>");
            return;
        }
        PropertyBlock Modernization = Item.Modernizations.FirstOrDefault(x => x.GetName() == ModName);
        if (Modernization == null)
        {
            Debug.Log("<color=red>Has no modernization with name \"" + ModName + "\" in item \"" +  ItemName + "\" </color>");
            return;
        }
        Explore(Item, Modernization);
    }
    /// <summary>
    /// Adds a new modernization to the stock if it is opened
    /// </summary>
    public static void Explore(ItemSet Item, string ModName)
    {
        PropertyBlock Modernization = Item.Modernizations.FirstOrDefault(x => x.GetName() == ModName);
        if (Modernization == null)
        {
            Debug.Log("<color=red>Has no modernization with name \"" + ModName + "\" in item \"" +  Item.Name + "\" </color>");
            return;
        }
        Explore(Item, Modernization);
    }
    /// <summary>
    /// Adds a new modernization to the stock if it is opened
    /// </summary>
    public static void Explore(string ItemName, PropertyBlock Modernization)
    {
        ItemSet Item = GetItem(ItemName);
        if (Item == null)
        {
            Debug.Log("<color=red>Has no item with name \"" + ItemName + "\"</color>");
            return;
        }
        Explore(Item, Modernization);
    }
    /// <summary>
    /// Adds a new modernization to the stock if it is opened
    /// </summary>
    public static void Explore(ItemSet Item, PropertyBlock Modernization)
    {
        string id = Modernization.GetID();
        if (Item.Stock.Contains(Modernization.GetID()))
        {
            Debug.Log("<color=yellow>modernization alredy exist</color>");
            return;
        }

        if (AvailableWithParents(Modernization, Item))
        {
            if (AvailableWithResources(Modernization, Item))
            {
                foreach (var hit in Modernization.GlobalResourceDependences)
                {
                    var res = Instance.GlobalResources.FirstOrDefault(x => x.Name == hit.ResourceName);

                    if (res.ResourceType == ResourceTypes.Material)
                    {
                        res.Count -= hit.Cost;
                    }
                }
                foreach (var hit in Modernization.SelfResourceDependences)
                {
                    var res = Item.Resources.FirstOrDefault(x => x.Name == hit.ResourceName);
                    if (res.ResourceType == ResourceTypes.Material)
                    {
                        res.Count -= hit.Cost;
                    }
                }
                OnResourcesChainge?.Invoke();
                Item.Stock.Add(id);
            }
            else
            {
                Debug.Log("<color=red>shortage of currency</color>");
            }
        }
        else
            Debug.Log("<color=red>Modernization \"" + Modernization.Name + "\" is not open yet.\nCall \"ExploreExtra()\" to explore modernization with ignoring modernization tree</color>");
    }


    /// <summary>
    /// Adds a new modernization to the configure, ignoring the Modernization tree and cost. Stock of the item was not chainged.
    /// </summary>
    /// <returns>Chainged configure</returns>
    public static string ExploreExtraToConfigure(string configure, string ModName)
    {
        string[] split = configure.Split(new char[] { ':' });
        var Item = GetItem(split[0]);
        if (Item == null)
        {
            Debug.Log("<color=red>Has no item with name \"" + split[0] + "\"</color>");
            return configure;
        }
        PropertyBlock Modernization = Item.Modernizations.FirstOrDefault(x => x.GetName() == ModName);
        if (Modernization == null)
        {
            Debug.Log("<color=red>Has no modernization with name \"" + ModName + "\" in item \"" + split[0] + "\" </color>");
            return configure;
        }
        return ExploreExtraToConfigure(configure, Modernization);
    }
    /// <summary>
    /// Adds a new modernization to the configure, ignoring the Modernization tree and cost. Stock of the item was not chainged.
    /// </summary>
    /// <returns>Chainged configure</returns>
    public static string ExploreExtraToConfigure(string configure, PropertyBlock Modernization)
    {
        string[] split = configure.Split(new char[] { ':' });
        if (split.Contains(Modernization.GetID()))
        {
            Debug.Log("<color=yellow>modernization alredy exist</color>");
            return configure;
        }
        var Item = GetItem(split[0]);
        if (Item == null)
        {
            Debug.Log("<color=red>Has no item with name \"" + split[0] + "\"</color>");
            return configure;
        }
        if (!Item.Modernizations.Contains(Modernization))
        {
            Debug.Log("<color=red>Has no match modernization in item \"" + split[0] + "\" </color>");
            return configure;
        }
        configure = configure + "," + Modernization.GetID();
        return configure;
    }
    /// <summary>
    /// Adds a new modernization to the Stock, ignoring the Modernization tree and cost.
    /// </summary>
    public static void ExploreExtra(string ItemName, string ModName)
    {
        ItemSet Item = GetItem(ItemName);
        if (Item == null)
        {
            Debug.Log("<color=red>Has no item with name \"" + ItemName + "\"</color>");
            return;
        }
        PropertyBlock Modernization = Item.Modernizations.FirstOrDefault(x => x.GetName() == ModName);
        if (Modernization == null)
        {
            Debug.Log("<color=red>Has no modernization with name \"" + ModName + "\" in item \"" +  ItemName + "\" </color>");
            return;
        }
        ExploreExtra(Item, Modernization);
    }
    /// <summary>
    /// Adds a new Modernization to the Stock, ignoring the Modernization tree and cost
    /// </summary>
    public static void ExploreExtra(ItemSet Item, string ModName)
    {
        PropertyBlock Modernization = Item.Modernizations.FirstOrDefault(x => x.GetName() == ModName);
        if (Modernization == null)
        {
            Debug.Log("<color=red>Has no modernization with name \"" + ModName + "\" in item \"" +  Item.Name + "\" </color>");
            return;
        }
        ExploreExtra(Item, Modernization);
    }
    /// <summary>
    /// Adds a new modernization to the Stock, ignoring the Modernization tree and cost
    /// </summary>
    public static void ExploreExtra(string ItemName, PropertyBlock Modernization)
    {
        ItemSet Item = GetItem(ItemName);
        if (Item == null)
        {
            Debug.Log("<color=red>Has no item with name \"" + ItemName + "\"</color>");
            return;
        }
        ExploreExtra(Item, Modernization);
    }
    /// <summary>
    /// Adds a new modernization to the Stock, ignoring the Modernization tree and cost
    /// </summary>
    public static void ExploreExtra(ItemSet Item, PropertyBlock Modernization)
    {
        if (Item.Stock.Contains(Modernization.GetID()))
        {
            Debug.Log("<color=yellow>modernization alredy exist</color>");
            return;
        }
        Item.Stock.Add(Modernization.GetID());
    }


    /// <summary>
    /// Return a string with format "ItemName:Stock[0],Stock[1],Stock[2]..."
    /// </summary>
    public static string GetConfiguration(string ItemName)
    {
        ItemSet Item = Instance.Items.FirstOrDefault(x => x.Name == ItemName);
        if (Item == null)
        {
            Debug.Log("<color=red>Has no item with name \"" + ItemName + "\"</color>");
            return null;
        }
        return GetConfiguration(Item);
    }

    /// <summary>
    /// Return a string with format "ItemName:Stock[0],Stock[1],Stock[2]..."
    /// </summary>
    public static string GetConfiguration(ItemSet Item)
    {
        string result = Item.Name + ":";
        for (int i = 0; i < Item.Stock.Count; i++)
        {
            result += Item.Stock[i];
            if (i < Item.Stock.Count - 1)
                result += ",";
        }
        return result;
    }

    /// <summary>
    /// Set item as configure
    /// </summary>
    /// <param name="configure">String with format "ItemName:Stock[0],Stock[1],Stock[2]..."</param>
    public static void SetConfiguration(string configure)
    {
        string[] split = configure.Split(new char[] { ':' });
        var Item = GetItem(split[0]);
        string[] conf = split[1].Split(new char[] { ',' });
        SetConfiguration(Item, conf.ToList());
    }
    /// <summary>
    /// Set item as configure
    /// </summary>
    public static void SetConfiguration(ItemSet Item, List<string> configure)
    {
        for (int i = 0; i < configure.Count; i++)
        {
            Item.Stock.Add(configure[i]);
        }
    }

    /// <summary>
    /// Returns a clone of an existing Item by applying the new configuration
    /// </summary>
    /// <param name="configure">String with format "ItemName:Stock[0],Stock[1],Stock[2]..."</param>
    public ItemSet CreateItemWithConfiguration(string configure)
    {
        string[] split = configure.Split(new char[] { ':' });
        var Item = GetItem(split[0]);
        if (Item == null)
        {
            Debug.Log("<color=red>Has no item with name \"" + split[0] + "\"</color>");
            return null;
        }

        Item = Item.Clone();

        Item.Stock = new List<string>();

        string[] conf = split[1].Split(new char[] { ',' });
        for (int i = 0; i < conf.Length; i++)
        {
            Item.Stock.Add(conf[i]);
        }
        return Item;
    }

    public static void AddResourceToItem(string ItemName, string resourceName, int count)
    {
        ItemSet Item = GetItem(ItemName);
        var resource = Item.Resources.FirstOrDefault(x => x.Name == resourceName);
        if (resource == null)
        {
            Debug.Log("<color=red>Has no resource with name \"" + resourceName + "\"</color>");
            return;
        }
        resource.Count += count;
        OnResourcesChainge?.Invoke();
    }

    public static int GetResourceOnItem(string ItemName, string resourceName)
    {
        ItemSet Item = GetItem(ItemName);
        var resource = Item.Resources.FirstOrDefault(x => x.Name == resourceName);
        if (resource == null)
        {
            Debug.Log("<color=red>Has no resource with name \"" + resourceName + "\"</color>");
            return 0;
        }
        return resource.Count;
    }

    public static void SetResourceOnItem(string ItemName, string resourceName, int count)
    {
        ItemSet Item = GetItem(ItemName);
        var resource = Item.Resources.FirstOrDefault(x => x.Name == resourceName);
        if (resource == null)
        {
            Debug.Log("<color=red>Has no resource with name \"" + resourceName + "\"</color>");
            return;
        }
        resource.Count = count;
        OnResourcesChainge?.Invoke();
    }

    public static void AddGlobalResource(string resourceName, int count)
    {
        var resource = Instance.GlobalResources.FirstOrDefault(x => x.Name == resourceName);
        if (resource == null)
        {
            Debug.Log("<color=red>Has no global resource with name \"" + resourceName + "\"</color>");
            return;
        }
        resource.Count += count;
        OnResourcesChainge?.Invoke();
    }

    public static int GetGlobalResourceValue(string resourceName)
    {
        var resource = Instance.GlobalResources.FirstOrDefault(x => x.Name == resourceName);
        if (resource == null)
        {
            Debug.Log("<color=red>Has no global resource with name \"" + resourceName + "\"</color>");
            return 0;
        }
        return resource.Count;
    }

    public static void SetGlobalResourceValue(string resourceName, int count)
    {
        var resource = Instance.GlobalResources.FirstOrDefault(x => x.Name == resourceName);
        if (resource == null)
        {
            Debug.Log("<color=red>Has no global resource with name \"" + resourceName + "\"</color>");
            return;
        }
        resource.Count = count;
        OnResourcesChainge?.Invoke();
    }
    #endregion

    #region Editor
    public static ItemSet GetItem(string Name)
    {
        foreach (var item in Instance.Items)
        {
            if (item.Name == Name)
                return item;
        }
        return null;
    }

    System.Type modT;
    public void RefreshPropertiesList()
    {
        modT = typeof(Modifiable);
        SavedProperties = new List<SavedProperty>();

        Assembly[] AS = System.AppDomain.CurrentDomain.GetAssemblies();
        System.Type BaseClass = typeof(MonoBehaviour);

        foreach (var A in AS)
        {
            if (!A.FullName.StartsWith("Assembly-"))
            {
                continue;
            }
            System.Type[] types = A.GetTypes();
            foreach (var T in types)
            {
                string namePrefix = string.Empty;
                string descrPrefix = string.Empty;
                var attr = T.GetCustomAttribute(modT);
                namePrefix = T.Name + ".";
                if (attr != null)
                {
                    descrPrefix = GetDescription(attr) + ". ";
                }
                if (T.GetInterfaces().Contains(typeof(IModifiable)))
                {
                    FieldInfo[] fields = T.GetFields();

                    foreach (FieldInfo field in fields)
                    {
                        if (field.IsDefined(modT))
                        {
                            AddProperty(namePrefix, descrPrefix, field);
                        }
                    }
                }
            }
        }
    }

    private void AddProperty(string namePrefix, string descrPrefix, FieldInfo field)
    {
        var arrType = field.FieldType;
        if (arrType.IsArray)
        {
            var fs = arrType.GetElementType().GetFields();
            foreach (var hit in fs)
                AddProperty(namePrefix + field.Name + ".", descrPrefix + GetDescription(field) + ". ", hit);
        }
        else
        {
            if (field.FieldType.IsClass && field.FieldType != typeof(string))
            {
                var fs = field.FieldType.GetFields();
                foreach (var hit in fs)
                {
                    if (hit.IsDefined(modT))
                    {
                        AddProperty(namePrefix + field.Name + ".", descrPrefix + GetDescription(field) + ". ", hit);
                    }
                }
            }
            else
            {
                SavedProperties.Add(new SavedProperty(namePrefix + field.Name, descrPrefix + GetDescription(field), field.FieldType));
                //Debug.Log(field.FieldType.Name);
            }
        }
    }

    public string GetDescription(MemberInfo field)
    {
        var modT = typeof(Modifiable);
        var descr = modT.GetField("Description");
        var attr = field.GetCustomAttribute(modT);
        return (string)descr.GetValue(attr);
    }
    public string GetDescription(System.Attribute attribute)
    {
        var modT = typeof(Modifiable);
        var descr = modT.GetField("Description");
        return (string)descr.GetValue(attribute);
    }
    #endregion
}

#if UNITY_EDITOR
    [CustomEditor(typeof(Storage))]
public class Storage_Editor : Editor
{
    public Storage Target;
    private void OnEnable()
    {
        Target = (Storage)target;
        Target.RefreshPropertiesList();
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(20);
        if (GUILayout.Button("RefreshPropertiesList"))
        {
            Target.RefreshPropertiesList();
        }
    }
}
#endif