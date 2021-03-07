using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEditor;

public class StorageEditor : GUIEditor
{
    public static Storage storage;
    public static Storage.ShipSet SelectedShip;
    public static Dictionary<Rect, PropertyBlock> Blocks;
    public static PropertyBlock SelectedBlock;
    public static bool CanExplore;
    public static bool DragMouse;
    public static Rect DescriptionRect;
    public static List<bool> dropdown;
    public static List<string> BlocksNames;
    public static float Drag;
    public static bool IsEditor = false;
    public static bool DownIsOnDescription = false;
    public static Dictionary<string, string> SavedProperties;
    public static float PropertiesScroll;
    public static UnityEvent Back;

    public static void Init()
    {
        storage = Resources.Load<Storage>("Storage");
        Back = new UnityEvent();
        DescriptionRect = new Rect();
        SavedProperties = new Dictionary<string, string>();
        for(int i = 0; i < storage.SavedProperties.Count; i++)
        {
            SavedProperties.Add(storage.SavedProperties[i], storage.SavedDescriptions[i]);
        }
    }
    private void Start()
    {
        Init();
    }
    public static Vector2 offset;
    public static bool redraw;
    private void OnGUI()
    {
        DrawGUI(ref offset, out bool redraw); 
    }

    public static void DrawGUI(ref Vector2 offset, out bool Redraw)
    {
        Begin();
        GUI.skin = storage.Skin;
        if (popupIsOpen)
            ResetBlockNames();

#if UNITY_EDITOR
        IsEditor = GUI.Toggle(new Rect(180, 5, 100, 20), IsEditor, new GUIContent { text = "IsEditor" });
        if(GUI.Button(new Rect(290, 5, 100, 20), "Сохранить"))
            EditorUtility.SetDirty(storage);

        Undo.RecordObject(storage, "StorageEditor");

        if (Event.current.type != EventType.Repaint && SelectedBlock != null)
            SetFullNames();
#endif

        Redraw = false;
        if(SelectedShip == null)
        {
            if(IsEditor)
                DrawShipsList();
        }
        else
        {
            Blocks = new Dictionary<Rect, PropertyBlock>();
            if (GUI.Button(new Rect(Vector2.one * 5, new Vector2(100, 30)), "Назад"))
            {
                SelectedShip = null;
                Back.Invoke();
                return;
            }

            if(UsersDATA.currentAccount != null)
                GUI.Box(new Rect(new Vector2(Screen.width - 200, 5), new Vector2(195, 30)), "Свободный опыт: " + UsersDATA.currentAccount.FreeExperience);

            DrawGroupsBranch(offset);

            var alignment = GUI.skin.box.alignment;
            GUI.skin.box.alignment = TextAnchor.UpperLeft;
            if (IsEditor)
                DrawGroupsList();

            if (Event.current.type == EventType.MouseUp && Event.current.button == 0 && !DragMouse && !DescriptionRect.Contains(Event.current.mousePosition))
            {
                BlockSelection();
            }

            if (SelectedBlock != null)
            {
                DrawProperties();
            }
            GUI.skin.box.alignment = alignment;

            Redraw = true;
        }

        if (Event.current.type == EventType.MouseDown)
        {
            DownIsOnDescription = DescriptionRect.Contains(Event.current.mousePosition);
        }

        if(Event.current.type == EventType.ScrollWheel)
        {
            if (DescriptionRect.Contains(Event.current.mousePosition))
            {
                PropertiesScroll += Event.current.delta.y * 9;
                PropertiesScroll = Mathf.Clamp(PropertiesScroll, 0, 800);
            }
        }

        if (Event.current.type == EventType.MouseDrag)
        {
            Drag += Event.current.delta.magnitude;
            if(Drag > 5)
            {
                DragMouse = true;
            }
            if (Event.current.button == 1 && !DownIsOnDescription)
            {
                if (DragMouse)
                {
                    offset += Event.current.delta;
                    Redraw = true;
                }
            }
        }

        if (Event.current.type == EventType.MouseUp)
        {
            DragMouse = false;
            Drag = 0f;
        }
    }

