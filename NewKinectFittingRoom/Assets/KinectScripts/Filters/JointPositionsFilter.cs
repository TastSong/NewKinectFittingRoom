using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;
//using Windows.Kinect;

public class JointPositionsFilter
{
    private FilterDoubleExponentialData[,] history;

	private KinectInterop.SmoothParameters smoothParameters;

    private bool init;
	
	
    public JointPositionsFilter()
    {
        this.init = false;
    }

    public void Init()
    {
        // Specify some defaults
        //this.Init(0.25f, 0.25f, 0.25f, 0.03f, 0.05f);
		this.Init(0.5f, 0.5f, 0.5f, 0.05f, 0.04f);
    }

    /// <summary>
    /// Initialize the filter with a set of manually specified TransformSmoothParameters.
    /// </summary>
    /// <param name="smoothingValue">Smoothing = [0..1], lower values is closer to the raw data and more noisy.</param>
    /// <param name="correctionValue">Correction = [0..1], higher values correct faster and feel more responsive.</param>
    /// <param name="predictionValue">Prediction = [0..n], how many frames into the future we want to predict.</param>
    /// <param name="jitterRadiusValue">JitterRadius = The deviation distance in m that defines jitter.</param>
    /// <param name="maxDeviationRadiusValue">MaxDeviation = The maximum distance in m that filtered positions are allowed to deviate from raw data.</param>
    public void Init(float smoothingValue, float correctionValue, float predictionValue, float jitterRadiusValue, float maxDeviationRadiusValue)
    {
		this.smoothParameters = new KinectInterop.SmoothParameters();

        this.smoothParameters.smoothing = smoothingValue;                   // How much soothing will occur.  Will lag when too high
        this.smoothParameters.correction = correctionValue;                 // How much to correct back from prediction.  Can make things springy
        this.smoothParameters.prediction = predictionValue;                 // Amount of prediction into the future to use. Can over shoot when too high
        this.smoothParameters.jitterRadius = jitterRadiusValue;             // Size of the radius where jitter is removed. Can do too much smoothing when too high
        this.smoothParameters.maxDeviationRadius = maxDeviationRadiusValue; // Size of the max prediction radius Can snap back to noisy data when too high
        
		// Check for divide by zero. Use an epsilon of a 10th of a millimeter
		this.smoothParameters.jitterRadius = Math.Max(0.0001f, this.smoothParameters.jitterRadius);
		
		this.Reset();
        this.init = true;
    }

	public void Init(KinectInterop.SmoothParameters smoothParameters)
    {
		this.smoothParameters = smoothParameters;
		
        this.Reset();
        this.init = true;
    }

    public void Reset()
    {
		KinectManager manager = KinectManager.Instance;
		this.history = new FilterDoubleExponentialData[manager.GetBodyCount(), manager.GetJointCount()];
    }

    public void UpdateFilter(ref KinectInterop.BodyFrameData bodyFrame)
    {
        if (this.init == false)
        {
            this.Init();    // initialize with default parameters                
        }

		KinectInterop.SmoothParameters tempSmoothingParams = new KinectInterop.SmoothParameters();

		tempSmoothingParams.smoothing = this.smoothParameters.smoothing;
		tempSmoothingParams.correction = this.smoothParameters.correction;
		tempSmoothingParams.prediction = this.smoothParameters.prediction;

		KinectManager manager = KinectManager.Instance;
		int bodyCount = manager.GetBodyCount();

		for(int bodyIndex = 0; bodyIndex < bodyCount; bodyIndex++)
		{
			if(bodyFrame.bodyData[bodyIndex].bIsTracked != 0)
			{
				FilterBodyJoints(ref bodyFrame.bodyData[bodyIndex], bodyIndex, ref tempSmoothingParams);
			}
		}
	}

