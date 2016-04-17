using UnityEngine;
using System.Collections;

public class offset2 : MonoBehaviour {

	// Update is called once per frame
	void Update () {
		float y = Mathf.Repeat (Time.time * 0.04f, 1);
		Vector2 offset = new Vector2 (0.4f, y+0.4f);
		renderer.sharedMaterial.SetTextureOffset ("_MainTex", offset);
	}
}
