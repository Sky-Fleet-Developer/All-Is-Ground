using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using static MonoBehaviourPlus;

namespace Curves
{
    public class Curve : MonoBehaviour
    {
        public List<int> SelectedPoints;
        public List<int> SelectedEdges;

        public List<CurveItem3d> Anchors;
        public List<int> Connections;
        public List<bool> Inverted;
        public List<Vector3> InterpolatedAnchors;
        public List<int> InterpolatedConnections;
        public List<bool> InterpolatedInverted;
        public Symmetrys Symmetry;
        public bool Flip;
        public Axis Orientation = Axis.X;
        public Vector2 VolumeScale;
        public Vector2 Offset;
        public Vector3 Max;
        public Vector3 Min;

        public float WeldDistance = 0.01f;

        public UnityEvent OnChainge = new UnityEvent();

        [Range(1, 50)]
        public int Interpolation = 10;
        [Range(3, 50)]
        [System.NonSerialized]
        public Transform Tr;
        public void Reset()
        {
            Connections = new List<int>();
            InterpolatedConnections = new List<int>();
            Inverted = new List<bool>();
            InterpolatedInverted = new List<bool>();
            InterpolatedAnchors = new List<Vector3>();
            Anchors = new List<CurveItem3d>();
            Anchors.Add(new CurveItem3d(Vector3.zero, Vector3.forward));
            Anchors.Add(new CurveItem3d(Vector3.forward, Vector3.forward));
            Connections.Add(0);
            Connections.Add(1);
            Tr = transform;
        }


        public void Awake()
        {
            Tr = transform;
        }
#if UNITY_EDITOR
        public void OnDrawGizmos()
        {
            if (Selection.Contains(this.gameObject))
                return;
            Tr = transform;
            for (int i = 1; i < InterpolatedConnections.Count; i += 2)
            {
                Debug.DrawLine(Tr.TransformPoint(InterpolatedAnchors[InterpolatedConnections[i]]), Tr.TransformPoint(InterpolatedAnchors[InterpolatedConnections[i - 1]]), Color.green);
            }
        }
#endif
        public void Calculate()
        {
            InterpolatedAnchors = new List<Vector3>();
            InterpolatedConnections = new List<int>();
            InterpolatedInverted = new List<bool>();
            Max = Vector3.one * -10000;
            Min = Vector3.one * 10000;

            for (int i = 1; i < Connections.Count; i += 2)
            {
                int last = Connections[i - 1];
                int next = Connections[i];
                if (Inverted.Count <= i / 2)
                    Inverted.Add(false);
                AddInterpolated(last, next, Symmetrys.None, Inverted[i / 2]);
            }
            if (Symmetry != Symmetrys.None)
                for (int i = 1; i < Connections.Count; i += 2)
                {
                    int last = Connections[i - 1];
                    int next = Connections[i];
                    AddInterpolated(last, next, Symmetry, Inverted[i / 2]);
                }

            Weld();

            for (int i = 0; i < OnChainge.GetPersistentEventCount(); i++)
            {
                if (OnChainge.GetPersistentTarget(i))
                    OnChainge.GetPersistentTarget(i).GetType().GetMethod(OnChainge.GetPersistentMethodName(i)).Invoke(OnChainge.GetPersistentTarget(i), new object[] { null });
            }
        }

        void AddInterpolated(int last, int next, Symmetrys symmetry, bool inverted)
        {
            int conns = 0;
            for (int i = 0; i < Connections.Count; i++)
            {
                if (Connections[i] == last)
                    i++;
            }
            int interp = Interpolation;
            if (Anchors[last].direction.magnitude < 0.01 && Anchors[next].direction.magnitude < 0.01)
                interp = 1;
            for (int n = 0; n <= interp; n++)
            {
                float val = (float)n / interp;
                Vector3 plane = SplineLerp4(Anchors[last].position, Anchors[last].position + Anchors[last].direction, Anchors[next].position - Anchors[next].direction, Anchors[next].position, val);
                switch (symmetry)
                {
                    case Symmetrys.None:
                        break;
                    case Symmetrys.X:
                        plane.x *= -1;
                        break;
                    case Symmetrys.Y:
                        plane.y *= -1;
                        break;
                    case Symmetrys.Z:
                        plane.z *= -1;
                        break;
                }
                AddInterpolatedLine(plane, !(conns < 2 && n < 1), inverted);
            }
        }

        void AddInterpolatedLine(Vector3 point, bool createConnections, bool inverted)
        {
            InterpolatedAnchors.Add(point);
            Max.x = Mathf.Max(Max.x, point.x);
            Max.y = Mathf.Max(Max.y, point.y);
            Max.z = Mathf.Max(Max.z, point.z);
            Min.x = Mathf.Min(Min.x, point.x);
            Min.y = Mathf.Min(Min.y, point.y);
            Min.z = Mathf.Min(Min.z, point.z);
            if (createConnections)
            {
                InterpolatedConnections.Add(InterpolatedAnchors.Count - 2);
                InterpolatedConnections.Add(InterpolatedAnchors.Count - 1);
                InterpolatedInverted.Add(inverted);
            }
        }

        public void Weld()
        {
            List<Vector3> vertices = new List<Vector3>();
            List<int> edges = new List<int>();
            for (int i = 1; i < InterpolatedConnections.Count; i += 2)
            {
                int A, B;
                A = GetCloseVertex(ref vertices, InterpolatedAnchors[InterpolatedConnections[i] - 1], WeldDistance);
                B = GetCloseVertex(ref vertices, InterpolatedAnchors[InterpolatedConnections[i]], WeldDistance);
                if (A != B)
                {
                    edges.Add(A);
                    edges.Add(B);
                }
            }
            InterpolatedAnchors = vertices;
            InterpolatedConnections = edges;
        }

        public void DeleteByAngle(float Minimal, float Maximal)
        {
            for (int i = 0; i < Anchors.Count; i++)
            {
                int A, B;
                GetConnections(i, out A, out B);
                if (A != -1 && B != -1)
                {
                    Vector3 AI = Anchors[i].position - Anchors[A].position;
                    Vector3 IB = Anchors[B].position - Anchors[i].position;
                    float angle = Vector3.Angle(AI, IB);
                    if (angle >= Minimal && angle <= Maximal)
                    {
                        int revers, C = GetConnection(A, i, out revers);
                        Connections.RemoveRange(C, 2);
                        C = GetConnection(i, B, out revers);
                        Connections.RemoveRange(C, 2);
                        Connections.Add(A);
                        Connections.Add(B);
                        RemuveAnchor(i);
                        i--;
                    }
                }
            }
            Calculate();
        }

