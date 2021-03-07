using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using static MonoBehaviourPlus;

[CreateAssetMenu(fileName = "Storage", menuName = "Storage")]
public class Storage : ScriptableObject
{
    public List<string> MyShips;
    public List<ShipSet> Ships;
    public GUISkin Skin;
    public List<string> SavedProperties;
    public List<string> SavedDescriptions;
    public List<ComponentAttachment> SavedComponentAttachments;

    [System.Serializable]
    public class ShipSet
    {
        public string PrefabName;
        public string StorageName;
        public string FullName;
        public int Cost;
        public List<string> GrowthEntries;
        public List<string> BlocksInStock;
        public List<PropertyBlock> GrowthStock;

        public void ApplyGrowth(GameObject Instance)
        {
            ShipInstance SI = new ShipInstance(Instance);
            List<string> Exist = new List<string>();
            foreach (var Hit in GrowthEntries)
            {
                if (BlocksInStock.Contains(Hit))
                {
                    var entry = GrowthStock.Where(x => x.Name == Hit).SingleOrDefault();
                    Exist.Add(entry.Name);
                    entry.Apply(SI, GrowthStock, BlocksInStock, Exist);
                }
            }
        }
    }

    public ShipSet GetShip(string PrefabName)
    {
        foreach(var ship in Ships)
        {
            if (ship.PrefabName == PrefabName)
                return ship;
        }
        return null;
    }

    public static void SetStandart(ShipSet ship)
    {
        ship.BlocksInStock = new List<string>();
        foreach (var entry in ship.GrowthEntries)
        {
            ship.BlocksInStock.Add(entry);
        }
    }

    [ContextMenu("WriteItemsInDB")]
    public void WriteItemsInDB()
    {
        string send = string.Empty;
        foreach (var ship in Ships)
        {
            foreach(var item in ship.GrowthStock)
            {
                if(!ship.GrowthEntries.Contains(item.Name))
                    send += ship.PrefabName + "." + item.Name + ":" + item.Cost + ",";
            }
        }
        UsersDATA.Instance.StartCoroutine(UsersDATA.Instance.SetItemsCosts(send));
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(Storage))]
public class Storage_Editor : Editor
{
    public string NewProperty;
    public string NewDescription;
    public ComponentAttachment NewComponentAttachment;
    public Storage Target;
    private void OnEnable()
    {
        Target = (Storage)target;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        GUILayout.Space(20);
        NewProperty = EditorGUILayout.DelayedTextField("New Property", NewProperty);
        NewDescription = EditorGUILayout.DelayedTextField("New Description", NewDescription);
        NewComponentAttachment = (ComponentAttachment)EditorGUILayout.EnumPopup("NewComponentAttachment", NewComponentAttachment);
        GUILayout.Space(10);
        if (GUILayout.Button("Write"))
        {
            if (string.IsNullOrEmpty(NewProperty))
                return;
            if (string.IsNullOrEmpty(NewDescription))
                return;
            Target.SavedProperties.Add(NewProperty);
            Target.SavedDescriptions.Add(NewDescription);
            Target.SavedComponentAttachments.Add(NewComponentAttachment);
            NewProperty = string.Empty;
            NewDescription = string.Empty;
        }
        GUILayout.Space(20);
        if (GUILayout.Button("SetDefaulsStock"))
        {
            foreach(var ship in Target.Ships)
            {
                ship.BlocksInStock = new List<string>();
                foreach (var entry in ship.GrowthEntries)
                {
                    ship.BlocksInStock.Add(entry);
                }
            }
        }
    }
}
#endif