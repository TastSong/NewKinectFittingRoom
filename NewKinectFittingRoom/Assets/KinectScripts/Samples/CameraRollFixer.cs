using UnityEngine;
using System.Collections;

public class CameraRollFixer : MonoBehaviour 
{
	void LateUpdate () 
	{
//		Vector3 cameraUp = transform.up; // Camera.main ? Camera.main.transform.up : transform.up;
//		Quaternion invPitchRot = Quaternion.FromToRotation(cameraUp, Vector3.up);
//		Quaternion targetRot = transform.rotation * invPitchRot;

		Quaternion targetRot = Quaternion.LookRotation(transform.forward, Vector3.up);

		transform.rotation = targetRot; // Quaternion.Slerp(transform.rotation, targetRot, 100f * Time.deltaTime);
	}
}
