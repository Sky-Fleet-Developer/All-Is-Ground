using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.IO;

public class ExportHM : EditorWindow{
        public enum hType{Min,Max,Avg}
        
        static  TerrainData  terrainData;
        static Terrain terrain;
       
        [MenuItem ("Tools/Terraform/Export heightmap")] 
        public static void Init () {
           
            if ( Selection.activeGameObject )
                terrain = Selection.activeGameObject.GetComponent<Terrain>();
     
            if (!terrain) {
                terrain = Terrain.activeTerrain;
            }
            if (terrain) {
                terrainData = terrain.terrainData;
            }
            if (terrainData == null) {
                EditorUtility.DisplayDialog("No terrain selected", "Please select a terrain.", "Cancel");
                return;
            }
           
            //// get the terrain heights into an array and apply them to a texture2D
            byte[] myBytes;
            // int myIndex = 0;
            float[,] rawHeights;
            float hCurrent, hMin, hMax;
            var duplicateHeightMap = new Texture2D(terrainData.heightmapResolution, terrainData.heightmapResolution, TextureFormat.ARGB32, false);
            rawHeights = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);
            // hMin=TerrainStatistics(terrain,hType.Min);
            // hMax=TerrainStatistics(terrain,hType.Min);
            /// run through the array row by row
            for (int y=0; y < duplicateHeightMap.height; y++)
            {
                EditorUtility.DisplayProgressBar("Export heightmap",terrain.name,y/duplicateHeightMap.height);
                for (int x=0; x < duplicateHeightMap.width; x++)
                {
                    /// for wach pixel set RGB to the same so it's gray
                    // Vector4 color = new Vector4(rawHeights[], rawHeights[myIndex], rawHeights[myIndex], 1);
                    // Vector4 color = new Vector4(rawHeights[x,y], rawHeights[myIndex], rawHeights[myIndex], 1);
                    // duplicateHeightMap.SetPixel (x, y, color);
                    // myIndex++;
                    hCurrent=rawHeights[x,y];
                    //hCurrent=(hCurrent-hMin)/hMax;                   
                    duplicateHeightMap.SetPixel (x, y, new Vector4(hCurrent,hCurrent,hCurrent,1f) );                   
                }
            }
            EditorUtility.ClearProgressBar();
            // Apply all SetPixel calls
            duplicateHeightMap.Apply();
            
            /// make it a PNG and save it to the Assets folder
            myBytes = duplicateHeightMap.EncodeToPNG();
            
            // TODO Потом добавить к имени файла размер террейна
            // string filename  = string.Format("HM_{0}_({1}x{2})_Min_{3}_Max_{4}.png", terrain.name,terrainData.heightmapHeight-1,terrainData.heightmapWidth-1, hMin,hMax);
            string filename  = string.Format("HM_{0}_({1}x{2}).png", terrain.name,terrainData.heightmapResolution-1,terrainData.heightmapResolution-1);
            File.WriteAllBytes(Application.dataPath + "/" + filename, myBytes);
            EditorUtility.DisplayDialog("Heightmap Duplicated", "Saved as PNG in Assets/ as: " + filename, "");
        }

        //  Статистика по высотам террейна
        static float TerrainStatistics(Terrain terrain, hType type){
            float ht=0;
            float[,] heights=terrain.terrainData.GetHeights(0, 0, terrain.terrainData.heightmapResolution, terrain.terrainData.heightmapResolution);
            switch (type){
                case hType.Min:
                    ht=300f;
                    break;
                case hType.Max:
                    ht=0f;
                    break;
                case hType.Avg:
                    ht=0.5f;
                    break;                
            }
            foreach(float hcur in heights){                
                switch (type){
                    case hType.Min:
                        if (hcur<ht)
                            ht=hcur;
                        break;
                    case hType.Max:
                        if (hcur>ht)
                            ht=hcur;
                        break;
                    // case hType.Avg:
                    //     ht=0.5f;
                    //     break;                
                }                             
            }
            
            return ht;
        }


    }


