using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public interface DepthSensorInterface
{
	KinectInterop.DepthSensorPlatform GetSensorPlatform();

	bool InitSensorInterface(bool bCopyLibs, ref bool bNeedRestart);

	void FreeSensorInterface(bool bDeleteLibs);

	bool IsSensorAvailable();
	
	int GetSensorsCount();

	KinectInterop.SensorData OpenDefaultSensor(KinectInterop.FrameSource dwFlags, float sensorAngle, bool bUseMultiSource);

	void CloseSensor(KinectInterop.SensorData sensorData);

	bool UpdateSensorData(KinectInterop.SensorData sensorData);

	bool GetMultiSourceFrame(KinectInterop.SensorData sensorData);

	void FreeMultiSourceFrame(KinectInterop.SensorData sensorData);

	bool PollBodyFrame(KinectInterop.SensorData sensorData, ref KinectInterop.BodyFrameData bodyFrame, ref Matrix4x4 kinectToWorld, bool bIgnoreJointZ);

	bool PollColorFrame(KinectInterop.SensorData sensorData);

	bool PollDepthFrame(KinectInterop.SensorData sensorData);

	bool PollInfraredFrame(KinectInterop.SensorData sensorData);

	void FixJointOrientations(KinectInterop.SensorData sensorData, ref KinectInterop.BodyData bodyData);


	Vector2 MapSpacePointToDepthCoords(KinectInterop.SensorData sensorData, Vector3 spacePos);

	Vector3 MapDepthPointToSpaceCoords(KinectInterop.SensorData sensorData, Vector2 depthPos, ushort depthVal);

	bool MapDepthFrameToSpaceCoords (KinectInterop.SensorData sensorData, ref Vector3[] vSpaceCoords);

	Vector2 MapDepthPointToColorCoords(KinectInterop.SensorData sensorData, Vector2 depthPos, ushort depthVal);

	bool MapDepthFrameToColorCoords(KinectInterop.SensorData sensorData, ref Vector2[] vColorCoords);

	bool MapColorFrameToDepthCoords (KinectInterop.SensorData sensorData, ref Vector2[] vDepthCoords);

	int GetJointIndex(KinectInterop.JointType joint);
	
	KinectInterop.JointType GetParentJoint(KinectInterop.JointType joint);
	
	KinectInterop.JointType GetNextJoint(KinectInterop.JointType joint);

	bool IsFaceTrackingAvailable(ref bool bNeedRestart);

	bool InitFaceTracking(bool bUseFaceModel, bool bDrawFaceRect);

	void FinishFaceTracking();

	bool UpdateFaceTracking();

	bool IsFaceTrackingActive();

	bool IsDrawFaceRect();

	bool IsFaceTracked(long userId);

	bool GetFaceRect(long userId, ref Rect faceRect);

	void VisualizeFaceTrackerOnColorTex(Texture2D texColor);

	bool GetHeadPosition(long userId, ref Vector3 headPos);
	
	bool GetHeadRotation(long userId, ref Quaternion headRot);

	bool GetAnimUnits(long userId, ref Dictionary<KinectInterop.FaceShapeAnimations, float> afAU);

	bool GetShapeUnits(long userId, ref Dictionary<KinectInterop.FaceShapeDeformations, float> afSU);

	int GetFaceModelVerticesCount(long userId);

	bool GetFaceModelVertices(long userId, ref Vector3[] avVertices);
	
	int GetFaceModelTrianglesCount();
	
	bool GetFaceModelTriangles(bool bMirrored, ref int[] avTriangles);

	bool IsSpeechRecognitionAvailable(ref bool bNeedRestart);

	int InitSpeechRecognition(string sRecoCriteria, bool bUseKinect, bool bAdaptationOff);

	void FinishSpeechRecognition();
	
	int UpdateSpeechRecognition();

	int LoadSpeechGrammar(string sFileName, short iLangCode, bool bDynamic);

	// adds a phrase to the from-rule in dynamic grammar. if the to-rule is empty, this means end of the phrase recognition
	int AddGrammarPhrase(string sFromRule, string sToRule, string sPhrase, bool bClearRulePhrases, bool bCommitGrammar);

	void SetSpeechConfidence(float fConfidence);

	bool IsSpeechStarted();

	bool IsSpeechEnded();

	bool IsPhraseRecognized();

	float GetPhraseConfidence();

	// returns the tag of the recognized grammar phrase, empty string if no phrase is recognized at the moment
	string GetRecognizedPhraseTag();

	// clears the currently recognized grammar phrase (prepares SR system for next phrase recognition)
	void ClearRecognizedPhrase();

	bool IsBackgroundRemovalAvailable(ref bool bNeedRestart);
	
	bool InitBackgroundRemoval(KinectInterop.SensorData sensorData, bool isHiResPrefered);
	
	void FinishBackgroundRemoval(KinectInterop.SensorData sensorData);
	
	bool UpdateBackgroundRemoval(KinectInterop.SensorData sensorData, bool isHiResPrefered, Color32 defaultColor, bool bAlphaTexOnly);
	
	bool IsBackgroundRemovalActive();

	bool IsBRHiResSupported();

	Rect GetForegroundFrameRect(KinectInterop.SensorData sensorData, bool isHiResPrefered);
	
	int GetForegroundFrameLength(KinectInterop.SensorData sensorData, bool isHiResPrefered);
	
	bool PollForegroundFrame(KinectInterop.SensorData sensorData, bool isHiResPrefered, Color32 defaultColor, bool bLimitedUsers, ICollection<int> alTrackedIndexes, ref byte[] foregroundImage);
	
}
