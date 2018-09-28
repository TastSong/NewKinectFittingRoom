using UnityEngine;
using System.Collections;

public class SimpleBackgroundRemoval : MonoBehaviour 
{
	public bool displayForeground = true;

	[Multiline]
	public string instructions = "Set 'Compute user map'-setting of KinectManager-component to 'Cut-Out Texture'.";

	private Texture foregroundTex;
	
	private Rect foregroundRect;

	private KinectManager manager;
	

	void Start () 
	{
		manager = KinectManager.Instance;

		if(manager && manager.IsInitialized())
		{
			Rect cameraRect = Camera.main.pixelRect;
			float rectHeight = cameraRect.height;
			float rectWidth = cameraRect.width;

			KinectInterop.SensorData sensorData = manager.GetSensorData();

			if(sensorData != null && sensorData.sensorInterface != null)
			{
				if(rectWidth > rectHeight)
					rectWidth = rectHeight * sensorData.depthImageWidth / sensorData.depthImageHeight;
				else
					rectHeight = rectWidth * sensorData.depthImageHeight / sensorData.depthImageWidth;
				
				foregroundRect = new Rect((cameraRect.width - rectWidth) / 2, cameraRect.height - (cameraRect.height - rectHeight) / 2, rectWidth, -rectHeight);
			}
		}
	}
	
	void Update () 
	{
		if(manager && manager.IsInitialized())
		{
			foregroundTex = manager.GetUsersLblTex();
		}
	}

	void OnGUI()
	{
		if(displayForeground && foregroundTex)
		{
			// get the foreground rectangle (use the portrait background, if available)
			PortraitBackground portraitBack = PortraitBackground.Instance;
			if(portraitBack && portraitBack.enabled)
			{
				foregroundRect = portraitBack.GetBackgroundRect();

				foregroundRect.y += foregroundRect.height;  // invert y
				foregroundRect.height = -foregroundRect.height;
			}
			
			GUI.DrawTexture(foregroundRect, foregroundTex);
		}
	}
	
}
