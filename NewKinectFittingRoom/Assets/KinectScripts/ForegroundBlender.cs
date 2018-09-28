using UnityEngine;
using System.Collections;

public class ForegroundBlender : MonoBehaviour 
{
	public Texture backgroundTexture;

	public bool flipTextureX = false;

	public bool flipTextureY = false;

	public bool swapTextures = false;

	private Material foregroundBlendMat;
	private KinectManager kinectManager;
	private BackgroundRemovalManager backManager;
	private long lastDepthFrameTime;


	private static ForegroundBlender instance;

	public static ForegroundBlender Instance
	{
		get
		{
			return instance;
		}
	}


	void Awake()
	{
		instance = this;
	}


	void Start () 
	{
		kinectManager = KinectManager.Instance;

		if(kinectManager && kinectManager.IsInitialized())
		{
			if(!backgroundTexture)
			{
				// by default get the color texture
				backgroundTexture = kinectManager.GetUsersClrTex();
			}

			Shader foregoundBlendShader = Shader.Find("Custom/ForegroundBlendShader");
			if(foregoundBlendShader != null)
			{
				foregroundBlendMat = new Material(foregoundBlendShader);

				foregroundBlendMat.SetInt("_ColorFlipH", flipTextureX ? 1 : 0);
				foregroundBlendMat.SetInt("_ColorFlipV", flipTextureY ? 1 : 0);
				foregroundBlendMat.SetInt("_SwapTextures", swapTextures ? 1 : 0);

				// apply color image scale
				KinectInterop.SensorData sensorData = kinectManager.GetSensorData();
				foregroundBlendMat.SetInt("_BodyFlipH", sensorData.colorImageScale.x < 0 ? 1 : 0);
				foregroundBlendMat.SetInt("_BodyFlipV", sensorData.colorImageScale.y < 0 ? 1 : 0); 

				foregroundBlendMat.SetTexture("_ColorTex", backgroundTexture);
			}
		}
	}

	void OnDestroy()
	{
	}

	void Update () 
	{
		if(foregroundBlendMat && backgroundTexture && 
			kinectManager && kinectManager.IsInitialized())
		{
			if (!backManager) 
			{
				backManager = BackgroundRemovalManager.Instance;
			}

			Texture alphaBodyTex = backManager ? backManager.GetAlphaBodyTex () : null;
			KinectInterop.SensorData sensorData = kinectManager.GetSensorData();

			if(backManager && backManager.IsBackgroundRemovalInitialized() && 
				alphaBodyTex && backgroundTexture && lastDepthFrameTime != sensorData.lastDepthFrameTime)
			{
				lastDepthFrameTime = sensorData.lastDepthFrameTime;
				foregroundBlendMat.SetTexture("_BodyTex", alphaBodyTex);
			}
		}
	}

	void OnRenderImage (RenderTexture source, RenderTexture destination)
	{
		if(foregroundBlendMat != null)
		{
			Graphics.Blit(source, destination, foregroundBlendMat);
		}
	}

}
