using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class LoadLevelWithDelay : MonoBehaviour 
{
	public float waitSeconds = 0f;

	public int nextLevel = -1;

	public bool validateKinectManager = true;

	public UnityEngine.UI.Text debugText;

	private float timeToLoadLevel = 0f;
	private bool levelLoaded = false;


	void Start()
	{
		timeToLoadLevel = Time.realtimeSinceStartup + waitSeconds;

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
			if(Time.realtimeSinceStartup >= timeToLoadLevel)
			{
				levelLoaded = true;
				SceneManager.LoadScene(nextLevel);
			}
			else
			{
				float timeRest = timeToLoadLevel - Time.realtimeSinceStartup;

				if(debugText != null)
				{
					debugText.text = string.Format("Time to the next level: {0:F0} s.", timeRest);
				}
			}
		}
	}
	
}
