using UnityEngine;
using System.Collections;

public class MC_AutoRotate : MonoBehaviour
{
	public Vector3 rotation;
	
	void Update ()
	{
		this.transform.Rotate(rotation * Time.deltaTime, Space.World);
	}
}
