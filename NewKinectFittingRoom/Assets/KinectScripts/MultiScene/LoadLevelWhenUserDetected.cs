using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class LoadLevelWhenUserDetected : MonoBehaviour 
{
	public KinectGestures.Gestures expectedUserPose = KinectGestures.Gestures.None;
	
	public int nextLevel = -1;

	public bool validateKinectManager = true;

	public UnityEngine.UI.Text debugText;


	private bool levelLoaded = false;
	private KinectGestures.Gestures savedCalibrationPose;


	void Start()
	{
		KinectManager manager = KinectManager.Instance;
		
		if(validateKinectManager && debugText != null)
		{
			if(manager == null || !manager.IsInitialized())
			{
				debugText.text = "KinectManager is not initialized!";
				levelLoaded = true;
			}
		}

		if(manager != null && manager.IsInitialized())
		{
			savedCalibrationPose = manager.playerCalibrationPose;
			manager.playerCalibrationPose = expectedUserPose;
		}
	}

	
	void Update() 
	{
		if(!levelLoaded && nextLevel >= 0)
		{
			KinectManager manager = KinectManager.Instance;
			
			if(manager != null && manager.IsUserDetected())
			{
				manager.playerCalibrationPose = savedCalibrationPose;

				levelLoaded = true;
				SceneManager.LoadScene(nextLevel);
			}
		}
	}
	
}
