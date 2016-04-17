using UnityEngine;
using System.Collections;

//import Google Play Services
using GooglePlayGames;
using UnityEngine.SocialPlatforms;

public class MenuScript : MonoBehaviour {

	public GUIStyle MainSkin;
	public Texture2D playButton;
	public Texture2D title;
	public Texture2D backMenu;
	public Texture2D tutorialButton;

	// Use this for initialization
	void Start () {
		//googleplayservices activate
		PlayGamesPlatform.Activate ();
		
		
		//Log the user into Google Play Services
		Social.localUser.Authenticate((bool success) => {
			//handle sucess/failure
			if (success) {
				Debug.Log("Log In Success");
			} else {
				Debug.Log("Log In Failed");
			}
		});
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	void OnGUI() {
		GUI.skin.button = MainSkin;
		GUI.DrawTexture (new Rect(0f, ((Screen.height/2f)-(Screen.width/(1536f/2726f))/2), Screen.width, 
		                          Screen.width/(1536f/2726f)), backMenu);
		//Tutorial Button
		if (GUI.Button (new Rect (Screen.width * .1f, Screen.height * .6f, Screen.width * .8f, Screen.height * .15f), 
		                tutorialButton)) {
						Application.LoadLevel (2);
				}	
			//Play Button
		if (GUI.Button (new Rect (Screen.width * .1f, Screen.height * .8f, Screen.width * .8f, Screen.height * .15f), 
		                playButton)) {
						Application.LoadLevel (1);
				}
		GUI.DrawTexture (new Rect(Screen.width * .1f, Screen.height * .05f, Screen.width * .8f, Screen.height * .3f), 
		                 title);
	}
}
