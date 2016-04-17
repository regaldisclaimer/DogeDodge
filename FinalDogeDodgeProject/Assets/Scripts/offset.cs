using UnityEngine;
using System.Collections;

public class offset : MonoBehaviour {

	void Update () {
		float y = Mathf.Repeat (Time.time * 0.025f, 1);
		Vector2 offset = new Vector2 (0, y);
		renderer.sharedMaterial.SetTextureOffset ("_MainTex", offset);
	}
}
