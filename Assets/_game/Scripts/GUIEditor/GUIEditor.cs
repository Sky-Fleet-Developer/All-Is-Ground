using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GUIEditor
{
    static Vector2 DragAndDropStart;
    static bool drag = false;

    public static Vector2 GetNearest(List<Vector2> list, Vector2 to)
    {
        Vector2 result = Vector2.zero;
        float dist = 10000000;
        foreach(var hit in list)
        {
            float d = Vector2.Distance(hit, to);
            if (d < dist)
            {
                dist = d;
                result = hit;
            }
        }
        return result;
    }

    public static bool DragAndDrop(Vector2 position, bool down, List<Vector2> Snaps)
    {
        Vector2 nearest = GetNearest(Snaps, position);
        if (Vector2.SqrMagnitude(nearest - position) < 100)
                position = nearest;
        
        if (!drag)
        {
            if (down)
            {
                drag = true;
                DragAndDropStart = position;
            }
        }
        else
        {
            Vector2 offset = position - DragAndDropStart;
            Vector2 mid = (DragAndDropStart + position) / 2f;
            float angle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;

            var mat = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, DragAndDropStart);
            GUI.Box(new Rect(DragAndDropStart.x, DragAndDropStart.y - 5, offset.magnitude, 10), "");
            GUI.matrix = mat;
            if (!down)
            {
                drag = false;
                return true;
            }
        }
        return false;
    }

    public static Vector3 Vector3DelayedInputField(Vector3 value, string name)
    {
        GUILayout.BeginHorizontal();
        float X = value.x;
        X = DelayedFloatField(X, name + "X");
        float Y = value.y;
        Y = DelayedFloatField(Y, name + "Y");
        float Z = value.z;
        Z = DelayedFloatField(Z, name + "Z");
        GUILayout.EndHorizontal();
        return new Vector3(X, Y, Z);
    }

    public static Vector3 Vector3InputFieldLayout(Vector3 value, string name)
    {
        GUILayout.BeginHorizontal();
        float X = value.x;
        X = FloatFieldLayout(X, name + "X");
        float Y = value.y;
        Y = FloatFieldLayout(Y, name + "Y");
        float Z = value.z;
        Z = FloatFieldLayout(Z, name + "Z");
        GUILayout.EndHorizontal();
        return new Vector3(X, Y, Z);
    }
    public static Vector2 Vector2InputFieldLayout(Vector2 value, string name)
    {
        GUILayout.BeginHorizontal();
        float X = value.x;
        X = FloatFieldLayout(X, name + "X");
        float Y = value.y;
        Y = FloatFieldLayout(Y, name + "Y");
        GUILayout.EndHorizontal();
        return new Vector2(X, Y);
    }
    public static Vector3 Vector3InputField(Rect rect, Vector3 value, string name)
    {
        rect.width /= 3;
        float X = value.x;
        X = FloatField(rect, X, name + "X");
        float Y = value.y;
        Y = FloatField(rect.Add(new Vector2(rect.width, 0)), Y, name + "Y");
        float Z = value.z;
        Z = FloatField(rect.Add(new Vector2(rect.width*2, 0)), Z, name + "Z");
        return new Vector3(X, Y, Z);
    }
    public static Vector2 Vector2InputField(Rect rect, Vector2 value, string name)
    {
        rect.width /= 2;
        float X = value.x;
        X = FloatField(rect, X, name + "X");
        float Y = value.y;
        Y = FloatField(rect.Add(new Vector2(rect.width, 0)), Y, name + "Y");
        return new Vector3(X, Y);
    }


    static Dictionary<string, string> FieldStringValues = new Dictionary<string, string>();
    static Dictionary<string, bool> FieldFocuses = new Dictionary<string, bool>();
    static Dictionary<string, bool> FieldDown = new Dictionary<string, bool>();
    public static bool popupIsOpen;
    static Texture2D line;
    static Texture2D arrow;

    static float delta;
    public static Vector2 GetPointFromAlignment(Rect rect, SpriteAlignment alignment, bool invert = false)
    {
        Vector2 ret = new Vector2();
        switch (alignment)
        {
            case SpriteAlignment.Center:
                ret = rect.center;
                break;
            case SpriteAlignment.TopLeft:
                ret = rect.position;
                break;
            case SpriteAlignment.TopCenter:
                ret = rect.position + new Vector2(rect.size.x * 0.5f, 0);
                break;
            case SpriteAlignment.TopRight:
                ret = rect.position + new Vector2(rect.size.x, 0);
                break;
            case SpriteAlignment.LeftCenter:
                ret = rect.position + new Vector2(0, rect.size.y * 0.5f);
                break;
            case SpriteAlignment.RightCenter:
                ret = rect.position + new Vector2(rect.size.x, rect.size.y * 0.5f);
                break;
            case SpriteAlignment.BottomLeft:
                ret = rect.position + new Vector2(0, rect.size.y);
                break;
            case SpriteAlignment.BottomCenter:
                ret = rect.position + new Vector2(rect.size.x * 0.5f, rect.size.y);
                break;
            case SpriteAlignment.BottomRight:
                ret = rect.position + new Vector2(rect.size.x, rect.size.y);
                break;
        }
        if (invert)
        {
            ret -= rect.center;
            ret *= -1;
            ret += rect.center;
        }
        return ret;
    }
    public static float DelayedFloatField(float value, string name)
    {
        if (!FieldFocuses.ContainsKey(name))
        {
            FieldFocuses.Add(name, false);
            FieldStringValues.Add(name, value + "");
            FieldDown.Add(name, false);
        }

        if (FieldDown[name])
        {
            GUILayout.Box("", GUILayout.Width(10), GUILayout.Height(20));
            if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
            {
                delta = 0f;
                FieldDown[name] = false;
            }
            GUILayout.TextField(value + "");
            delta = delta + Event.current.delta.y * 0.15f;
            value = Mathf.Ceil((value + Event.current.delta.y * Mathf.Clamp(Mathf.Abs(delta), 0.3f, 100f) * 0.1f) * 200) / 200;
            FieldStringValues[name] = value + "";
        }
        else
        {
            delta = 0f;
            if (GUILayout.RepeatButton("", GUILayout.Width(10), GUILayout.Height(20)))
                FieldDown[name] = true;

            GUI.SetNextControlName(name);
            FieldStringValues[name] = GUILayout.TextField(FieldStringValues[name]);

            bool curr = GUI.GetNameOfFocusedControl() == name;
            if (FieldFocuses[name] != curr && !curr)
            {
                FieldStringValues[name] = FieldStringValues[name].Replace('.', ',');
                if (FieldStringValues[name] == string.Empty)
                    FieldStringValues[name] = "0";
                float v;
                if (float.TryParse(FieldStringValues[name], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.CurrentCulture, out v))
                {
                    value = v;
                }
                FieldStringValues[name] = value + "";
            }

            FieldFocuses[name] = curr;
        }



        return value;
    }
    public static float FloatFieldLayout(float value, string name)
    {
        if (!FieldFocuses.ContainsKey(name))
        {
            FieldFocuses.Add(name, false);
            FieldStringValues.Add(name, value + "");
            FieldDown.Add(name, false);
        }
        if (FieldDown[name])
        {
            GUILayout.Box("", GUILayout.Width(10), GUILayout.Height(20));
            if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
            {
                delta = 0f;
                FieldDown[name] = false;
                Event.current.Use();
            }
            GUILayout.TextField(value + "");
            delta = delta - Event.current.delta.y * 0.1f;
            value = Mathf.Ceil((value - Event.current.delta.y * Mathf.Clamp(Mathf.Abs(delta), 0.3f, 100f) * 0.1f) * 200) / 200;
            FieldStringValues[name] = value + "";
        }
        else
        {
            if (GUILayout.RepeatButton("", GUILayout.Width(10), GUILayout.Height(20)))
                FieldDown[name] = true;

            GUI.SetNextControlName(name);
            FieldStringValues[name] = GUILayout.TextField(FieldStringValues[name]);

            bool curr = GUI.GetNameOfFocusedControl() == name;

            if (!curr)
            {
                FieldStringValues[name] = value + "";
                return value;
            }

            FieldStringValues[name] = FieldStringValues[name].Replace('.', ',');
            if (FieldStringValues[name] == string.Empty)
                FieldStringValues[name] = "0";

            char start = FieldStringValues[name][0];
            bool startIsDot = start == ',';
            if (startIsDot)
            {
                FieldStringValues[name].Insert(0, "0");
            }
            char end = FieldStringValues[name][FieldStringValues[name].Length - 1];
            bool endIsNotDot = end != ',';
            float v;
            if (endIsNotDot && float.TryParse(FieldStringValues[name], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.CurrentCulture, out v))
            {
                value = v;
                FieldStringValues[name] = value + "";
            }
            FieldFocuses[name] = curr;
        }

        return value;
    }
    public static float FloatField(Rect rect, float value, string name)
    {
        if (!FieldFocuses.ContainsKey(name))
        {
            FieldFocuses.Add(name, false);
            FieldStringValues.Add(name, value + "");
            FieldDown.Add(name, false);
        }
        rect.width -= 10;
        if (FieldDown[name])
        {
            GUI.Box(new Rect(rect.x, rect.y, 10, 20), "");
            if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
            {
                delta = 0f;
                FieldDown[name] = false;
                Event.current.Use();
            }
            GUI.TextField(rect.Add(new Vector2(10, 0)), value + "");
            delta = delta - Event.current.delta.y * 0.1f;
            value = Mathf.Ceil((value - Event.current.delta.y * Mathf.Clamp(Mathf.Abs(delta), 0.3f, 100f) * 0.1f) * 200) / 200;
            FieldStringValues[name] = value + "";
        }
        else
        {
            if (GUI.RepeatButton(new Rect(rect.x, rect.y, 10, 20), ""))
                FieldDown[name] = true;

            GUI.SetNextControlName(name);
            FieldStringValues[name] = GUI.TextField(rect.Add(new Vector2(10, 0)), FieldStringValues[name]);

            bool curr = GUI.GetNameOfFocusedControl() == name;

            if (!curr)
            {
                FieldStringValues[name] = value.ToString();
                return value;
            }

            FieldStringValues[name] = FieldStringValues[name].Replace('.', ',');
            if (FieldStringValues[name] == string.Empty)
                FieldStringValues[name] = "0";

            char start = FieldStringValues[name][0];
            bool startIsDot = start == ',';
            if(startIsDot)
            {
                FieldStringValues[name].Insert(0, "0");
            }
            char end = FieldStringValues[name][FieldStringValues[name].Length - 1];
            bool endIsNotDot = end != ',';
            float v;
            if (endIsNotDot && float.TryParse(FieldStringValues[name], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.CurrentCulture, out v))
            {
                value = v;
                FieldStringValues[name] = value.ToString();
            }
            FieldFocuses[name] = curr;
        }

        return value;
    }

    public static void DrawLine(Vector2 from, Vector2 to, Color color, bool CutByRect = false, bool drawArrow = false)
    {
        if (CutByRect)
        {
            Rect LinesCutout = new Rect(0, 0, Screen.width, Screen.height);
            bool fromIn = LinesCutout.Contains(from);
            bool toIn = LinesCutout.Contains(to);
            if (fromIn == false && toIn == false)
                return;
            if (fromIn != toIn)
            {
                if (fromIn)
                {
                    to = to.RectangleCut(from, LinesCutout);
                }
                else
                {
                    from = from.RectangleCut(to, LinesCutout);
                }
            }
        }
        if (!line)
            SetLineTex();
        if (!arrow)
            SetArrowTex();
        Vector2 direction = to - from;
        Vector2 mid = (from + to) / 2;
        Vector2 size = new Vector2(direction.magnitude, 3);
        float angle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
        var mat = GUI.matrix;
        GUIUtility.RotateAroundPivot(-angle + 90, mid);
        GUI.color = color;
        GUI.DrawTexture(new Rect(mid - size * 0.5f + Vector2.down, size), line);
        if(drawArrow)
            GUI.DrawTexture(new Rect(mid - Vector2.one * 5 + Vector2.down, Vector2.one * 10), arrow);
        GUI.color = Color.white;
        GUI.matrix = mat;
    }

    public static void SetLineTex()
    {
        Color[] p = new Color[3];
        p[0] = new Color(1, 1, 1, 0.5f);
        p[1] = Color.black;
        p[2] = new Color(1, 1, 1, 0.5f);
        line = new Texture2D(1, 3);
        line.filterMode = FilterMode.Bilinear;
        line.SetPixels(p);
        line.Apply();
    }
    public static void SetArrowTex()
    {
        Color[] p = new Color[10*9];
        bool[] b = new bool[]
        {
            true, true, false, false, false, false, false, false, false, false,
            true, true, true, true, false, false, false, false, false, false,
            true, true, true, true, true, true, false, false, false, false,
            true, true, true, true, true, true, true, true, false, false,
            true, true, true, true, true, true, true, true, true, true,
            true, true, true, true, true, true, true, true, false, false,
            true, true, true, true, true, true, false, false, false, false,
            true, true, true, true, false, false, false, false, false, false,
            true, true, false, false, false, false, false, false, false, false
        };
        for (int w = 0; w < 10; w++)
        {
            for (int h = 0; h < 9; h++)
            {
                p[w * 9 + h] = b[w * 9 + h] ? Color.white : Color.clear;
            }
        }
        arrow = new Texture2D(10, 9);
        arrow.filterMode = FilterMode.Bilinear;
        arrow.wrapMode = TextureWrapMode.Clamp;
        arrow.SetPixels(p);
        arrow.Apply();
    }

    static Vector2 mousePosition;

    public static List<string> GetNamedLinksNames(List<INamedID> links)
    {
        List<string> names = new List<string>();
        links.ForEach(x => names.Add(x.GetName()));
        return names;
    }
    public static List<string> GetNamedLinksIDs(List<INamedID> links)
    {
        List<string> names = new List<string>();
        links.ForEach(x => names.Add(x.GetID()));
        return names;
    }

    public static List<Popup> popups;
    public class Popup
    {
        public string value;
        public List<string> variants;
        public string name;
        public Rect rect;
        public bool Drown = false;
    }

    public static System.Enum PopUp(Rect rect, System.Enum value, string name)
    {
        var variants = System.Enum.GetNames(value.GetType()).ToList();

        return System.Enum.ToObject(value.GetType(), variants.IndexOf(PopUp(rect, value.ToString(), variants, name))) as System.Enum;
    }

    public static INamedID PopUp(Rect rect, INamedID value, List<INamedID> variants, string name)
    {
        List<string> names = GetNamedLinksNames(variants);
        List<string> ids = GetNamedLinksIDs(variants);

        var pu = PopUp(rect, value.GetID(), value.GetName(), ids, names, name);
        var val = variants.FirstOrDefault(x => x.GetID() == pu);
        if (val != null)
            return val;
        return value;
    }

    public static string PopUp(Rect rect, string value, List<INamedID> variants, string name)
    {
        List<string> names = GetNamedLinksNames(variants);
        List<string> ids = GetNamedLinksIDs(variants);
        string vName = value;
        var val = ids.IndexOf(value);
        if (val != -1)
            vName = names[val];

        return PopUp(rect, value, vName, ids, names, name);
    }

    public static string PopUp(Rect rect, string value, List<string> variants, string name)
    {
        return PopUp(rect, value, value, variants, variants, name);
    }

    public static string PopUp(Rect rect, string value, string showValue, List<string> variants, List<string> showVariants, string name)
    {
        if (!FieldStringValues.ContainsKey(name))
        {
            FieldStringValues.Add(name, value + "");
            FieldDown.Add(name, false);
        }

        GUI.color = Color.white * 0.8f;
        GUI.Box(rect, showValue);
        GUI.color = Color.white;
        Rect lastRect = rect;
        if (Event.current.type == EventType.MouseDown && lastRect.Contains(Event.current.mousePosition))
        {
            FieldDown[name] = !FieldDown[name];
            Event.current.Use();
        }
        List<Rect> vars = new List<Rect>();
        for (int i = 0; i < variants.Count; i++)
        {
            Rect pos = rect.Add(Vector2.up * 20 * (i - 1));
            vars.Add(pos);
        }
        if (FieldDown[name])
        {
            if (Event.current.type == EventType.MouseUp)
            {
                for (int i = 0; i < vars.Count; i++)
                    if (vars[i].Contains(Event.current.mousePosition))
                        value = variants[i];
                FieldDown[name] = false;
                popupIsOpen = false;
                Event.current.Use();
            }
        }
        Popup old = popups.Where(x => x.name == name).SingleOrDefault();
        if (old != null)
        {
            old.value = showValue;
            old.rect = lastRect;
            old.variants = showVariants;
            old.Drown = false;
        }
        else
        {
            popups.Add(new Popup { value = showValue, rect = lastRect, variants = showVariants, name = name, Drown = false });
        }
        return value;
    }

    static void DrawPopup(Popup popup)
    {
        if (FieldDown[popup.name])
        {
            /*if (HasWallOnMouse(popup.time))
            {
                FieldDown[popup.name] = false;
                return popup.value;
            }*/
            popupIsOpen = true;
            List<Rect> vars = new List<Rect>();
            GUI.color = Color.gray;
            Rect common = new Rect(popup.rect.position - Vector2.one * 2 - Vector2.up * 20, new Vector2(popup.rect.size.x + 4, popup.rect.size.y * popup.variants.Count * 0.95f +2));
            if (!common.Contains(Event.current.mousePosition) && Event.current.type == EventType.Repaint)
                GUI.color = Color.red;

            //FieldDown[name] = false;
            GUI.Box(common, "");
            GUI.color = Color.white;
            for (int i = 0; i < popup.variants.Count; i++)
            {
                Rect pos = popup.rect.Add(Vector2.up * 20 * (i-1));
                vars.Add(pos);
                if (pos.Contains(Event.current.mousePosition))
                    GUI.color = Color.gray;
                else
                    GUI.color = Color.white;

                GUI.Box(pos, popup.variants[i]);
            }
            GUI.color = Color.white;

        }
    }

    public static void Begin()
    {
        mousePosition = Event.current.mousePosition;
        
        if (popups == null)
            popups = new List<Popup>();
    }

    public static void Draw()
    {
        foreach(var hit in popups)
        {
            if (!hit.Drown)
            {
                hit.Drown = true;
                DrawPopup(hit);
            }
        }
    }
}