        public void BrakeForSymmetry()
        {
            Plane SymmPlane = new Plane(SymmetryToNormal(Symmetry, Flip), 0);
            CutByPlane(SymmPlane);
        }

        public void CutByPlane(Plane plane)
        {
            for (int i = 1; i < Connections.Count; i += 2)
            {
                int A = Connections[i - 1], B = Connections[i];
                Vector3 vA = Anchors[A].position;
                Vector3 vB = Anchors[B].position;
                float dA = plane.GetDistanceToPoint(vA);
                float dB = plane.GetDistanceToPoint(vB);
                if (Side(dA) != Side(dB) && Mathf.Abs(dA) > WeldDistance + 0.01f && Mathf.Abs(dB) > WeldDistance + 0.01f)
                {
                    bool inv = false;
                    if (dB < 0)
                        inv = true;

                    dA = Mathf.Abs(dA);
                    dB = Mathf.Abs(dB);
                    int del;
                    if (inv)
                    {
                        Anchors[B].position = Vector3.Lerp(vA, vB, dA / (dA + dB));
                        del = B;
                    }
                    else
                    {
                        Anchors[A].position = Vector3.Lerp(vA, vB, dA / (dA + dB));
                        del = A;
                    }

                    /*GetConnections(del, out A, out B);
                    if (A != -1 && plane.GetDistanceToPoint(Anchors[A].position) < -0.05f)
                    {
                        int revers, c = GetConnection(A, del, out revers);
                        Connections.RemoveRange(c, 2);
                    }
                    if (B != -1 && plane.GetDistanceToPoint(Anchors[B].position) < -0.05f)
                    {
                        int revers, c = GetConnection(B, del, out revers);
                        Connections.RemoveRange(c, 2);
                    }*/
                }
            }

            for (int i = 0; i < Anchors.Count; i++)
            {
                if (plane.GetDistanceToPoint(Anchors[i].position) < -WeldDistance - 0.01f)
                {
                    int A, B, revers;
                    GetConnections(i, out A, out B);
                    if (A != -1)
                    {
                        int cA;
                        cA = GetConnection(A, i, out revers);
                        Connections.RemoveRange(cA, 2);
                    }
                    if (B != -1)
                    {
                        int cB;
                        cB = GetConnection(B, i, out revers);
                        Connections.RemoveRange(cB, 2);
                    }
                    RemuveAnchor(i--);
                }
            }
            Calculate();
        }

        public void RemuveAnchor(int i)
        {
            Anchors.RemoveRange(i, 1);

            for (int n = 0; n < Connections.Count; n++)
            {
                if (Connections[n] > i)
                    Connections[n] -= 1;
            }
            if (SelectedPoints != null)
                for (int n = 0; n < SelectedPoints.Count; n++)
                {
                    if (SelectedPoints[n] > i)
                        SelectedPoints[n] -= 1;
                }
        }

        public int GetCloseVertex(ref List<Vector3> vertices, Vector3 point, float weldDistance)
        {
            int vert = vertices.Count;
            bool found = false;
            for (int i = 0; i < vertices.Count; i++)
            {
                if (Vector3.Distance(vertices[i], point) < weldDistance)
                {
                    vert = i;
                    found = true;
                }
            }
            if (!found)
            {
                vertices.Add(point);
            }
            return vert;
        }

        public int GetConnection(int A, int B, out int revers)
        {
            for (int c = 1; c < Connections.Count; c += 2)
            {
                if (Connections[c] == A && Connections[c - 1] == B)
                {
                    revers = 0;
                    return c - 1;
                }
                if (Connections[c - 1] == A && Connections[c] == B)
                {
                    revers = 1;
                    return c - 1;
                }
            }
            revers = -1;
            return -1;
        }

        public void GetConnections(int Anchor, out int A, out int B)
        {
            A = -1;
            B = -1;
            for (int c = 1; c < Connections.Count; c += 2)
            {
                if (Connections[c] == Anchor)
                {
                    if (A == -1)
                    {
                        A = Connections[c - 1];
                    }
                    else
                    {
                        B = Connections[c - 1];
                    }
                }
                if (Connections[c - 1] == Anchor)
                {
                    if (A == -1)
                    {
                        A = Connections[c];
                    }
                    else
                    {
                        B = Connections[c];
                    }
                }
            }
        }

        public int GetNextIA(int IA, int LastIA)
        {
            for (int i = 1; i < InterpolatedConnections.Count; i += 2)
            {
                if (InterpolatedConnections[i] == IA && InterpolatedConnections[i - 1] != LastIA)
                {
                    return InterpolatedConnections[i - 1];
                }
                if (InterpolatedConnections[i - 1] == IA && InterpolatedConnections[i] != LastIA)
                {
                    return InterpolatedConnections[i];
                }
            }
            return -1;
        }

        public int GetNextAnchor(int AnchorN, int LastAnchorN)
        {
            for (int i = 1; i < Connections.Count; i += 2)
            {
                if (Connections[i] == AnchorN && Connections[i - 1] != LastAnchorN)
                {
                    return Connections[i - 1];
                }
                if (Connections[i - 1] == AnchorN && Connections[i] != LastAnchorN)
                {
                    return Connections[i];
                }
            }
            return -1;
        }

        public Vector3 SymmetryToNormal(Symmetrys s, bool Flip)
        {
            Vector3 ret = Vector3.zero;
            switch (s)
            {
                case Symmetrys.X:
                    ret = Vector3.right;
                    break;
                case Symmetrys.Y:
                    ret = Vector3.up;
                    break;
                case Symmetrys.Z:
                    ret = Vector3.forward;
                    break;
            }
            if (Flip)
                ret *= -1;
            return ret;
        }

        public int GetConnectionsCount(int anh)
        {
            int count = 0;
            for (int c = 0; c < Connections.Count; c++)
            {
                if (Connections[c] == anh)
                    count++;
            }
            return count;
        }

