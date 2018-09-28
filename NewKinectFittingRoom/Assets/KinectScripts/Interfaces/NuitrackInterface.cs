#if !UNITY_WSA
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System;
using System.IO;


public class NuitrackInterface : DepthSensorInterface 
{

	private static class Constants
	{
		public const int SkeletonCount = 6;
		public const int JointCount = 25;
		public const bool RealSenseD2CMappingEnabled = true;
	}

	private static readonly int[] BodyJoint2NormalNuitrackJoint = {
		(int)nuitrack.JointType.Waist, 				//SpineBase
		(int)nuitrack.JointType.Torso,				//SpineMid
		(int)nuitrack.JointType.Neck,				//Neck
		(int)nuitrack.JointType.Head, 				//Head
		(int)nuitrack.JointType.LeftShoulder,		//ShoulderLeft
		(int)nuitrack.JointType.LeftElbow, 			//ElbowLeft
		(int)nuitrack.JointType.LeftWrist,			//WristLeft
		(int)nuitrack.JointType.LeftHand,			//HandLeft
		(int)nuitrack.JointType.RightShoulder,		//ShoulderRight
		(int)nuitrack.JointType.RightElbow,			//ElbowRight
		(int)nuitrack.JointType.RightWrist,			//WristRight
		(int)nuitrack.JointType.RightHand,			//HandRight
		(int)nuitrack.JointType.LeftHip,			//HipLeft
		(int)nuitrack.JointType.LeftKnee,			//KneeLeft
		(int)nuitrack.JointType.LeftAnkle,			//AnkleLeft
		(int)nuitrack.JointType.LeftFoot, 			//FootLeft
		(int)nuitrack.JointType.RightHip,			//HipRight
		(int)nuitrack.JointType.RightKnee,			//KneeRight
		(int)nuitrack.JointType.RightAnkle,			//AnkleRight
		(int)nuitrack.JointType.RightFoot,			//FootRight
		-1,											//SpineShoulder
		(int)nuitrack.JointType.LeftFingertip,		//HandTipLeft
		-1,											//ThumbLeft
		(int)nuitrack.JointType.RightFingertip,		//HandTipRight
		-1											//ThumbRight
	};


//	private static readonly int[] BodyJoint2MirroredNuitrackJoint = {
//		(int)nuitrack.JointType.Waist, 				//SpineBase
//		(int)nuitrack.JointType.Torso,				//SpineMid
//		(int)nuitrack.JointType.Neck,				//Neck
//		(int)nuitrack.JointType.Head, 				//Head
//		(int)nuitrack.JointType.RightShoulder,		//ShoulderLeft
//		(int)nuitrack.JointType.RightElbow, 		//ElbowLeft
//		(int)nuitrack.JointType.RightWrist,			//WristLeft
//		(int)nuitrack.JointType.RightHand,			//HandLeft
//		(int)nuitrack.JointType.LeftShoulder,		//ShoulderRight
//		(int)nuitrack.JointType.LeftElbow,			//ElbowRight
//		(int)nuitrack.JointType.LeftWrist,			//WristRight
//		(int)nuitrack.JointType.LeftHand,			//HandRight
//		(int)nuitrack.JointType.RightHip,			//HipLeft
//		(int)nuitrack.JointType.RightKnee,			//KneeLeft
//		(int)nuitrack.JointType.RightAnkle,			//AnkleLeft
//		(int)nuitrack.JointType.RightFoot, 			//FootLeft
//		(int)nuitrack.JointType.LeftHip,			//HipRight
//		(int)nuitrack.JointType.LeftKnee,			//KneeRight
//		(int)nuitrack.JointType.LeftAnkle,			//AnkleRight
//		(int)nuitrack.JointType.LeftFoot,			//FootRight
//		-1,											//SpineShoulder
//		(int)nuitrack.JointType.RightFingertip,		//HandTipLeft
//		-1,											//ThumbLeft
//		(int)nuitrack.JointType.LeftFingertip,		//HandTipRight
//		-1											//ThumbRight
//	};


	// local variables
	private KinectInterop.FrameSource sensorFlags;
	private bool bNuitrackInited = false;

	private bool bMultiSource = false;
	private bool bMultiFramesReady = false;

	private bool bMultiFrameColor = false;
	private bool bMultiFrameDepth = false;
	private bool bMultiFrameBodyIndex = false;
	//private bool bMultiFrameBody = false;

	// sync frames time tolerance
	private const long MAX_MULTI_SYNC_TIME = 20000;  // 30000

	// whether to horizontally flip the frame data
	private static bool dontHFlipFrame = false;

	private bool bBackgroundRemovalInited = false;
	private bool bWebColorStream = false;

	private WebCamTexture colorWebCam = null;
	private Color32[] colorWebcamData = null;
	private long colorWebcamTimestamp = 0;

	private NuitrackInitState initState = NuitrackInitState.INIT_NUITRACK_MANAGER_NOT_INSTALLED;

	private nuitrack.DepthSensor depthSensor = null;
	private nuitrack.ColorSensor colorSensor = null;
	private nuitrack.UserTracker userTracker = null;
	private nuitrack.SkeletonTracker skeletonTracker = null;
	private nuitrack.HandTracker handTracker = null;
	private nuitrack.GestureRecognizer gestureRecognizer = null;

	private nuitrack.DepthFrame depthFrame = null;
	private nuitrack.ColorFrame colorFrame = null;
	private nuitrack.UserFrame userFrame = null;

	private nuitrack.SkeletonData skeletonData = null;
	private long skeletonDataTimestamp = 0;
	private nuitrack.HandTrackerData handTrackerData = null;
	//private long handDataTimestamp = 0;

	private long lastDepthFrameTimestamp = 0;
	private long lastColorFrameTimestamp = 0;
	private long lastUserFrameTimestamp = 0;
	private long lastSkeletonFrameTimestamp = 0;
	//private long lastHandFrameTimestamp = 0;

	private OrbbecAstraMapper coordMapper = null;

	private int lastBodyCount = 0;
	private System.Text.StringBuilder sbDebugBodies = new System.Text.StringBuilder();

	private Dictionary<ushort, byte> bodyIdToIndex = new Dictionary<ushort, byte>();
	private Dictionary<ushort, float> bodyIdToTime = new Dictionary<ushort, float>();
	private bool[] bodyIndexUsed = null;  // array of body index flags
	private float waitTimeBeforeRemove = 1f;  // time tolerance in seconds


	public KinectInterop.DepthSensorPlatform GetSensorPlatform ()
	{
		return KinectInterop.DepthSensorPlatform.Nuitrack;
	}

	public bool InitSensorInterface (bool bCopyLibs, ref bool bNeedRestart)
	{
		bool bOnceRestarted = bNeedRestart;
		bNeedRestart = false;

		if(!bCopyLibs)
		{
			// check if the app was restarted or if the astra native library is there (to avoid using OrbbecAstraInterface) 
			string sTargetPath = KinectInterop.GetTargetDllPath(".", KinectInterop.Is64bitArchitecture()) + "/";
			string sTargetLib = sTargetPath + "OrbbecAstraInterface.dll";

			return bOnceRestarted || KinectInterop.IsFileExists(sTargetLib);
		}

		return true;

//		bNeedRestart = false;
//		return true;
	}

