using UnityEngine;
using System.Collections;

public class MapDisplay : MonoBehaviour {



	public void DrawMesh(MeshData meshData, Texture2D texture) {
		this.GetComponent<MeshFilter>().sharedMesh = meshData.CreateMesh ();
		this.GetComponent<MeshRenderer>().sharedMaterial.mainTexture = texture;
	}

}
