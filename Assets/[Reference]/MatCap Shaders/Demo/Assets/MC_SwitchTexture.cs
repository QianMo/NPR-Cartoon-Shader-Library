using UnityEngine;
using System.Collections;

[RequireComponent(typeof(GUITexture))]
public class MC_SwitchTexture : MonoBehaviour
{
	public Material linkedMat;
	public Texture[] textures;
	private int index = 0;
	
	void Update ()
	{
		if(GetComponent<GUITexture>().GetScreenRect().Contains(Input.mousePosition))
		{
			GetComponent<GUITexture>().color = new Color(0.65f,0.65f,0.65f,0.5f);
			
			if(Input.GetMouseButtonDown(0))
				NextTexture();
			else if(Input.GetMouseButtonDown(2))
				PrevTexture();
		}
		else
		{
			GetComponent<GUITexture>().color = Color.gray;
		}
	}
	
	private void NextTexture()
	{
		index++;
		if(index >= textures.Length)
			index = 0;
		ReloadTexture();
	}
	private void PrevTexture()
	{
		index--;
		if(index < 0)
			index = textures.Length-1;
		ReloadTexture();
	}
	private void ReloadTexture()
	{
		linkedMat.SetTexture("_MatCap", textures[index]);
		GetComponent<GUITexture>().texture = textures[index];
	}
}
