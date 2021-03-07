using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class BuildingUVUnwarp : MonoBehaviour
{
    public bool world;
    [ContextMenu("Bake")]
    public void Bake()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
#if UNITY_EDITOR
        Undo.RecordObject(mf, "BuildingUVUnwarp");
#endif
        Mesh New = Instantiate(mf.sharedMesh);

        int[] tris = New.triangles;
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        New.GetVertices(vertices);
        Transform Tr = transform;
        New.GetNormals(normals);
        if (world)
        {
            for (int i = 0; i < vertices.Count; i++)
            {
                vertices[i] = Tr.TransformPoint(vertices[i]);
                normals[i] = Tr.TransformDirection(normals[i]);
            }
        }
        List<Vector2> uvs = new Vector2[vertices.Count].ToList();
        for(int i = 0; i < tris.Length - 2; i += 3)
        {
            Vector3 normal = (normals[tris[i]] + normals[tris[i + 1]] + normals[tris[i + 2]]) / 3;
            float xComp = Mathf.Abs(normal.x);
            float yComp = Mathf.Abs(normal.y);
            float zComp = Mathf.Abs(normal.z);
            if(xComp > yComp && xComp > zComp)
            {
                uvs[tris[i]] = new Vector2(vertices[tris[i]].z, vertices[tris[i]].y);
                uvs[tris[i+1]] = new Vector2(vertices[tris[i+1]].z, vertices[tris[i+1]].y);
                uvs[tris[i+2]] = new Vector2(vertices[tris[i+2]].z, vertices[tris[i+2]].y);
            }
            else if (yComp > xComp && yComp > zComp)
            {
                uvs[tris[i]] = new Vector2(vertices[tris[i]].x, vertices[tris[i]].z);
                uvs[tris[i + 1]] = new Vector2(vertices[tris[i + 1]].x, vertices[tris[i + 1]].z);
                uvs[tris[i + 2]] = new Vector2(vertices[tris[i + 2]].x, vertices[tris[i + 2]].z);
            }
            if (zComp > xComp && zComp > yComp)
            {
                uvs[tris[i]] = new Vector2(vertices[tris[i]].x, vertices[tris[i]].y);
                uvs[tris[i + 1]] = new Vector2(vertices[tris[i + 1]].x, vertices[tris[i + 1]].y);
                uvs[tris[i + 2]] = new Vector2(vertices[tris[i + 2]].x, vertices[tris[i + 2]].y);
            }
        }
        New.SetUVs(0, uvs);
        mf.sharedMesh = New;
#if UNITY_EDITOR
        EditorUtility.SetDirty(mf);
#endif
    }
}
