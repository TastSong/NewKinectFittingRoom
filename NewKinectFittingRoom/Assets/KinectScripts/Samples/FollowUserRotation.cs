﻿using UnityEngine;
using System.Collections;

public class FollowUserRotation : MonoBehaviour 
{
	public int playerIndex = 0;

	void Update () 
	{
		KinectManager manager = KinectManager.Instance;

		if(manager && manager.IsInitialized())
		{
			if(manager.IsUserDetected(playerIndex))
			{
				long userId = manager.GetUserIdByIndex(playerIndex);

				if(manager.IsJointTracked(userId, (int)KinectInterop.JointType.ShoulderLeft) &&
				   manager.IsJointTracked(userId, (int)KinectInterop.JointType.ShoulderRight))
				{
					Vector3 posLeftShoulder = manager.GetJointPosition(userId, (int)KinectInterop.JointType.ShoulderLeft);
					Vector3 posRightShoulder = manager.GetJointPosition(userId, (int)KinectInterop.JointType.ShoulderRight);

					posLeftShoulder.z = -posLeftShoulder.z;
					posRightShoulder.z = -posRightShoulder.z;

					Vector3 dirLeftRight = posRightShoulder - posLeftShoulder;
					dirLeftRight -= Vector3.Project(dirLeftRight, Vector3.up);

					Quaternion rotationShoulders = Quaternion.FromToRotation(Vector3.right, dirLeftRight);

					transform.rotation = rotationShoulders;
				}
			}
		}
	}
}
