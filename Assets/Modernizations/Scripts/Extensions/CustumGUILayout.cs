using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CustumGUILayout
{
    public static Rect Default;
    public static Rect Last;
    public static void SetDefault(Rect rect)
    {
        Default = rect;
        Last = rect;
    }
    public static Rect GetOffset(float x, float y, float w, float h)
    {
        return new Rect(Last.position + new Vector2(x, y), Last.size + new Vector2(w, h));
    }
    public static Rect SetOffset(float x, float y, float w, float h)
    {
        Last = new Rect(Last.position + new Vector2(x, y), Last.size + new Vector2(w, h));
        return Last;
    }
    public static Rect GetNextWithOffset(float x, float y, float w, float h)
    {
        Last.position += new Vector2(x, y);
        return new Rect(Last.position, Last.size + new Vector2(w, h));
    }
    public static Rect GetNextWithoutOffset(float x, float y, float w, float h)
    {
        return new Rect(Last.position + new Vector2(x, y), new Vector2(w, h));
    }
}
