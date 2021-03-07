using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
[AddComponentMenu("UIHelpers/UILink"), DefaultExecutionOrder(-1000)]
public partial class UILink : MonoBehaviour
{
    [NonSerialized]
    public RectTransform RectTransform;
    [NonSerialized]
    public Image Image;
    [NonSerialized]
    public Button Button;
    [NonSerialized]
    public Text Text;
    [NonSerialized]
    public Slider Slider;
    [NonSerialized]
    public Scrollbar Scrollbar;
    [NonSerialized]
    public InputField InputField;
    [NonSerialized]
    public Outline Outline;
    [NonSerialized]
    public CanvasGroup CanvasGroup;
    [NonSerialized]
    public Toggle Toggle;
    [NonSerialized]
    public ScrollRing ScrollRing;
    [NonSerialized]
    public Pooling Pooling;
    [NonSerialized]
    public ColorFaiding ColorFaiding;

    public Dictionary<string, UILink> Childrens = new Dictionary<string, UILink>();
    [NonSerialized]
    public List<UILink> AllChildrens;
    public bool HideChildsAtStart;
    public bool HideSelfAtStart;

    bool init = false;

    void Awake()
    {
        AllChildrens = new List<UILink>();
        if (!init)
            Init();
    }

    public void Init()
    {
        if (init)
            return;
        init = true;
        RectTransform = GetComponent<RectTransform>();
        Image = GetComponent<Image>();
        Button = GetComponent<Button>();
        Text = GetComponent<Text>();
        Slider = GetComponent<Slider>();
        Scrollbar = GetComponent<Scrollbar>();
        InputField = GetComponent<InputField>();
        Outline = GetComponent<Outline>();
        CanvasGroup = GetComponent<CanvasGroup>();
        Toggle = GetComponent<Toggle>();
        ScrollRing = GetComponent<ScrollRing>();
        if (TryGetComponent(out Pooling)) Pooling.Initialize();
        ColorFaiding = GetComponent<ColorFaiding>();

        GetChildrens();
        if (HideChildsAtStart)
        {
            foreach (KeyValuePair<string, UILink> Hit in Childrens)
            {
                Hit.Value.Init();
                Hit.Value.gameObject.SetActive(false);
            }
        }

        OnInit();
    }

    private void Start()
    {
        if (HideSelfAtStart)
            gameObject.SetActive(false);
    }

    void GetChildrens()
    {
        try
        {
            Childrens = new Dictionary<string, UILink>();
            foreach (UILink Hit in GetComponentsInChildren<UILink>(true))
            {
                if (Hit.transform.parent == transform)
                {
                    if (!Childrens.ContainsKey(Hit.name))
                    {
                        if (Hit && Hit != this)
                            Childrens.Add(Hit.name, Hit);
                    }
                    else
                        Debug.Log("KeyAlredyExist! Ёбана");
                }
                else
                {
                    if (Hit && Hit != this)
                        AllChildrens.Add(Hit);
                }
            }
        }catch(Exception e)
        {
            Debug.LogError(e);
        }
    }

    public UILink GetChildByName(string name, bool FirstChild = true)
    {
        if (!FirstChild)
        {
            foreach (UILink Hit in AllChildrens)
                if (Hit.name == name)
                    return Hit;
        }
        if (Childrens.ContainsKey(name))
            return Childrens[name];
        else
            return null;
    }
}