    public static void SetFullNames()
    {
        foreach (var hit in SelectedBlock.Properties)
        {
            var list = SelectedBlock.Properties.Where(x => x.Name == hit.Name && x.Group > 0 ).ToList();
            if (list.Count > 0)
            {
                list = SelectedBlock.Properties.Where(x => x.Name == hit.Name && x.UnderGroup > 0).ToList();
                if (list.Count > 0)
                {
                    hit.groupName = (" (group:" + hit.Group + " / under group:" + hit.UnderGroup + ") ");
                }
                else
                {
                    hit.groupName = (" (group:" + hit.Group + ") ");
                }
            }
            else
            {
                if (hit.Group > 0)
                {
                    if (hit.UnderGroup > 0)
                        hit.groupName = (" (group:" + hit.Group + " / under group:" + hit.UnderGroup + ") ");
                    else
                        hit.groupName = (" (group:" + hit.Group + ") ");
                }
                else
                {
                    hit.groupName = string.Empty;
                }
            }
        }
    }

    public static void DrawProperties()
    {

        if (!IsEditor)
        {
            DescriptionRect = new Rect(5, 50, 400, 600);
            SetDefault(new Rect(DescriptionRect.x + 5, DescriptionRect.y + 20, 0, 0));
            GUI.Box(DescriptionRect, SelectedBlock.Description + ":");

            //string descr =  + "\n";
            foreach (var hit in SelectedBlock.Properties)
            {
                string prefix = string.Empty;
                string postfix = string.Empty;
                object value = hit.GetValue(out PropertyTypes propertyType);
                switch (hit.ApplyType)
                {
                    case PropertyApplyTypes.Apply:
                        prefix = " = ";
                        break;
                    case PropertyApplyTypes.Add:
                        prefix = " + ";
                        break;
                    case PropertyApplyTypes.Multiply:
                        prefix = " + ";
                        switch (propertyType)
                        {
                            case PropertyTypes.Float:
                                value = (float)value * 100 - 100;
                                break;
                            case PropertyTypes.Int:
                                value = (int)value * 100 - 100;
                                break;
                            case PropertyTypes.Vector2:
                                value = (Vector2)value * 100 - Vector2.one * 100;
                                break;
                            case PropertyTypes.Vector3:
                                value = (Vector3)value * 100 - Vector3.one * 100;
                                break;
                        }
                        postfix = "%";
                        break;
                }
                GUI.Label(GetNextWithOffset(0, 22, 380, 22), hit.Description + prefix + value + postfix);
            }

            if (SelectedShip.BlocksInStock.Contains(SelectedBlock.Name))
            {
                GUI.Label(GetNextWithOffset(0, 30, 380, 22), "<color=green>Исследован</color>");
            }
            else
            {
                if (CanExplore)
                {
                    if (GUI.Button(GetNextWithOffset(0, 30, 380, 22), "Исследовать: " + SelectedBlock.Cost))
                    {
                        UsersDATA.Instance.StartCoroutine(UsersDATA.Instance.Explore(SelectedShip.PrefabName + "." + SelectedBlock.Name, "item"));
                    }
                }
                else
                {
                    GUI.Label(GetNextWithOffset(0, 30, 380, 44), "<color=red>Для исследования этого блока,\nисследуйте предыдущий.</color>");
                }
            }
        }
        else
        {
            DescriptionRect = new Rect(160, 50, 400, 750);
            GUI.Box(DescriptionRect, "Setup");
            GUI.BeginClip(new Rect(160, 70, 400, 730));
            SetDefault(new Rect(5, -PropertiesScroll, 0, 0));
            AddWall(DescriptionRect);
            GUI.Label(GetNextWithOffset(0, 5, 85, 22), "Block:");
            SelectedBlock.Name = GUI.TextField(GetNextWithoutOffset(85, 0, 300, 22), SelectedBlock.Name);

            GUI.Label(GetNextWithOffset(0, 22, 85, 22), "Description:");
            SelectedBlock.Description = GUI.TextArea(GetNextWithoutOffset(85, 0, 300, 44), SelectedBlock.Description);

            GUI.Label(GetNextWithOffset(0, 44, 70, 22), "Cost:");
            SelectedBlock.Cost = (int)FloatField(GetNextWithoutOffset(70, 0, 300, 22), SelectedBlock.Cost, SelectedShip.FullName + SelectedBlock.Name + "cost");

            GUI.Label(GetNextWithOffset(0, 22, 65, 22), "Open next:");
            int i = 0;
            for (i = 0; i < SelectedBlock.OpenNext.Count; i++)
            {
                GUI.Label(GetNextWithOffset(0, 22, 15, 22), (i + 1) + ":");
                SelectedBlock.OpenNext[i] = PopUp(GetNextWithoutOffset(15, 0, 90, 22), SelectedBlock.OpenNext[i], BlocksNames, SelectedShip.FullName + SelectedBlock.Name + i);
                if (GUI.Button(GetNextWithoutOffset(105, 0, 15, 22), "-") || SelectedBlock.OpenNext[i] == SelectedBlock.Name)
                {
#if UNITY_EDITOR
                    EditorUtility.SetDirty(storage);
#endif
                    SelectedBlock.OpenNext.Remove(SelectedBlock.OpenNext[i]);
                    i--;
                }
            }
            if (GUI.Button(GetNextWithOffset(0, 22, 55, 22), "++Next++"))
            {
                SelectedBlock.OpenNext.Add("new");
            }
            
            i = 0;
            if (dropdown == null)
                dropdown = new List<bool>();
            foreach (var hit in SelectedBlock.Properties)
            {
                if (dropdown.Count >= i)
                    dropdown.Add(false);
                if (dropdown[i])
                {
                    dropdown[i] = GUI.Toggle(GetNextWithOffset(0, 22, 300, 22), dropdown[i], hit.Name + hit.groupName);
                    GUI.Label(GetNextWithOffset(20, 22, 65, 22), "Property:");
                    hit.Name = PopUp(GetNextWithoutOffset(65, 0, 180, 22), hit.Name, storage.SavedProperties, SelectedShip.FullName + SelectedBlock.Name + hit.Name + "Property" + i);
                    int prop = storage.SavedProperties.IndexOf(hit.Name);
                    if (prop != -1)
                    {
                        hit.Description = storage.SavedDescriptions[prop];
                        hit.ComponentAttachment = storage.SavedComponentAttachments[prop];
                    }
                    GUI.Label(GetNextWithOffset(0, 22, 300, 22), "Description: " + hit.Description);
                    GUI.Label(GetNextWithOffset(0, 22, 300, 22), "Attachment: " + hit.ComponentAttachment.ToString());

                    GUI.Label(GetNextWithOffset(0, 22, 80, 22), "Group:");
                    hit.Group = (int)FloatField(GetNextWithoutOffset(80, 0, 80, 22), hit.Group, SelectedShip.FullName + SelectedBlock.Name + hit.Name + "Group"+i);
                    GUI.Label(GetNextWithOffset(0, 22, 80, 22), "UnderGroup:");
                    hit.UnderGroup = (int)FloatField(GetNextWithoutOffset(80, 0, 80, 22), hit.UnderGroup, SelectedShip.FullName + SelectedBlock.Name + hit.Name + "UnderGroup" + i);

                    GUI.Label(GetNextWithOffset(0, 22, 65, 22), "Apply type:");
                    string AT = hit.ApplyType.ToString();
                    var list = Enums.EnumList(hit.ApplyType);
                    AT = PopUp(GetNextWithoutOffset(65, 0, 150, 22), AT, list, SelectedShip.FullName + SelectedBlock.Name + hit.Name + "AT" + i);
                    hit.ApplyType = (PropertyApplyTypes)Enums.EnumValue(PropertyApplyTypes.Add, list.IndexOf(AT));
                    
                    GUI.Label(GetNextWithOffset(0, 22, 50, 22), "Float:");
                    hit.FValue = FloatField(GetNextWithoutOffset(50, 0, 100, 22), hit.FValue, SelectedShip.FullName + SelectedBlock.Name + hit.Name + "FVAL"+i);

                    GUI.Label(GetNextWithOffset(0, 22, 50, 22), "Int:");
                    hit.IValue = (int)FloatField(GetNextWithoutOffset(50, 0, 100, 22), hit.IValue, SelectedShip.FullName + SelectedBlock.Name + hit.Name + "IVAL" + i);
                    
                    GUI.Label(GetNextWithOffset(0, 22, 50, 22), "Vector2:");
                    hit.V2Value = Vector2InputField(GetNextWithoutOffset(50, 0, 150, 22), hit.V2Value, SelectedShip.FullName + SelectedBlock.Name + hit.Name + "V2Val" + i);

                    GUI.Label(GetNextWithOffset(0, 22, 50, 22), "Vector3:");
                    hit.V3Value = Vector3InputField(GetNextWithoutOffset(50, 0, 150, 22), hit.V3Value, SelectedShip.FullName + SelectedBlock.Name + hit.Name + "V3Val" + i);

                    if (GUI.Button(GetNextWithOffset(0, 22, 80, 22), "--Delete--"))
                    {
                        SelectedBlock.Properties.Remove(hit);
#if UNITY_EDITOR
                        EditorUtility.SetDirty(storage);
#endif
                    }
                    GetNextWithOffset(-20, 10, 0, 0);
                }
                else
                {
                    dropdown[i] = GUI.Toggle(GetNextWithOffset(0, 22, 300, 22), dropdown[i], hit.Name + hit.groupName);
                }
                i++;
            }
            if (GUI.Button(GetNextWithOffset(0, 22, 100, 22), "++Property++"))
            {
                SelectedBlock.Properties.Add(new PropertyBlock.Property());
#if UNITY_EDITOR
                EditorUtility.SetDirty(storage);
#endif
            }
            GetNextWithOffset(0, 40, 0, 0);
            GUI.Label(GetNextWithoutOffset(0, 22, 340, 22), "------------------------------------------------------------");
            if (GUI.Button(GetNextWithoutOffset(120, 0, 100, 22), "Delete block"))
            {
                SelectedShip.GrowthStock.Remove(SelectedBlock);
                SelectedBlock = null;
#if UNITY_EDITOR
                EditorUtility.SetDirty(storage);
#endif
            }
            Draw();
            GUI.EndClip();
        }
    }

