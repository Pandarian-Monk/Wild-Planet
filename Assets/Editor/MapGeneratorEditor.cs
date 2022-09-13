using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor (typeof (MapGenerator))]
public class MapGeneratorEditor : Editor {
	bool startCkeck;

	public override void OnInspectorGUI() {
		MapGenerator mapGen = (MapGenerator)target;

		if (DrawDefaultInspector ()) {

			mapGen.DrawMapInEditor();
            
		}

		if (GUILayout.Button ("Generate")) {
			mapGen.DrawMapInEditor ();
		}

        if (mapGen.noiseMapTextureRender != null)
        {
            if (GUILayout.Button("Noise Map"))
            {
                mapGen.DrawNoiseMap();
            }

            if (GUILayout.Button("Falloff Map"))
            {
                mapGen.DrawFalloffMap();
            }
        }
    }
}
