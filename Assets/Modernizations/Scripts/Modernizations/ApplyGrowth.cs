using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[AddComponentMenu("Modernizations/ApplyGrowth")]
public class ApplyGrowth : MonoBehaviour
{
    [HideInInspector]
    public int Item;
    [Tooltip("search for child objects?")]
    public bool ApplyToChildrens;
    public bool ApplyOnStart = true;
    void Start()
    {
        if(ApplyOnStart)
            Apply();
        StorageEditor.OnCloseWindow.AddListener(Apply);
    }
    public void Apply()
    {
        Storage.Instance.Items[Item].ApplyGrowth(gameObject, ApplyToChildrens);
    }

}
#if UNITY_EDITOR
[CustomEditor(typeof(ApplyGrowth))]
public class ApplyGrowthEditor : Editor
{
    public ApplyGrowth Target;
    public List<string> variants;
    private void OnEnable()
    {
        Target = (ApplyGrowth)target;
        variants = new List<string>();
        foreach(var hit in Storage.Instance.Items)
        {
            variants.Add(hit.Name);
        }
    }
    public override void OnInspectorGUI()
    {
        if (variants.Count > 0)
        {
            GUIEditor.Begin();
            GUILayout.BeginHorizontal();
            GUILayout.Label("ItemSet:");
            string val = GUIEditor.PopUp(GUILayoutUtility.GetRect(150, 22), variants[Target.Item], variants, "AppGr" + Target.name + Target.GetInstanceID());
            Target.Item = variants.IndexOf(val);
            GUILayout.EndHorizontal();
        }
        base.OnInspectorGUI();
        if (variants.Count > 0)
            GUIEditor.Draw();
    }
}
#endif