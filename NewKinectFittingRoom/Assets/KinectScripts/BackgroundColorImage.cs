using UnityEngine;
using System.Collections;

public class BackgroundColorImage : MonoBehaviour 
{
	public UnityEngine.UI.RawImage backgroundImage;


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
				backgroundImage.texture = manager.GetUsersClrTex();
				backgroundImage.rectTransform.localScale = manager.GetColorImageScale();
				backgroundImage.color = Color.white;
			}
		}	
	}
}
