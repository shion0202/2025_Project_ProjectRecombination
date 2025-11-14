using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationPlay : MonoBehaviour {
	//vars for the whole sheet
	public int colCount =  4;
	public int rowCount =  4;

	//vars for animation
	public int rowNumber =  0; //Zero Indexed
	public int colNumber =  0; //Zero Indexed
	public int totalCells =  4;
	public int fps = 10;

	private Vector2 offset;
	private Vector2 size;
	private int index;
	private float timing;

	// Update is called once per frame
	void Update () {
		SetSpriteAnimation(colCount,rowCount,rowNumber,colNumber,totalCells,fps);
	}

	void SetSpriteAnimation(int colCount,int rowCount,int rowNumber,int colNumber,int totalCells,int fps){

		// Calculate index
		timing = Time.time;
		index = (int)(timing * fps);

		// Repeat when exhausting all cells
		index = index % totalCells;

		// Size of every cell
		size = new Vector2(1.0f / colCount, 1.0f / rowCount);

		// split into horizontal and vertical index
		int uIndex = index % colCount;
		int vIndex = index / colCount;

		// build offset
		// v coordinate is the bottom of the image in opengl so we need to invert.
		offset = new Vector2 ((uIndex+colNumber) * size.x, (1.0f - size.y) - (vIndex+rowNumber) * size.y);

		GetComponent<Renderer>().material.SetTextureOffset ("_MainTex", offset);
		GetComponent<Renderer>().material.SetTextureScale  ("_MainTex", size);
	}
}
