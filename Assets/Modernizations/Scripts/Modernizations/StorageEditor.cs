using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEditor;
using Modernizations;
using static GUIEditor;
using static CustumGUILayout;

public class StorageEditor : MonoBehaviour
{
    #region Variables
    public static Storage storage;
    public static ItemSet SelectedItem;
    public static List<System.Tuple<Rect, PropertyBlock>> Blocks;
    public static PropertyBlock SelectedBlock;
    public static List<PropertyBlock> SelectedBlockRoots;
    public static bool CanExplore;
    public static Rect DescriptionRect;
    public static List<bool> dropdown;
    public static List<INamedID> BlocksIDs;
    public static Dictionary<string, string> SavedProperties;
    public static float PropertiesScroll;
    public static bool IsEditor
    { get
        {
#if UNITY_EDITOR
            return storage.IsEditor;
#else
            return false;
#endif
        }
        set
        {
            storage.IsEditor = value;
        }
    }
    public static UnityEvent OnCloseWindow;
    #endregion

    private static bool control;

    #region Realtime
    private void Awake()
    {
        Init();
    }

    static Vector2 offset;
    private void OnGUI()
    {
        if (!IsEditor && SelectedItem == null)
            return;
        GUI.skin = storage.Skin;
        Rect window = new Rect(25, 25, Screen.width - 50, Screen.height - 50);
        GUI.Box(window, "");
        DrawGUI(window);
    }

    public static void DrawGUI(Rect rect)
    {
        GUI.BeginClip(rect);
        GUI.skin = storage.Skin;
        Begin();
        if (SelectedItem == null)
        {
            if (IsEditor)
                DrawItemsList();
        }
        else
        {
            Rect workspace = new Rect(0, 0, Screen.width, Screen.height);
            if (SelectedBlock != null)
            {
                if (IsEditor)
                {
                    ResetBlocksList();
                    if (Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.D && control)
                    {
                        SelectedBlock = SelectedBlock.GetClone();
                        SelectedItem.Modernizations.Add(SelectedBlock);
                    }
                    SelectedBlock.DrawEditor(new Rect(Screen.width - 445, 0, 440, Screen.height), ref PropertiesScroll, SelectedItem);
                }
                else
                    SelectedBlock.DrawPlaymode(new Rect(Screen.width - 445, 0, 440, Screen.height), SelectedItem);
                workspace.width -= 445;
            }
            if (IsEditor)
            {
                workspace.y += 30;
                workspace.height -= 30;
                GUI.Box(new Rect(0, 0, workspace.width, 30), "");
            }

            if (Event.current.type == EventType.Repaint || Event.current.isMouse || Event.current.isKey || Event.current.isScrollWheel)
            {
                BeginWorkspace(workspace, ref offset);
                DrawBlocks(offset);
                if (IsEditor)
                    DrawGroupsList(new Rect(15, 40, 140, Screen.height - 50));
                BlockSelection();

            EndWorkspace();
            }
        }
        EditorOptions();
        GUI.EndClip();
        control = Event.current.control;
    }

    #endregion
    public static void Init()
    {
        OnCloseWindow = new UnityEvent();
        storage = Storage.Instance;
        storage.RefreshPropertiesList();
        ScyncChainges();

        offset = Vector3.zero;
        DescriptionRect = new Rect();
        SavedProperties = new Dictionary<string, string>();
        for (int i = 0; i < storage.SavedProperties.Count; i++)
        {
            SavedProperties.Add(storage.SavedProperties[i].Name, storage.SavedProperties[i].Description);
        }
    }


