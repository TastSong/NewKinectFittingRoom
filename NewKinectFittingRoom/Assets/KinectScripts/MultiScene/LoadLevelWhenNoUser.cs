using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class LoadLevelWhenNoUser : MonoBehaviour 
{
	public int nextLevel = -1;

	public bool validateKinectManager = true;

	public UnityEngine.UI.Text debugText;

	private bool levelLoaded = false;


	void Start()
	{
		if(validateKinectManager && debugText != null)
		{
			KinectManager manager = KinectManager.Instance;

			if(manager == null || !manager.IsInitialized())
			{
				debugText.text = "KinectManager is not initialized!";
				levelLoaded = true;
			}
		}
	}

	
	void Update() 
	{
		if(!levelLoaded && nextLevel >= 0)
		{
			KinectManager manager = KinectManager.Instance;
			
			if(manager != null && !manager.IsUserDetected())
			{
				levelLoaded = true;
				SceneManager.LoadScene(nextLevel);
			}
		}
	}
	
}
