using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static MonoBehaviourPlus;

namespace MeshEditing
{
    public static class MeshEditor
    {
        public static void MeshPlaneIntersection(Mesh mesh, Plane plane, Vector3 mid, Vector3 directionMask, out List<Vector3> result)
        {
            result = new List<Vector3>();
            var vertices = new List<Vector3>();
            var triangles = mesh.GetTriangles(0);
            mesh.GetVertices(vertices);
            
            for (int i = 2; i < triangles.Length; i+= 3)
            {
                Vector3 A, B, C;
                A = vertices[triangles[i - 2]];
                B = vertices[triangles[i - 1]];
                C = vertices[triangles[i]];

                CheckThePoint(plane, result, A, B);
                CheckThePoint(plane, result, B, C);
                CheckThePoint(plane, result, C, A);
            }
            List<SortableObject<Vector3>> sotring = new List<SortableObject<Vector3>>();
            Vector3 Up = Vector3.Cross(directionMask, Vector3.up);
            if (directionMask == Vector3.up)
                Up = Vector3.forward;
            for (int i = 0; i < result.Count; i++)
            {
                sotring.Add(new SortableObject<Vector3>(result[i], Vector3.SignedAngle(Up, Vector3.ProjectOnPlane(result[i] - mid, directionMask), directionMask)));
            }
            sotring.Sort();
            result = new List<Vector3>();
            for (int i = 0; i < sotring.Count; i++)
            {
                AddPoint(result, sotring[i].Value);
            }
            if(sotring.Count > 0)
            result.Add(sotring[0].Value);

        }

        public static Mesh ShellMesh(Mesh mesh, float offset)
        {
            List<Vector3> Verticis = new List<Vector3>();
            mesh.GetVertices(Verticis);
            List<Vector3> Normals = new List<Vector3>();
            mesh.GetNormals(Normals);
            int[] triangles;
            triangles = mesh.GetTriangles(0);

            Dictionary<int, List<int>> similar = new Dictionary<int, List<int>>();

            for(int i = 0; i < Verticis.Count; i++)
            {
                similar.Add(i, GetSimilar(i, Verticis));
            }

            Mesh extr = Mesh.Instantiate(mesh);
            extr = InvertNormals(extr);
            extr = NormalOffset(extr, offset);
            List<int> EdgeCorner = new List<int>();
            bool done = false;
            for (int i = 0; i < Verticis.Count; i++)
            {
                List<int> conns = GetConnections(i, triangles); // находим связи
                for (int c = 0; c < conns.Count; c++) // для каждой из них
                {
                    List<int> tris = GetTris(i, conns[c], triangles); // находим количество совпадающих треугольников
                    if (tris.Count == 3) // если только один треугольник
                    {
                        EdgeCorner.Add(i);
                        EdgeCorner.Add(c);
                        int Curr = c, Last = i, Help;
                        for (int n = 0; n < 1000; n++)
                        {
                            Help = Curr;
                            Curr = GetNextEdge(Curr, Last, triangles);
                            Last = Help;
                            if (EdgeCorner.Contains(Curr))
                                done = true;

                            EdgeCorner.Add(Curr);
                            if (done)
                                break;
                        }
                    }
                    if (done)
                        break;
                }
                if (done)
                    break;
            }

            List<Vector3> nrms = new List<Vector3>();
            for (int i = 0; i < EdgeCorner.Count; i++)
            {
                nrms.Add(GetNormal(EdgeCorner[i], triangles, Normals));
            }
            Mesh edge = new Mesh();
            List<Vector3> eVerts = new List<Vector3>();
            List<Vector3> eNrms = new List<Vector3>();
            List<int> eTris = new List<int>();
            for (int i = 1; i < EdgeCorner.Count; i++)
            {
                int anh = eVerts.Count;
                eVerts.Add(Verticis[EdgeCorner[i-1]]);
                eVerts.Add(Verticis[EdgeCorner[i]]);
                eVerts.Add(Verticis[EdgeCorner[i - 1]] + nrms[i - 1] * offset);
                eVerts.Add(Verticis[EdgeCorner[i]] + nrms[i] * offset);

                eTris.AddRange(new[] { anh, anh + 2, anh + 1, anh + 1, anh + 2, anh + 3 });
                //triangles.AddRange(new[] { anh, anh + 1, anh + 2, anh + 2, anh + 1, anh + 3 });
            }
            edge.SetVertices(eVerts);
            edge.SetTriangles(eTris, 0);
            edge.RecalculateNormals();
            edge.RecalculateTangents();

            mesh = SimpleCombine(mesh, extr);
            mesh = SimpleCombine(mesh, edge);
            return mesh;
        }

        public static List<int> GetSimilar(int vert, List<Vector3> vertices)
        {
            List<int> result = new List<int>();
            for (int i = 0; i < vertices.Count; i++)
            {
                if (Vector3.SqrMagnitude(vertices[vert] - vertices[i]) < 0.001f)
                    result.Add(i);
            }
            return result;
        }

