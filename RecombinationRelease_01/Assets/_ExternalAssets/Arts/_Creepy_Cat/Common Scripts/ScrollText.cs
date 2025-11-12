using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScrollText : MonoBehaviour {
	public float scrollSpeed = 0.5f;
	public float offset;
		
	// Update is called once per frame
	void Update () {
		offset+= (Time.deltaTime*scrollSpeed)/10.0f;
		GetComponent<Renderer>().material.SetTextureOffset("_MainTex", new Vector2(offset,0));
	}
}