    public static void DrawGroupsList()
    {
        Vector2 pos = new Vector2(20, 70);
        Vector2 cellSize = new Vector2(0, 35);
        Rect cell = new Rect(pos, new Vector2(130, 30));
        GUI.Box(new Rect(15, 50, 140, SelectedShip.GrowthStock.Count * 35 + 55), "Growth Stock");
        int i = 0;
        GUI.color = Color.white * 0.8f;
        foreach (var hit in SelectedShip.GrowthStock)
        {
            Rect current = cell.Add(cellSize * i);
            Blocks.Add(current, hit);
            GUI.Box(current, hit.Name);
            i++;
        }
        GUI.color = Color.white;
        if (GUI.Button(cell.Add(cellSize * SelectedShip.GrowthStock.Count), "+new+"))
        {
            SelectedShip.GrowthStock.Add(new PropertyBlock());
            ResetBlockNames();
#if UNITY_EDITOR
            EditorUtility.SetDirty(storage);
#endif
        }
    }

    public static void ResetBlockNames()
    {
        if (SelectedShip == null)
            return;
        BlocksNames = new List<string>();
        foreach (var Hit in SelectedShip.GrowthStock)
        {
            BlocksNames.Add(Hit.Name);
        }
    }

    public static void DrawShipsList()
    {
        GUILayout.BeginArea(new Rect(Vector2.one * 5, new Vector2(150, 440)));
        foreach (var hit in storage.Ships)
        {
            if (GUILayout.Button(hit.FullName))
            {
                SelectedShip = hit;
                ResetBlockNames();
            }
        }
        GUILayout.EndArea();
    }

