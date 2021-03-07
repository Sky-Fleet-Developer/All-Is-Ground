using UnityEditor;
using UnityEngine;

[CustomEditor (typeof (MassErosion))]
public class MassErosionEditor : Editor {

    MassErosion massErosion;
    public int Iterations;
    public override void OnInspectorGUI () {
        DrawDefaultInspector ();

        string numIterationsString = massErosion.numErosionIterations.ToString();
        if (massErosion.numErosionIterations >= 1000) {
            numIterationsString = (massErosion.numErosionIterations/1000) + "k";
        }
        Iterations = EditorGUILayout.IntField("Iterations:", Iterations);
        if (GUILayout.Button ("Erode (" + numIterationsString + " iterations)")) {

            massErosion.StartCoroutine(massErosion.StartErode(Iterations, massErosion.GetComponent<Terrain>().terrainData));
           /* var sw = new System.Diagnostics.Stopwatch ();

            sw.Start();
            massErosion.GenerateHeightMap();
            int heightMapTimer = (int)sw.ElapsedMilliseconds;
            sw.Reset();

            sw.Start();
            massErosion.Erode ();
            int erosionTimer = (int)sw.ElapsedMilliseconds;
            sw.Reset();

            sw.Start();
            int meshTimer = (int)sw.ElapsedMilliseconds;

            if (massErosion.printTimers) {
                Debug.Log($"{massErosion.mapSize}x{massErosion.mapSize} heightmap generated in {heightMapTimer}ms");
                Debug.Log ($"{numIterationsString} erosion iterations completed in {erosionTimer}ms");
                Debug.Log ($"Mesh constructed in {meshTimer}ms");
            }
            */
        }
        if (GUILayout.Button("Undo"))
            massErosion.UnDo();
    }

    void OnEnable () {
        massErosion = (MassErosion) target;
        Tools.hidden = true;
    }

    void OnDisable () {
        Tools.hidden = false;
    }
}