	public void FreeSensorInterface (bool bDeleteLibs)
	{
		if (bNuitrackInited) 
		{
			bNuitrackInited = false;
			NuitrackTerm();
		}
	}

	public bool IsSensorAvailable ()
	{
		bool bAvailable = GetSensorsCount() > 0;
		return bAvailable;
	}

	public int GetSensorsCount ()
	{
		bNuitrackInited = NuitrackInit();

//		if(bNuitrackInited)
//		{
//			nuitrack.Nuitrack.Run();
//			NuitrackTerm();
//		}
		
		return (bNuitrackInited ? 1 : 0);
	}

	// tries to initialize Nuitrack
	private bool NuitrackInit()
	{
		bool bInited = false;

		if (initState != NuitrackInitState.INIT_OK) 
		{
			initState = NuitrackLoader.InitNuitrackLibraries();
		}

		if (initState == NuitrackInitState.INIT_OK) 
		{
			try 
			{
#if UNITY_IOS
				nuitrack.Nuitrack.Init("", nuitrack.Nuitrack.NuitrackMode.DEBUG);
#else
				nuitrack.Nuitrack.Init();
#endif
				bInited = true;
				Screen.sleepTimeout = SleepTimeout.NeverSleep;

//				// set Depth2ColorRegistration value
//				nuitrack.Nuitrack.SetConfigValue("Realsense2Module.Depth2ColorRegistration", "true");
//				string sConfigValue = nuitrack.Nuitrack.GetConfigValue("Realsense2Module.Depth2ColorRegistration");
			} 
			catch (Exception ex) 
			{
				Debug.Log(ex.ToString());
			}
		}

		return bInited;
	}

	// shuts down Nuitrack
	private void NuitrackTerm()
	{
		try 
		{
			nuitrack.Nuitrack.Release();
		} 
		catch (Exception ex) 
		{
			Debug.Log(ex.ToString());
		}
	}


	public KinectInterop.SensorData OpenDefaultSensor (KinectInterop.FrameSource dwFlags, float sensorAngle, bool bUseMultiSource)
	{
		// init interface
		if (!bNuitrackInited) 
		{
			bNuitrackInited = NuitrackInit();
			if(!bNuitrackInited)
				return null;
		}

		sensorFlags = dwFlags;
		bMultiSource = bUseMultiSource;

		KinectInterop.SensorData sensorData = new KinectInterop.SensorData();

		if((dwFlags & KinectInterop.FrameSource.TypeColor) != 0)
		{
			for(int i = 0; i < WebCamTexture.devices.Length; i++)
			{
				if(WebCamTexture.devices[i].name.IndexOf("astra", StringComparison.CurrentCultureIgnoreCase) >= 0)
				{
					Debug.Log("    " + WebCamTexture.devices[i].name + "- AstraPro detected.");
					colorWebCam = new WebCamTexture(WebCamTexture.devices[i].name, 640, 480, 30);
					break;
				}
			}

			if(!colorWebCam)
			{
				colorSensor = nuitrack.ColorSensor.Create();
				colorSensor.OnUpdateEvent += HandleOnColorSensorUpdateEvent;
			}
			else
			{
				bWebColorStream = true;
				colorWebCam.Play();

				sensorData.colorImageTexture = colorWebCam;
			}
		}

		dontHFlipFrame = false;
		if((dwFlags & KinectInterop.FrameSource.TypeDepth) != 0)
		{
			depthSensor = nuitrack.DepthSensor.Create();
			depthSensor.SetMirror(true); dontHFlipFrame = true;
			depthSensor.OnUpdateEvent += HandleOnDepthSensorUpdateEvent;
		}
		
		if((dwFlags & KinectInterop.FrameSource.TypeBodyIndex) != 0)
		{
			userTracker = nuitrack.UserTracker.Create();
			userTracker.OnUpdateEvent += HandleOnUserTrackerUpdateEvent;
		}

		if((dwFlags & KinectInterop.FrameSource.TypeBody) != 0)
		{
			skeletonTracker = nuitrack.SkeletonTracker.Create();
			skeletonTracker.OnSkeletonUpdateEvent += HandleOnSkeletonUpdateEvent;

			handTracker = nuitrack.HandTracker.Create();
			handTracker.OnUpdateEvent += HandleOnHandsUpdateEvent;

			gestureRecognizer = nuitrack.GestureRecognizer.Create();
			gestureRecognizer.OnNewGesturesEvent += OnNewGestures;
		}

//		if((dwFlags & KinectInterop.FrameSource.TypeInfrared) != 0)
//		{
//		}

		nuitrack.Nuitrack.onIssueUpdateEvent += OnIssuesUpdate;
		nuitrack.Nuitrack.Run();

		sensorData.bodyCount = Constants.SkeletonCount;
		sensorData.jointCount = Constants.JointCount;

		sensorData.depthCameraOffset = 0f;
		sensorData.faceOverlayOffset = 0f;

		if(!bWebColorStream)
		{
//			// wait for color frame
//			if (colorSensor != null) 
//			{
//				colorFrame = colorSensor.GetColorFrame();
//				float waitTillTime = Time.realtimeSinceStartup + 2.5f;
//
//				while (colorFrame == null && Time.realtimeSinceStartup <= waitTillTime) 
//				{
//					nuitrack.Nuitrack.Update();
//					System.Threading.Thread.Sleep(50);
//					colorFrame = colorSensor.GetColorFrame();
//				}
//			}

			sensorData.colorImageWidth = colorSensor != null ? colorSensor.GetOutputMode().XRes : 640;
			sensorData.colorImageHeight = colorSensor != null ? colorSensor.GetOutputMode().YRes : 480;

			// flip color image vertically
			sensorData.colorImageScale = new Vector3(1f, -1f, 1f);
		}
		else
		{
			sensorData.colorImageWidth = colorWebCam.width;
			sensorData.colorImageHeight = colorWebCam.height;

			// flip color image horizontally
			sensorData.colorImageScale = new Vector3(-1f, 1f, 1f);
		}

		Debug.Log("    Color sensor: " + (colorSensor != null ? colorSensor.ToString() : "-") + 
			", width: " + sensorData.colorImageWidth + ", height: " + sensorData.colorImageHeight);

//		// wait for depth frame
//		if (depthSensor != null) 
//		{
//			depthFrame = depthSensor.GetDepthFrame();
//			float waitTillTime = Time.realtimeSinceStartup + 2.5f;
//
//			while (depthFrame == null && Time.realtimeSinceStartup <= waitTillTime) 
//			{
//				nuitrack.Nuitrack.Update();
//				System.Threading.Thread.Sleep(50);
//				depthFrame = depthSensor.GetDepthFrame();
//			}
//		}

		sensorData.depthImageWidth = depthSensor != null ? depthSensor.GetOutputMode().XRes : 640;
		sensorData.depthImageHeight = depthSensor != null ? depthSensor.GetOutputMode().YRes : 480;

		Debug.Log("    Depth sensor: " + (depthSensor != null ? depthSensor.ToString() : "-") + 
			", width: " + sensorData.depthImageWidth + ", height: " + sensorData.depthImageHeight);

		// color & depth FOV
		float hfovr = colorSensor != null ? colorSensor.GetOutputMode().HFOV : 0f;
		float vfovr = 2f * Mathf.Atan(Mathf.Tan(hfovr / 2f) * sensorData.depthImageHeight / sensorData.depthImageWidth);
		sensorData.colorCameraFOV = vfovr * Mathf.Rad2Deg;

		hfovr = depthSensor != null ? depthSensor.GetOutputMode().HFOV : 0f;
		vfovr = 2f * Mathf.Atan(Mathf.Tan(hfovr / 2f) * sensorData.depthImageHeight / sensorData.depthImageWidth);
		sensorData.depthCameraFOV = vfovr * Mathf.Rad2Deg;

		if((dwFlags & KinectInterop.FrameSource.TypeColor) != 0)
		{
			int colorImageSize = !colorWebCam ? (sensorData.colorImageWidth * sensorData.colorImageHeight * 3) : 0;
			sensorData.colorImage = new byte[colorImageSize];

			if(!colorWebCam)
				sensorData.colorImageTexture2D = new Texture2D(sensorData.colorImageWidth, sensorData.colorImageHeight, TextureFormat.RGB24, false);
		}

		if((dwFlags & KinectInterop.FrameSource.TypeDepth) != 0)
		{
			int depthImageSize = sensorData.depthImageWidth * sensorData.depthImageHeight;
			sensorData.depthImage = new ushort[depthImageSize];
		}
		
		if((dwFlags & KinectInterop.FrameSource.TypeBodyIndex) != 0)
		{
			int bodyIndexImageSize = sensorData.depthImageWidth * sensorData.depthImageHeight;
			sensorData.bodyIndexImage = new byte[bodyIndexImageSize];
		}
		
		if((dwFlags & KinectInterop.FrameSource.TypeInfrared) != 0)
		{
			int depthImageSize = sensorData.depthImageWidth * sensorData.depthImageHeight;
			sensorData.infraredImage = new ushort[depthImageSize];
		}
		
		// setup coordinate mapper
		coordMapper = new OrbbecAstraMapper();
		coordMapper.SetupSpaceMapping(sensorData.depthImageWidth, sensorData.depthImageHeight, hfovr, vfovr);

		if (Constants.RealSenseD2CMappingEnabled && sensorData.depthImageWidth == sensorData.colorImageWidth && 
			sensorData.depthImageHeight == sensorData.colorImageHeight) 
		{
			coordMapper.SetupRegCalibrationData(Constants.RealSenseD2CMappingEnabled, sensorData.depthImageWidth, sensorData.depthImageHeight);
		} 
		else 
		{
			// d2c-calibration data is valid for Orbbec-Astra only (sensor id & calibration data - not provided by Nuitrack SDK so far)
			coordMapper.SetupCalibrationData(bWebColorStream);
		}

		// set lost-user time tolerance equal to KM
		if (KinectManager.Instance != null) 
		{
			waitTimeBeforeRemove = KinectManager.Instance.waitTimeBeforeRemove;
		}

		Debug.Log("Nuitrack sensor opened");
		
		return sensorData;
	}

