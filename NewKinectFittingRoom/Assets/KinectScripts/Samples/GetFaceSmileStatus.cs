#if (UNITY_STANDALONE_WIN)
using UnityEngine;
using System.Collections;

public class GetFaceSmileStatus : MonoBehaviour 
{
	public int playerIndex = 0;

	public UnityEngine.UI.Text debugText;

	public Windows.Kinect.DetectionResult smileStatus = Windows.Kinect.DetectionResult.Unknown;


	private KinectManager kinectManager;
	private KinectInterop.SensorData sensorData;


	void Start () 
	{
		kinectManager = KinectManager.Instance;

		if (kinectManager != null) 
		{
			sensorData = kinectManager.GetSensorData ();
		}
	}
	

	void Update () 
	{
		if (kinectManager == null || sensorData == null || sensorData.sensorInterface == null)
			return;

		long userId = kinectManager.GetUserIdByIndex (playerIndex);
		Kinect2Interface k2int = (Kinect2Interface)sensorData.sensorInterface;

		for (int i = 0; i < sensorData.bodyCount; i++)
		{
			if(k2int.faceFrameSources != null && k2int.faceFrameSources[i] != null && k2int.faceFrameSources[i].TrackingId == (ulong)userId)
			{
				if(k2int.faceFrameResults != null && k2int.faceFrameResults[i] != null)
				{
					Windows.Kinect.DetectionResult newStatus = k2int.faceFrameResults [i].FaceProperties [Microsoft.Kinect.Face.FaceProperty.Happy];

					if (newStatus != Windows.Kinect.DetectionResult.Unknown) 
					{
						smileStatus = newStatus;
					}

					debugText.text = "Smile-status: " + smileStatus;
				}
			}
		}
	
	}

}
#endif
