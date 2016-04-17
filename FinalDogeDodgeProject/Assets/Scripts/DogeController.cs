using UnityEngine;
using System.Collections.Generic;

//import Google Play Services
using GooglePlayGames;
using UnityEngine.SocialPlatforms;

public class DogeController : MonoBehaviour {
	
	public GameObject preAster;
	public GameObject Coin;
	public GameObject Sparkles;
	public GUIStyle MainSkin;
	
	public float projectileSpeed = -3.0f;
	
	public int numberOfObjects;
	public float sideToSide;
	public float timeCoin;
	public float delayModifier;
	public float sidewaysMovementSpeed = 3.0f;
	public float timeBetween;
	
	public Texture2D scoreScreen;
	public Texture2D newHighScore;
	public Texture2D againButton;
	public Texture2D awardsButton;
	public Texture2D scoresButton;
	
	private Vector2 projectileVelocity;
	
	private Vector3 leftLanePos, midLanePos, rightLanePos, playerLanePos;
	private Vector3 topMid, topRight, topLeft, botMid, botRight, botLeft, botOff;
	
	private bool left = false;
	private bool dead = false;
	private bool displayRan = false;

	private Vector3 dogeScale, asterScale, coinScale, newCoinScale, newAsterScale;
	private float scaleMod;
	private float sheight;
	private float swidth;
	
	private float lastDelay;
	private float delay;

	private GameObject coinSparkles;
	private GameObject Aster;
	private float timePassed;
	private float currentTimeObject;
	private float currentTimeCoin;
	private float currentTimeTouch;
	private float coinMoveTime;
	
	private Queue<GameObject> asterQueue;
	
	private int coins = 0;
	private int score = 0;
	bool newHigh = false;
	public Texture2D coinIconTexture;

	//end of vars


	public void Awake() {
		//Google play services activate



		//Ad initialization
		AdColony.Configure(
			"version:2.0,store:google",
			"app0c620af3b8854f0eaa",
			"vzff120c3f1c414eae9c"
		);
	}

	// Use this for initialization
	void Start () { 

		displayRan = false;

		//request Ad

		if(!PlayerPrefs.HasKey("HighScore")){
			PlayerPrefs.SetInt ("HighScore",0);
		}
		
		dogeScale = transform.localScale;
		coinScale = Coin.transform.localScale;
		asterScale = preAster.transform.localScale;
		
		swidth = Screen.width;
		sheight = Screen.height;
		
		scaleMod = (swidth/sheight)/(9f/16f);
		
		leftLanePos = Camera.main.ViewportToWorldPoint(new Vector3(.13f,.175f,10));
		midLanePos = Camera.main.ViewportToWorldPoint(new Vector3(.5f,.175f,10));
		rightLanePos = Camera.main.ViewportToWorldPoint(new Vector3(.87f,.175f,10));
		topLeft = Camera.main.ViewportToWorldPoint(new Vector3(.20833f,1.2f,10));
		botLeft = Camera.main.ViewportToWorldPoint(new Vector3(.20833f,-1f,10));
		topMid = Camera.main.ViewportToWorldPoint(new Vector3(.5f,1.2f,10));
		botMid = Camera.main.ViewportToWorldPoint(new Vector3(.5f,-1f,10));
		topRight = Camera.main.ViewportToWorldPoint(new Vector3(.79167f,1.2f,10));
		botRight = Camera.main.ViewportToWorldPoint(new Vector3(.79167f,-1f,10));
		botOff = Camera.main.ViewportToWorldPoint(new Vector3(2f,-1f,10));
		
		transform.localScale = (new Vector3(dogeScale.x * scaleMod, dogeScale.y * scaleMod, 1));
		newCoinScale = (new Vector3(coinScale.x * scaleMod, coinScale.y * scaleMod, 1));
		newAsterScale = (new Vector3(asterScale.x * scaleMod, asterScale.y * scaleMod, 1));
		
		Coin.transform.localPosition = botMid;
		Coin.transform.localScale =  newCoinScale;
		
		projectileSpeed *= scaleMod;
		sidewaysMovementSpeed *= scaleMod;
		
		projectileVelocity = new Vector2(0f, projectileSpeed);
		
		Coin.rigidbody2D.velocity = projectileVelocity;
		
		Aster = null;
		
		asterQueue = new Queue<GameObject>(numberOfObjects);
		for (int i = 0; i < numberOfObjects; i++) {
			Aster = (GameObject)Instantiate (preAster);
			Aster.transform.localPosition = botLeft;
			Aster.transform.localScale = newAsterScale+(newAsterScale*(Random.Range(-0.4f,-0.1f)));
			Aster.transform.localRotation = Quaternion.Euler(0,0,Random.Range (0,360));
			Aster.rigidbody2D.velocity = projectileVelocity+(projectileVelocity*Random.Range (-0.15f,0.15f));
			Aster.rigidbody2D.angularVelocity = Random.Range (-50,50);
			asterQueue.Enqueue (Aster);
		}
		
		lastDelay = 0;
		delay = 0;
		currentTimeCoin = 0;

		//set initial velocity
		Vector2 tempVelocity = rigidbody2D.velocity;
		tempVelocity.x = -sidewaysMovementSpeed;
		rigidbody2D.velocity = tempVelocity;
	}
	
