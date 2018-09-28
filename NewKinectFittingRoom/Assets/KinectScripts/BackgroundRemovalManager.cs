using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class BackgroundRemovalManager : MonoBehaviour 
{
	public int playerIndex = -1;
	
	public Camera foregroundCamera;

	public bool colorCameraResolution = true;

	public bool computeBodyTexOnly = false;

	public bool invertAlphaColorMask = false;

	private Color32 defaultColor = new Color32(64, 64, 64, 255);

    [Range(0, 9)]
    public int erodeIterations0 = 0;  // 1

	[Range(0, 9)]
	public int dilateIterations = 0;  // 3;

	[Range(0, 9)]
	public int erodeIterations = 0;  // 4;

	public KinectInterop.BrBlurType bodyBlurFilter = KinectInterop.BrBlurType.Blur;

	public Color bodyContourColor = Color.black;

	public UnityEngine.UI.Text debugText;

	private byte[] foregroundImage;
	
	private Texture2D foregroundTex;
	
	private Rect foregroundRect;
	
	private KinectInterop.SensorData sensorData = null;
	
	private bool isBrInited = false;
	
	private static BackgroundRemovalManager instance;
	
    public static BackgroundRemovalManager Instance
    {
        get
        {
            return instance;
        }
    }

	public bool IsBackgroundRemovalInitialized()
	{
		return isBrInited;
	}
	
//	// returns the raw foreground image
//	public byte[] GetForegroundImage()
//	{
//		return foregroundImage;
//	}
	
	/// <summary>
	/// Gets the foreground image texture.
	/// </summary>
	/// <returns>The foreground image texture.</returns>
	public Texture GetForegroundTex()
	{ 
//		bool bHiResSupported = sensorData != null && sensorData.sensorInterface != null ?
//			sensorData.sensorInterface.IsBRHiResSupported() : false;
		bool bKinect1Int = sensorData != null && sensorData.sensorInterface != null ?
			(sensorData.sensorInterface.GetSensorPlatform() == KinectInterop.DepthSensorPlatform.KinectSDKv1) : false;

		if(computeBodyTexOnly && sensorData != null && sensorData.alphaBodyTexture)
		{
			return sensorData.alphaBodyTexture;
		}
		else if(sensorData != null /**&& bHiResSupported*/ && !bKinect1Int && sensorData.color2DepthTexture)
		{
			return sensorData.color2DepthTexture;
		}
		else if(sensorData != null && !bKinect1Int && sensorData.depth2ColorTexture)
		{
			return sensorData.depth2ColorTexture;
		}
		
		return foregroundTex;
	}

	/// <summary>
	/// Gets the alpha body texture.
	/// </summary>
	/// <returns>The alpha body texture.</returns>
	public Texture GetAlphaBodyTex()
	{
		if(sensorData != null)
		{
			if(sensorData.alphaBodyTexture != null)
				return sensorData.alphaBodyTexture;
			else if(foregroundTex != null)
				return foregroundTex;  // fallback for k1
			else
				return sensorData.bodyIndexTexture;  // general fallback (may have different dimensions)
		}

		return null;
	}
	
	//----------------------------------- end of public functions --------------------------------------//

	void Awake()
	{
		instance = this;
	}

	public void Start() 
	{
		try 
		{
			// get sensor data
			KinectManager kinectManager = KinectManager.Instance;
			if(kinectManager && kinectManager.IsInitialized())
			{
				sensorData = kinectManager.GetSensorData();
			}
			
			if(sensorData == null || sensorData.sensorInterface == null)
			{
				throw new Exception("Background removal cannot be started, because KinectManager is missing or not initialized.");
			}
			
			// ensure the needed dlls are in place and speech recognition is available for this interface
			bool bNeedRestart = false;
			bool bSuccess = sensorData.sensorInterface.IsBackgroundRemovalAvailable(ref bNeedRestart);

			if(bSuccess)
			{
				if(bNeedRestart)
				{
					KinectInterop.RestartLevel(gameObject, "BR");
					return;
				}
			}
			else
			{
				string sInterfaceName = sensorData.sensorInterface.GetType().Name;
				throw new Exception(sInterfaceName + ": Background removal is not supported!");
			}
			
			// inverted alpha-body mask to color texture
			sensorData.invertAlphaColorMask = invertAlphaColorMask;

			if(invertAlphaColorMask &&
				(sensorData.sensorInterface.GetSensorPlatform() == KinectInterop.DepthSensorPlatform.KinectSDKv1))
			{
				// enable the foreground blender if found
				ForegroundBlender foreBlender = ForegroundBlender.Instance;

				if(foreBlender)
				{
					foreBlender.enabled = true;

					// disable the foreground camera, too
					foregroundCamera = null;
				}
			}

			// Initialize the background removal
			bSuccess = sensorData.sensorInterface.InitBackgroundRemoval(sensorData, colorCameraResolution);

			if (!bSuccess)
	        {
				throw new Exception("Background removal could not be initialized.");
	        }

			// create the foreground image and alpha-image
			int imageLength = sensorData.sensorInterface.GetForegroundFrameLength(sensorData, colorCameraResolution);
			foregroundImage = new byte[imageLength];

			// get the needed rectangle
			Rect neededFgRect = sensorData.sensorInterface.GetForegroundFrameRect(sensorData, colorCameraResolution);

			// create the foreground texture
			foregroundTex = new Texture2D((int)neededFgRect.width, (int)neededFgRect.height, TextureFormat.RGBA32, false);

			// calculate the foreground rectangle
			if(foregroundCamera != null)
			{
				Rect cameraRect = foregroundCamera.pixelRect;
				float rectHeight = cameraRect.height;
				float rectWidth = cameraRect.width;
				
				if(rectWidth > rectHeight)
					rectWidth = Mathf.Round(rectHeight * neededFgRect.width / neededFgRect.height);
				else
					rectHeight = Mathf.Round(rectWidth * neededFgRect.height / neededFgRect.width);
				
				foregroundRect = new Rect((cameraRect.width - rectWidth) / 2, cameraRect.height - (cameraRect.height - rectHeight) / 2, rectWidth, -rectHeight);

				// apply color image scale
				if(sensorData.colorImageScale.x < 0f)
				{
					foregroundRect.x = cameraRect.width - (cameraRect.width - rectWidth) / 2;
					foregroundRect.width = -foregroundRect.width;
				}

				if(sensorData.colorImageScale.y > 0f)
				{
					foregroundRect.y = (cameraRect.height - rectHeight) / 2;
					foregroundRect.height = -foregroundRect.height;
				}
			}

			isBrInited = true;
			
			//DontDestroyOnLoad(gameObject);
		} 
		catch(DllNotFoundException ex)
		{
			Debug.LogError(ex.ToString());
			if(debugText != null)
				debugText.text = "Please check the Kinect and BR-Library installations.";
		}
		catch (Exception ex) 
		{
			Debug.LogError(ex.ToString());
			if(debugText != null)
				debugText.text = ex.Message;
		}
	}

	void OnDestroy()
	{
		if(isBrInited && sensorData != null && sensorData.sensorInterface != null)
		{
			// finish background removal
			sensorData.sensorInterface.FinishBackgroundRemoval(sensorData);
		}
		
		isBrInited = false;
		instance = null;
	}
	
	void Update () 
	{
		if(isBrInited)
		{
			// select one player or all players
			if(playerIndex != -1)
			{
				KinectManager kinectManager = KinectManager.Instance;
				long userID = 0;

				if(kinectManager && kinectManager.IsInitialized())
				{
					userID = kinectManager.GetUserIdByIndex(playerIndex);

					if(userID != 0)
					{
						sensorData.selectedBodyIndex = (byte)kinectManager.GetBodyIndexByUserId(userID);
					}
				}

				if(userID == 0)
				{
					// don't display anything - set fictive index
					sensorData.selectedBodyIndex = 222;
				}
			}
			else
			{
				// show all players
				sensorData.selectedBodyIndex = 255;
			}

			// filter parameters
			sensorData.erodeIterations0 = erodeIterations0;
			sensorData.dilateIterations1 = dilateIterations;
            sensorData.erodeIterations2 = erodeIterations;
            sensorData.alphaBlurType = bodyBlurFilter;
			sensorData.bodyContourColor = bodyContourColor;

			// update the background removal
			bool bSuccess = sensorData.sensorInterface.UpdateBackgroundRemoval(sensorData, colorCameraResolution, defaultColor, computeBodyTexOnly);
			
			if(bSuccess)
			{
				KinectManager kinectManager = KinectManager.Instance;
				if(kinectManager && kinectManager.IsInitialized())
				{
					bool bLimitedUsers = kinectManager.IsTrackedUsersLimited();
					List<int> alTrackedIndexes = kinectManager.GetTrackedBodyIndices();
					bSuccess = sensorData.sensorInterface.PollForegroundFrame(sensorData, colorCameraResolution, defaultColor, bLimitedUsers, alTrackedIndexes, ref foregroundImage);

					if(bSuccess)
					{
						foregroundTex.LoadRawTextureData(foregroundImage);
						foregroundTex.Apply();
					}
				}
			}
		}
	}
	
	void OnGUI()
	{
		if(isBrInited && foregroundCamera)
		{
			// get the foreground rectangle (use the portrait background, if available)
			PortraitBackground portraitBack = PortraitBackground.Instance;
			if(portraitBack && portraitBack.enabled)
			{
				foregroundRect = portraitBack.GetBackgroundRect();

				foregroundRect.y += foregroundRect.height;  // invert y
				foregroundRect.height = -foregroundRect.height;
			}

			// update the foreground texture
//			bool bHiResSupported = sensorData != null && sensorData.sensorInterface != null ?
//				sensorData.sensorInterface.IsBRHiResSupported() : false;
			bool bKinect1Int = sensorData != null && sensorData.sensorInterface != null ?
				(sensorData.sensorInterface.GetSensorPlatform() == KinectInterop.DepthSensorPlatform.KinectSDKv1) : false;

			if(computeBodyTexOnly && sensorData != null && sensorData.alphaBodyTexture)
			{
				GUI.DrawTexture(foregroundRect, sensorData.alphaBodyTexture);
			}
			else if(sensorData != null /**&& bHiResSupported*/ && !bKinect1Int && sensorData.color2DepthTexture)
			{
				//GUI.DrawTexture(foregroundRect, sensorData.alphaBodyTexture);
				GUI.DrawTexture(foregroundRect, sensorData.color2DepthTexture);
			}
			else if(sensorData != null && !bKinect1Int && sensorData.depth2ColorTexture)
			{
				//GUI.DrawTexture(foregroundRect, sensorData.alphaBodyTexture);
				GUI.DrawTexture(foregroundRect, sensorData.depth2ColorTexture);
			}
			else if(foregroundTex)
			{
				//GUI.DrawTexture(foregroundRect, sensorData.alphaBodyTexture);
				GUI.DrawTexture(foregroundRect, foregroundTex);
			}
		}
	}


}