    public static void BlockSelection()
    {
        SelectedBlock = null;
        DescriptionRect = new Rect();
        foreach (var hit in Blocks)
        {
            if (hit.Key.RectMatch(Event.current.mousePosition))
            {
                CanExplore = false;
                SelectedBlock = hit.Value;
                foreach(var block in SelectedShip.GrowthStock.Where(x => SelectedShip.BlocksInStock.Contains(x.Name)))
                {
                    if (block.OpenNext.Contains(SelectedBlock.Name))
                        CanExplore = true;
                }
            }
        }
    }

    public static void DrawGroupsBranch(Vector2 offset)
    {
        float Y = 0;
        SetDefault(new Rect(offset + Vector2.one * 200, new Vector2(130, 30)));
        for (int i = 0; i < SelectedShip.GrowthEntries.Count; i++)
        {
            var select = SelectedShip.GrowthStock.Where(x => x.Name == SelectedShip.GrowthEntries[i]).ToList();
            if (select.Count > 0)
            {
                var Ob = select.First();
                if (Ob != null)
                {
                    Ob.DrawGUI(GetOffset(0, Y, 0, 0), SelectedShip.GrowthStock, SelectedShip.BlocksInStock, Blocks, SelectedBlock == null ? "" : SelectedBlock.Name);
                }
            }
            if (IsEditor)
            {
                SelectedShip.GrowthEntries[i] = PopUp(GetOffset(0, Y + 30, 0, -8), SelectedShip.GrowthEntries[i], BlocksNames, SelectedShip.FullName + "Entry" + i);

                if (GUI.Button(GetWithSize(130, Y + 30, 22, 22), "-"))
                {
                    SelectedShip.GrowthEntries.Remove(SelectedShip.GrowthEntries[i]);
#if UNITY_EDITOR
                    EditorUtility.SetDirty(storage);
#endif
                }
            }
            if (i < SelectedShip.GrowthEntries.Count - 1 || IsEditor)
                Y += 200;
        }
        DrawLine(Default.position, GetOffset(0, Y + (IsEditor ? 52 : 30), 0, 0).position, Color.gray);
        if (IsEditor)
        {
            if (GUI.Button(GetOffset(0, Y + 30, 0, -8), "+new+"))
            {
                SelectedShip.GrowthEntries.Add("new entry");
#if UNITY_EDITOR
                EditorUtility.SetDirty(storage);
#endif
            }
        }
        Draw();
    }
}

#if UNITY_EDITOR
public class StorageEditorWindow : EditorWindow
{
    public static EditorWindow window;
    public static Vector2 offset;
    public static bool redraw;
    [MenuItem("Tools/Storage editor")]
    public static void Initiate()
    {
        window = GetWindow(typeof(StorageEditorWindow), false, "StorageEditor", true);
        window.minSize = new Vector2(700, 800);
    }

    private void OnEnable()
    {
        StorageEditor.Init();
        offset = Vector2.zero;
    }

    private void OnGUI()
    {
        if(!StorageEditor.storage)
            StorageEditor.Init();
        if(!window)
            window = GetWindow(typeof(StorageEditorWindow), false, "StorageEditor", true);

        if (redraw)
        {
            redraw = false;
            window.Repaint();
        }
        if(Event.current.type == EventType.Repaint || Event.current.isKey || Event.current.isMouse || Event.current.isScrollWheel || Event.current.type == EventType.Layout)
            StorageEditor.DrawGUI(ref offset, out redraw);

    }
}
#endif