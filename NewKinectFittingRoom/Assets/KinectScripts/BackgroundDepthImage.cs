﻿using UnityEngine;
using System.Collections;

public class BackgroundDepthImage : MonoBehaviour 
{
	public UnityEngine.UI.RawImage backgroundImage;

	public Camera backgroundCamera;

	public bool useTexture2D = false;


	void Start()
	{
		if (backgroundImage == null) 
		{
			backgroundImage = GetComponent<UnityEngine.UI.RawImage>();
		}
	}


	void Update () 
	{
		KinectManager manager = KinectManager.Instance;

		if (manager && manager.IsInitialized()) 
		{
			if (backgroundImage && (backgroundImage.texture == null)) 
			{
				backgroundImage.texture = !useTexture2D ? manager.GetUsersLblTex() : manager.GetUsersLblTex2D();
				backgroundImage.color = Color.white;

				KinectInterop.SensorData sensorData = manager.GetSensorData();
				if (sensorData != null && sensorData.sensorInterface != null && backgroundCamera != null) 
				{
					// get depth image size
					int depthImageWidth = sensorData.depthImageWidth;
					int depthImageHeight = sensorData.depthImageHeight;

					// calculate insets
					Rect cameraRect = backgroundCamera.pixelRect;
					float rectWidth = cameraRect.width;
					float rectHeight = cameraRect.height;

					if (rectWidth > rectHeight)
						rectWidth = rectHeight * depthImageWidth / depthImageHeight;
					else
						rectHeight = rectWidth * depthImageHeight / depthImageWidth;

					float deltaWidth = cameraRect.width - rectWidth;
					float deltaHeight = cameraRect.height - rectHeight;

//					float leftX = deltaWidth / 2;
//					float rightX = -deltaWidth;
//					float bottomY = -deltaHeight / 2;
//					float topY = deltaHeight;
//
//					backgroundImage.pixelInset = new Rect(leftX, bottomY, rightX, topY);

					RectTransform rectImage = backgroundImage.GetComponent<RectTransform>();
					if (rectImage) 
					{
						rectImage.sizeDelta = new Vector2(-deltaWidth, -deltaHeight);
					}
				}
			}
		}	
	}
}
