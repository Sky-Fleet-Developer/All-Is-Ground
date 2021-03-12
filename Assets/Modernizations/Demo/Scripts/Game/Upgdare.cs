using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Modernizations;

public class Upgdare : MonoBehaviour
{
    public Text Exp;
    public Text Money;
    public Button ShowUpgrades;
    public Text AvailableMods;
    public Animator NewModAvailebleAnim;
    public UIStorageEditor UIEditor;
    int availeble;

    private void Start()
    {
        ShowUpgrades.onClick.AddListener(delegate
        {
            if (UIEditor)
            {
                UIEditor.gameObject.SetActive(true);
                UIEditor.SelectedItem.Item = Storage.GetItem("Main");
            }
            else
            StorageEditor.SelectedItem = Storage.GetItem("Main");
        });
        StorageEditor.OnCloseWindow += (delegate
        {
            availeble = 0;
        });
        Storage.OnResourcesChainge += (delegate
        {
            int nm = Storage.GetAvailableModernizations("Main").Count;
            if (nm != availeble)
            {
                availeble = nm;
                NewModAvailebleAnim.enabled = true;
                NewModAvailebleAnim.Play(0);
            }
            AvailableMods.text = nm > 0 ? "+" + nm + " new mods" : "";
        });
        int c = Storage.GetAvailableModernizations("Main").Count;
        if (c != availeble)
        {
            availeble = c;
            NewModAvailebleAnim.enabled = true;
            NewModAvailebleAnim.Play(0);
        }
        AvailableMods.text = c > 0 ? "+" + c + " new mods" : "";
        if (UIEditor)
        {
            UIEditor.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        Exp.text = "Experience: " + Storage.GetGlobalResourceValue("Experience") + ".";
        Money.text = "Money: " + Storage.GetGlobalResourceValue("Money") + ".";
    }
}
