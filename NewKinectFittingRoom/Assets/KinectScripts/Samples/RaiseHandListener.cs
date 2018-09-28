using UnityEngine;
//using Windows.Kinect;
using System.Collections;
using System;


public class RaiseHandListener : MonoBehaviour, KinectGestures.GestureListenerInterface
{
	private static RaiseHandListener instance = null;

	private bool bRaiseLeftHand = false;
	private bool bRaiseRightHand = false;

	public static RaiseHandListener Instance
	{
		get
		{
			return instance;
		}
	}

	/// <summary>
	/// Determines whether the user has raised his left hand.
	/// </summary>
	/// <returns><c>true</c> if the user has raised his left hand; otherwise, <c>false</c>.</returns>
	public bool IsRaiseLeftHand()
	{
		if(bRaiseLeftHand)
		{
			bRaiseLeftHand = false;
			return true;
		}
		
		return false;
	}
	
	/// <summary>
	/// Determines whether the user has raised his right hand.
	/// </summary>
	/// <returns><c>true</c> if the user has raised his right hand; otherwise, <c>false</c>.</returns>
	public bool IsRaiseRightHand()
	{
		if(bRaiseRightHand)
		{
			bRaiseRightHand = false;
			return true;
		}
		
		return false;
	}

	public void UserDetected(long userId, int userIndex)
	{
		KinectManager manager = KinectManager.Instance;

		manager.DetectGesture(userId, KinectGestures.Gestures.RaiseLeftHand);
		manager.DetectGesture(userId, KinectGestures.Gestures.RaiseRightHand);
	}

	public void UserLost(long userId, int userIndex)
	{
	}

	public void GestureInProgress(long userId, int userIndex, KinectGestures.Gestures gesture, 
	                              float progress, KinectInterop.JointType joint, Vector3 screenPos)
	{
	}

	public bool GestureCompleted(long userId, int userIndex, KinectGestures.Gestures gesture, 
	                              KinectInterop.JointType joint, Vector3 screenPos)
	{
		if(gesture == KinectGestures.Gestures.RaiseLeftHand)
			bRaiseLeftHand = true;
		else if(gesture == KinectGestures.Gestures.RaiseRightHand)
			bRaiseRightHand = true;
		
		return true;
	}

	public bool GestureCancelled(long userId, int userIndex, KinectGestures.Gestures gesture, 
	                              KinectInterop.JointType joint)
	{
		if(gesture == KinectGestures.Gestures.RaiseLeftHand)
			bRaiseLeftHand = false;
		else if(gesture == KinectGestures.Gestures.RaiseRightHand)
			bRaiseRightHand = false;
		
		return true;
	}


	void Awake()
	{
		instance = this;
	}

}
