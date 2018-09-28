using UnityEngine;
using System.Collections;

public class OverlayController : MonoBehaviour 
{
	public Camera backgroundCamera;

	public Camera backgroundCamera2;

	public Camera foregroundCamera;

	void Start () 
	{
		KinectManager manager = KinectManager.Instance;
		
		if(manager && manager.IsInitialized())
		{
			KinectInterop.SensorData sensorData = manager.GetSensorData();

			if(foregroundCamera != null && sensorData != null && sensorData.sensorInterface != null)
			{
//				foregroundCamera.transform.position = new Vector3(sensorData.depthCameraOffset + adjustedCameraOffset, 
//				                                                  manager.sensorHeight, 0f);
				foregroundCamera.transform.position = new Vector3(0f, manager.sensorHeight, 0f);
				foregroundCamera.transform.rotation = Quaternion.Euler(-manager.sensorAngle, 0f, 0f);
//				currentCameraOffset = adjustedCameraOffset;

//				foregroundCamera.fieldOfView = sensorData.colorCameraFOV;
			}

			if(backgroundCamera != null && sensorData != null && sensorData.sensorInterface != null)
			{
				backgroundCamera.transform.position = new Vector3(0f, manager.sensorHeight, 0f);
				backgroundCamera.transform.rotation = Quaternion.Euler(-manager.sensorAngle, 0f, 0f);
			}

			if(backgroundCamera2 != null && sensorData != null && sensorData.sensorInterface != null)
			{
				backgroundCamera2.transform.position = new Vector3(0f, manager.sensorHeight, 0f);
				backgroundCamera2.transform.rotation = Quaternion.Euler(-manager.sensorAngle, 0f, 0f);
			}
		}
	}

	void Update () 
	{
		KinectManager manager = KinectManager.Instance;
		
		if(manager && manager.IsInitialized())
		{
			KinectInterop.SensorData sensorData = manager.GetSensorData();
			
			if(manager.autoHeightAngle == KinectManager.AutoHeightAngle.AutoUpdate || 
				manager.autoHeightAngle == KinectManager.AutoHeightAngle.AutoUpdateAndShowInfo) // ||
			   //currentCameraOffset != adjustedCameraOffset)
			{
				if(foregroundCamera != null && sensorData != null)
				{
//					foregroundCamera.transform.position = new Vector3(sensorData.depthCameraOffset + adjustedCameraOffset, 
//					                                                  manager.sensorHeight, 0f);
					foregroundCamera.transform.position = new Vector3(0f, manager.sensorHeight, 0f);
					foregroundCamera.transform.rotation = Quaternion.Euler(-manager.sensorAngle, 0f, 0f);
//					currentCameraOffset = adjustedCameraOffset;
				}
				
				if(backgroundCamera != null && sensorData != null)
				{
					backgroundCamera.transform.position = new Vector3(0f, manager.sensorHeight, 0f);
					backgroundCamera.transform.rotation = Quaternion.Euler(-manager.sensorAngle, 0f, 0f);
				}
				
				if(backgroundCamera2 != null && sensorData != null)
				{
					backgroundCamera2.transform.position = new Vector3(0f, manager.sensorHeight, 0f);
					backgroundCamera2.transform.rotation = Quaternion.Euler(-manager.sensorAngle, 0f, 0f);
				}
			}
			
//			if(backgroundImage)
//			{
//				if(backgroundImage.texture == null)
//				{
//					backgroundImage.texture = manager.GetUsersClrTex();
//					//backgroundImage.texture = BackgroundRemovalManager.Instance.GetForegroundTex();
//				}
//			}
		}

	}

}
