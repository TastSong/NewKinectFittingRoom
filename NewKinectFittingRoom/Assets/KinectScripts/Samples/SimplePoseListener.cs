using UnityEngine;
//using Windows.Kinect;
using System.Collections;
using System;


public class SimplePoseListener : MonoBehaviour, KinectGestures.GestureListenerInterface
{
	public int playerIndex = 0;

	public UnityEngine.UI.Text gestureInfo;
	
	private bool progressDisplayed;
	private float progressGestureTime;

	private static SimplePoseListener instance = null;

	private bool touchLeftElbow = false;
	private bool touchRightElbow = false;

	public static SimplePoseListener Instance
	{
		get
		{
			return instance;
		}
	}

	public bool IsTouchingLeftElbow()
	{
		if(touchLeftElbow)
		{
			touchLeftElbow = false;
			return true;
		}

		return false;
	}

	public bool IsTouchingRightElbow()
	{
		if(touchRightElbow)
		{
			touchRightElbow = false;
			return true;
		}

		return false;
	}


	public void UserDetected(long userId, int userIndex)
	{
		if (userIndex != playerIndex)
			return;

		KinectManager manager = KinectManager.Instance;

		manager.DetectGesture(userId, KinectGestures.Gestures.TouchRightElbow);
		manager.DetectGesture(userId, KinectGestures.Gestures.TouchLeftElbow);

		if(gestureInfo != null)
		{
			gestureInfo.text = "Looking for TouchedRightElbow or TouchedLeftElbow";
		}
	}
	
	public void UserLost(long userId, int userIndex)
	{
		if (userIndex != playerIndex)
			return;

		if(gestureInfo != null)
		{
			gestureInfo.text = string.Empty;
		}
	}

	public void GestureInProgress(long userId, int userIndex, KinectGestures.Gestures gesture, 
	                              float progress, KinectInterop.JointType joint, Vector3 screenPos)
	{
		if (userIndex != playerIndex)
			return;

		if (progress >= 0.1f && gestureInfo != null) 
		{
			string sGestureText = string.Format ("{0} {1:F0}%", gesture, progress * 100f);
			gestureInfo.text = sGestureText;

			progressDisplayed = true;
			progressGestureTime = Time.realtimeSinceStartup;
		}
	}

	public bool GestureCompleted(long userId, int userIndex, KinectGestures.Gestures gesture, 
	                              KinectInterop.JointType joint, Vector3 screenPos)
	{
		if (userIndex != playerIndex)
			return false;

		string sGestureText = string.Format ("{0} detected", gesture);
		if(gestureInfo != null)
		{
			gestureInfo.text = sGestureText;
			progressDisplayed = false;
		}

		if(gesture == KinectGestures.Gestures.TouchLeftElbow)
			touchLeftElbow = true;
		else if(gesture == KinectGestures.Gestures.TouchRightElbow)
			touchRightElbow = true;

		return true;
	}

	public bool GestureCancelled(long userId, int userIndex, KinectGestures.Gestures gesture, 
	                              KinectInterop.JointType joint)
	{
		if (userIndex != playerIndex)
			return false;

		if(progressDisplayed)
		{
			progressDisplayed = false;

			if(gestureInfo != null)
			{
				gestureInfo.text = String.Empty;
			}
		}
		
		return true;
	}


	void Awake()
	{
		instance = this;
	}


	public void Update()
	{
		if(progressDisplayed && ((Time.realtimeSinceStartup - progressGestureTime) > 2f))
		{
			progressDisplayed = false;
			
			if(gestureInfo != null)
			{
				gestureInfo.text = String.Empty;
			}

			Debug.Log("Forced gesture progress to end.");
		}
	}
	
}
