using UnityEngine;
using System.Collections;
using System.IO;

public class GetJointPositionDemo : MonoBehaviour 
{
	public int playerIndex = 0;

	public KinectInterop.JointType joint = KinectInterop.JointType.HandRight;

	public Vector3 jointPosition;

	public bool isSaving = false;

	public string saveFilePath = "joint_pos.csv";
	
	public float secondsToSave = 0f;

	private float saveStartTime = -1f;


	void Start()
	{
		if(isSaving && File.Exists(saveFilePath))
		{
			File.Delete(saveFilePath);
		}
	}


	void Update() 
	{
		if(isSaving)
		{
			if(!File.Exists(saveFilePath))
			{
				using(StreamWriter writer = File.CreateText(saveFilePath))
				{
					string sLine = "time,joint,pos_x,pos_y,poz_z";
					writer.WriteLine(sLine);
				}
			}

			if(saveStartTime < 0f)
			{
				saveStartTime = Time.time;
			}
		}

		KinectManager manager = KinectManager.Instance;

		if(manager && manager.IsInitialized())
		{
			if(manager.IsUserDetected(playerIndex))
			{
				long userId = manager.GetUserIdByIndex(playerIndex);

				if(manager.IsJointTracked(userId, (int)joint))
				{
					Vector3 jointPos = manager.GetJointPosition(userId, (int)joint);
					jointPosition = jointPos;

					if(isSaving)
					{
						if((secondsToSave == 0f) || ((Time.time - saveStartTime) <= secondsToSave))
						{
#if !UNITY_WSA
							using(StreamWriter writer = File.AppendText(saveFilePath))
							{
								string sLine = string.Format("{0:F3},{1},{2:F3},{3:F3},{4:F3}", Time.time, ((KinectInterop.JointType)joint).ToString(), jointPos.x, jointPos.y, jointPos.z);
								writer.WriteLine(sLine);
							}
#else
							string sLine = string.Format("{0:F3},{1},{2:F3},{3:F3},{4:F3}", Time.time, ((KinectInterop.JointType)joint).ToString(), jointPos.x, jointPos.y, jointPos.z);
							Debug.Log(sLine);
#endif
						}
					}
				}
			}
		}

	}

}