	public void CloseSensor (KinectInterop.SensorData sensorData)
	{
		if(colorWebCam)
		{
			colorWebCam.Stop();
			colorWebCam = null;
		}

		if (coordMapper != null) 
		{
			coordMapper.CleanUp();
			coordMapper = null;
		}

		if (colorSensor != null) 
		{
			colorSensor.OnUpdateEvent -= HandleOnColorSensorUpdateEvent;
			colorSensor = null;
			colorFrame = null;
		}

		if (depthSensor != null) 
		{
			depthSensor.OnUpdateEvent -= HandleOnDepthSensorUpdateEvent;
			depthSensor = null;
			depthFrame = null;
		}

		if (userTracker != null) 
		{
			userTracker.OnUpdateEvent -= HandleOnUserTrackerUpdateEvent;
			userTracker = null;
			userFrame = null;
		}

		if (skeletonTracker != null) 
		{
			skeletonTracker.OnSkeletonUpdateEvent -= HandleOnSkeletonUpdateEvent;
			skeletonTracker = null;
			skeletonData = null;
		}

		if (handTracker != null) 
		{
			handTracker.OnUpdateEvent -= HandleOnHandsUpdateEvent;
			handTracker = null;
			handTrackerData = null;
		}

		if (gestureRecognizer != null) 
		{
			gestureRecognizer.OnNewGesturesEvent -= OnNewGestures;
			gestureRecognizer = null;
		}

		if (bNuitrackInited) 
		{
			bNuitrackInited = false;

			nuitrack.Nuitrack.onIssueUpdateEvent -= OnIssuesUpdate;
			NuitrackTerm();
		}

		Debug.Log("Nuitrack sensor closed");
	}

	public bool UpdateSensorData (KinectInterop.SensorData sensorData)
	{
#if UNITY_ANDROID
		if (NuitrackLoader.initState == NuitrackInitState.INIT_OK)
#endif
		nuitrack.Nuitrack.Update();

		if(bWebColorStream && colorWebCam && colorWebCam.didUpdateThisFrame)
		{
			if (colorWebcamData == null) 
			{
				colorWebcamData = new Color32[colorWebCam.width * colorWebCam.height];
			}

			colorWebCam.GetPixels32(colorWebcamData);
			colorWebcamTimestamp = DateTime.Now.Ticks / 10000;

			if (bMultiSource) 
			{
				bMultiFrameColor = true;
				SyncAllFrames();
			}
		}

		return true;
	}

	void OnIssuesUpdate (nuitrack.issues.IssuesData issuesData)
	{
		nuitrack.issues.SensorIssue sensorIssue = issuesData.GetIssue<nuitrack.issues.SensorIssue>();
		if (sensorIssue != null) 
		{
			Debug.LogError("Sensor issue detected: " + sensorIssue.Name);
		}
	}

	private void HandleOnColorSensorUpdateEvent(nuitrack.ColorFrame frame)
	{
		colorFrame = frame;

		if (bMultiSource && colorFrame != null) 
		{
			bMultiFrameColor = true;
			SyncAllFrames();
		}
	}

	private void HandleOnDepthSensorUpdateEvent (nuitrack.DepthFrame frame)
	{
		depthFrame = frame;

		if (bMultiSource && depthFrame != null) 
		{
			bMultiFrameDepth = true;
			SyncAllFrames();
		}
	}

