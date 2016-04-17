using UnityEngine;
using System.Collections;

public class BackPlace : MonoBehaviour {

	//private float leftPlace;
	//private float rightPlace;

	private Vector3 leftPos;
	private Vector3 rightPos;
	private Vector3 newLeftPos;
	private Vector3 newRightPos;

	// Use this for initialization
	void Start () {
		newLeftPos = Camera.main.ViewportToWorldPoint(new Vector3(0f,.5f,10f));
		newRightPos = Camera.main.ViewportToWorldPoint(new Vector3(1f, .5f, 10f));
		leftPos = transform.FindChild("left").localPosition;
		rightPos = transform.FindChild("right").localPosition;
		leftPos.x = newLeftPos.x;
		rightPos.x = newRightPos.x;
		transform.FindChild("left").localPosition = leftPos;
		transform.FindChild("right").localPosition = rightPos;
	}
}
