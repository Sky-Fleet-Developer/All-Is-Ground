using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
[AddComponentMenu("UIHelpers/UILink")]
public class UILink : MonoBehaviour
{
    public RectTransform RectTransform;
    public Image Image;
    public Button Button;
    public Text Text;
    public Slider Slider;
    public Scrollbar Scrollbar;
    public InputField InputField;
    public Outline Outline;
    public ColorFaiding ColorFaiding;
    public ScrollRing ScrollRing;
    public Dictionary<string, UILink> Childrens = new Dictionary<string, UILink>();
    public List<UILink> AllChildrens;
    public Pooling Pooling;
    public PoolObject PoolObject;
    public bool HideChildsAtStart;
    public bool HideSelfAtStart;
    public int Ch;

    bool init = false;
    [Space(15)]
    public StaticObjectTypes SiaticObject;

    public static UILink ProgressBar;
    public static UILink ProgressSlider;
    public static UILink CentralMessege;
    public static UILink LeverScroll;
    public static UILink Scroll;
    public static UILink DarkScreen;
    public static UILink MainCanvas;
    public static UILink LevelLoad;

    public enum StaticObjectTypes
    {
        None = 0,
        ProgressBar = 1,
        CentralMessege = 2,
        LeverScroll = 3,
        Scroll = 4,
        ProgressSlider = 5,
        DarkScreen = 6,
        MainCanvas = 7,
        LevelLoad = 8
    }

    void Awake()
    {
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
        Pooling = GetComponent<Pooling>();
        if (Pooling)
            Pooling.Initialize();
        PoolObject = GetComponent<PoolObject>();
        Outline = GetComponent<Outline>();
        ColorFaiding = GetComponent<ColorFaiding>();
        ScrollRing = GetComponent<ScrollRing>();
        GetChildrens();
        foreach (KeyValuePair<string, UILink> Hit in Childrens)
        {
            Hit.Value.Init();
            if(HideChildsAtStart)
                Hit.Value.gameObject.SetActive(false);
        }


        switch (SiaticObject)
        {
            case StaticObjectTypes.None:
                break;
            case StaticObjectTypes.ProgressBar:
                ProgressBar = this;
                break;
            case StaticObjectTypes.CentralMessege:
                CentralMessege = this;
                break;
            case StaticObjectTypes.LeverScroll:
                LeverScroll = this;
                break;
            case StaticObjectTypes.Scroll:
                Scroll = this;
                break;
            case StaticObjectTypes.ProgressSlider:
                ProgressSlider = this;
                break;
            case StaticObjectTypes.DarkScreen:
                DarkScreen = this;
                break;
            case StaticObjectTypes.MainCanvas:
                MainCanvas = this;
                break;
            case StaticObjectTypes.LevelLoad:
                LevelLoad = this;
                break;
        }
        if (HideSelfAtStart)
            gameObject.SetActive(false);
    }

    void GetChildrens()
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
            }
            else
            {
                if (Hit && Hit != this)
                AllChildrens.Add(Hit);
            }
        }
        Ch = Childrens.Count;
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