	private void HandleOnUserTrackerUpdateEvent (nuitrack.UserFrame frame)
	{
		userFrame = frame;

		if (bMultiSource && userFrame != null) 
		{
			bMultiFrameBodyIndex = true;
			SyncAllFrames();
		}
	}

	private void HandleOnSkeletonUpdateEvent (nuitrack.SkeletonData _skeletonData)
	{
		skeletonData = _skeletonData;
		skeletonDataTimestamp = DateTime.Now.Ticks / 10000;
	}

	private void HandleOnHandsUpdateEvent (nuitrack.HandTrackerData _handTrackerData)
	{
		handTrackerData = _handTrackerData;
		//handDataTimestamp = DateTime.Now.Ticks / 10000;
	}

	private void OnNewGestures(nuitrack.GestureData gestures)
	{
		// do nothing
	}

	// synchronizes existing frames depending on their sync times
	private void SyncAllFrames()
	{
//		// find the max frame time
//		long maxFrameTime = 0;
//
//		if (bMultiFrameColor) 
//		{
//			if (!bWebColorStream) 
//			{
//				if(maxFrameTime < (long)colorFrame.Timestamp)
//					maxFrameTime = (long)colorFrame.Timestamp;
//			}
//			else
//			{
//				if(maxFrameTime < colorWebcamTimestamp)
//					maxFrameTime = colorWebcamTimestamp;
//			}
//		}
//			
//		if (bMultiFrameDepth && maxFrameTime < (long)depthFrame.Timestamp)
//			maxFrameTime = (long)depthFrame.Timestamp;
//		if (bMultiFrameBodyIndex && maxFrameTime < (long)userFrame.Timestamp)
//			maxFrameTime = (long)userFrame.Timestamp;
//
//		// apply the tolerance
//		maxFrameTime -= MAX_MULTI_SYNC_TIME;
//
//		// release old frames
//		if (bMultiFrameColor && (long)colorFrame.Timestamp < maxFrameTime) 
//		{
//			bMultiFrameColor = false;
//
//			if (!bWebColorStream)
//				colorFrame = null;
//			else
//				colorWebcamTimestamp = 0;
//		}
//
//		if (bMultiFrameDepth && (long)depthFrame.Timestamp < maxFrameTime) 
//		{
//			bMultiFrameDepth = false;
//			depthFrame = null;
//		}
//
//		if (bMultiFrameBodyIndex && (long)userFrame.Timestamp < maxFrameTime) 
//		{
//			bMultiFrameBodyIndex = false;
//			userFrame = null;
//		}
	}

	public bool GetMultiSourceFrame (KinectInterop.SensorData sensorData)
	{
		if (bMultiSource) 
		{
			bMultiFramesReady =
				((sensorFlags & KinectInterop.FrameSource.TypeColor) == 0 || bMultiFrameColor) &&
				((sensorFlags & KinectInterop.FrameSource.TypeDepth) == 0 || bMultiFrameDepth) &&
				((sensorFlags & KinectInterop.FrameSource.TypeBodyIndex) == 0 || bMultiFrameBodyIndex);

			return bMultiFramesReady;
		}

		return false;
	}

	public void FreeMultiSourceFrame (KinectInterop.SensorData sensorData)
	{
		bMultiFramesReady = false;
	}

