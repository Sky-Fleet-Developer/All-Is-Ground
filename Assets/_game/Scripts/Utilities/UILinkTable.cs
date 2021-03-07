using UnityEngine;
using System.Collections;
public partial class UILink
{
    public static UILink ProgressBar;
    public static UILink CentralMessege;
    public static UILink LeverScroll;
    public static UILink Scroll;
    public static UILink DarkScreen;
    public static UILink MainCanvas;
    public static UILink LevelLoad;
    public StaticObjectTypes StaticObject = StaticObjectTypes.None;
    public enum StaticObjectTypes
    {
        None = 1000,
        ProgressBar = 0,
        CentralMessege = 1,
        LeverScroll = 2,
        Scroll = 3,
        DarkScreen = 4,
        MainCanvas = 5,
        LevelLoad = 6,
    }
    public void OnInit()
    {
        switch (StaticObject)
        {
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
    }
}