	// Update is called once per frame
	void Update () {
		
		timePassed = Time.timeSinceLevelLoad;
		 
		if (Input.GetMouseButtonDown(0)){
			left = !left;
			if (left){
				rigidbody2D.velocity = new Vector2 (sidewaysMovementSpeed, rigidbody2D.velocity.y);
			}
			else {
				rigidbody2D.velocity = new Vector2 (-sidewaysMovementSpeed, rigidbody2D.velocity.y);
			}
		}

		//shoot projectiles
		if (timePassed - currentTimeObject > timeBetween) {
			currentTimeObject = timePassed;
			int quad = Random.Range (0,4);
			for (int i = 0; i < 4; i++) {
				if (i == quad) {
					if (timePassed - currentTimeCoin > timeCoin) {
						currentTimeCoin = timePassed;
						CoinShoot(i);
					}
					continue;
				}
				AsterShoot (i);
			}
		}
		
	}
	
	void FixedUpdate () {
		
	}
	
	void OnTriggerEnter2D (Collider2D collider) {
		if (collider.gameObject.CompareTag("coins"))
			CoinCollection(collider);
		else
			HitByAsteroid(collider);
	}
	
	void CoinCollection(Collider2D coinCollider) {

		//add sparkles under coin
		ParticleSystem sparkle = coinCollider.gameObject.transform.GetChild(0).gameObject.particleSystem;
		sparkle.enableEmission = true;
		sparkle.Play();
		coins++;
		SpriteRenderer spriteRend = (SpriteRenderer) coinCollider.gameObject.GetComponent("SpriteRenderer");
		spriteRend.enabled = false;
		Light lite = (Light)Coin.GetComponent ("Light");
		lite.range = 0;
		//coinCollider.gameObject.SetActive(false);
	}
	
	void HitByAsteroid(Collider2D asterCollider) {
		dead = true;
		transform.localPosition = new Vector3(0,20,0);
	}
	
	void OnGUI () {
		GUI.skin.button = MainSkin;
		if (!dead) {
			DisplayCoinsCount();
		} else {
			DisplayRestartButton();
		}
	}
	
