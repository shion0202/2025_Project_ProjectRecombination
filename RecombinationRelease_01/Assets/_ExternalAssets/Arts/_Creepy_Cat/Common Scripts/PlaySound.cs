using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaySound : MonoBehaviour {
	public  AudioClip sound;

	// Use this for initialization
	void Start () {
		GetComponent<AudioSource>().clip = sound;
	}
	
	// Update is called once per frame
	void Update () {
		if (Input.GetButtonUp("Fire1")){
			if (!GetComponent<AudioSource>().isPlaying){
				GetComponent<AudioSource>().PlayOneShot(sound);

			}else{
				GetComponent<AudioSource>().Stop();
			}
		}		
	}
}