	public bool PollBodyFrame (KinectInterop.SensorData sensorData, ref KinectInterop.BodyFrameData bodyFrame, 
	                           ref Matrix4x4 kinectToWorld, bool bIgnoreJointZ)
	{
		bool bNewFrame = false;

		// look for skeleton frame
		if(skeletonData != null && skeletonDataTimestamp != lastSkeletonFrameTimestamp)
		{
			lastSkeletonFrameTimestamp = skeletonDataTimestamp;
			//long timeNowTicks = DateTime.Now.Ticks;

			bodyFrame.liPreviousTime = bodyFrame.liRelativeTime;
			bodyFrame.liRelativeTime = skeletonDataTimestamp;

			int bodyCount = skeletonData.Skeletons != null ? skeletonData.Skeletons.Length : 0;
			if(lastBodyCount != bodyCount)
			{
				sbDebugBodies.Append(bodyCount).Append(" bodies - ");
			}

			// clear id2index
			//bodyIdToIndex.Clear();

			// create bodyIndexUsed-array on the 1st use
			if (bodyIndexUsed == null) 
			{
				bodyIndexUsed = new bool[sensorData.bodyCount];
			}

			// clear the tracked flags and find empty index
			int eIndex = -1;

			for (int i = 0; i < sensorData.bodyCount; i++) 
			{
				bodyFrame.bodyData[i].bIsTracked = 0;

				if (eIndex < 0 && !bodyIndexUsed[i])
					eIndex = i;
			}

			for(int i = 0; i < sensorData.bodyCount; i++)
			{
				// compare to real body count
				if(i >= bodyCount)
				{
					//bodyFrame.bodyData[i].bIsTracked = 0;
					continue;
				}

				// get body and joints data
				nuitrack.Skeleton nuiBody = skeletonData.Skeletons[i];
				if(lastBodyCount != bodyCount)
				{
					sbDebugBodies.Append(nuiBody.ID).Append(":");

					nuitrack.Joint jUser = nuiBody.Joints[(int)nuitrack.JointType.Waist];
					Vector3 vUserPos = new Vector3(dontHFlipFrame ? jUser.Real.X : -jUser.Real.X, jUser.Real.Y, jUser.Real.Z);
					sbDebugBodies.Append(vUserPos);
						
					sbDebugBodies.Append("  ");
				}

				// create the body index if needed
				ushort uBodyId = (ushort)nuiBody.ID;
				if (!bodyIdToIndex.ContainsKey(uBodyId)) 
				{
					Debug.Log("  New body ID:" + uBodyId + ", index: " + eIndex);

					bodyIdToIndex[uBodyId] = (byte)eIndex;
					bodyIndexUsed[eIndex] = true;
				}

				// get existing body index
				int bi = bodyIdToIndex[uBodyId];
				bodyIdToTime[uBodyId] = Time.time;

				// set body tracking state
				bodyFrame.bodyData[bi].bIsTracked = 1;

				// transfer body and joints data
				bodyFrame.bodyData[bi].liTrackingID = (long)nuiBody.ID;
				//bodyIdToIndex[(ushort)nuiBody.ID] = (byte)i;

				// z-position of the waist
				float waistPosZ = nuiBody.Joints[(int)nuitrack.JointType.Waist].Real.Z / 1000f;

				for(int j = 0; j < sensorData.jointCount; j++)
				{
					KinectInterop.JointData jointData = bodyFrame.bodyData[bi].joint[j];

					int nuiJI = BodyJoint2NormalNuitrackJoint[j]; // dontHFlipFrame ? BodyJoint2NormalNuitrackJoint[j] : BodyJoint2MirroredNuitrackJoint[j];

					if(nuiJI >= 0)
					{
						nuitrack.Joint nuiJoint = nuiBody.Joints[nuiJI];

						if (nuiJoint.Confidence >= 0.5f)
							jointData.trackingState = KinectInterop.TrackingState.Tracked;
						else if (nuiJoint.Confidence >= 0.1f)
							jointData.trackingState = KinectInterop.TrackingState.Inferred;
						else
							jointData.trackingState = KinectInterop.TrackingState.NotTracked;

						Vector3 jointPos = new Vector3((dontHFlipFrame ? nuiJoint.Real.X : -nuiJoint.Real.X) / 1000f, nuiJoint.Real.Y / 1000f, nuiJoint.Real.Z / 1000f);
						float jPosZ = (bIgnoreJointZ && j > 0) ? waistPosZ : jointPos.z;

						jointData.kinectPos = jointPos;
						jointData.position = kinectToWorld.MultiplyPoint3x4(new Vector3(jointPos.x, jointPos.y, jPosZ));

						jointData.orientation = Quaternion.identity;
					}
					else
					{
						jointData.trackingState = KinectInterop.TrackingState.NotTracked;
					}

					if(j == 0)
					{
						bodyFrame.bodyData[bi].position = jointData.position;
						bodyFrame.bodyData[bi].orientation = jointData.orientation;
					}

					bodyFrame.bodyData[bi].joint[j] = jointData;
				}

				if (handTrackerData != null) 
				{
					nuitrack.UserHands hands = handTrackerData.GetUserHandsByID(nuiBody.ID);

					if (hands != null) 
					{
						nuitrack.HandContent? leftHand = hands.LeftHand; // dontHFlipFrame ? hands.LeftHand : hands.RightHand;
						bodyFrame.bodyData[bi].leftHandState = leftHand.HasValue && leftHand.Value.Click ? KinectInterop.HandState.Closed : KinectInterop.HandState.Open;
						bodyFrame.bodyData[bi].leftHandConfidence = leftHand.HasValue ? KinectInterop.TrackingConfidence.High : KinectInterop.TrackingConfidence.Low;
					
						nuitrack.HandContent? rightHand = hands.RightHand; // dontHFlipFrame ? hands.RightHand : hands.LeftHand;
						bodyFrame.bodyData[bi].rightHandState = rightHand.HasValue && rightHand.Value.Click ? KinectInterop.HandState.Closed : KinectInterop.HandState.Open;
						bodyFrame.bodyData[bi].rightHandConfidence = rightHand.HasValue ? KinectInterop.TrackingConfidence.High : KinectInterop.TrackingConfidence.Low;
					}
				}

				// processes some special joint cases
				ProcessBodyFrameSpecialCases(bi, ref bodyFrame, ref kinectToWorld);
			}

			// check for lost users
			List<ushort> lostUsers = new List<ushort>();

			foreach(ushort uBodyId in bodyIdToTime.Keys)
			{
				// prevent user removal upon sporadical tracking failures
				if((Time.time - bodyIdToTime[uBodyId]) > waitTimeBeforeRemove)
				{
					lostUsers.Add(uBodyId);
				}
			}

			// remove the lost users
			if (lostUsers.Count > 0) 
			{
				foreach (ushort uBodyId in lostUsers) 
				{
					Debug.Log("  Lost body ID:" + uBodyId + ", index: " + bodyIdToIndex[uBodyId]);

					int bi = bodyIdToIndex[uBodyId];
					bodyIndexUsed[bi] = false;

					bodyIdToIndex.Remove(uBodyId);
					bodyIdToTime.Remove(uBodyId);
				}

				// clean up
				lostUsers.Clear();
			}

			// write bodies-debug info, if needed
			if(sbDebugBodies.Length > 0)
			{
				sbDebugBodies.Append("Time: ").Append(Time.realtimeSinceStartup);
				//Debug.Log(sbDebugBodies.ToString());

				sbDebugBodies.Remove(0, sbDebugBodies.Length);
			}

			lastBodyCount = bodyCount;
			bNewFrame = true;
		}

		return bNewFrame;
	}