	void DisplayRestartButton()
	{
		//Player dead

		int highScore = PlayerPrefs.GetInt ("HighScore");

		//Run this portion only once
		if (!displayRan) {
			displayRan = true;

			float diceRoll = Random.Range(0,100);
			float threshold = 100f-(coins*15);

		    if((diceRoll>threshold)&&AdColony.IsVideoAvailable("vzff120c3f1c414eae9c")) {
		    		Debug.Log("Play AdColony Video");
			    	AdColony.ShowVideoAd("vzff120c3f1c414eae9c"); 
			}

			//Highscore		
			newHigh = SetHighScore(coins);
		}

		//GUI

		Rect buttonRect = new Rect(swidth * 0.1f, sheight * 0.8f, swidth * 0.8f, sheight * 0.1f);

		//Again Button
		if (GUI.Button (new Rect (swidth * .1f, sheight * .75f, swidth * .8f, sheight * .15f), 
		                againButton)) {
			Application.LoadLevel(Application.loadedLevelName);	
		}
		//Rest

		
		GUI.DrawTexture(new Rect (swidth * .1f, sheight * .35f, swidth * .8f, sheight * .3f),
		                scoreScreen);
		
		GUIStyle newStyle = new GUIStyle();
		newStyle.fontSize = (int) swidth/10;
		newStyle.fontStyle = FontStyle.Bold;
		newStyle.normal.textColor = Color.yellow;
		newStyle.font = (Font)Resources.Load("COMIC");
		
		Rect scoreLabelRect = new Rect(swidth * .26f, sheight * .475f, 10, 10);
		GUI.Label(scoreLabelRect, coins.ToString(), newStyle);
		
		Rect highLabelRect = new Rect(swidth * .645f, sheight * .475f, 10, 10);
		GUI.Label(highLabelRect, highScore.ToString(), newStyle);

		//Achievements(Awards) Button
				if (GUI.Button (new Rect (swidth * .1f, sheight * .175f, swidth * .35f, sheight * .10f), 
				                awardsButton)) {
					//first, authenticate
					Social.localUser.Authenticate((bool success) => {
						//handle sucess/failure
						if (success) {
							Debug.Log("Log In Success");
						} else {
							Debug.Log("Log In Failed");
						}
					});	
					//then show
					Social.ShowAchievementsUI();
				}
				
		//Scores Button
				if (GUI.Button (new Rect (swidth * .55f, sheight * .175f, swidth * .35f, sheight * .10f), 
				                scoresButton)) {
					//first, authenticate
					Social.localUser.Authenticate((bool success) => {
						//handle sucess/failure
						if (success) {
							Debug.Log("Log In Success");
						} else {
							Debug.Log("Log In Failed");
						}
					});	
					//then show
					((PlayGamesPlatform)Social.Active).ShowLeaderboardUI("CgkIgbqWkoIcEAIQBg");
				}
		


		if (newHigh) {
			GUI.DrawTexture(new Rect(swidth * .574f, sheight * .57f, swidth * .2f, sheight * .04f),
			                newHighScore);
		}
	}	


	//admob interstitial delegates
	//void OnEnable() {
	//	AdMobPlugin.InterstitialLoaded += HandleInterstitialLoaded;

	//}
	//void OnDisable() {
	//	AdMobPlugin.InterstitialLoaded -= HandleInterstitialLoaded;
		
	//}
	//void HandleInterstitialLoaded ()
	//{
	//	admob.ShowInterstitial ();
	//}

	
	bool SetHighScore(int score) {
		
		//Give Achievements for playing
		
		//First play
		Social.ReportProgress ("CgkIgbqWkoIcEAIQAg", 100.0f, (bool success) => {
			//handle success/failure
		});

		//10 Plays
		((PlayGamesPlatform) Social.Active).IncrementAchievement(
			"CgkIgbqWkoIcEAIQEQ", 1, (bool success) => {
			// handle success or failure
		});
		//100 Plays
		((PlayGamesPlatform) Social.Active).IncrementAchievement(
			"CgkIgbqWkoIcEAIQAw", 1, (bool success) => {
			// handle success or failure
		});
		//250 Plays
		((PlayGamesPlatform) Social.Active).IncrementAchievement(
			"CgkIgbqWkoIcEAIQFQ", 1, (bool success) => {
			// handle success or failure
		});
		
		//poor shibe
		((PlayGamesPlatform) Social.Active).IncrementAchievement(
			"CgkIgbqWkoIcEAIQEg", score, (bool success) => {
			// handle success or failure
		});
		//rich shibe
		((PlayGamesPlatform) Social.Active).IncrementAchievement(
			"CgkIgbqWkoIcEAIQEw", score, (bool success) => {
			// handle success or failure
		});
		//dogeillinaire
		((PlayGamesPlatform) Social.Active).IncrementAchievement(
			"CgkIgbqWkoIcEAIQFA", score, (bool success) => {
			// handle success or failure
		});

		//give achievements

		if (score==0){
			Social.ReportProgress ("CgkIgbqWkoIcEAIQEA", 100.0f, (bool success) => {
				//handle success/failure
			});
		}

		if (score==1){
			Social.ReportProgress ("CgkIgbqWkoIcEAIQDA", 100.0f, (bool success) => {
				//handle success/failure
			});
		}

		if (score==2){
			Social.ReportProgress ("CgkIgbqWkoIcEAIQDQ", 100.0f, (bool success) => {
				//handle success/failure
			});
		}
		if (score==3){
			Social.ReportProgress ("CgkIgbqWkoIcEAIQDg", 100.0f, (bool success) => {
				//handle success/failure
			});
		}		
		if (score==5){
			Social.ReportProgress ("CgkIgbqWkoIcEAIQDw", 100.0f, (bool success) => {
				//handle success/failure
			});
		}
		if (score==7){
			Social.ReportProgress ("CgkIgbqWkoIcEAIQBw", 100.0f, (bool success) => {
				//handle success/failure
			});
		}
		if (score==13){
			Social.ReportProgress ("CgkIgbqWkoIcEAIQCQ", 100.0f, (bool success) => {
				//handle success/failure
			});
		}
		if (score==77){
			Social.ReportProgress ("CgkIgbqWkoIcEAIQCA", 100.0f, (bool success) => {
				//handle success/failure
			});
		}
		if (score==99){
			Social.ReportProgress ("CgkIgbqWkoIcEAIQCg", 100.0f, (bool success) => {
				//handle success/failure
			});
		}

		//if New High Score
		//Update HS and return true
		if(score > PlayerPrefs.GetInt ("HighScore")){
			//set local HS
			PlayerPrefs.SetInt("HighScore", score);
			//Post HS
			Social.ReportScore(score,"CgkIgbqWkoIcEAIQBg", (bool success) => {
				//handle s/f
			});

			//give HS achievements
			if (score >= 10){
				//10 coins
				Social.ReportProgress ("CgkIgbqWkoIcEAIQBA", 100.0f, (bool success) => {
					//handle success/failure
				});
				if (score >= 25) {
					//25 Coins
					Social.ReportProgress ("CgkIgbqWkoIcEAIQBQ", 100.0f, (bool success) => {
						//handle success/failure
					});
					if (score >= 100) {
						Social.ReportProgress ("CgkIgbqWkoIcEAIQAQ", 100.0f, (bool success) => {
							//handle success/failure
						});
						if (score >=200) {
							Social.ReportProgress ("CgkIgbqWkoIcEAIQCw", 100.0f, (bool success) => {
								//handle success/failure
							});
						}
					}
				}
			}
			return true;
		}
		return false;
	}
	
