using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class RectExtensions
{
    public static bool RectMatch(this Rect rect, Vector2 point)
    {
        return point.x > rect.x && point.y > rect.y && point.x < rect.xMax && point.y < rect.yMax;
    }

    public static Rect TransformRect(this Rect rect, Rect transform)
    {
        return new Rect(rect.x + transform.x, rect.y + transform.y, rect.width, rect.height);
    }
    public static Rect Add(this Rect rect, Vector2  position)
    {
        return new Rect(rect.x + position.x, rect.y + position.y, rect.width, rect.height);
    }

    public static Rect FollowMouse(Vector2 Size, Rect Screen)
    {
        return (new Rect(Mathf.Clamp(Event.current.mousePosition.x - Size.x, Screen.x, Screen.width - Size.x), Mathf.Clamp(Event.current.mousePosition.y - Size.y, Screen.y, Screen.height - Size.y), Size.x, Size.y));
    }
}