	// processes some special cases in body joints
	private void ProcessBodyFrameSpecialCases(int i, ref KinectInterop.BodyFrameData bodyFrame, ref Matrix4x4 kinectToWorld)
	{
		// special case - shoulder center
		int r = (int)KinectInterop.JointType.ShoulderRight;
		int l = (int)KinectInterop.JointType.ShoulderLeft;
		int c = (int)KinectInterop.JointType.SpineShoulder;

		if(bodyFrame.bodyData[i].joint[r].trackingState == KinectInterop.TrackingState.Tracked &&
			bodyFrame.bodyData[i].joint[l].trackingState == KinectInterop.TrackingState.Tracked)
		{
			KinectInterop.JointData jointData = bodyFrame.bodyData[i].joint[c];

			jointData.trackingState = bodyFrame.bodyData[i].joint[r].trackingState;
			jointData.orientation = Quaternion.identity;

			Vector3 posRight = bodyFrame.bodyData[i].joint[r].kinectPos;
			Vector3 posLeft = bodyFrame.bodyData[i].joint[l].kinectPos;
			jointData.kinectPos = (posRight + posLeft) * 0.5f;

			posRight = bodyFrame.bodyData[i].joint[r].position;
			posLeft = bodyFrame.bodyData[i].joint[l].position;
			jointData.position = (posRight + posLeft) * 0.5f;

			bodyFrame.bodyData[i].joint[c] = jointData;
		}
		else
		{
			bodyFrame.bodyData[i].joint[c].trackingState = KinectInterop.TrackingState.NotTracked;
		}

		// special case - hip center
		r = (int)KinectInterop.JointType.HipRight;
		l = (int)KinectInterop.JointType.HipLeft;
		c = (int)KinectInterop.JointType.SpineBase;

		if(bodyFrame.bodyData[i].joint[r].trackingState == KinectInterop.TrackingState.Tracked &&
			bodyFrame.bodyData[i].joint[l].trackingState == KinectInterop.TrackingState.Tracked)
		{
			KinectInterop.JointData jointData = bodyFrame.bodyData[i].joint[c];

			jointData.trackingState = bodyFrame.bodyData[i].joint[r].trackingState;
			jointData.orientation = Quaternion.identity;

			Vector3 posRight = bodyFrame.bodyData[i].joint[r].kinectPos;
			Vector3 posLeft = bodyFrame.bodyData[i].joint[l].kinectPos;
			jointData.kinectPos = (posRight + posLeft) * 0.5f;

			posRight = bodyFrame.bodyData[i].joint[r].position;
			posLeft = bodyFrame.bodyData[i].joint[l].position;
			jointData.position = (posRight + posLeft) * 0.5f;

			bodyFrame.bodyData[i].joint[c] = jointData;

			// modify the body position, too
			bodyFrame.bodyData[i].position = jointData.position;
			bodyFrame.bodyData[i].orientation = jointData.orientation;
		}

		// special case - foot left
		r = (int)KinectInterop.JointType.HipRight;
		l = (int)KinectInterop.JointType.HipLeft;
		int d = (int)KinectInterop.JointType.SpineBase;
		int u = (int)KinectInterop.JointType.SpineShoulder;

		int f = (int)KinectInterop.JointType.FootLeft;
		int a = (int)KinectInterop.JointType.AnkleLeft;

		if(bodyFrame.bodyData[i].joint[r].trackingState == KinectInterop.TrackingState.Tracked &&
			bodyFrame.bodyData[i].joint[l].trackingState == KinectInterop.TrackingState.Tracked &&
			bodyFrame.bodyData[i].joint[a].trackingState == KinectInterop.TrackingState.Tracked &&
			bodyFrame.bodyData[i].joint[f].trackingState == KinectInterop.TrackingState.NotTracked)
		{
			KinectInterop.JointData jointData = bodyFrame.bodyData[i].joint[f];

			jointData.trackingState = bodyFrame.bodyData[i].joint[a].trackingState;
			jointData.orientation = Quaternion.identity;

			Vector3 posRight = bodyFrame.bodyData[i].joint[r].kinectPos;
			Vector3 posLeft = bodyFrame.bodyData[i].joint[l].kinectPos;
			Vector3 dirLeftRight = (posRight - posLeft).normalized;

			Vector3 posDown = bodyFrame.bodyData[i].joint[d].kinectPos;
			Vector3 posUp = bodyFrame.bodyData[i].joint[u].kinectPos;
			Vector3 dirDownUp = (posUp - posDown).normalized;

			Vector3 posAnkle = bodyFrame.bodyData[i].joint[a].kinectPos;
			Vector3 dirForward = Vector3.Cross(dirLeftRight, dirDownUp).normalized;

			jointData.kinectPos = posAnkle - dirForward * 0.1f;
			jointData.position = kinectToWorld.MultiplyPoint3x4(jointData.kinectPos);

			bodyFrame.bodyData[i].joint[f] = jointData;
		}

		// special case - foot right
		f = (int)KinectInterop.JointType.FootRight;
		a = (int)KinectInterop.JointType.AnkleRight;

		if(bodyFrame.bodyData[i].joint[r].trackingState == KinectInterop.TrackingState.Tracked &&
			bodyFrame.bodyData[i].joint[l].trackingState == KinectInterop.TrackingState.Tracked &&
			bodyFrame.bodyData[i].joint[a].trackingState == KinectInterop.TrackingState.Tracked &&
			bodyFrame.bodyData[i].joint[f].trackingState == KinectInterop.TrackingState.NotTracked)
		{
			KinectInterop.JointData jointData = bodyFrame.bodyData[i].joint[f];

			jointData.trackingState = bodyFrame.bodyData[i].joint[a].trackingState;
			jointData.orientation = Quaternion.identity;

			Vector3 posRight = bodyFrame.bodyData[i].joint[r].kinectPos;
			Vector3 posLeft = bodyFrame.bodyData[i].joint[l].kinectPos;
			Vector3 dirLeftRight = (posRight - posLeft).normalized;

			Vector3 posDown = bodyFrame.bodyData[i].joint[d].kinectPos;
			Vector3 posUp = bodyFrame.bodyData[i].joint[u].kinectPos;
			Vector3 dirDownUp = (posUp - posDown).normalized;

			Vector3 posAnkle = bodyFrame.bodyData[i].joint[a].kinectPos;
			Vector3 dirForward = Vector3.Cross(dirLeftRight, dirDownUp).normalized;

			jointData.kinectPos = posAnkle - dirForward * 0.1f;
			jointData.position = kinectToWorld.MultiplyPoint3x4(jointData.kinectPos);

			bodyFrame.bodyData[i].joint[f] = jointData;
		}
	}

	public bool PollColorFrame (KinectInterop.SensorData sensorData)
	{
		bool bNewFrame = false;
		if (bMultiSource && !bMultiFramesReady)
			return bNewFrame;

		// check if color camera is webcam
		if(bWebColorStream && colorWebcamData != null && colorWebcamTimestamp != lastColorFrameTimestamp)
		{
			lastColorFrameTimestamp = colorWebcamTimestamp;

			if(sensorData.colorImageTexture2D && sensorData.colorImageTexture2D.width == colorWebCam.width && sensorData.colorImageTexture2D.height == colorWebCam.height)
			{
				sensorData.colorImageTexture2D.SetPixels32(colorWebcamData);
			}

			sensorData.lastColorFrameTime = colorWebcamTimestamp;
			bNewFrame = true;
		}

		// poll for color frame
		else if(colorFrame != null && (long)colorFrame.Timestamp != lastColorFrameTimestamp) 
		{
			lastColorFrameTimestamp = (long)colorFrame.Timestamp;

			//sensorData.colorImage = colorFrame.Data;

			// copy color data h-flip, BGR to RGB
			int lenColorFrame = sensorData.colorImage.Length;
			int lenColorRow = sensorData.colorImageWidth * 3;

			if (dontHFlipFrame) 
			{
				// copy depth data, no flip
				for (int i = 0; i < lenColorFrame; i += lenColorRow) 
				{
					int iNext = i + lenColorRow; 
					for (int s = i, d = i; s < iNext; s += 3, d += 3) 
					{
						sensorData.colorImage[d] = colorFrame.Data[s + 2];
						sensorData.colorImage[d + 1] = colorFrame.Data[s + 1];
						sensorData.colorImage[d + 2] = colorFrame.Data[s];
					}
				}
			} 
			else 
			{
				// copy color data, h-flip
				for (int i = 0; i < lenColorFrame; i += lenColorRow) 
				{
					int iNext = i + lenColorRow; 
					for (int s = i, d = iNext - 3; s < iNext; s += 3, d -= 3) 
					{
						sensorData.colorImage[d] = colorFrame.Data[s + 2];
						sensorData.colorImage[d + 1] = colorFrame.Data[s + 1];
						sensorData.colorImage[d + 2] = colorFrame.Data[s];
					}
				}
			}

			sensorData.lastColorFrameTime = (long)colorFrame.Timestamp;
			bNewFrame = true;
		}

		if (bNewFrame && bMultiSource) 
		{
			bMultiFrameColor = false;
		}

		return bNewFrame;
	}

