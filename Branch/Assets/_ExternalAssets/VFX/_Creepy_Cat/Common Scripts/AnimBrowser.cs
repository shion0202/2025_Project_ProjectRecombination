/// --------------------------------------------------------------------------------
/// I know i code like a shit... I do what i can :) But i'm coming from BASIC world!
/// 
/// This is a small fun script to browse/work with my spritesheet, i hope it help you?
/// (C)2017/2018 by Creepy Cat / Barking Dog / Bad Raccoon publisher's
/// --------------------------------------------------------------------------------
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimBrowser : MonoBehaviour {
	
	public static AnimBrowser Instance;

	public GameObject[] selector;
	public GameObject target;

	private GameObject newParticleSystem;
	private int current=0;
	private string loadingLabel = "LOADING...";
	private bool isCor = false; //for Coroutine check start/stop


	// ---------------------------
	// Use this for initialization
	// ---------------------------
	void Start () {
		LaunchSprite ();
	}

	// -------------------------------
	// Update is called once per frame
	// -------------------------------
	void Update () {
		if(loadingLabel != "" && !isCor) {
			StartCoroutine(RemoveLabel());
		}

		// --------------------------------------------------------------------------------------
		// When i put this shit into the ONGUI procedure, current is increased/decreased twice...
		// --------------------------------------------------------------------------------------
		if (Input.GetKeyUp ("left")) {
			current = current - 1;
			LaunchSprite ();
		}

		if (Input.GetKeyUp ("right")) {
			current = current + 1;
			LaunchSprite ();
		}

		if (Input.GetKeyUp ("up")) {
			ScreenCapture.CaptureScreenshot(selector[current].name+".png");
			loadingLabel = "SCREENSHOOT SAVED...";
		}
	}

	void OnGUI() {
		// --------------------
		// Display informations
		// --------------------
		GUI.Box(new Rect(10, 10, Screen.width-20, 25), "Animation name : " + selector[current].name + "  /  "+"Animation number : " + current+ " (TIPS : Press key UP to save a screenshoot into the directory of the project)");
		DisplayList ();

		// -------------------------
		// Get button/keyboard click
		// -------------------------
		if (GUI.Button(new Rect(0, Screen.height-40, Screen.width/2, 40), "< PREV ANIMATION (KEY LEFT)")) {
			current = current - 1;
			LaunchSprite ();
		}

		if (GUI.Button (new Rect (Screen.width/2, Screen.height-40, Screen.width/2, 40), "(KEY RIGHT) NEXT ANIMATION >")) {
			current = current + 1;
			LaunchSprite ();
		}

		// ---------------------
		// Loading label display
		// ---------------------
		if (loadingLabel != "") {
			GUI.Box (new Rect (Screen.width/2-150, Screen.height/2, 300, 25), loadingLabel);
		}
	}

	// --------------------------------------
	// Fuck yeah!! :) I'm the king of CSharp!
	// --------------------------------------
	void DisplayList(){
		

		for(int i = 0; i < selector.Length; i++){

			if (current == i) {
				GUI.backgroundColor = Color.red;
			} else {
				GUI.backgroundColor = Color.white;
			}

			if (GUI.Button (new Rect (10, 40 + (i * 30), 200, 25), selector [i].name + "(" + i + ")")) {
				current = i;
				LaunchSprite ();
			}
		}

		GUI.backgroundColor = Color.black;
	}


	// ------------------
	// I know.. i know...
	// ------------------
	void ClampMyValue(int maximum, int minimum) {
		if (current <= minimum) {
			current = minimum;
		}

		if (current >= maximum) {
			current = maximum;
		}
	}

	// ------------------------------
	// Function to launch instanciate
	// ------------------------------
	void LaunchSprite(){
		loadingLabel = "LOADING...";
		Destroy (newParticleSystem);
		ClampMyValue (selector.Length-1, 0);

		instantiate(selector[current], target.transform.localPosition);
		//Debug.Log (target.transform.position);
	}

	/// -----------------------------------------
	/// Instantiate a Particle system from prefab
	/// -----------------------------------------
	private GameObject instantiate(GameObject prefab, Vector3 position)	{
		newParticleSystem = Instantiate(prefab,position,Quaternion.identity) as GameObject;
		return newParticleSystem;
	}

	/// ---------------------------------------------------------
	/// Multitask procedure for loading text display (one second)
	/// ---------------------------------------------------------
	IEnumerator RemoveLabel() {
		isCor = true;
		yield return new WaitForSeconds (1);
		loadingLabel = "";
		isCor = false;
	}
}