    #region DrawMeshods
    public static void DrawGroupsList(Rect rect)// display modernizations list if enable editor mode
    {
        SetDefault(new Rect(rect.position, Vector2.zero));
        if (SelectedItem == null)
            return;
        int count = SelectedItem.Modernizations.Count(x => !x.IsDefault);
        GUI.Box(GetNextWithoutOffset(0, 0, rect.width, 30 * count + 60), "Growth Stock");
        GetNextWithOffset(5, -5, 0, 0);
        GUI.color = Color.white * 0.8f;
        foreach (var hit in SelectedItem.Modernizations)
        {
            if (hit.IsDefault)
                continue;
            Rect current = GetNextWithOffset(0, 30, 130, 28);
            Blocks.Add(new System.Tuple<Rect, PropertyBlock>(current, hit));
            GUI.Box(current, hit.Name);
        }
        GUI.color = Color.white;
        if (GUI.Button(GetNextWithOffset(-5, 30, 130, 28), "+new+"))
        {
            SelectedItem.Modernizations.Add(new PropertyBlock(true));
            ResetBlocksList();
#if UNITY_EDITOR
            EditorUtility.SetDirty(storage);
#endif
        }
    }
    public static void DrawItemsList()// display items if enable editor mode
    {
        GUILayout.BeginArea(new Rect(Vector2.one * 5, new Vector2(270, 440)));
        foreach (var hit in storage.Items)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(hit.Name, GUILayout.Width(90)))
            {
                SelectedItem = hit;
                ResetBlocksList();
            }
            GUILayout.Box("Name:", GUILayout.Width(55));
            hit.Name = GUILayout.TextField(hit.Name);
#if UNITY_EDITOR
            if (GUILayout.Button("-", GUILayout.Width(18)))
            {
                if (EditorUtility.DisplayDialog("Deleting", "Do you whant to delete this item?", "yes", "no"))
                {
                    storage.Items.Remove(hit);
                    break;
                }
            }
#endif
            GUILayout.EndHorizontal();
        }
        if (GUILayout.Button("++New Item++"))
        {
            storage.Items.Add(new ItemSet { Name = "new item", Modernizations = new List<PropertyBlock>(), Stock = new List<string>(), Resources = new List<Resource>() });
        }

        GUILayout.EndArea();
    }


    public static void BeginWorkspace(Rect rect, ref Vector2 offset)//Begin clipping space and draw the background mesh
    {
        Blocks = new List<System.Tuple<Rect, PropertyBlock>>();

        GUI.BeginClip(new Rect(rect.x + 3, rect.y + 3, rect.width - 6, rect.height - 6));
        if (storage.BackgroundMesh)
        {
            Vector2 meshTexSize = new Vector2(storage.BackgroundMesh.width, storage.BackgroundMesh.height);
            Vector2Int cells = new Vector2Int((int)(Screen.width / meshTexSize.x), (int)(Screen.height / meshTexSize.y));
            Vector2Int celloffset = new Vector2Int((int)(offset.x / meshTexSize.x), (int)(offset.y / meshTexSize.y));
            for (int w = -2; w <= cells.x + 1; w++)
            {
                for (int h = -2; h <= cells.y + 1; h++)
                {
                    GUI.DrawTexture(new Rect(offset + new Vector2((w - celloffset.x) * meshTexSize.x, (h - celloffset.y) * meshTexSize.y), meshTexSize), storage.BackgroundMesh);
                }
            }
        }
        if (Event.current.keyCode == KeyCode.Escape && Event.current.type == EventType.KeyDown)
        {
            if (SelectedBlock != null)
                SelectedBlock = null;
            else if (SelectedItem != null)
                SelectedItem = null;
        }
        if (GUI.Button(new Rect(Vector2.one * 4, new Vector2(80, 30)), IsEditor ? "Back" : "Close"))
        {
            if (SelectedItem != null)
                SelectedItem = null;
            OnCloseWindow.Invoke();
        }
    }
    static bool drag;
    static bool canDrag;
    static float dragDelta;
    public static void EndWorkspace()//End clipping space
    {
        if (Event.current.button == 1)
        {
            if(Event.current.type == EventType.MouseDown)
            {
                canDrag = true;
                foreach (var hit in Blocks)
                {
                    if (hit.Item1.Contains(Event.current.mousePosition))
                    {
                        canDrag = false;
                        break;
                    }
                }
            }
            if (canDrag && Event.current.type == EventType.MouseDrag)
            {
                dragDelta += Event.current.delta.magnitude;
                if (dragDelta > 5)
                {
                    drag = true;
                }
                if (drag)
                {
                    offset += Event.current.delta;
                }
            }
            if (Event.current.type == EventType.MouseUp)
            {
                drag = false;
                dragDelta = 0f;
            }
        }
        TryCreateNewConnection();

        GUI.EndClip();
    }

    public static void DrawBlocks(Vector2 offset) // display modernizations
    {
        SetDefault(new Rect(offset + Vector2.one * 200, storage.BlockGUISize));
        if (SelectedItem == null)
            return;
        for (int i = 0; i < SelectedItem.Modernizations.Count; i++)
        {
            SelectedItem.Modernizations[i].DrawLines(SelectedItem.Modernizations);
        }
        for (int i = 0; i < SelectedItem.Modernizations.Count; i++)
        {
            List<PropertyBlock> branch = new List<PropertyBlock>();
            SelectedItem.Modernizations[i].DrawGUI(Default, SelectedItem.Stock, Blocks, SelectedBlock == null ? "" : SelectedBlock.GetID(), IsEditor);
        }
        Draw();
    }
    #endregion


    #region Helpers
    private static NewConnection newConnection;
    private class NewConnection
    {
        public PropertyBlock from;
        public Vector2 to;

        public NewConnection(PropertyBlock from, Vector2 to)
        {
            this.from = from;
            this.to = to;
        }
    }
    public static void CreateNewConnection(PropertyBlock from, Vector2 to)
    {
        newConnection = new NewConnection(from, to);
    }
    private static void TryCreateNewConnection()
    {
        if (newConnection != null)
        {
            foreach (var hit in Blocks)
            {
                if (hit.Item1.Contains(newConnection.to))
                {
                    if (hit.Item2 != newConnection.from && newConnection.from.Connections.Contains(hit.Item2.GetID()) == false)
                    {
                        newConnection.from.Connections.Add(hit.Item2.GetID());
                        break;
                    }
                }
            }
            newConnection = null;
        }
    }

    public static void ScyncChainges()
    {
        foreach (var item in storage.Items)
        {
            if (item.Resources.Count > storage.SelfResources.Count)
            {
                item.Resources.RemoveRange(storage.SelfResources.Count, item.Resources.Count - storage.SelfResources.Count);
            }
            for (int i = 0; i < storage.SelfResources.Count; i++)
            {
                if (item.Resources.Count < storage.SelfResources.Count)
                {
                    item.Resources.Add(new Resource());
                }

                item.Resources[i].Name = storage.SelfResources[i].Name;
                item.Resources[i].ResourceType = storage.SelfResources[i].ResourceType;
            }
            foreach(var block in item.Modernizations)
            {
                for(int i = 0; i < block.Properties.Count; i++)
                {
                    var pr = storage.SavedProperties.FirstOrDefault(x => x.Name == block.Properties[i].property.Name);
                    if (pr != null)
                    {
                        block.Properties[i].property = pr;
                    }
                    else
                    {
                        block.Properties[i].property = new SavedProperty("empty", "", typeof(float));
                    }
                }
            }
        }
    }
    static void SetFullNames()// add group prefix to modernizations
    {
        foreach (var hit in SelectedBlock.Properties)
        {
            if (hit.property.Name == string.Empty)
                continue;
            var list = SelectedBlock.Properties.Where(x => x.property.FieldName == hit.property.FieldName && x.Group > 0 ).ToList();
            if (list.Count > 0)
            {

                hit.groupName = (" (group:" + hit.Group + ") ");

            }
            else
            {
                if (hit.Group > 0)
                {
                    hit.groupName = (" (group:" + hit.Group + ") ");
                }
                else
                {
                    hit.groupName = string.Empty;
                }
            }
        }
    }
    public static void ResetBlocksList()// collect modernizations list as List<INamedID>
    {
        if (SelectedItem == null)
            return;
        BlocksIDs = new List<INamedID>();
        foreach (var Hit in SelectedItem.Modernizations)
        {
            if(Hit.IsDefault == false && Hit != SelectedBlock)
                BlocksIDs.Add(Hit);
        }
    }
    public static void BlockSelection()// select modernization on mouse click
    {
        if (Event.current.type == EventType.MouseUp && Event.current.button == 0 && !drag)
        {
            SelectedBlock = null;
            DescriptionRect = new Rect();
            foreach (var hit in Blocks)
            {
                if (hit.Item1.Contains(Event.current.mousePosition))
                {
                    GUI.FocusControl("");
                    SelectedBlock = hit.Item2;
                    SelectedBlockRoots = SelectedItem.GetModernizationRoots(SelectedBlock);
                    CheckCanExplore();
                    break;
                }
            }
        }
    }
    public static void CheckCanExplore()
    {
        CanExplore = false;
        foreach (var block in SelectedItem.Modernizations.Where(x => SelectedItem.Stock.Contains(x.GetID()) || x.IsDefault))
        {
            if (block.Connections.Contains(SelectedBlock.GetID()))
                CanExplore = true;
        }
    }
    static void EditorOptions()
    {
#if UNITY_EDITOR
        IsEditor = GUI.Toggle(new Rect(300, 5, 100, 20), IsEditor, new GUIContent { text = "IsEditor" });
        if (GUI.Button(new Rect(400, 5, 150, 22), "Set Storage Dirty"))
            EditorUtility.SetDirty(storage);

        Undo.RecordObject(storage, "StorageEditor");

        if (Event.current.type != EventType.Repaint && SelectedBlock != null)
            SetFullNames();
#endif
    }

    #endregion

}
#region Editor
#if UNITY_EDITOR
public class StorageEditorWindow : EditorWindow
{
    public static EditorWindow window;
    public static bool redraw;
    [MenuItem("Tools/Storage editor")]
    public static void Initiate()
    {
        window = GetWindow(typeof(StorageEditorWindow), false, "StorageEditor", true);
        StorageEditor.IsEditor = true;
        window.minSize = new Vector2(700, 500);
    }

    private void OnEnable()
    {
        StorageEditor.Init();
        Storage.IPAisapplyed = false;
    }

    private void OnFocus()
    {
        StorageEditor.ScyncChainges();
    }

    private void OnGUI()
    {
        if (!StorageEditor.storage)
            StorageEditor.Init();
        if (!window)
            window = GetWindow(typeof(StorageEditorWindow), false, "StorageEditor", true);


        //if (Event.current.type == EventType.Repaint)
        {
            StorageEditor.DrawGUI(new Rect(0, 0, Screen.width, Screen.height));
            window.Repaint();
        }
    }
}
#endif
#endregion