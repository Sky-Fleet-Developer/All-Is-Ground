using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
[CreateAssetMenu(fileName = "GameValues", menuName = "GameValues")]
public class GameValues : ScriptableObject
{
    public static GameValues Instance;
    public static bool EnableDebug { get => Instance.enableDebug; }
    public bool enableDebug;
    public static LayerMask SupportsGround { get => Instance.supportsGround; }
    public LayerMask supportsGround;
    public static LayerMask ChargeLayer { get => Instance.chargeLayer; }
    public LayerMask chargeLayer;
    public static LayerMask CumulativeChargeLayer { get => Instance.cumulativeChargeLayer; }
    public LayerMask cumulativeChargeLayer;
    public static LayerMask AimingLayer { get => Instance.aimingLayer; }
    public LayerMask aimingLayer;
    public static LayerMask BotAimingLayer { get => Instance.botAimingLayer; }
    public LayerMask botAimingLayer;
    public static float DischargeDelay { get => Instance.dischargeDelay; }
    public float dischargeDelay = 0.06f;
    public static Gradient BattleTimerGradient { get => Instance.battleTimerGradient; }
    public Gradient battleTimerGradient;
    public static List<PlayebleLevel> Levels { get => Instance.levels; }
    public List<PlayebleLevel> levels;
    public static float SupportRayRadius { get => Instance.supportRayRadius; }
    public float supportRayRadius = 0.2f;
    public static string GetRandomTooltip()
    {
        return Instance.Tooltips[Random.Range(0, Instance.Tooltips.Count - 1)];
    }

    public List<string> Tooltips;

    public static void Init()
    {
        Instance = (GameValues)Resources.Load("GameValues", typeof(GameValues));
    }

    
}
[System.Serializable]
public class PlayebleLevel
{
    public string Name;
    public int BuildID;
    public Sprite Image;
}