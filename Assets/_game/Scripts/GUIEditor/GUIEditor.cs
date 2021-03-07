using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GUIEditor : MonoBehaviourPlus
{
    protected Vector2 DragAndDropStart;
    bool drag = false;

    Vector2 GetNearest(List<Vector2> list, Vector2 to)
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

    protected bool DragAndDrop(Vector2 position, bool down, List<Vector2> Snaps)
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
    static List<System.Tuple<int, Rect>> Walls;
    static int time;
    public static bool popupIsOpen;
    static Texture2D pixel;
    public static Rect Default;
    public static Rect Last;
    static float delta;
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
            }
            GUILayout.TextField(value + "");
            delta = delta + Event.current.delta.y * 0.15f;
            value = Mathf.Ceil((value + Event.current.delta.y * Mathf.Clamp(Mathf.Abs(delta), 0.3f, 100f) * 0.1f) * 200) / 200;
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
            float v;
            if (FieldStringValues[name][FieldStringValues[name].Length - 1] != ',' && float.TryParse(FieldStringValues[name], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.CurrentCulture, out v))
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
            }
            GUI.TextField(rect.Add(new Vector2(10, 0)), value + "");
            delta = delta + Event.current.delta.y * 0.15f;
            value = Mathf.Ceil((value + Event.current.delta.y * Mathf.Clamp(Mathf.Abs(delta), 0.3f, 100f) * 0.1f) * 200) / 200;
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
                FieldStringValues[name] = value + "";
                return value;
            }

            FieldStringValues[name] = FieldStringValues[name].Replace('.', ',');
            if (FieldStringValues[name] == string.Empty)
                FieldStringValues[name] = "0";
            float v;
            if (FieldStringValues[name][FieldStringValues[name].Length - 1] != ',' && float.TryParse(FieldStringValues[name], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.CurrentCulture, out v))
            {
                value = v;
                FieldStringValues[name] = value + "";
            }
            FieldFocuses[name] = curr;
        }

        return value;
    }
    public static void DrawLine(Vector2 from, Vector2 to, Color color)
    {
        if (!pixel)
            SetPixel();
        Vector2 direction = to - from;
        Vector2 mid = (from + to) / 2;
        Vector2 size = new Vector2(2, direction.magnitude);
        float angle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
        var mat = GUI.matrix;
        GUIUtility.RotateAroundPivot(-angle, mid);
        GUI.color = color;
        GUI.DrawTexture(new Rect(mid - size * 0.5f, size), pixel);
        GUI.color = Color.white;
        GUI.matrix = mat;
    }
    public static void SetPixel()
    {
        Color[] p = new Color[1];
        p[0] = Color.white;
        pixel = new Texture2D(1, 1);
        pixel.SetPixels(p);
        pixel.Apply();
    }
    public static void SetDefault(Rect rect)
    {
        Default = rect;
        Last = rect;
    }
    public static Rect GetOffset(float x, float y, float w, float h)
    {
        return new Rect(Default.position + new Vector2(x, y), Default.size + new Vector2(w, h));
    }
    public static Rect GetNextWithOffset(float x, float y, float w, float h)
    {
        Last.position += new Vector2(x, y);
        return new Rect(Last.position, Last.size + new Vector2(w, h));
    }
    public static Rect GetNext(Vector2 offset)
    {
        Last.position += offset;
        return Last;
    }
    public static Rect GetNextWithoutOffset(float x, float y, float w, float h)
    {
        return new Rect(Last.position + new Vector2(x, y), Last.size + new Vector2(w, h));
    }
    public static Rect GetWithSize(float x, float y, float w, float h)
    {
        return new Rect(Default.position + new Vector2(x, y), new Vector2(w, h));
    }
    static Vector2 mousePosition;

    public static List<Popup> popups;
    public class Popup
    {
        public string value;
        public List<string> variants;
        public string name;
        public Rect rect;
        public bool Drown = false;
        public int time;
    }
    public static string PopUpLayout(string value, List<string> variants, string name)
    {
        if (!FieldStringValues.ContainsKey(name))
        {
            FieldStringValues.Add(name, value + "");
            FieldDown.Add(name, false);
        }

        GUI.color = Color.white * 0.8f;
        GUILayout.Box(value, GUILayout.Width(120), GUILayout.Height(22));
        GUI.color = Color.white;
        Rect lastRect = GUILayoutUtility.GetLastRect();
        if (Event.current.type == EventType.MouseDown && lastRect.Contains(Event.current.mousePosition))
        {
            FieldDown[name] = !FieldDown[name];
        }

        Popup old = popups.Where(x => x.name == name).SingleOrDefault();
        if (old != null)
        {
            old.rect = lastRect;
            old.variants = variants;
            old.Drown = false;
            return old.value;
        }
        else
        {
            popups.Add(new Popup { value = value, time = time++, rect = lastRect, variants = variants, name = name, Drown = false });
            return value;
        }
    }
    public static string PopUp(Rect rect, string value, List<string> variants, string name)
    {
        if (!FieldStringValues.ContainsKey(name))
        {
            FieldStringValues.Add(name, value + "");
            FieldDown.Add(name, false);
        }

        GUI.color = Color.white * 0.8f;
        GUI.Box(rect, value);
        GUI.color = Color.white;
        Rect lastRect = rect;
        if (Event.current.type == EventType.MouseDown && lastRect.Contains(Event.current.mousePosition))
        {
            FieldDown[name] = !FieldDown[name];
        }

        Popup old = popups.Where(x => x.name == name).SingleOrDefault();
        if (old != null)
        {
            old.rect = lastRect;
            old.variants = variants;
            old.Drown = false;
            return old.value;
        }
        else
        {
            popups.Add(new Popup { value = value, time = time++, rect = lastRect, variants = variants, name = name, Drown = false });
            return value;
        }
    }
    static string DrawPopup(Popup popup)
    {
        if (FieldDown[popup.name])
        {
            if (HasWallOnMouse(popup.time))
            {
                FieldDown[popup.name] = false;
                return popup.value;
            }
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
            if (Event.current.type == EventType.MouseUp)
            {
                for (int i = 0; i < vars.Count; i++)
                    if (vars[i].Contains(Event.current.mousePosition))
                        popup.value = popup.variants[i];
                FieldDown[popup.name] = false;
                popupIsOpen = false;
            }

        }
        return popup.value;
    }
    public static void AddWall(Rect rect)
    {
        Walls.Add( new System.Tuple<int, Rect>(time, rect));
    }

    public static bool HasWallOnMouse(int Time)
    {
        foreach(var hit in Walls)
        {
            if (Time > hit.Item1 && hit.Item2.Contains(mousePosition))
                return true;
        }
        return false;
    }

    public static void Begin()
    {
        mousePosition = Event.current.mousePosition;

        if (popups == null)
            popups = new List<Popup>();
        Walls = new List<System.Tuple<int, Rect>>();
    }

    public static void Draw()
    {
        foreach(var hit in popups)
        {
            if (!hit.Drown)
            {
                hit.Drown = true;
                hit.value = DrawPopup(hit);
            }
        }
    }
}