        public void SortConnections()
        {
            List<int> NewConnections = new List<int>();
            for (int i = 0; i < Anchors.Count; i++)
            {
                if (!NewConnections.Contains(i))
                {
                    int A, B;
                    GetConnections(i, out A, out B);
                    if (B != -1)
                        NewConnections.AddRange(GetLine(i, B, true));
                    if (!NewConnections.Contains(A))
                        if (A != -1)
                            NewConnections.AddRange(GetLine(i, A, false));
                    Debug.Log(NewConnections.Count);
                }
            }
            Connections = NewConnections;
        }

        public List<int> GetLine(int Anchor, int Next, bool Revers)
        {
            List<int> Line = new List<int>();
            int next = Next;
            int last = Anchor;
            if (Revers)
            {
                Line.Add(Next);
                Line.Add(Anchor);
            }
            else
            {
                Line.Add(Anchor);
                Line.Add(Next);
            }
            int i = 0, Last = last;
            while (i++ < 1000)
            {
                last = next;
                next = GetNextAnchor(next, Last);
                if (next == -1)
                    break;
                if (Revers)
                {
                    Line.Insert(0, last);
                    Line.Insert(0, next);
                }
                else
                {
                    Line.Add(last);
                    Line.Add(next);
                }

                if (next == Anchor)
                    break;
                Last = last;
            }
            return Line;
        }
        public List<int> GetInterpolatedLine(int Anchor, int Next, bool Revers)
        {
            List<int> Line = new List<int>();
            int next = Next;
            int last = Anchor;
            if (Revers)
            {
                Line.Add(Next);
                Line.Add(Anchor);
            }
            else
            {
                Line.Add(Anchor);
                Line.Add(Next);
            }
            int i = 0, Last = last;
            while (i++ < 1000)
            {
                last = next;
                next = GetNextIA(next, Last);
                if (next == -1)
                    break;
                if (Revers)
                {
                    Line.Insert(0, last);
                    Line.Insert(0, next);
                }
                else
                {
                    Line.Add(last);
                    Line.Add(next);
                }

                if (next == Anchor)
                    break;
                Last = last;
            }
            return Line;
        }
    }

    public enum Symmetrys
    {
        None = 0,
        X = 1,
        Y = 2,
        Z = 3

    }

    public enum Axis
    {
        X = 0,
        Y = 1,
        Z = 2
    }

    [System.Serializable]
    public class CurveItem3d : System.ICloneable
    {
        public Vector3 position;
        public Vector3 direction;
        public CurveItem3d(Vector3 position, Vector3 direction)
        {
            this.position = position;
            this.direction = direction;
        }

        public object Clone()
        {
            var clone = new CurveItem3d(this.position, this.direction);
            return clone;
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(Curve))]
    public class Curve_Editor : Selectible
    {
        public EditLayers EditLayer = EditLayers.Object;
        public PivotPoses PivotPose;
        bool showDef;
        bool showDeb;
        bool showNormals;
        Curve script;
        Vector3 position = Vector3.zero;
        Quaternion rotation = Quaternion.LookRotation(Vector3.right);
        Vector3 scale = Vector3.one;

        public enum PivotPoses
        {
            Local = 0,
            LocalWithSymmetry = 1
        }
        public class edge
        {
            public int A, B;

            public edge(int a, int b)
            {
                A = a;
                B = b;
            }
        }
        public enum EditLayers
        {
            Object = 0,
            Points = 1,
            Lines = 2
        }

