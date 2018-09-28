using UnityEngine;
using System.Collections;
//using Windows.Kinect;


public class JointOrientationView : MonoBehaviour 
{
	public int playerIndex = 0;

	public KinectInterop.JointType trackedJoint = KinectInterop.JointType.SpineBase;

	public bool mirroredView = false;

	public float smoothFactor = 5f;

	public UnityEngine.UI.Text debugText;
	
	private Quaternion initialRotation = Quaternion.identity;

	
	void Start()
	{
		initialRotation = transform.rotation;
		//transform.rotation = Quaternion.identity;
	}
	
	void Update () 
	{
		KinectManager manager = KinectManager.Instance;
		
		if(manager && manager.IsInitialized())
		{
			int iJointIndex = (int)trackedJoint;

			if(manager.IsUserDetected(playerIndex))
			{
				long userId = manager.GetUserIdByIndex(playerIndex);
				
				if(manager.IsJointTracked(userId, iJointIndex))
				{
					Quaternion qRotObject = manager.GetJointOrientation(userId, iJointIndex, !mirroredView);
					qRotObject = initialRotation * qRotObject;
					
					if(debugText)
					{
						Vector3 vRotAngles = qRotObject.eulerAngles;
						debugText.text = string.Format("{0} - R({1:000}, {2:000}, {3:000})", trackedJoint, 
						                                       vRotAngles.x, vRotAngles.y, vRotAngles.z);
					}

					if(smoothFactor != 0f)
						transform.rotation = Quaternion.Slerp(transform.rotation, qRotObject, smoothFactor * Time.deltaTime);
					else
						transform.rotation = qRotObject;
				}
				
			}
			
		}
	}
}
