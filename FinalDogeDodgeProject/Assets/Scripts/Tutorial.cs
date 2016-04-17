using UnityEngine;
using System.Collections;

public class Tutorial : MonoBehaviour {

	public Texture2D backMenu;
	public Texture2D playButton;
	public GUIStyle MainSkin;

	void OnGUI() {
		GUI.skin.button = MainSkin;
		GUI.DrawTexture (new Rect(0f, ((Screen.height/2f)-(Screen.width/(1536f/2726f))/2), Screen.width, 
		                          Screen.width/(1536f/2726f)), backMenu);

		if (GUI.Button (new Rect (Screen.width * .1f, Screen.height * .75f, Screen.width * .8f, Screen.height * .15f), 
		                playButton)) {
			Application.LoadLevel (1);
		}
	}
}