        public override void OnInspectorGUI()
        {
            script = (Curve)target;
            EditorGUILayout.HelpBox("Selection - left shift + LMB + drag. Select more - left shift + control + LMB + drag.", MessageType.Info, true);
            showDef = EditorGUILayout.BeginFoldoutHeaderGroup(showDef, "Values");
            if (showDef)
                DrawDefaultInspector();
            EditorGUILayout.EndFoldoutHeaderGroup();
            showDeb = EditorGUILayout.BeginFoldoutHeaderGroup(showDeb, "debug");
            if (showDeb)
            {
                showNormals = EditorGUILayout.Toggle("Show normals", showNormals);
                string selpoi = string.Empty;
                for (int i = 0; i < script.SelectedPoints.Count; i++)
                    selpoi += script.SelectedPoints[i] + ", ";
                GUILayout.Box("Selected points: " + selpoi);
                string seled = string.Empty;
                for (int i = 0; i < script.SelectedEdges.Count; i++)
                    seled += script.SelectedEdges[i] + ", ";
                GUILayout.Box("Selected edges: " + seled);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            PivotSettings();

            EditLayerButtons();

            switch (EditLayer)
            {
                case EditLayers.Points:
                    if (GUILayout.Button("Add Point"))
                    {
                        Undo.RecordObject(script, "add point");
                        script.Anchors.Add(new CurveItem3d(script.Anchors[script.Anchors.Count - 1].position + script.Anchors[script.Anchors.Count - 1].direction * 2, script.Anchors[script.Anchors.Count - 1].direction));
                        script.Connections.Add(script.Anchors.Count - 2);
                        script.Connections.Add(script.Anchors.Count - 1);
                        script.Inverted.Add(false);
                        Bake();
                        EditorUtility.SetDirty(script);
                    }
                    AlignmentButtons(script.SelectedPoints);
                    EditPointButtons();
                    break;
                case EditLayers.Lines:
                    Vector3 position = Vector3.zero;
                    List<int> selpoi = new List<int>();
                    EdgesToPoints(selpoi);
                    AlignmentButtons(selpoi);
                    EditEdgeButtons();
                    break;
            }
            GUILayout.Label("-------------------------------");
            if (GUILayout.Button("Calculate"))
            {
                Bake();
            }
            if (GUILayout.Button("Sort connections"))
            {
                Undo.RecordObject(script, "Sort connections");
                script.SortConnections();
                EditorUtility.SetDirty(script);
            }
        }

        float CharfmerSize = 0.1f;
        int DivideCount = 1;

        #region Editors
        public void PivotSettings()
        {
            GUILayout.Box("Pivot");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Local"))
            {
                PivotPose = PivotPoses.Local;
                PlacePivot();
            }
            if (GUILayout.Button("Symmetry"))
            {
                PivotPose = PivotPoses.LocalWithSymmetry;
                PlacePivot();
            }
            GUILayout.EndHorizontal();
        }
        public void EditLayerButtons()
        {
            GUILayout.Box("Layers:");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Obj"))
                EditLayer = EditLayers.Object;
            if (GUILayout.Button("*"))
                EditLayer = EditLayers.Points;
            if (GUILayout.Button("/"))
                EditLayer = EditLayers.Lines;
            GUILayout.EndHorizontal();
            GUILayout.Label("-------------------------------");
        }
        public void AlignmentButtons(List<int> selpoi)
        {
            GUILayout.Box("Alignment");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("X+"))
            {
                Undo.RecordObject(script, "alignment");
                float val = -10000f;
                for (int i = 0; i < selpoi.Count; i++)
                {
                    val = Mathf.Max(script.Anchors[selpoi[i]].position.x, val);
                }
                for (int i = 0; i < selpoi.Count; i++)
                {
                    Vector3 p = script.Anchors[selpoi[i]].position;
                    script.Anchors[selpoi[i]].position = new Vector3(val, p.y, p.z);
                }
                Bake();
                EditorUtility.SetDirty(script);
            }
            if (GUILayout.Button("Y+"))
            {
                Undo.RecordObject(script, "alignment");
                float val = -10000f;
                for (int i = 0; i < selpoi.Count; i++)
                {
                    val = Mathf.Max(script.Anchors[selpoi[i]].position.y, val);
                }
                for (int i = 0; i < selpoi.Count; i++)
                {
                    Vector3 p = script.Anchors[selpoi[i]].position;
                    script.Anchors[selpoi[i]].position = new Vector3(p.x, val, p.z);
                }
                Bake();
                EditorUtility.SetDirty(script);
            }
            if (GUILayout.Button("Z+"))
            {
                Undo.RecordObject(script, "alignment");
                float val = -10000f;
                for (int i = 0; i < selpoi.Count; i++)
                {
                    val = Mathf.Max(script.Anchors[selpoi[i]].position.z, val);
                }
                for (int i = 0; i < selpoi.Count; i++)
                {
                    Vector3 p = script.Anchors[selpoi[i]].position;
                    script.Anchors[selpoi[i]].position = new Vector3(p.x, p.y, val);
                }
                Bake();
                EditorUtility.SetDirty(script);
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("X-"))
            {
                Undo.RecordObject(script, "alignment");
                float val = 10000f;
                for (int i = 0; i < selpoi.Count; i++)
                {
                    val = Mathf.Min(script.Anchors[selpoi[i]].position.x, val);
                }
                for (int i = 0; i < selpoi.Count; i++)
                {
                    Vector3 p = script.Anchors[selpoi[i]].position;
                    script.Anchors[selpoi[i]].position = new Vector3(val, p.y, p.z);
                }
                Bake();
                EditorUtility.SetDirty(script);
            }
            if (GUILayout.Button("Y-"))
            {
                Undo.RecordObject(script, "alignment");
                float val = 10000f;
                for (int i = 0; i < selpoi.Count; i++)
                {
                    val = Mathf.Min(script.Anchors[selpoi[i]].position.y, val);
                }
                for (int i = 0; i < selpoi.Count; i++)
                {
                    Vector3 p = script.Anchors[selpoi[i]].position;
                    script.Anchors[selpoi[i]].position = new Vector3(p.x, val, p.z);
                }
                Bake();
                EditorUtility.SetDirty(script);
            }
            if (GUILayout.Button("Z-"))
            {
                Undo.RecordObject(script, "alignment");
                float val = 10000f;
                for (int i = 0; i < selpoi.Count; i++)
                {
                    val = Mathf.Min(script.Anchors[selpoi[i]].position.z, val);
                }
                for (int i = 0; i < selpoi.Count; i++)
                {
                    Vector3 p = script.Anchors[selpoi[i]].position;
                    script.Anchors[selpoi[i]].position = new Vector3(p.x, p.y, val);
                }
                Bake();
                EditorUtility.SetDirty(script);
            }
            GUILayout.EndHorizontal();
        }
        public void EditPointButtons()
        {
            GUILayout.Box("Edit:");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Charfmer"))
            {
                Undo.RecordObject(script, "Charfmer");
                for (int i = 0; i < script.SelectedPoints.Count; i++)
                {
                    CurveItem3d Old = script.Anchors[script.SelectedPoints[i]];
                    CurveItem3d New = Old.Clone() as CurveItem3d;
                    script.Anchors.Add(New);

                    int A = -1;
                    int B = -1;
                    GetConnections(script.SelectedPoints[i], out A, out B);


                    if (A != -1)
                    {
                        CurveItem3d iA = script.Anchors[A];
                        New.position = FollowSpline(New.position, New.direction, iA.position, iA.direction, CharfmerSize, script.Interpolation);
                    }
                    if (B != -1)
                    {
                        CurveItem3d iB = script.Anchors[B];
                        Old.position = FollowSpline(Old.position, Old.direction, iB.position, iB.direction, CharfmerSize, script.Interpolation);
                    }
                    int revers;
                    int cA = GetConnection(A, script.SelectedPoints[i], out revers);
                    script.Connections[cA + revers] = script.Anchors.Count - 1;
                    script.Connections.Add(script.Anchors.Count - 1);
                    script.Connections.Add(script.SelectedPoints[i]);
                    script.Inverted.Add(false);
                }
                EditorUtility.SetDirty(script);
                script.Calculate();
            }
            CharfmerSize = EditorGUILayout.DelayedFloatField(CharfmerSize);
            GUILayout.EndHorizontal();


            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Delete"))
            {
                DeleteAnchors();
            }
            if (GUILayout.Button("Connect"))
            {
                Connect();
            }
            if (GUILayout.Button("Collapse"))
            {
                CollapsePoints(script.SelectedPoints);
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Turn 180"))
            {
                Turn180();
            }
            if (GUILayout.Button("Dublicate"))
            {
                DublicatePoints();
            }
            if (GUILayout.Button("Continue"))
            {
                ContinuePoints();
            }
            GUILayout.EndHorizontal();
            GUILayout.Box("Point:");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Smooth"))
            {
                MakeSmooth();
            }
            if (GUILayout.Button("Linear"))
            {
                MakeLinear();
            }
            GUILayout.EndHorizontal();
        }
        public void EditEdgeButtons()
        {
            GUILayout.Box("Edit:");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Delete"))
            {
                DeleteEdges();
            }
            if (GUILayout.Button("Collapse"))
            {
                List<int> selpoi = new List<int>();
                EdgesToPoints(selpoi);
                CollapsePoints(selpoi);
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Invert"))
            {
                Invert();
            }
            if (GUILayout.Button("Invert normal"))
            {
                InvertNormal();
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Divide"))
            {
                Divide();
            }
            DivideCount = EditorGUILayout.IntField(DivideCount);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Dublicate"))
            {
                DublicateEdges();
            }
            GUILayout.EndHorizontal();
        }
        #endregion

        #region Operations

        public void MakeSmooth()
        {
            Undo.RecordObject(script, "Smooth");

            for (int i = 0; i < script.SelectedPoints.Count; i++)
            {
                int A, B, I = script.SelectedPoints[i];
                GetConnections(I, out A, out B);
                if (A != -1 && B != -1)
                {
                    Vector3 direct = (script.Anchors[A].position - script.Anchors[I].position).normalized + (script.Anchors[B].position - script.Anchors[I].position).normalized;
                    Vector3 AB = script.Anchors[B].position - script.Anchors[A].position;
                    Vector3 cross1 = Vector3.Cross(direct, (script.Anchors[A].position - script.Anchors[B].position).normalized);
                    Vector3 cross2 = Vector3.Cross(direct, cross1);
                    script.Anchors[I].direction = cross2.normalized * AB.magnitude * 0.5f;
                }
                else
                {

                    Vector3 direct = (script.Anchors[A].position - script.Anchors[I].position) * 0.5f;
                    script.Anchors[I].direction = direct;
                }
            }

            EditorUtility.SetDirty(script);
            Bake();
        }
        public void MakeLinear()
        {
            Undo.RecordObject(script, "Linear");

            for (int i = 0; i < script.SelectedPoints.Count; i++)
            {
                script.Anchors[script.SelectedPoints[i]].direction = Vector3.zero;
            }

            EditorUtility.SetDirty(script);
            Bake();
        }
        public void Turn180()
        {
            Undo.RecordObject(script, "Turn180");

            for (int i = 0; i < script.SelectedPoints.Count; i++)
            {
                script.Anchors[script.SelectedPoints[i]].direction *= -1;
            }

            EditorUtility.SetDirty(script);
            Bake();
        }

        public void Invert()
        {
            Undo.RecordObject(script, "Invert");

            for (int i = 0; i < script.SelectedEdges.Count; i++)
            {
                int A, B;
                A = script.Connections[script.SelectedEdges[i]];
                B = script.Connections[script.SelectedEdges[i] + 1];
                script.Connections[script.SelectedEdges[i]] = B;
                script.Connections[script.SelectedEdges[i] + 1] = A;
            }

            EditorUtility.SetDirty(script);
            Bake();
        }

        public void DeleteAnchors()
        {
            Undo.RecordObject(script, "DeletePoint");

            for (int i = 0; i < script.SelectedPoints.Count; i++)
            {
                int A, B;
                GetConnections(script.SelectedPoints[i], out A, out B);

                for (int n = 1; n < script.Connections.Count; n += 2)
                {
                    if (script.Connections[n] == script.SelectedPoints[i] || script.Connections[n - 1] == script.SelectedPoints[i])
                        script.Connections.RemoveRange(n - 1, 2);
                }

                if (A != -1 && B != -1)
                {
                    script.Connections.Add(A);
                    script.Connections.Add(B);
                    script.Inverted.Add(false);
                }

                script.RemuveAnchor(script.SelectedPoints[i]);

            }
            script.SelectedPoints = new List<int>();

            EditorUtility.SetDirty(script);
            Bake();
        }

        public void Bake()
        {
            List<edge> edges = new List<edge>();
            for (int i = 1; i < script.Connections.Count; i += 2)
            {
                edge a = new edge(script.Connections[i - 1], script.Connections[i]);
                if (edges.Contains(a) || edges.Contains(new edge(script.Connections[i], script.Connections[i - 1])) || script.Connections[i - 1] == script.Connections[i])
                {
                    script.Connections.RemoveRange(i - 1, 2);
                    i -= 2;
                }
                else
                {
                    edges.Add(a);
                }
            }
            script.Calculate();
        }

        public void DeleteEdges()
        {
            Undo.RecordObject(script, "DeleteEdge");
            for (int i = 0; i < script.SelectedEdges.Count; i++)
            {
                script.Connections.RemoveRange(script.SelectedEdges[i], 2);
                for (int n = i; n < script.SelectedEdges.Count; n++)
                {
                    if (script.SelectedEdges[n] >= script.SelectedEdges[i])
                        script.SelectedEdges[n] -= 2;
                }
            }
            script.SelectedEdges = new List<int>();
            EditorUtility.SetDirty(script);
            Bake();
        }

        public void Connect()
        {
            if (script.SelectedPoints.Count <= 1)
                return;
            Undo.RecordObject(script, "Connect");
            for (int i = 1; i < script.SelectedPoints.Count; i += 2)
            {
                int A, B;
                GetConnections(script.SelectedPoints[i], out A, out B);
                if (A != -1 && B != -1)
                    continue;
                GetConnections(script.SelectedPoints[i - 1], out A, out B);
                if (A != -1 && B != -1)
                    continue;

                script.Connections.Add(script.SelectedPoints[i]);
                script.Connections.Add(script.SelectedPoints[i - 1]);
                script.Inverted.Add(false);
            }
            EditorUtility.SetDirty(script);
            Bake();
        }

        public void DublicatePoints()
        {
            Undo.RecordObject(script, "DublicatePoints");
            int count = script.SelectedPoints.Count;
            for (int i = 0; i < script.SelectedPoints.Count; i++)
            {
                script.Anchors.Add(script.Anchors[script.SelectedPoints[i]].Clone() as CurveItem3d);
            }

            script.SelectedPoints = new List<int>();

            for (int i = 0; i < count; i++)
            {
                script.SelectedPoints.Add(script.Anchors.Count - i - 1);
            }

            EditorUtility.SetDirty(script);
            Bake();
        }

        public void ContinuePoints()
        {
            Undo.RecordObject(script, "ContinuePoints");
            int count = 0;
            for (int i = 0; i < script.SelectedPoints.Count; i++)
            {
                int A, B, I = script.SelectedPoints[i];
                GetConnections(I, out A, out B);
                if (A != -1 && B == -1)
                {
                    script.Anchors.Add(script.Anchors[script.SelectedPoints[i]].Clone() as CurveItem3d);
                    script.Connections.Add(script.SelectedPoints[i]);
                    script.Connections.Add(script.Anchors.Count - 1);
                    script.Inverted.Add(false);
                    count++;
                }
            }

            script.SelectedPoints = new List<int>();

            for (int i = 0; i < count; i++)
            {
                script.SelectedPoints.Add(script.Anchors.Count - i - 1);
            }

            EditorUtility.SetDirty(script);
            Bake();
        }

        public void DublicateEdges()
        {
            Undo.RecordObject(script, "DublicateEdges");
            int count = script.SelectedEdges.Count;

            Dictionary<int, int> dubl = new Dictionary<int, int>(); //число - номер исходной точки. Точка - номер дубликата.
            for (int i = 0; i < script.SelectedEdges.Count; i++)
            {
                int A = script.Connections[script.SelectedEdges[i]], B = script.Connections[script.SelectedEdges[i] + 1];
                var pA = script.Anchors[A];
                var pB = script.Anchors[B];
                if (!dubl.ContainsKey(A))
                {
                    pA = pA.Clone() as CurveItem3d;
                    script.Anchors.Add(pA);
                    dubl.Add(A, script.Anchors.Count - 1);
                }
                else
                {
                    pA = script.Anchors[dubl[A]];
                }
                if (!dubl.ContainsKey(B))
                {
                    pB = pB.Clone() as CurveItem3d;
                    script.Anchors.Add(pB);
                    dubl.Add(B, script.Anchors.Count - 1);
                }
                else
                {
                    pB = script.Anchors[dubl[B]];
                }

                script.Connections.Add(dubl[A]);
                script.Connections.Add(dubl[B]);
                script.Inverted.Add(false);
            }

            script.SelectedEdges = new List<int>();

            for (int i = 0; i < count; i++)
            {
                script.SelectedEdges.Add(script.Connections.Count - i * 2 - 2);
            }

            EditorUtility.SetDirty(script);
            Bake();
        }

        public void CollapsePoints(List<int> selpoi)
        {
            Undo.RecordObject(script, "CollapsePoints");
            int count = selpoi.Count;
            for (int i = 0; i < selpoi.Count; i++)
            {
                int A, B, I = selpoi[i];
                GetConnections(I, out A, out B);

                if (selpoi.Contains(A))
                {
                    int revers;
                    int Conn = GetConnection(A, I, out revers);
                    script.Connections.RemoveRange(Conn, 2);

                    Vector3 mid = (script.Anchors[I].position + script.Anchors[A].position) / 2f;
                    script.Anchors[I].position = mid;

                    for (int c = 0; c < script.SelectedEdges.Count; c++)
                    {
                        if (script.SelectedEdges[c] >= Conn)
                            script.SelectedEdges[c]--;
                        int a, b;
                        a = script.Connections[script.SelectedEdges[c]];
                        b = script.Connections[script.SelectedEdges[c] + 1];

                        if (a == A || b == A || b == A || b == B)
                            script.SelectedEdges.RemoveRange(c, 1);

                        if (script.SelectedEdges[c] > script.Connections.Count - 1)
                            script.SelectedEdges.RemoveRange(c, 1);
                    }

                    for (int c = 0; c < script.Connections.Count; c++)
                    {
                        if (script.Connections[c] == A)
                            script.Connections[c] = I;
                    }
                    for (int c = 0; c < script.Connections.Count; c++)
                    {
                        if (script.Connections[c] > A)
                            script.Connections[c]--;
                    }
                    selpoi.RemoveRange(i, 1);

                    for (int c = 0; c < selpoi.Count; c++)
                    {
                        if (selpoi[c] == A)
                            selpoi[c] = I;
                        if (selpoi[c] > A)
                            selpoi[c]--;
                        if (selpoi[c] < 0)
                            selpoi.RemoveRange(c, 1);
                    }


                    script.Anchors.Remove(script.Anchors[A]);
                }
            }
            EditorUtility.SetDirty(script);
            Bake();
        }

        public void Divide()
        {
            Undo.RecordObject(script, "DivideEdges");
            int count = script.SelectedEdges.Count;

            for (int i = 0; i < script.SelectedEdges.Count; i++)
            {
                int A = script.Connections[script.SelectedEdges[i]], B = script.Connections[script.SelectedEdges[i] + 1];

                for (int c = 1; c <= DivideCount; c++)
                {
                    script.Anchors.Add(script.Anchors[A].Clone() as CurveItem3d);
                    script.Anchors[script.Anchors.Count - 1].position = FollowSpline(script.Anchors[A].position, script.Anchors[A].direction, script.Anchors[B].position, script.Anchors[B].direction, (float)c / (DivideCount + 1), DivideCount * 2, false); //(float)c / (DivideCount+1)
                }

                script.Connections[script.SelectedEdges[i] + 1] = script.Anchors.Count - DivideCount;

                for (int c = 0; c < DivideCount - 1; c++)
                {
                    script.Connections.Add(script.Anchors.Count - DivideCount + c);
                    script.Connections.Add(script.Anchors.Count - DivideCount + c + 1);
                    script.Inverted.Add(false);
                }
                script.Connections.Add(script.Anchors.Count - 1);
                script.Connections.Add(B);
                script.Inverted.Add(false);
            }

            EditorUtility.SetDirty(script);
            Bake();
        }

        public void InvertNormal()
        {
            Undo.RecordObject(script, "InvertEdges");
            for (int i = 0; i < script.SelectedEdges.Count; i++)
            {
                script.Inverted[script.SelectedEdges[i] / 2] = !script.Inverted[script.SelectedEdges[i] / 2];
            }
            EditorUtility.SetDirty(script);
            Bake();
        }

        #endregion

        public int GetConnection(int A, int B, out int revers)
        {
            return script.GetConnection(A, B, out revers);
        }

        public void GetConnections(int Anchor, out int A, out int B)
        {
            script.GetConnections(Anchor, out A, out B);
        }

        public int GetNextIA(int IA, int LastIA)
        {
            return script.GetNextIA(IA, LastIA);
        }

        public void OnSceneGUI()
        {
            script = (Curve)target;
            script.Tr = script.transform;
            if (script.SelectedPoints == null)
                script.SelectedPoints = new List<int>();
            if (script.SelectedEdges == null)
                script.SelectedEdges = new List<int>();
            EditorGUI.BeginChangeCheck();
            Undo.RecordObject(script, "move point");

            Matrix4x4 mat = Handles.matrix;
            Handles.matrix = script.Tr.localToWorldMatrix;
            switch (EditLayer)
            {
                case EditLayers.Points:
                    {
                        for (int i = 1; i < script.InterpolatedConnections.Count; i += 2)
                        {
                            Debug.DrawLine(script.Tr.TransformPoint(script.InterpolatedAnchors[script.InterpolatedConnections[i]]), script.Tr.TransformPoint(script.InterpolatedAnchors[script.InterpolatedConnections[i - 1]]), script.InterpolatedInverted[i / 2] ? Color.blue : Color.green);
                        }

                        MuvePoints(script.SelectedPoints, ref position);

                        if (PointsSelection(ref script.Anchors, ref script.SelectedPoints, script.Tr))
                        {
                            PlacePivot();
                        }
                    }
                    break;
                case EditLayers.Lines:
                    {
                        List<int> selpoi = new List<int>();
                        EdgesToPoints(selpoi);

                        MuvePoints(selpoi, ref position);

                        if (EdgesSelection(ref script.Anchors, script.Interpolation, ref script.Connections, ref script.SelectedEdges, script.Inverted, script.Tr, script.Symmetry))
                        {
                            PlacePivot();
                        }
                    }
                    break;
                case EditLayers.Object:
                    for (int i = 1; i < script.InterpolatedConnections.Count; i += 2)
                    {
                        Debug.DrawLine(script.Tr.TransformPoint(script.InterpolatedAnchors[script.InterpolatedConnections[i]]), script.Tr.TransformPoint(script.InterpolatedAnchors[script.InterpolatedConnections[i - 1]]), script.InterpolatedInverted[i / 2] ? Color.blue : Color.green);
                    }
                    break;
            }

            if (EditorGUI.EndChangeCheck())
            {
                PlacePivot();
                Bake();
                EditorUtility.SetDirty(script);
            }

            if (script.VolumeScale != Vector2.zero)
            {
                int oneC = 0;
                int towC = 0;
                int All = 0;
                Vector3 Center = (script.Min + script.Max) / 2;
                Handles.color = Color.green;
                Handles.DrawLine(Center, Center + Vector3.up);
                Handles.color = Color.red;
                Handles.DrawLine(Center, Center + Vector3.right);
                Handles.color = Color.blue;
                Handles.DrawLine(Center, Center + Vector3.forward);
                Handles.color = Color.white;

                for (int i = 1; i < script.InterpolatedConnections.Count; i += 2)
                {
                    int A = script.InterpolatedConnections[i - 1];
                    int B = script.InterpolatedConnections[i];
                    int Last = GetNextIA(A, B);
                    int Next = GetNextIA(B, A);

                    Vector3 lastDir, nextDir, direction = (script.InterpolatedAnchors[B] - script.InterpolatedAnchors[A]).normalized;
                    bool some = false;
                    bool all = true;
                    if (Last >= 0)
                    {
                        lastDir = (script.InterpolatedAnchors[A] - script.InterpolatedAnchors[Last]).normalized;
                        some = true;
                    }
                    else
                    {
                        all = false;
                        lastDir = direction;
                    }
                    if (Next >= 0)
                    {
                        nextDir = (script.InterpolatedAnchors[Next] - script.InterpolatedAnchors[B]).normalized;
                        if (some)
                            all = true;
                        some = true;
                    }
                    else
                    {
                        all = false;
                        nextDir = direction;
                    }
                    if (some && !all)
                        oneC++;
                    if (all)
                        towC++;
                    All++;

                    float s = Side(script.InterpolatedAnchors[A].x + script.InterpolatedAnchors[B].x);

                    Vector3 Up = Vector3.forward;
                    switch (script.Orientation)
                    {
                        case Axis.X:
                            Up = Vector3.right;
                            break;
                        case Axis.Y:
                            Up = Vector3.up;
                            break;
                        case Axis.Z:
                            Up = Vector3.forward;
                            break;
                    }

                    Vector3 Mid = (script.InterpolatedAnchors[B] + script.InterpolatedAnchors[A]) * 0.5f;

                    Quaternion Rot = Quaternion.LookRotation(direction, Up);



                    /*if ((Mid + Rot * Vector3.up - Center).magnitude < (Mid - Center).magnitude)
                    {
                        Rot = Quaternion.LookRotation(Rot * Vector3.forward, -(Rot * Vector3.up));
                        Handles.color = Color.cyan;
                        Handles.DrawLine(Mid, Mid + Rot * Vector3.up);
                    }*/
                    if ((Vector3.Dot(Rot * Vector3.right, Mid - Center) < 0))
                    {
                        Rot = Quaternion.LookRotation(-(Rot * Vector3.forward), Up);
                    }

                    if (showNormals)
                    {
                        Handles.color = Color.green;
                        Handles.DrawLine(Mid, Mid + Rot * Vector3.up);
                        Handles.color = Color.red;
                        Handles.DrawLine(Mid, Mid + Rot * Vector3.right);
                        Handles.color = Color.white;
                    }

                    Vector3 offsetA, offsetB, offsetC, offsetD;
                    offsetA = Rot * (Vector3.right * (-script.VolumeScale.x + script.Offset.x) + Vector3.up * (script.VolumeScale.y + script.Offset.y));
                    offsetB = Rot * (Vector3.right * (script.VolumeScale.x + script.Offset.x) + Vector3.up * (-script.VolumeScale.y + script.Offset.y));
                    offsetC = Rot * (Vector3.right * (-script.VolumeScale.x + script.Offset.x) + Vector3.up * (-script.VolumeScale.y + script.Offset.y));
                    offsetD = Rot * (Vector3.right * (script.VolumeScale.x + script.Offset.x) + Vector3.up * (script.VolumeScale.y + script.Offset.y));

                    Vector3 lastNrm = (lastDir + direction).normalized;
                    Vector3 nextNrm = (nextDir + direction).normalized;
                    CutAndDraw(Mid + offsetA, direction, script.InterpolatedAnchors[A], script.InterpolatedAnchors[B], lastNrm, nextNrm);
                    CutAndDraw(Mid + offsetB, direction, script.InterpolatedAnchors[A], script.InterpolatedAnchors[B], lastNrm, nextNrm);
                    CutAndDraw(Mid + offsetC, direction, script.InterpolatedAnchors[A], script.InterpolatedAnchors[B], lastNrm, nextNrm);
                    CutAndDraw(Mid + offsetD, direction, script.InterpolatedAnchors[A], script.InterpolatedAnchors[B], lastNrm, nextNrm);
                }
            }

            Handles.matrix = mat;
        }

        public void CutAndDraw(Vector3 position, Vector3 direction, Vector3 aPos, Vector3 bPos, Vector3 NormalA, Vector3 NormalB) //direction - from A to B, normal - from A/B to position
        {
            Plane PlaneA = new Plane(NormalA, aPos);
            Plane PlaneB = new Plane(NormalB, bPos);
            if (!PlaneA.GetSide(position))
                PlaneA.Flip();

            if (!PlaneB.GetSide(position))
                PlaneB.Flip();

            float a, b;
            PlaneA.Raycast(new Ray(position, direction), out a);
            PlaneB.Raycast(new Ray(position, -direction), out b);
            Vector3 A = position + direction * a;
            Vector3 B = position - direction * b;
            Handles.DrawLine(A, B);
        }

        public float SignedAngle(Vector3 from, Vector3 to, Vector3 Axis)
        {
            Quaternion rot = Quaternion.LookRotation(-Axis, from).GetInverse();

            to = rot * to;
            return Mathf.Atan2(to.x, to.y);
        }

        public Vector3 GetProjectionPoint(Vector3 point, Vector3 direction, Plane plane)
        {
            float d = 0;
            if (!plane.Raycast(new Ray(point, direction), out d))
            {
                plane.Flip();
                if (!plane.Raycast(new Ray(point, direction), out d))
                    if (!plane.Raycast(new Ray(point, -direction), out d))
                    {
                        plane.Flip();
                        if (!plane.Raycast(new Ray(point, -direction), out d))
                            Debug.Log("!");
                    }
            }
            return point + direction.normalized * d;
        }

        public void PlacePivot()
        {
            List<int> selpoi = new List<int>();

            switch (EditLayer)
            {
                case EditLayers.Points:
                    selpoi = script.SelectedPoints;
                    break;
                case EditLayers.Lines:
                    EdgesToPoints(selpoi);
                    break;
            }
            position = CentrOfSelected(selpoi);

            if (PivotPose == PivotPoses.LocalWithSymmetry)
            {
                switch (script.Symmetry)
                {
                    case Symmetrys.X:
                        position.x = 0;
                        break;
                    case Symmetrys.Y:
                        position.y = 0;
                        break;
                    case Symmetrys.Z:
                        position.z = 0;
                        break;
                }
            }
        }

        public Vector3 CentrOfSelected(List<int> selpoi)
        {
            Vector3 pos = Vector3.zero;
            if (selpoi.Count > 0)
            {
                for (int i = 0; i < selpoi.Count; i++)
                {
                    pos += script.Anchors[selpoi[i]].position;
                }
                pos /= selpoi.Count;
            }
            return pos;
        }

        void EdgesToPoints(List<int> selpoi)
        {
            for (int i = 0; i < script.SelectedEdges.Count; i++)
            {
                if (!selpoi.Contains(script.Connections[script.SelectedEdges[i]]))
                {
                    selpoi.Add(script.Connections[script.SelectedEdges[i]]);
                }
                if (!selpoi.Contains(script.Connections[script.SelectedEdges[i] + 1]))
                {
                    selpoi.Add(script.Connections[script.SelectedEdges[i] + 1]);
                }
            }
        }

        public void MuvePoints(List<int> selpoi, ref Vector3 pos)
        {
            switch (Tools.current)
            {
                case Tool.Move:
                    Vector3 lastpos = pos;
                    pos = Handles.DoPositionHandle(pos, Quaternion.identity);
                    for (int i = 0; i < selpoi.Count; i++)
                    {
                        script.Anchors[selpoi[i]].position += pos - lastpos;
                    }
                    break;
                case Tool.Rotate:
                    Quaternion lasrRot = rotation;
                    rotation = Handles.RotationHandle(rotation, pos);
                    Quaternion res = Quaternion.FromToRotation(lasrRot * Vector3.forward, rotation * Vector3.forward);
                    Quaternion res2 = Quaternion.FromToRotation(lasrRot * Vector3.right, rotation * Vector3.right);
                    res = res * res2;

                    for (int i = 0; i < selpoi.Count; i++)
                    {
                        Vector3 loc = script.Anchors[selpoi[i]].position - pos;
                        loc = res * loc;
                        script.Anchors[selpoi[i]].position = loc + pos;
                        script.Anchors[selpoi[i]].direction = res * script.Anchors[selpoi[i]].direction;
                    }
                    if (Event.current.isMouse)
                        rotation = Quaternion.LookRotation(Vector3.right);
                    break;
                case Tool.Scale:
                    Vector3 lastScale = scale;
                    scale = Handles.ScaleHandle(scale, pos, Quaternion.identity, HandleUtility.GetHandleSize(pos));
                    Vector3 Res = new Vector3(scale.x / lastScale.x, scale.y / lastScale.y, scale.z / lastScale.z);

                    for (int i = 0; i < selpoi.Count; i++)
                    {
                        Vector3 loc = script.Anchors[selpoi[i]].position - pos;
                        loc.Scale(Res);
                        script.Anchors[selpoi[i]].position = loc + pos;
                        script.Anchors[selpoi[i]].direction.Scale(Res);
                    }
                    if (Event.current.isMouse)
                        scale = Vector3.one;
                    break;
            }
        }
    }
#endif

}