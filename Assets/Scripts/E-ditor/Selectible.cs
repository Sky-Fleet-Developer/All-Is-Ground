using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Curves;
using static MonoBehaviourPlus;

#if UNITY_EDITOR
public class Selectible : Editor
{
    int controlID;
    Event e;
    public Vector2 SBStart;
    public Vector2 SBEnd;
    public bool send;

    public bool PointsSelection(ref List<CurveItem3d> Points, ref List<int> Selected, Transform Local)
    {
        if (Vector2.Distance(SBStart, SBEnd) > 4f)
        {
            DrawRectangle(SBStart, SBEnd);
        }

        for (int i = 0; i < Points.Count; i++)
        {
            if (Selected.Contains(i))
                Handles.color = Color.red;
            Handles.CubeHandleCap(0, Points[i].position, Quaternion.identity, 0.05f * HandleUtility.GetHandleSize(Points[i].position), EventType.Repaint);
            Handles.color = Color.white;
        }

        if (SelectionDrag())
        {
            if (!e.control)
                Selected = new List<int>();
            if (Vector2.Distance(SBStart, SBEnd) > 4f)
            {
                Vector2 st = new Vector2(Mathf.Min(SBStart.x, SBEnd.x), Mathf.Min(SBStart.y, SBEnd.y));
                Vector2 en = new Vector2(Mathf.Max(SBStart.x, SBEnd.x), Mathf.Max(SBStart.y, SBEnd.y));
                for (int i = 0; i < Points.Count; i++)
                {
                    Vector3 point = SceneView.lastActiveSceneView.camera.WorldToScreenPoint(Local.TransformPoint(Points[i].position));
                    if (new Rect(st, en - st).RectMatch(new Vector2(point.x, point.y)))
                    {
                        if (!Selected.Contains(i))
                            Selected.Add(i);
                    }
                }
            }
            SBStart = Vector2.zero;
            SBEnd = Vector2.zero;
            return true;
        }
        return false;
    }


    public bool EdgesSelection(ref List<CurveItem3d> Points, int interpolation, ref List<int> Connections, ref List<int> Selected, List<bool> inverted, Transform Local, Symmetrys symmetry)
    {
        if (Vector2.Distance(SBStart, SBEnd) > 4f)
        {
            DrawRectangle(SBStart, SBEnd);
        }

        for (int i = 1; i < Connections.Count; i += 2)
        {
            if (Selected.Contains(i-1))
                Handles.color = Color.red;
            else
                Handles.color = inverted[i / 2] ? Color.blue : Color.green;

            //Handles.DrawLine(Points[Selected[i]], Points[Selected[i]]);
            int last = Connections[i - 1], next = Connections[i];
            int interp = interpolation;
            if (Points[last].direction.magnitude < 0.01 && Points[next].direction.magnitude < 0.01)
                interp = 1;

            Vector3 scale = Vector3.one;
            switch (symmetry)
            {
                case Symmetrys.None:
                    break;
                case Symmetrys.X:
                    scale.x *= -1;
                    break;
                case Symmetrys.Y:
                    scale.y *= -1;
                    break;
                case Symmetrys.Z:
                    scale.z *= -1;
                    break;
            }

            Vector3 lastPos = Points[last].position;

            for (int n = 1; n <= interp; n++)
            {
                float val = (float)n / interp;
                Vector3 plane = SplineLerp4(Points[last].position, Points[last].position + Points[last].direction, Points[next].position - Points[next].direction, Points[next].position, val);
                Debug.DrawLine(Local.TransformPoint(lastPos), Local.TransformPoint(plane), Handles.color);

                if (symmetry != Symmetrys.None)
                    Debug.DrawLine(Local.TransformPoint(Vector3.Scale(lastPos, scale)), Local.TransformPoint(Vector3.Scale(plane, scale)), Handles.color);
                lastPos = plane;
            }
        }

        Handles.color = Color.white;

        if (SelectionDrag())
        {
            int frenquency = 10;

            if (!e.control)
                Selected = new List<int>();
            if (Vector2.Distance(SBStart, SBEnd) > 4f)
            {
                Vector2 st = new Vector2(Mathf.Min(SBStart.x, SBEnd.x), Mathf.Min(SBStart.y, SBEnd.y));
                Vector2 en = new Vector2(Mathf.Max(SBStart.x, SBEnd.x), Mathf.Max(SBStart.y, SBEnd.y));
                for (int i = 1; i < Connections.Count; i+=2)
                {
                    int last = Connections[i - 1], next = Connections[i];
                    int interp = interpolation;
                    if (Points[last].direction.magnitude < 0.01 && Points[next].direction.magnitude < 0.01)
                        interp = 1;
                    for (int n = 0; n < interp; n++)
                    {
                        float valA = (float)n / interp;
                        float valB = (float)(n + 1) / interp;
                        Vector3 planeA = SplineLerp4(Points[last].position, Points[last].position + Points[last].direction, Points[next].position - Points[next].direction, Points[next].position, valA);
                        Vector3 planeB = SplineLerp4(Points[last].position, Points[last].position + Points[last].direction, Points[next].position - Points[next].direction, Points[next].position, valB);
                        Vector3 pointA = SceneView.lastActiveSceneView.camera.WorldToScreenPoint(Local.TransformPoint(planeA));
                        Vector3 pointB = SceneView.lastActiveSceneView.camera.WorldToScreenPoint(Local.TransformPoint(planeB));
                        for (int f = 0; f <= frenquency; f++)
                        {
                            Vector3 point = Vector3.Lerp(pointA, pointB, (float)f / frenquency);
                            if (new Rect(st, en - st).RectMatch(new Vector2(point.x, point.y)))
                            {
                                if (!Selected.Contains(i - 1))
                                    Selected.Add(i - 1);
                            }
                        }
                    }
                }
            }
            SBStart = Vector2.zero;
            SBEnd = Vector2.zero;
            return true;
        }
        return false;
    }