	public bool PollDepthFrame (KinectInterop.SensorData sensorData)
	{
		bool bNewFrame = false;
		if (bMultiSource && !bMultiFramesReady)
			return bNewFrame;

		// poll for depth frame
		if(depthFrame != null && (long)depthFrame.Timestamp != lastDepthFrameTimestamp) 
		{
			lastDepthFrameTimestamp = (long)depthFrame.Timestamp;

			if (dontHFlipFrame) 
			{
				// copy depth data, no flip
				int copyLength = sensorData.depthImage.Length * sizeof(ushort);
				Buffer.BlockCopy(depthFrame.Data, 0, sensorData.depthImage, 0, copyLength);
			} 
			else 
			{
				// copy depth data, h-flip
				int lenDepthFrame = sensorData.depthImage.Length;
				int lenDepthRow = sensorData.depthImageWidth;

				for (int i = 0; i < lenDepthFrame; i += lenDepthRow) 
				{
					int iNext = i + lenDepthRow;
					for (int s = i, d = iNext - 1; s < iNext; s++, d--) 
					{
						sensorData.depthImage[d] = depthFrame[s];
					}
				}
			}

			sensorData.lastDepthFrameTime = (long)depthFrame.Timestamp;
			bNewFrame = true;

			if (bMultiSource) 
			{
				bMultiFrameDepth = false;
			}
		}

		// poll for body-index frame
		if(userFrame != null && (long)userFrame.Timestamp != lastUserFrameTimestamp) 
		{
			lastUserFrameTimestamp = (long)userFrame.Timestamp;

			int lenBodyIndexFrame = sensorData.bodyIndexImage.Length;
			int lenBodyIndexRow = sensorData.depthImageWidth;

			if (dontHFlipFrame) 
			{
				// copy body-index data, no flip
				for (int i = 0; i < lenBodyIndexFrame; i += lenBodyIndexRow) 
				{
					int iNext = i + lenBodyIndexRow;
					for (int s = i, d = i; s < iNext; s++, d++) 
					{
						ushort u = userFrame[s];

						byte b = 255;
						if (u != 0 && bodyIdToIndex.ContainsKey(u)) 
							b = bodyIdToIndex[u];

						sensorData.bodyIndexImage[d] = b;
					}
				}
			} 
			else 
			{
				// copy body-index data, h-flip
				for (int i = 0; i < lenBodyIndexFrame; i += lenBodyIndexRow) 
				{
					int iNext = i + lenBodyIndexRow;
					for (int s = i, d = iNext - 1; s < iNext; s++, d--) 
					{
						ushort u = userFrame[s];

						byte b = 255;
						if (u != 0 && bodyIdToIndex.ContainsKey(u)) 
							b = bodyIdToIndex[u];

						sensorData.bodyIndexImage[d] = b;
					}
				}
			}

			sensorData.lastBodyIndexFrameTime = (long)userFrame.Timestamp;
			bNewFrame = true;

			if (bMultiSource) 
			{
				bMultiFrameBodyIndex = false;
			}

			// move floor estimation here
			if(sensorData.hintHeightAngle)
			{
				Vector3 floorNormal = new Vector3(userFrame.FloorNormal.X, userFrame.FloorNormal.Y, userFrame.FloorNormal.Z);

				if(floorNormal != Vector3.zero)
				{
					sensorData.sensorRotDetected = Quaternion.FromToRotation(floorNormal, Vector3.up);
					sensorData.sensorHgtDetected = Mathf.Abs(userFrame.Floor.Y / 1000f);
				}
			}
		}

		return bNewFrame;
	}

	public bool PollInfraredFrame (KinectInterop.SensorData sensorData)
	{
		return false;
	}

	public void FixJointOrientations (KinectInterop.SensorData sensorData, ref KinectInterop.BodyData bodyData)
	{
	}

//	public bool IsBodyTurned(ref KinectInterop.BodyData bodyData)
//	{
//		return false;
//	}

	public Vector2 MapSpacePointToDepthCoords (KinectInterop.SensorData sensorData, Vector3 spacePos)
	{
		Vector2 depthPos = coordMapper != null ? coordMapper.MapSpacePointToDepthCoords(spacePos) : Vector2.zero;
		return depthPos;
	}
	
	public Vector3 MapDepthPointToSpaceCoords (KinectInterop.SensorData sensorData, Vector2 depthPos, ushort depthVal)
	{
		Vector3 spacePos = coordMapper != null ? coordMapper.MapDepthPointToSpaceCoords(depthPos, depthVal) : Vector3.zero;
		return spacePos;
	}
	
	public bool MapDepthFrameToSpaceCoords (KinectInterop.SensorData sensorData, ref Vector3[] vSpaceCoords)
	{
		bool bSuccess = coordMapper != null ? coordMapper.MapDepthFrameToSpaceCoords(sensorData, ref vSpaceCoords) : false;
		return bSuccess;
	}
	
	public Vector2 MapDepthPointToColorCoords (KinectInterop.SensorData sensorData, Vector2 depthPos, ushort depthVal)
	{
		Vector2 colorPos = coordMapper != null ? coordMapper.MapDepthPointToColorCoords(depthPos, depthVal) : Vector2.zero;
		return colorPos;
	}

	public bool MapDepthFrameToColorCoords (KinectInterop.SensorData sensorData, ref Vector2[] vColorCoords)
	{
		bool bReadyToMap = /**bMultiSource ? (bMultiFrameColor && bMultiFrameDepth && bMultiFrameBody) : */
			sensorData.depthImage != null && sensorData.colorImage != null;

		if (bReadyToMap && coordMapper != null) 
		{
			coordMapper.MapDepthFrameToColorCoords(sensorData, bWebColorStream, ref vColorCoords);
		}

		return true;
	}

	public bool MapColorFrameToDepthCoords (KinectInterop.SensorData sensorData, ref Vector2[] vDepthCoords)
	{
		bool bReadyToMap = /**bMultiSource ? (bMultiFrameColor && bMultiFrameDepth && bMultiFrameBody) : */
			sensorData.depthImage != null && sensorData.colorImage != null;

		if(bReadyToMap && coordMapper != null) 
		{ 
			coordMapper.MapColorFrameToDepthCoords(sensorData, bWebColorStream, ref vDepthCoords); 
		}

		return true;
	}

	public int GetJointIndex (KinectInterop.JointType joint)
	{
		return (int)joint;
	}

//	// returns the joint at given index
//	public KinectInterop.JointType GetJointAtIndex(int index)
//	{
//		return (KinectInterop.JointType)(index);
//	}

	public KinectInterop.JointType GetParentJoint (KinectInterop.JointType joint)
	{
		switch(joint)
		{
		case KinectInterop.JointType.SpineBase:
			return KinectInterop.JointType.SpineBase;

		case KinectInterop.JointType.Neck:
			return KinectInterop.JointType.SpineShoulder;

		case KinectInterop.JointType.SpineShoulder:
			return KinectInterop.JointType.SpineMid;

		case KinectInterop.JointType.ShoulderLeft:
		case KinectInterop.JointType.ShoulderRight:
			return KinectInterop.JointType.SpineShoulder;

		case KinectInterop.JointType.HipLeft:
		case KinectInterop.JointType.HipRight:
			return KinectInterop.JointType.SpineBase;

		case KinectInterop.JointType.HandTipLeft:
			return KinectInterop.JointType.HandLeft;

		case KinectInterop.JointType.ThumbLeft:
			return KinectInterop.JointType.WristLeft;

		case KinectInterop.JointType.HandTipRight:
			return KinectInterop.JointType.HandRight;

		case KinectInterop.JointType.ThumbRight:
			return KinectInterop.JointType.WristRight;
		}

		return (KinectInterop.JointType)((int)joint - 1);
	}

