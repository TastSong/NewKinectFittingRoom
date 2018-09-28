﻿#if (UNITY_STANDALONE_WIN)
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class K2SensorChecker : MonoBehaviour 
{
	public UnityEngine.UI.Text infoText;

	private DepthSensorInterface sensorInterface = null;

	private bool bSensorAvailable = false;

	public bool IsSensorAvailable()
	{
		return bSensorAvailable;
	}


	void Awake()
	{
		try
		{
//			bool bOnceRestarted = false;
//			if(System.IO.File.Exists("SCrestart.txt"))
//			{
//				bOnceRestarted = true;
//				
//				try 
//				{
//					System.IO.File.Delete("SCrestart.txt");
//				} 
//				catch(Exception ex)
//				{
//					Debug.LogError("Error deleting SCrestart.txt");
//					Debug.LogError(ex.ToString());
//				}
//			}

			// init the available sensor interfaces
			sensorInterface = new Kinect2Interface();

			bool bNeedRestart = false;
			if(sensorInterface.InitSensorInterface(true, ref bNeedRestart))
			{
				if(bNeedRestart)
				{
					System.IO.File.WriteAllText("SCrestart.txt", "Restarting level...");
					KinectInterop.RestartLevel(gameObject, "SC");
					return;
				}
				else
				{
					// check if a sensor is connected
					bSensorAvailable = sensorInterface.GetSensorsCount() > 0;
					
					if(infoText != null)
					{
						infoText.text = bSensorAvailable ? "Sensor is connected." : "No sensor is connected.";
					}
				}
			}
			else
			{
				sensorInterface.FreeSensorInterface(true);
				sensorInterface = null;
			}

		}
		catch (Exception ex) 
		{
			Debug.LogError(ex.ToString());
			
			if(infoText != null)
			{
				infoText.text = ex.Message;
			}
		}
		
	}
	
}
#endif
