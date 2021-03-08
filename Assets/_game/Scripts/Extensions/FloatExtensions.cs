using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public  static class FloatExtensions
{
    public static float ClampAngle(this float value)
    {
        if (value >= 180f)
            value -= 360f;
        if (value <= -180f)
            value += 360f;
        return value;
    }

    public static float ClampAngle(this float value, float less, float more)
    {
        if (value >= less)
            value -= 360f;
        if (value <= more)
            value += 360f;
        return value;
    }
    public static float Side(this float value, bool zeroIsOne = false)
    {
        if (value == 0f)
            return zeroIsOne ? 1 : 0;
        return value / Mathf.Abs(value);
    }
}
