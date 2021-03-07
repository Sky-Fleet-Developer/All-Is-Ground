using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class TerrainAlignment : MonoBehaviourPlus
{
    public int TerrainHeight = 1500;
    public LayerMask GroundLayer;
    [Space(15)]
    public Vector2 RectangleSize;
    public Vector2 RealRectangleSize;
    public float HeightRange;
    public float YOffset;

    public Texture2D Mask;
    public Texture2D Heightmap;

    Transform Tr;
    [ContextMenu("Bake")]
    public void Bake()
    {
        RaycastHit FLHit;
        RaycastHit FRHit;
        RaycastHit BLHit;
        RaycastHit BRHit;
        Vector3 lf = -Tr.right * RectangleSize.x + Tr.forward * RectangleSize.y;
        Vector3 rf = Tr.right * RectangleSize.x + Tr.forward * RectangleSize.y;
        Vector3 lb = -Tr.right * RectangleSize.x - Tr.forward * RectangleSize.y;
        Vector3 rb = Tr.right * RectangleSize.x - Tr.forward * RectangleSize.y;
        RealRectangleSize = new Vector2(Mathf.Max(lf.x, rf.x, lb.x, rb.x), Mathf.Max(lf.z, rf.z, lb.z, rb.z));
        Vector3 FLPos = Tr.position + new Vector3(-RealRectangleSize.x * 0.5f, 5000, RealRectangleSize.y * 0.5f);
        Vector3 FRPos = Tr.position + new Vector3(RealRectangleSize.x * 0.5f, 5000, RealRectangleSize.y * 0.5f);
        Vector3 BLPos = Tr.position + new Vector3(-RealRectangleSize.x * 0.5f, 5000, -RealRectangleSize.y * 0.5f);
        Vector3 BRPos = Tr.position + new Vector3(RealRectangleSize.x * 0.5f, 5000, -RealRectangleSize.y * 0.5f);
        if (Physics.Raycast(FLPos, Vector3.down, out FLHit, 10000f, GroundLayer))
        {
            Debug.DrawLine(FLPos, FLHit.point, Color.red, 10);
            if (Physics.Raycast(FRPos, Vector3.down, out FRHit, 10000f, GroundLayer))
            {
            Debug.DrawLine(FLPos, FRHit.point, Color.red, 10);
                if (Physics.Raycast(BLPos, Vector3.down, out BLHit, 10000f, GroundLayer))
                {
            Debug.DrawLine(FLPos, BLHit.point, Color.red, 10);
                    if (Physics.Raycast(BRPos, Vector3.down, out BRHit, 10000f, GroundLayer))
                    {
            Debug.DrawLine(FLPos, BRHit.point, Color.red, 10);
                        if(FRHit.transform == FLHit.transform && FRHit.transform == BLHit.transform && FRHit.transform == BRHit.transform)
                        {
                            Terrain terr = FLHit.transform.GetComponent<Terrain>();
                            if (!terr)
                                return;
                            Debug.Log("AlignTerrain");
#if UNITY_EDITOR
                            Undo.RecordObject(terr.terrainData, "TerrainAlignment");
#endif
                            Vector3 center = WorldPointToTerrainPoint(Tr.position, terr);
                            Vector3 FL = WorldPointToTerrainVertex(FLPos, terr, false);
                            Vector3 FR = WorldPointToTerrainVertex(FRPos, terr, false);
                            Vector3 BL = WorldPointToTerrainVertex(BLPos, terr);
                            Vector3 BR = WorldPointToTerrainVertex(BRPos, terr);
                            Vector2Int min = new Vector2Int((int)Mathf.Min(FL.x, FR.x, BL.x, BR.x), (int)Mathf.Min(FL.z, FR.z, BL.z, BR.z));
                            Vector2Int max = new Vector2Int((int)Mathf.Max(FL.x, FR.x, BL.x, BR.x), (int)Mathf.Max(FL.z, FR.z, BL.z, BR.z));
                            int mapSize = terr.terrainData.heightmapWidth;
                            Vector2Int diff = max - min;
                            float[,] map = terr.terrainData.GetHeights(min.x, min.y, diff.x, diff.y);
                            for(int w = 0; w < map.GetLength(1); w++)
                            {
                                for (int h = 0; h < map.GetLength(0); h++)
                                {
                                    Vector2 position = new Vector2((float)w / map.GetLength(1), (float)(map.GetLength(0) - h) / map.GetLength(0));
                                    Vector3 worldPos = FLPos + Vector3.right * position.x * (FRPos.x - FLPos.x) + Vector3.back * position.y * (FLPos.z - BLPos.z);
                                    Vector3 itrp = Tr.InverseTransformPoint(worldPos);
                                    Vector2 texpos = new Vector2(itrp.x / RectangleSize.x + 0.5f, itrp.z / RectangleSize.y + 0.5f);
                                    if (texpos.x >= 0f && texpos.x <= 1f && texpos.y >= 0f && texpos.y <= 1f)
                                    {
                                        float mask = Mask ? Mask.GetPixelBilinear(texpos.x, texpos.y).grayscale : Mathf.Clamp01((1 - Mathf.Max(Mathf.Abs((texpos.x - 0.5f) * 2), Mathf.Abs((texpos.y - 0.5f) * 2))) * 2);
                                        float zero = (Tr.position.y + YOffset) / TerrainHeight;
                                        float New = Mathf.Lerp(map[h, w], zero + Mathf.Lerp(-HeightRange * 0.5f / TerrainHeight, HeightRange * 0.5f / TerrainHeight, Heightmap ? Heightmap.GetPixelBilinear(texpos.x, texpos.y).grayscale : 0.5f), mask);
                                        float delta = New - map[h, w];
                                        Debug.DrawRay(new Vector3(worldPos.x, New * TerrainHeight, worldPos.z), Vector3.up * delta * TerrainHeight, delta > 0 ? Color.green : Color.red, 10f);
                                        map[h, w] = New;
                                    }
                                }
                            }
                            terr.terrainData.SetHeights(min.x, min.y, map);
#if UNITY_EDITOR
                            EditorUtility.SetDirty(terr.terrainData);
#endif
                        }
                    }
                }
            }
        }

    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if(!Tr)
            Tr = transform;

        UltiDraw.Begin();
        UltiDraw.DrawWireCuboid(Tr.position + Vector3.up * YOffset, Tr.rotation, new Vector3(RectangleSize.x, HeightRange, RectangleSize.y), Color.white);
        Vector3 lf = -Tr.right * RectangleSize.x + Tr.forward * RectangleSize.y;
        Vector3 rf = Tr.right * RectangleSize.x + Tr.forward * RectangleSize.y;
        Vector3 lb = - Tr.right * RectangleSize.x - Tr.forward * RectangleSize.y;
        Vector3 rb = Tr.right * RectangleSize.x - Tr.forward * RectangleSize.y;
        RealRectangleSize = new Vector2(Mathf.Max(lf.x, rf.x, lb.x, rb.x), Mathf.Max(lf.z, rf.z, lb.z, rb.z));
        UltiDraw.DrawWireCuboid(Tr.position + Vector3.up * YOffset, Quaternion.identity, new Vector3(RealRectangleSize.x, 0, RealRectangleSize.y), Color.gray);
        UltiDraw.End();
    }
#endif

}