	private void FilterBodyJoints(ref KinectInterop.BodyData bodyData, int bodyIndex, ref KinectInterop.SmoothParameters tempSmoothingParams)
	{
		KinectManager manager = KinectManager.Instance;
		int jointsCount = manager.GetJointCount();

		for(int jointIndex = 0; jointIndex < jointsCount; jointIndex++)
		{
			if (bodyData.joint[jointIndex].trackingState != KinectInterop.TrackingState.Tracked ||
			    jointIndex == (int)KinectInterop.JointType.FootLeft || jointIndex == (int)KinectInterop.JointType.FootRight ||
			    jointIndex == (int)KinectInterop.JointType.HandTipLeft || jointIndex == (int)KinectInterop.JointType.HandTipRight ||
			    jointIndex == (int)KinectInterop.JointType.ThumbLeft || jointIndex == (int)KinectInterop.JointType.ThumbRight ||
			    jointIndex == (int)KinectInterop.JointType.Head)
			{
				tempSmoothingParams.jitterRadius = this.smoothParameters.jitterRadius * 2.0f;
				tempSmoothingParams.maxDeviationRadius = smoothParameters.maxDeviationRadius * 2.0f;
			}
			else
			{
				tempSmoothingParams.jitterRadius = smoothParameters.jitterRadius;
				tempSmoothingParams.maxDeviationRadius = smoothParameters.maxDeviationRadius;
			}

			bodyData.joint[jointIndex].position = FilterJoint(bodyData.joint[jointIndex].position, bodyIndex, jointIndex, ref tempSmoothingParams);
		}
		
		for(int jointIndex = 0; jointIndex < jointsCount; jointIndex++)
		{
			if(jointIndex == 0)
			{
				bodyData.position = bodyData.joint[jointIndex].position;
				bodyData.orientation = bodyData.joint[jointIndex].orientation;

				bodyData.joint[jointIndex].direction = Vector3.zero;
			}
			else
			{
				int jParent = (int)manager.GetParentJoint((KinectInterop.JointType)jointIndex);
				
				if(bodyData.joint[jointIndex].trackingState != KinectInterop.TrackingState.NotTracked && 
				   bodyData.joint[jParent].trackingState != KinectInterop.TrackingState.NotTracked)
				{
					bodyData.joint[jointIndex].direction = 
						bodyData.joint[jointIndex].position - bodyData.joint[jParent].position;
				}
			}
		}
	}

    private Vector3 FilterJoint(Vector3 rawPosition, int bodyIndex, int jointIndex, ref KinectInterop.SmoothParameters smoothingParameters)
    {
        Vector3 filteredPosition;
        Vector3 diffVec;
        Vector3 trend;
        float diffVal;

		Vector3 prevFilteredPosition = history[bodyIndex, jointIndex].filteredPosition;
		Vector3 prevTrend = this.history[bodyIndex, jointIndex].trend;
		Vector3 prevRawPosition = this.history[bodyIndex, jointIndex].rawPosition;
        bool jointIsValid = (rawPosition != Vector3.zero);

        if (!jointIsValid)
        {
			history[bodyIndex, jointIndex].frameCount = 0;
        }

		if (this.history[bodyIndex, jointIndex].frameCount == 0)
        {
            filteredPosition = rawPosition;
            trend = Vector3.zero;
        }
		else if (this.history[bodyIndex, jointIndex].frameCount == 1)
        {
            filteredPosition = (rawPosition + prevRawPosition) * 0.5f;
			diffVec = filteredPosition - prevFilteredPosition;
			trend = (diffVec * smoothingParameters.correction) + (prevTrend * (1.0f - smoothingParameters.correction));
        }
        else
        {              
            diffVec = rawPosition - prevFilteredPosition;
            diffVal = Math.Abs(diffVec.magnitude);

            if (diffVal <= smoothingParameters.jitterRadius)
            {
                filteredPosition = (rawPosition * (diffVal / smoothingParameters.jitterRadius)) + (prevFilteredPosition * (1.0f - (diffVal / smoothingParameters.jitterRadius)));
            }
            else
            {
                filteredPosition = rawPosition;
            }

            filteredPosition = (filteredPosition * (1.0f - smoothingParameters.smoothing)) + ((prevFilteredPosition + prevTrend) * smoothingParameters.smoothing);

            diffVec = filteredPosition - prevFilteredPosition;
            trend = (diffVec * smoothingParameters.correction) + (prevTrend * (1.0f - smoothingParameters.correction));
        }      

        Vector3 predictedPosition = filteredPosition + (trend * smoothingParameters.prediction);

        diffVec = predictedPosition - rawPosition;
        diffVal = Mathf.Abs(diffVec.magnitude);

        if (diffVal > smoothingParameters.maxDeviationRadius)
        {
            predictedPosition = (predictedPosition * (smoothingParameters.maxDeviationRadius / diffVal)) + (rawPosition * (1.0f - (smoothingParameters.maxDeviationRadius / diffVal)));
        }

		history[bodyIndex, jointIndex].rawPosition = rawPosition;
		history[bodyIndex, jointIndex].filteredPosition = filteredPosition;
		history[bodyIndex, jointIndex].trend = trend;
		history[bodyIndex, jointIndex].frameCount++;
        
		return predictedPosition;
    }
	

    private struct FilterDoubleExponentialData
    {  
        public Vector3 rawPosition;
  
        public Vector3 filteredPosition;

        public Vector3 trend;

        public uint frameCount;
    }
}