    public bool SelectionDrag()
    {
        e = Event.current;
        if (e.alt || e.button == 1 || e.button == 2)
            return false;

        controlID = GUIUtility.GetControlID(FocusType.Passive);
        switch (Event.current.GetTypeForControl(controlID))
        {
            case EventType.MouseDown:
                Down();
                GUIUtility.hotControl = controlID;
                Event.current.Use();
                break;
            case EventType.MouseDrag:
                if (send)
                    Drag();
                GUIUtility.hotControl = controlID;
                Event.current.Use();
                break;
            case EventType.MouseUp:
                GUIUtility.hotControl = 0;
                Event.current.Use();
                if (send)
                {
                    return true;
                }
                break;
            case EventType.KeyDown:
                if (e.control && e.keyCode == KeyCode.A)
                {
                    GUIUtility.hotControl = 0;
                    Event.current.Use();
                    SBStart = Vector2.zero;
                    SBEnd = new Vector2(Screen.width, Screen.height);
                    return true;
                }
                break;
        }
        return false;
    }

    void DrawRectangle(Vector2 start, Vector2 end)
    {
        Matrix4x4 mat = Handles.matrix;
        Handles.matrix = Matrix4x4.identity;
        Ray ray1 = SceneView.lastActiveSceneView.camera.ScreenPointToRay(start);
        Ray ray2 = SceneView.lastActiveSceneView.camera.ScreenPointToRay(end);
        Ray ray3 = SceneView.lastActiveSceneView.camera.ScreenPointToRay(new Vector2(start.x, end.y));
        Ray ray4 = SceneView.lastActiveSceneView.camera.ScreenPointToRay(new Vector2(end.x, start.y));


        Handles.DrawLine(ray1.origin + ray1.direction * 5, ray3.origin + ray3.direction * 5);
        Handles.DrawLine(ray1.origin + ray1.direction * 5, ray4.origin + ray4.direction * 5);
        Handles.DrawLine(ray2.origin + ray2.direction * 5, ray3.origin + ray3.direction * 5);
        Handles.DrawLine(ray2.origin + ray2.direction * 5, ray4.origin + ray4.direction * 5);
        Handles.matrix = mat;
    }

    void Down()
    {
        if (!e.shift)
            return;
        send = Event.current.button == 0;
        SBStart = new Vector2(Event.current.mousePosition.x, Screen.height - Event.current.mousePosition.y - 36);
        SBEnd = new Vector2(Event.current.mousePosition.x, Screen.height - Event.current.mousePosition.y - 36);
    }

    void Drag()
    {
        if (!e.shift)
            return;
        SBEnd = new Vector2(Event.current.mousePosition.x, Screen.height - Event.current.mousePosition.y - 36);
    }

}
#endif