        public static Vector3 GetNormal(int vert, int[] tris, List<Vector3> normals)
        {
            List<int> conns = GetConnections(vert, tris);
            Vector3 nrm = normals[vert];
            for(int i = 0; i < conns.Count; i++)
            {
                nrm += normals[conns[i]];
            }
            return nrm.normalized;
        }

        public static Mesh InvertNormals(Mesh mesh)
        {
            int[] triangles;
            triangles = mesh.GetTriangles(0);
            int[] tris;
            tris = mesh.GetTriangles(0);

            for (int i = 0; i < triangles.Length - 2; i += 3)
            {
                tris[i] = triangles[i];
                tris[i+1] = triangles[i+2];
                tris[i+2] = triangles[i+1];
            }
            mesh.SetTriangles(tris, 0);
            return mesh;
        }

        public static Mesh NormalOffset(Mesh mesh, float offset)
        {
            List<Vector3> Verticis = new List<Vector3>();
            mesh.GetVertices(Verticis);
            List<Vector3> Normals = new List<Vector3>();
            mesh.GetNormals(Normals);
            int[] triangles;
            triangles = mesh.GetTriangles(0);

            for (int i = 0; i < Verticis.Count; i++)
            {
                Verticis[i] += GetNormal(i, triangles, Normals) * offset;
            }
            mesh.SetVertices(Verticis);
            return mesh;
        }

        public static Mesh SimpleCombine(Mesh A, Mesh B)
        {
            List<Vector3> Verticis = new List<Vector3>();
            A.GetVertices(Verticis);
            int[] triangles;
            triangles = A.GetTriangles(0);

            triangles = AddTris(triangles, B.GetTriangles(0), Verticis.Count);
            Verticis.AddRange(B.vertices.ToArray());

            Mesh C = new Mesh();
            C.SetVertices(Verticis);
            C.SetTriangles(triangles, 0);
            return C;
        }

        public static int[] AddTris(int[] OldTris, int[] NewTris, int offset)
        {
            List<int> tris = OldTris.ToList();
            for (int i = 0; i < NewTris.Length; i++)
                tris.Add(NewTris[i] + offset);
            return tris.ToArray();
        }

        public static void AddPoint(List<Vector3> points, Vector3 add)
        {
            if (points.Count == 0 || (points[points.Count - 1] - add).magnitude > 0.1f)
                points.Add(add);
        }

        private static void CheckThePoint(Plane plane, List<Vector3> result, Vector3 A, Vector3 B)
        {

            if (!plane.SameSide(A, B) )
            {
                float dA = Mathf.Abs(plane.GetDistanceToPoint(A));
                float dB = Mathf.Abs(plane.GetDistanceToPoint(B));
                result.Add(Vector3.Lerp(A, B, dA / (dA + dB) ));
            }
        }

        static Ray FromTo(Vector3 from, Vector3 to)
        {
            return new Ray(from, to - from);
        }

        public static List<int> GetConnections(int vert, int[] tris)
        {
            List<int> result = new List<int>();
            for (int i = 0; i < tris.Length - 2; i+=3)
            {

                if(tris[i] == vert)
                {
                    if (!result.Contains(tris[i + 1]))
                        result.Add(tris[i + 1]);
                    if (!result.Contains(tris[i + 2]))
                        result.Add(tris[i + 2]);
                }
                if (tris[i+1] == vert)
                {
                    if (!result.Contains(tris[i]))
                        result.Add(tris[i]);
                    if (!result.Contains(tris[i + 2]))
                        result.Add(tris[i + 2]);
                }
                if (tris[i + 2] == vert)
                {
                    if (!result.Contains(tris[i]))
                        result.Add(tris[i]);
                    if (!result.Contains(tris[i + 1]))
                        result.Add(tris[i + 1]);
                }
            }
            return result;
        }

        public static List<int> GetTris(int a, int b, int[] tris)
        {
            List<int> result = new List<int>();
            for (int i = 0; i < tris.Length - 2; i += 3)
            {
                if (tris[i] == a)
                {
                    if(tris[i + 1] == b || tris[i + 2] == b)
                    {
                        result.Add(tris[i]);
                        result.Add(tris[i + 1]);
                        result.Add(tris[i + 2]);
                    }
                }
                if (tris[i + 1] == a)
                {
                    if (tris[i] == b || tris[i + 2] == b)
                    {
                        result.Add(tris[i]);
                        result.Add(tris[i + 1]);
                        result.Add(tris[i + 2]);
                    }
                }
                if (tris[i + 2] == a)
                {
                    if (tris[i] == b || tris[i + 1] == b)
                    {
                        result.Add(tris[i]);
                        result.Add(tris[i + 1]);
                        result.Add(tris[i + 2]);
                    }
                }
            }
            return result;
        }

        public static int GetNextEdge(int vert, int last, int[] tris)
        {
            List<int> conn = GetConnections(vert, tris);
            for (int i = 0; i < conn.Count; i++)
            {
                if (conn[i] != last)
                {
                    List<int> tri = GetTris(vert, conn[i], tris);
                    if (tri.Count == 3)
                        return conn[i];
                }
            }
            return -1;
        }
    }
}
