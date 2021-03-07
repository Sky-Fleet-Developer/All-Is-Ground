using System.Linq;
using UnityEngine;

public static class Vector3Extensions
{

    public static Vector3 GetRelativePositionFrom(this Vector3 position, Matrix4x4 from)
    {
        return from.MultiplyPoint(position);
    }

    public static Vector3 GetRelativePositionTo(this Vector3 position, Matrix4x4 to)
    {
        return to.inverse.MultiplyPoint(position);
    }

    public static Vector3 GetRelativeDirectionFrom(this Vector3 direction, Matrix4x4 from)
    {
        return from.MultiplyVector(direction);
    }

    public static Vector3 GetRelativeDirectionTo(this Vector3 direction, Matrix4x4 to)
    {
        return to.inverse.MultiplyVector(direction);
    }

    public static Vector3 GetMirror(this Vector3 vector, Vector3 axis)
    {
        if (axis == Vector3.right)
        {
            vector.x *= -1f;
        }
        if (axis == Vector3.up)
        {
            vector.y *= -1f;
        }
        if (axis == Vector3.forward)
        {
            vector.z *= -1f;
        }
        return vector;
    }

    public static float Distance2d(Vector3 a, Vector3 b)
    {
        Vector2 A = Vector2.right * a.x + Vector2.up * a.z;
        Vector2 B = Vector2.right * b.x + Vector2.up * b.z - A;
        return Mathf.Sqrt(B.x * B.x + B.y * B.y);
    }


    public static string ToStringEx(this Vector3 val)
    {
        string spl = ".";
        return val.x.ToString() + spl + val.y.ToString() + spl + val.z.ToString();
    }

    /// <summary>
    /// Parce string format "xx,xxx. yy,yyy. zz,zzzz"
    /// </summary>
    /// <param name="val">xx,xxx. yy,yyy. zz,zzzz</param>
    /// <returns></returns>
    public static Vector3 Parce(string val)
    {
        var parce = val.Split(new char[] { '.' });
        return new Vector3(float.Parse(parce[0]), float.Parse(parce[1]), float.Parse(parce[2]));
    }
}