	void DisplayCoinsCount()
	{
		Rect coinIconRect = new Rect(swidth/25,swidth/25,swidth/10,swidth/10);
		GUI.DrawTexture(coinIconRect, coinIconTexture);
		
		GUIStyle style = new GUIStyle();
		style.fontSize = (int) swidth/10;
		style.fontStyle = FontStyle.Bold;
		style.normal.textColor = Color.yellow;
		style.font = (Font)Resources.Load("COMIC");

		Rect labelRect = new Rect(coinIconRect.xMax+(swidth/75), coinIconRect.y-(sheight/75), 60, 32);
		GUI.Label(labelRect, coins.ToString(), style);
	}
	

	
	void AsterShoot(int quad) {
		//shoots one asteroid
		float lane;
		switch (quad) {
		case 0:
			lane = .125f;
			break;
		case 1:
			lane = .375f;
			break;
		case 2:
			lane = .625f;
			break;
		default:
			lane = .875f;
			break;
		}

		//shoots further up
		lastDelay = delay;

		//timeBetween: timebetween chunks
		//delay modifier: it's a ratio
		//ex) if modifier 0.25, timebetween is 2, then delay is less than 1/8 average.
		delay = Random.Range (0f,timeBetween*delayModifier);

		//check last delay to make sure the next one is not too close
		if (Mathf.Abs(lastDelay-delay)<0.22f) {

			if (delay>=lastDelay){
				delay +=0.22f;
			} else {
				delay -=0.22f;
			}
		}
		
		float sway = Random.Range (-sideToSide,sideToSide);
		Vector3 topActual = Camera.main.ViewportToWorldPoint(new Vector3(lane+sway,1.2f+delay,10));
		Aster = asterQueue.Dequeue();
		Aster.transform.localPosition = topActual;
		asterQueue.Enqueue(Aster);
	}
	
	void CoinShoot(int quad) {
		float lane;
		switch (quad) {
		case 0:
			lane = .125f;
			break;
		case 1:
			lane = .375f;
			break;
		case 2:
			lane = .625f;
			break;
		default:
			lane = .875f;
			break;
		}
		
		float sway = Random.Range (-sideToSide,sideToSide);
		float delay = Random.Range (0f,timeBetween*delayModifier);
		Vector3 topActual = Camera.main.ViewportToWorldPoint(new Vector3(lane+sway,1.2f,10));

		//Coin.SetActive (true);
		SpriteRenderer sprRend = (SpriteRenderer)Coin.GetComponent ("SpriteRenderer");
		Light lite = (Light)Coin.GetComponent ("Light");
		sprRend.enabled = true;
		lite.range = 2.5f;
		Coin.rigidbody2D.velocity = projectileVelocity*0.75f;
		Coin.transform.localPosition = topActual;
	}
}