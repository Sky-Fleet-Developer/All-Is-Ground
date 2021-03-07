using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
[RequireComponent(typeof(Button))]
public class ButtonObjectActive : MonoBehaviour
{
    public GameObject[] Activate;
    public GameObject[] Deactivate;
    public GameObject[] OnOff;
    void Start()
    {
        GetComponent<Button>().onClick.AddListener(delegate
        {
            foreach (var hit in Activate)
                hit.SetActive(true);
            foreach (var hit in Deactivate)
                hit.SetActive(false);
            foreach (var hit in OnOff)
                hit.SetActive(!hit.GetActive());
        });
    }
}
