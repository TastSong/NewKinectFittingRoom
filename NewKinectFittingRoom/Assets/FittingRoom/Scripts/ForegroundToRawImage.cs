using UnityEngine;
using System.Collections;
using UnityEngine.UI;


public class ForegroundToRawImage : MonoBehaviour 
{
	private RawImage rawImage;


	void Start()
	{
		rawImage = GetComponent<RawImage>();
	}


	void Update () 
	{
		if (rawImage && rawImage.texture == null) 
		{
			BackgroundRemovalManager backManager = BackgroundRemovalManager.Instance;
			KinectManager kinectManager = KinectManager.Instance;

			if (kinectManager && backManager && backManager.enabled /**&& backManager.IsBackgroundRemovalInitialized()*/) 
			{
				rawImage.texture = backManager.GetForegroundTex();  // user's foreground texture
				rawImage.rectTransform.localScale = kinectManager.GetColorImageScale();
				rawImage.color = Color.white;

			} 
			else if(kinectManager /**&& kinectManager.IsInitialized()*/)
			{
				SimpleBackgroundRemoval simpleBR = GameObject.FindObjectOfType<SimpleBackgroundRemoval>();
				bool isSimpleBR = simpleBR && simpleBR.enabled;

				rawImage.texture = kinectManager.GetUsersClrTex();  // color camera texture
				rawImage.rectTransform.localScale = kinectManager.GetColorImageScale();
				rawImage.color = !isSimpleBR ? Color.white : Color.clear;
			}
		}
//		else if(rawImage && rawImage.texture != null)
//		{
//			KinectManager kinectManager = KinectManager.Instance;
//			if(kinectManager == null)
//			{
//				rawImage.texture = null;
//				rawImage.color = Color.clear;
//			}
//		}
	}


	void OnApplicationPause(bool isPaused)
	{
		// fix for app pause & restore (UWP)
		if(isPaused && rawImage && rawImage.texture != null)
		{
			rawImage.texture = null;
			rawImage.color = Color.clear;
		}
	}

}
