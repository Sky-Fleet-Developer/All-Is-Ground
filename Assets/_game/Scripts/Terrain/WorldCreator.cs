using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
public class WorldCreator : MonoBehaviourPlus
{
    public MassErosion Erosion;
    public int ErosionIterations;
    [Space(15)]
    public TerrainData[] terrainsData;
    public Terrain[] terrains;
    public int TerrainCellScale = 6000;
    public int WItems = 5;
    public int HItems = 5;
    public Texture2D[] heightMapLayers;
    [Space(15)]
    public Texture2D[] groundMapTextures;
    [Space(5)]
    public int RedLayerMainMap;
    public int RedLayerSecondMap;
    public float RedLayerBalance;
    [Space(5)]
    public int GreenLayerMainMap;
    public int GreenLayerSecondMap;
    public float GreenLayerBalance;
    [Space(5)]
    public int BlueLayerMainMap;
    public int BlueLayerSecondMap;
    public float BlueLayerBalance;
    [Space(15)]
    [Header("Trees")]
    public AnimationCurve LODCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public LayerMask Ground;
    public ForestExemplar[] Trees;
    [Space(15)]
    public bool InvertCellWidth = false;
    public bool InvertCellHeight = false;
    public bool InvertWidth = false;
    public bool InvertHeight = false;
    public bool ReplaceWH = false;

    [System.Serializable]
    public class ForestExemplar
    {
        public LOD[] Objects;
        public int GroupsCount = 10;

        public int GroupIterations = 2;
        public int CountInGroup = 30;
        public float GroupRadius = 300;

        [Header("Scale")]
        public Vector2 HeightRandom = Vector2.one;
        public Vector2 WidthRandom = Vector2.one;

        [Header("Placing")]
        public Vector2 NearCorner;
        public Vector2 FarCorner;
        public float YOffest;
        public float SlopeAngle = 45f;
        public AnimationCurve CountPerHeight = AnimationCurve.Constant(0f, 1f, 1f);
    }
    [System.Serializable]
    public class LOD
    {
        public GameObject[] LODs;
    }

