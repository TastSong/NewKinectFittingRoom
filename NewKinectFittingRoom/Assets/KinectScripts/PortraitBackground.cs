using UnityEngine;
using System.Collections;

public class PortraitBackground : MonoBehaviour 
{

	public Vector2 targetAspectRatio = Vector2.zero;  // new Vector2 (9f, 16f);

	private bool isInitialized = false;
	private Rect pixelInsetRect;
	private Rect backgroundRect;
	private Rect inScreenRect;
	private Rect shaderUvRect;

	private static PortraitBackground instance = null;


	public static PortraitBackground Instance
	{
		get
		{
			return instance;
		}
	}

	public bool IsInitialized()
	{
		return isInitialized;
	}
	
	public Rect GetBackgroundRect()
	{
		return backgroundRect;
	}

	public Rect GetInScreenRect()
	{
		return inScreenRect;
	}

	public Rect GetShaderUvRect()
	{
		return shaderUvRect;
	}


	////////////////////////////////////////////////////////////////////////


	void Awake()
	{
		instance = this;
	}

	void Start () 
	{
		KinectManager kinectManager = KinectManager.Instance;
		if(kinectManager && kinectManager.IsInitialized())
		{
			// determine the target screen aspect ratio
			float screenAspectRatio = targetAspectRatio != Vector2.zero ? (targetAspectRatio.x / targetAspectRatio.y) : 
				((float)Screen.width / (float)Screen.height);

			float fFactorDW = 0f;
//			if(!useDepthImageResolution)
			{
				fFactorDW = (float)kinectManager.GetColorImageWidth() / (float)kinectManager.GetColorImageHeight() -
					//(float)kinectManager.GetColorImageHeight() / (float)kinectManager.GetColorImageWidth();
					screenAspectRatio;
			}
//			else
//			{
//				fFactorDW = (float)kinectManager.GetDepthImageWidth() / (float)kinectManager.GetDepthImageHeight() -
//					(float)kinectManager.GetDepthImageHeight() / (float)kinectManager.GetDepthImageWidth();
//			}

			float fDeltaWidth = (float)Screen.height * fFactorDW;
			float dOffsetX = -fDeltaWidth / 2f;

			float fFactorSW = 0f;
//			if(!useDepthImageResolution)
			{
				fFactorSW = (float)kinectManager.GetColorImageWidth() / (float)kinectManager.GetColorImageHeight();
			}
//			else
//			{
//				fFactorSW = (float)kinectManager.GetDepthImageWidth() / (float)kinectManager.GetDepthImageHeight();
//			}

			float fScreenWidth = (float)Screen.height * fFactorSW;
			float fAbsOffsetX = fDeltaWidth / 2f;

			pixelInsetRect = new Rect(dOffsetX, 0, fDeltaWidth, 0);
			backgroundRect = new Rect(dOffsetX, 0, fScreenWidth, Screen.height);

			inScreenRect = new Rect(fAbsOffsetX, 0, fScreenWidth - fDeltaWidth, Screen.height);
			shaderUvRect = new Rect(fAbsOffsetX / fScreenWidth, 0, (fScreenWidth - fDeltaWidth) / fScreenWidth, 1);

			GUITexture guiTexture = GetComponent<GUITexture>();
			if(guiTexture)
			{
				guiTexture.pixelInset = pixelInsetRect;
			}

			UnityEngine.UI.RawImage rawImage = GetComponent<UnityEngine.UI.RawImage>();
			if(rawImage)
			{
				rawImage.uvRect = shaderUvRect;
			}

			isInitialized = true;
		}
	}
}
