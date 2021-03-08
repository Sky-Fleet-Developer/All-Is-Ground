using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Vector2Extensions
{
    /// <summary>
    /// value must to be outside of the rect.
    /// </summary>
    public static Vector2 RectangleCut(this Vector2 value, Vector2 to, Rect rect)
    {
        Vector2 RightUpCorner = rect.position + Vector2.right * rect.size.x;
        Vector2 LeftDownCorner = rect.position + Vector2.up * rect.size.y;
        Vector2 RightDownCorner = rect.position + rect.size;

        value = value.Cut(to, rect.position, RightUpCorner);//Up edge cut
        value = value.Cut(to, rect.position, LeftDownCorner);//Left edge cut
        value = value.Cut(to, RightUpCorner, RightDownCorner);//Right edge cut
        value = value.Cut(to, RightDownCorner, LeftDownCorner);//Bottom edge cut

        return value;
    }

    /*\                \
     *from           from
     *  \                \            
     *   \                \           
     *    \                \          
     *st---ret---end        \
     *      \                \
     *       \                to
     *        \                \
     *         to        st-----ret-------end
     *          \                \
    */
    public static Vector2 Cut(this Vector2 from, Vector2 to, Vector2 cutStart, Vector2 cutEnd)
    {
        Vector2 BA = from - to;
        float ABLength = BA.magnitude;
        Vector2 CD = cutEnd - cutStart;
        float dot = Vector2.Dot(BA.normalized, CD.normalized);
        if (Mathf.Abs(dot) == 1) //is collinear
            return from;
        Vector2 TrStart = (cutStart - from).TransformDirection(BA);
        Vector2 TrEnd = (cutEnd - from).TransformDirection(BA);
        CD = TrEnd - TrStart;
        if (TrStart.y == 0 && TrEnd.y == 0 || TrStart.y == TrEnd.y)
            return from;
        float intersectionValue = TrStart.y / (TrStart.y - TrEnd.y);
        Vector2 intersect = Vector2.LerpUnclamped(TrStart, TrEnd, intersectionValue).InverseTransformDirection(BA) + from;
        Vector2 TrI = (intersect - from).TransformDirection(BA);
        if (TrI.x < 0 && TrI.x > -ABLength)
        {
            return intersect;
        }
        return from;
    }

    public static Vector2 TransformDirection(this Vector2 value, Vector2 argument)
    {
        float length = argument.magnitude;
        float angle = Mathf.Acos(argument.x / length) * argument.y.Side(true);
        return value.Rotate(-angle);
    }

    public static Vector2 InverseTransformDirection(this Vector2 value, Vector2 argument)
    {
        float length = argument.magnitude;
        float angle = Mathf.Acos(argument.x / length) * argument.y.Side(true);
        return value.Rotate(angle);
    }

    /// <param name="angle">Degree in radians</param>
    public static Vector2 Rotate(this Vector2 value, float angle)
    {
        return new Vector2(value.x * Mathf.Cos(angle) - value.y * Mathf.Sin(angle), value.x * Mathf.Sin(angle) + value.y * Mathf.Cos(angle));
    }
}