	public KinectInterop.JointType GetNextJoint (KinectInterop.JointType joint)
	{
		switch(joint)
		{
		case KinectInterop.JointType.SpineBase:
			return KinectInterop.JointType.SpineMid;
		case KinectInterop.JointType.SpineMid:
			return KinectInterop.JointType.SpineShoulder;
		case KinectInterop.JointType.SpineShoulder:
			return KinectInterop.JointType.Neck;
		case KinectInterop.JointType.Neck:
			return KinectInterop.JointType.Head;

		case KinectInterop.JointType.ShoulderLeft:
			return KinectInterop.JointType.ElbowLeft;
		case KinectInterop.JointType.ElbowLeft:
			return KinectInterop.JointType.WristLeft;
		case KinectInterop.JointType.WristLeft:
			return KinectInterop.JointType.HandLeft;
		case KinectInterop.JointType.HandLeft:
			return KinectInterop.JointType.HandTipLeft;

		case KinectInterop.JointType.ShoulderRight:
			return KinectInterop.JointType.ElbowRight;
		case KinectInterop.JointType.ElbowRight:
			return KinectInterop.JointType.WristRight;
		case KinectInterop.JointType.WristRight:
			return KinectInterop.JointType.HandRight;
		case KinectInterop.JointType.HandRight:
			return KinectInterop.JointType.HandTipRight;

		case KinectInterop.JointType.HipLeft:
			return KinectInterop.JointType.KneeLeft;
		case KinectInterop.JointType.KneeLeft:
			return KinectInterop.JointType.AnkleLeft;
		case KinectInterop.JointType.AnkleLeft:
			return KinectInterop.JointType.FootLeft;

		case KinectInterop.JointType.HipRight:
			return KinectInterop.JointType.KneeRight;
		case KinectInterop.JointType.KneeRight:
			return KinectInterop.JointType.AnkleRight;
		case KinectInterop.JointType.AnkleRight:
			return KinectInterop.JointType.FootRight;
		}

		return joint;  // in case of end joint - Head, HandTipLeft, HandTipRight, FootLeft, FootRight
	}

	public bool IsFaceTrackingAvailable (ref bool bNeedRestart)
	{
		bNeedRestart = false;
		return false;
	}

	public bool InitFaceTracking (bool bUseFaceModel, bool bDrawFaceRect)
	{
		return false;
	}

	public void FinishFaceTracking ()
	{
	}

	public bool UpdateFaceTracking ()
	{
		return false;
	}

	public bool IsFaceTrackingActive ()
	{
		return false;
	}

	public bool IsDrawFaceRect ()
	{
		return false;
	}

	public bool IsFaceTracked (long userId)
	{
		return false;
	}

	public bool GetFaceRect (long userId, ref Rect faceRect)
	{
		return false;
	}

	public void VisualizeFaceTrackerOnColorTex (Texture2D texColor)
	{
	}

	public bool GetHeadPosition (long userId, ref Vector3 headPos)
	{
		return false;
	}

	public bool GetHeadRotation (long userId, ref Quaternion headRot)
	{
		return false;
	}

	public bool GetAnimUnits (long userId, ref System.Collections.Generic.Dictionary<KinectInterop.FaceShapeAnimations, float> afAU)
	{
		return false;
	}

	public bool GetShapeUnits (long userId, ref System.Collections.Generic.Dictionary<KinectInterop.FaceShapeDeformations, float> afSU)
	{
		return false;
	}

	public int GetFaceModelVerticesCount (long userId)
	{
		return 0;
	}

	public bool GetFaceModelVertices (long userId, ref Vector3[] avVertices)
	{
		return false;
	}

	public int GetFaceModelTrianglesCount ()
	{
		return 0;
	}

	public bool GetFaceModelTriangles (bool bMirrored, ref int[] avTriangles)
	{
		return false;
	}

	public bool IsSpeechRecognitionAvailable (ref bool bNeedRestart)
	{
		bNeedRestart = false;
		return false;
	}

	public int InitSpeechRecognition (string sRecoCriteria, bool bUseKinect, bool bAdaptationOff)
	{
		return 0;
	}

	public void FinishSpeechRecognition ()
	{
	}

	public int UpdateSpeechRecognition ()
	{
		return 0;
	}

	public int LoadSpeechGrammar (string sFileName, short iLangCode, bool bDynamic)
	{
		return 0;
	}

	public int AddGrammarPhrase(string sFromRule, string sToRule, string sPhrase, bool bClearRulePhrases, bool bCommitGrammar)
	{
		return 0;
	}
	
	public void SetSpeechConfidence (float fConfidence)
	{
	}

	public bool IsSpeechStarted ()
	{
		return false;
	}

	public bool IsSpeechEnded ()
	{
		return false;
	}

	public bool IsPhraseRecognized ()
	{
		return false;
	}

	public float GetPhraseConfidence()
	{
		return 0;
	}
	
	public string GetRecognizedPhraseTag ()
	{
		return string.Empty;
	}

	public void ClearRecognizedPhrase ()
	{
	}

	public bool IsBackgroundRemovalAvailable(ref bool bNeedRestart)
	{
		bBackgroundRemovalInited = KinectInterop.IsOpenCvAvailable(ref bNeedRestart);
		return bBackgroundRemovalInited;
	}
	
	public bool InitBackgroundRemoval(KinectInterop.SensorData sensorData, bool isHiResPrefered)
	{
		return KinectInterop.InitBackgroundRemoval(sensorData, isHiResPrefered);
	}
	
	public void FinishBackgroundRemoval(KinectInterop.SensorData sensorData)
	{
		KinectInterop.FinishBackgroundRemoval(sensorData);
		bBackgroundRemovalInited = false;
	}
	
	public bool UpdateBackgroundRemoval(KinectInterop.SensorData sensorData, bool isHiResPrefered, Color32 defaultColor, bool bAlphaTexOnly)
	{
		return KinectInterop.UpdateBackgroundRemoval(sensorData, isHiResPrefered, defaultColor, bAlphaTexOnly);
	}
	
	public bool IsBackgroundRemovalActive()
	{
		return bBackgroundRemovalInited;
	}
	
	public bool IsBRHiResSupported()
	{
		return false;
	}
	
	public Rect GetForegroundFrameRect(KinectInterop.SensorData sensorData, bool isHiResPrefered)
	{
		return KinectInterop.GetForegroundFrameRect(sensorData, isHiResPrefered);
	}
	
	public int GetForegroundFrameLength(KinectInterop.SensorData sensorData, bool isHiResPrefered)
	{
		return KinectInterop.GetForegroundFrameLength(sensorData, isHiResPrefered);
	}
	
	public bool PollForegroundFrame(KinectInterop.SensorData sensorData, bool isHiResPrefered, Color32 defaultColor, bool bLimitedUsers, ICollection<int> alTrackedIndexes, ref byte[] foregroundImage)
	{
		return KinectInterop.PollForegroundFrame(sensorData, isHiResPrefered, defaultColor, bLimitedUsers, alTrackedIndexes, ref foregroundImage);
	}
	
}
#endif