    [ExecuteInEditMode]
    public void FirstGenerating()
    {
        /*StartCoroutine(FirstGeneratingCorutine());*/
        try
        {
            foreach (int i in FirstGeneratingRutine(terrainsData.Length))
            {
                EditorUtility.DisplayProgressBar("Generation: ", (i+1) + "/" + terrainsData.Length + " terrains", (float)i / terrainsData.Length);

            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    public IEnumerable<int> FirstGeneratingRutine(int count)
    {
        for (int i = 0; i < count; i++)
        {
            float[,] map = TexToArry(heightMapLayers[i], terrainsData[i].heightmapWidth, terrainsData[i].heightmapHeight);
            int tdH = i / HItems;
            if (InvertHeight)
                tdH = (HItems - 1) - tdH;
            int tdW = i % WItems;
            if (InvertWidth)
                tdW = (WItems - 1) - tdW;
            int td = tdH * WItems + tdW;

            if (Erosion)
                Erosion.StartCoroutine(Erosion.StartErode(ErosionIterations, terrainsData[td]));
            terrainsData[td].SetHeights(0, 0, map);

            yield return i;
        }
    }

    public void ErosionFilter()
    {
        try
        {
            foreach (int i in ErosionRutine(terrainsData.Length))
            {
                EditorUtility.DisplayProgressBar("Erosion: ", (i / ErosionIterations + 1) + "/" + terrainsData.Length + " terrains", (float)i / (terrainsData.Length * ErosionIterations));
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    public IEnumerable<int> ErosionRutine(int count)
    {
        for (int i = 0; i < count; i++)
        {
            for (int n = 0; n < ErosionIterations; n++)
            {
                Erosion.GenerateHeightMap(terrainsData[i]);
                Erosion.Erode(terrainsData[i]);
                yield return i * ErosionIterations + n;
            }
        }
    }

    public void StichGround()
    {
        for (int i = 0; i < terrains.Length; i++)
        {
            terrains[i].SetNeighbors(i / WItems > 0 ? terrains[i - WItems] : null, i % WItems < WItems - 1 ? terrains[i + 1] : null, i / WItems < WItems - 1 ? terrains[i + WItems] : null, i % WItems > 0 ? terrains[i - 1] : null);
        }
    }

    public void StichEdges()
    {
        for (int i = 0; i < terrainsData.Length; i++)
        {
            if (i / WItems > 0)
            {
                Stich(terrainsData[i - WItems], terrainsData[i], 1);
            }
            if (i % WItems < WItems - 1)
            {
                Stich(terrainsData[i], terrainsData[i+1], 0);
            }
            if (i / WItems < WItems - 1)
            {
                Stich(terrainsData[i], terrainsData[i + WItems], 1);
            }
            if (i % WItems > 0)
            {
                Stich(terrainsData[i - 1], terrainsData[i], 0);
            }
        }
    }

    public void Stich(TerrainData A, TerrainData B, int pose)
    {
        if (pose == 0)
        {
            float[,] edge1 = A.GetHeights(0, A.heightmapHeight - 1, A.heightmapWidth, 1);
            float[,,] aedge1 = A.GetAlphamaps(0, A.alphamapHeight - 1, A.alphamapWidth, 1);
            //Debug.Log("1. e1.w = " + edge1.GetLength(1) + " e1.h = " + edge1.GetLength(0));
            float[,] edge2 = B.GetHeights(0, 0, B.heightmapWidth, 1);
            float[,,] aedge2 = B.GetAlphamaps(0, 0, B.alphamapWidth, 1);
            //Debug.Log("1. e2.w = " + edge2.GetLength(1) + " e1.h = " + edge2.GetLength(0));
            for (int i = 0; i < A.heightmapHeight; i++)
                edge1[0, i] = (edge1[0, i] + edge2[0, i]) / 2f;
            for (int i = 0; i < A.alphamapWidth; i++)
                for (int ch = 0; ch < aedge1.GetLength(2); ch++)
                    aedge1[0, i, ch] = (aedge1[0, i, ch] + aedge2[0, i, ch]) / 2;

            A.SetHeights(0, A.heightmapWidth - 1, edge1);
            A.SetAlphamaps(0, A.alphamapWidth - 1, aedge1);
            B.SetHeights(0, 0, edge1);
            B.SetAlphamaps(0, B.alphamapWidth - 1, aedge1);
        }
        else if (pose == 1)
        {
            float[,] edge1 = A.GetHeights(A.heightmapWidth - 1, 0, 1, A.heightmapHeight);
            float[,,] aedge1 = A.GetAlphamaps(A.alphamapWidth - 1, 0, 1, A.alphamapHeight);
            //Debug.Log("1. e1.w = " + edge1.GetLength(1) + " e1.h = " + edge1.GetLength(0));
            float[,] edge2 = B.GetHeights(0, 0, 1, B.heightmapHeight);
            float[,,] aedge2 = B.GetAlphamaps(0, 0, 1, B.alphamapHeight);
            //Debug.Log("1. e2.w = " + edge2.GetLength(1) + " e1.h = " + edge2.GetLength(0));
            for (int i = 0; i < A.heightmapHeight; i++)
                edge1[i, 0] = (edge1[i, 0] + edge2[i, 0]) / 2f;
            for (int i = 0; i < A.alphamapWidth; i++)
                for (int ch = 0; ch < aedge1.GetLength(2); ch++)
                    aedge1[i, 0, ch] = (aedge1[i, 0, ch] + aedge2[i, 0, ch]) / 2;
            A.SetHeights(A.heightmapHeight - 1, 0, edge1);
            A.SetAlphamaps(A.alphamapHeight - 1, 0, aedge1);
            B.SetHeights(0, 0, edge1);
            B.SetAlphamaps(B.alphamapHeight - 1, 0, aedge1);
        }
    }

    public void Colorizate()
    {
        try
        {
            foreach (float i in ColorizateRutine(terrainsData.Length))
            {
                EditorUtility.DisplayProgressBar("Colorize: ", Mathf.Ceil(i * 100) + "%", i);
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    public IEnumerable<float> ColorizateRutine(int count)
    {
        for (int i = 0; i < count; i++)
        {
            float[,,] aMap = terrainsData[i].GetAlphamaps(0, 0, terrainsData[i].alphamapWidth, terrainsData[i].alphamapHeight);
            float width = aMap.GetLength(0);
            float height = aMap.GetLength(1);
            int progress = 0;
            for (int w = 0; w < width; w++)
            {
                for (int h = 0; h < height; h++)
                {
                    Color pixel;
                    if(ReplaceWH)
                        pixel = groundMapTextures[i].GetPixelBilinear(InvertCellWidth ? (1 - w / width) : w / width, InvertCellHeight ? (1 - h / height) : h / height);
                    else
                        pixel = groundMapTextures[i].GetPixelBilinear(InvertCellHeight ? (1 - h / height) : h / height, InvertCellWidth ? (1 - w / width) : w / width);
                    int RMS = Random.Range(0f, 1f) < RedLayerBalance ? RedLayerMainMap : RedLayerSecondMap;
                    int GMS = Random.Range(0f, 1f) < GreenLayerBalance ? GreenLayerMainMap : GreenLayerSecondMap;
                    int BMS = Random.Range(0f, 1f) < BlueLayerBalance ? BlueLayerMainMap : BlueLayerSecondMap;
                    bool[] layers = new bool[aMap.GetLength(2)];
                    float common = pixel.r + pixel.g + pixel.b;
                    if (common > 1)
                        pixel /= common;
                    layers[RMS] = true;
                    aMap[h, w, RMS] = pixel.r;
                    layers[GMS] = true;
                    aMap[h, w, GMS] = pixel.g;
                    layers[BMS] = true;
                    aMap[h, w, BMS] = pixel.b;
                    if (common < 0) {
                        float els = (1 - common) / layers.Length - 3;

                        for (int c = 0; c < layers.Length; c++)
                        {
                            if (!layers[c])
                                aMap[h, w, c] = els;
                        }
                    }
                    else
                    {
                        for (int c = 0; c < layers.Length; c++)
                        {
                            if (!layers[c])
                                aMap[h, w, c] = 0;
                        }
                    }
                }
                progress++;
                yield return Mathf.Ceil(((float)progress / height + i) / count * 1000) / 1000;
            }
            terrainsData[i].SetAlphamaps(0, 0, aMap);
        }
    }

    public void PlaceTrees()
    {
        try
        {
            foreach (float i in PlaceTreesRutine())
            {
                EditorUtility.DisplayProgressBar("Colorize: ", Mathf.Ceil(i * 100) + "%", i);
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    public IEnumerable<float> PlaceTreesRutine()
    {
        List<Transform> anchors = new List<Transform>();
        int all = 0;
        int done = 0;
        for (int i = 0; i < Trees.Length; i++)
        {
            all += Trees[i].GroupsCount * Trees[i].CountInGroup;
        }
        for (int i = 0; i < Trees.Length; i++)
        {
            int gropus = 0;
            Vector3 SpawnPose = Vector3.zero;
            int it = 0;
            int lastT = 0;
            while (gropus < Trees[i].GroupsCount)
            {
                it++;
                if (it > 3000)
                {
                    gropus++;
                    it = 0;
                    continue;
                }
                RaycastHit Hit;
                if (gropus % Trees[i].GroupIterations == 0 || it > 500 || lastT != gropus * terrains.Length / Trees[i].GroupsCount)
                {
                    lastT = gropus * terrains.Length / Trees[i].GroupsCount;
                    SpawnPose = terrains[lastT].transform.position + Vector3.up * 3000;
                    SpawnPose += (Vector3.forward * Random.Range(0, TerrainCellScale) + Vector3.right * Random.Range(0, TerrainCellScale));
                }
                Vector3 SpawnCenter = SpawnPose;
                if (Physics.Raycast(SpawnPose, Vector3.down, out Hit, 4000, Ground))
                {
                    if (Vector3.Angle(Hit.normal, Vector3.up) < Trees[i].SlopeAngle)
                    {
                       // List<Transform> LODs = new List<Transform>();
                        int count = 0;
                        Transform Anchor = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
                        Anchor.SetParent(terrains[lastT].transform, true);
                        Anchor.name = "TreeGroup " + i + "_" + gropus;
                        anchors.Add(Anchor);
                        DestroyImmediate(Anchor.GetComponent<Renderer>());
                        DestroyImmediate(Anchor.GetComponent<MeshFilter>());
                        DestroyImmediate(Anchor.GetComponent<SphereCollider>());
                        int maxLODCount = 0;
                        for (int n = 0; n < Trees[i].Objects.Length; n++)
                            maxLODCount = Mathf.Max(Trees[i].Objects[n].LODs.Length, maxLODCount);

                       /* for (int n = 0; n < maxLODCount; n++)
                        {
                            Transform lod = Instantiate(Anchor, null);
                            lod.position = Hit.point;
                            lod.name = Anchor.name + "_LOD" + n;
                            lod.gameObject.AddComponent(typeof(CombinedMeshInstance));
                            lod.GetComponent<CombinedMeshInstance>().Library = MeshesLibrarys[lastT];
                            LODs.Add(lod);
                        }
                        for (int n = 0; n < maxLODCount; n++)
                            LODs[n].SetParent(Anchor, true);
                            
                        LODGroup ALod = Anchor.gameObject.AddComponent(typeof(LODGroup)) as LODGroup;
                        */
                        Anchor.position = Hit.point;
                        while (count < Trees[i].CountInGroup)
                        {
                            float angle = Random.Range(0, 6.28f);
                            float distance = Random.Range(0.5f, Trees[i].GroupRadius) * (0.5f + ((float)count / Trees[i].CountInGroup));
                            SpawnPose = SpawnCenter + new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * distance;
                            if (Physics.Raycast(SpawnPose, Vector3.down, out Hit, 4000, Ground))
                            {
                                if (Vector3.Angle(Hit.normal, Vector3.up) < Trees[i].SlopeAngle)
                                {
                                    int PrefVar = Random.Range(0, Trees[i].Objects.Length);
                                    Quaternion rot = Quaternion.Euler(new Vector3(0, Random.Range(0, 360f), 0));
                                    float width = Random.Range(Trees[i].WidthRandom.x, Trees[i].WidthRandom.y);
                                    float height = Random.Range(Trees[i].HeightRandom.x, Trees[i].HeightRandom.y);
                                    for (int l = 0; l < Trees[i].Objects[PrefVar].LODs.Length; l++)
                                    {
                                        Transform instance = Instantiate(Trees[i].Objects[PrefVar].LODs[l], Hit.point + Vector3.down * Trees[i].YOffest, rot, Anchor).transform;
                                        instance.localScale = new Vector3(width, height, width);
                                    }
                                    it = 0;
                                }
                            }
                            count++;
                            done++;
                            yield return (float)done / all;
                        }
                        /*UnityEngine.LOD[] lods = new UnityEngine.LOD[maxLODCount];
                        for (int n = 0; n < maxLODCount; n++)
                        {
                            LODs[n].GetComponent<CombinedMeshInstance>().Bake(true, true);
                            Renderer[] renderers = new Renderer[1];
                            renderers[0] = LODs[n].GetComponent<Renderer>();
                            lods[n] = new UnityEngine.LOD(LODCurve.Evaluate(1.0f / (n + 1)), renderers);
                        }

                        ALod.SetLODs(lods);
                        ALod.fadeMode = LODFadeMode.CrossFade;
                        ALod.animateCrossFading = true;
                        ALod.RecalculateBounds();*/
                        gropus++;
                    }
                }

            }
        }
    }


    public float[,] TexToArry(Texture2D tex, int width, int height)
    {
        float[,] map = new float[height, width];
        if (ReplaceWH)
        {
            for (int w = 0; w < width; w++)
            {
                for (int h = 0; h < height; h++)
                {
                    Color u = tex.GetPixelBilinear(InvertCellHeight ? (1 - (float)Mathf.Clamp((h + 1), 0, height) / height) : (float)Mathf.Clamp((h + 1), 0, height) / height, InvertCellWidth ? (1 - (float)w / width) : (float)w / width);
                    Color d = tex.GetPixelBilinear(InvertCellHeight ? (1 - (float)Mathf.Clamp((h - 1), 0, height) / height) : (float)Mathf.Clamp((h - 1), 0, height) / height, InvertCellWidth ? (1 - (float)w / width) : (float)w / width);
                    Color r = tex.GetPixelBilinear(InvertCellHeight ? (1 - (float)h / height) : (float)h / height, InvertCellWidth ? (1 - (float)Mathf.Clamp((w + 1), 0, width) / width) : (float)Mathf.Clamp((w + 1), 0, width) / width);
                    Color l = tex.GetPixelBilinear(InvertCellHeight ? (1 - (float)h / height) : (float)h / height, InvertCellWidth ? (1 - (float)Mathf.Clamp((w - 1), 0, width) / width) : (float)Mathf.Clamp((w - 1), 0, width) / width);
                    map[w, h] = RGBToHeight((u + d + r + l) / 4);
                    //map[w, h] = Mathf.Ceil(map[w, h] * 256) / 256;
                }
            }
        }
        else
        {
            for (int w = 0; w < width; w++)
            {
                for (int h = 0; h < height; h++)
                {
                    Color u = tex.GetPixelBilinear(InvertCellHeight ? (1 - (float)Mathf.Clamp((h + 1), 0, height) / height) : (float)Mathf.Clamp((h + 1), 0, height) / height, InvertCellWidth ? (1 - (float)w / width) : (float)w / width);
                    Color d = tex.GetPixelBilinear(InvertCellHeight ? (1 - (float)Mathf.Clamp((h - 1), 0, height) / height) : (float)Mathf.Clamp((h - 1), 0, height) / height, InvertCellWidth ? (1 - (float)w / width) : (float)w / width);
                    Color r = tex.GetPixelBilinear(InvertCellHeight ? (1 - (float)h / height) : (float)h / height, InvertCellWidth ? (1 - (float)Mathf.Clamp((w + 1), 0, width) / width) : (float)Mathf.Clamp((w + 1), 0, width) / width);
                    Color l = tex.GetPixelBilinear(InvertCellHeight ? (1 - (float)h / height) : (float)h / height, InvertCellWidth ? (1 - (float)Mathf.Clamp((w - 1), 0, width) / width) : (float)Mathf.Clamp((w - 1), 0, width) / width);
                    map[h, w] = RGBToHeight((u + d + r + l) / 4);
                    //map[w, h] = Mathf.Ceil(map[w, h] * 256) / 256;
                }
            }
        }

        return map;
    }
}
[CustomEditor(typeof(WorldCreator))]
public class WorldCreatorEditor : Editor
{
    WorldCreator Target { get => ((WorldCreator)target); }
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if(GUILayout.Button("First generating", GUILayout.Width(150)))
        {
            Target.FirstGenerating();
        }
        if (GUILayout.Button("First colorizing", GUILayout.Width(150)))
        {
            Target.Colorizate();
        }
        if (GUILayout.Button("Erode", GUILayout.Width(150)))
        {
            Target.ErosionFilter();
        }
        if (GUILayout.Button("Stich", GUILayout.Width(150)))
        {
            Target.StichEdges();
        }
        if (GUILayout.Button("PlaceTrees", GUILayout.Width(150)))
        {
            Target.PlaceTrees();
        }
    }
}
#endif