using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class OrbbecAstraMapper //: MonoBehaviour
{
	//[Left Camera Intrinsic]
	private Vector3 lCamInt1 = new Vector3(507.808f, 0f, 356.237f);
	private Vector3 lCamInt2 = new Vector3(0f, 507.808f, 252.922f);
	private Vector3 lCamInt3 = new Vector3(0f, 0f, 1f);
	//[Right Camera Intrinsic]
	private Vector3 rCamInt1 = new Vector3(507.808f, 0f, 356.237f);
	private Vector3 rCamInt2 = new Vector3(0f, 507.808f, 252.922f);
	private Vector3 rCamInt3 = new Vector3(0f, 0f, 1f);
	//[Right to Left Camera Rotate Matrix]
	private Vector3 r2lRot1 = new Vector3(1f, 0f, 0f);
	private Vector3 r2lRot2 = new Vector3(0f, 1f, 0f);
	private Vector3 r2lRot3 = new Vector3(0f, 0f, 1f);
	//[Right to Left Camera Translate]
	private Vector3 r2lTrans = new Vector3(0f, 0f, 0f);

	private Matrix4x4 matCamLeft = Matrix4x4.identity;
	private Matrix4x4 matCamRight = Matrix4x4.identity;
	private Matrix4x4 matCamR2L = Matrix4x4.identity;
	private Matrix4x4 matCamD2C = Matrix4x4.identity;

	private Vector4 vMatAdjustX = Vector4.zero;
	private Vector4 vMatAdjustY = Vector4.zero;


//	private Matrix4x4 matTest = Matrix4x4.identity;

	// space mapping params
	private float _xzFactor;
	private float _yzFactor;
	private float _coeffX;
	private float _coeffY;
	private float _depthResX;
	private float _depthResY;
	private float _halfResX;
	private float _halfResY;

//	private Vector3[] depth2SpaceTable = null;
//	private ushort[] lastDepthDataBuf = null;

	private ComputeShader _spaceMapperShader = null;
	private int _depth2spaceKernel = -1;
	private long lastSpaceCoordsTime = 0;

	private ComputeBuffer _spaceUnitTableBuf = null;
	private ComputeBuffer _spaceDepthValuesBuf = null;
	private ComputeBuffer _spaceDepthCoordsBuf = null;

	private ComputeShader _coordMapperShader = null;
	private int _depth2colorKernel = -1;
	//private int _color2depthKernel = -1;

	private ComputeBuffer _depthPlaneCoordsBuf = null;
	private ComputeBuffer _depthDepthValuesBuf = null;
	private ComputeBuffer _colorPlaneCoordsBuf = null;

	// whether the depth2color registration is active
	private bool bRegCalibrationData = false;
	private int d2cTableW = 0, d2cTableH = 0, d2cTableLen = 0;
	private Vector2[] depth2ColorTable = null;

	// depth float values
	private float[] mapperDepthValues = null;

	// singleton mapper instance
	private static OrbbecAstraMapper instance;

	// d2c matrix row0 & row1 (for internal use)
	private Vector4 vD2CRow0, vD2CRow1;



	public static OrbbecAstraMapper Instance
	{
		get
		{
			return instance;
		}
	}


	public Vector4 GetMatRowX()
	{
		return vD2CRow0;
	}


	public Vector4 GetMatAdjX()
	{
		return vMatAdjustX;
	}


	public void SetMatAdjX(Vector4 vAdjust)
	{
		//float fValue = (fVal01 - 0.5f) * 40000f;
		//vMatAdjustX = new Vector4(0f, 0f, 0f, fValue);
		vMatAdjustX = vAdjust;
		Vector4 vD2CRow = vD2CRow0 + vMatAdjustX;

		matCamD2C.SetRow(0, vD2CRow);
		Debug.Log(string.Format("d2cMat0: {0}, {1}, {2}, {3}", matCamD2C.m00, matCamD2C.m01, matCamD2C.m02, matCamD2C.m03));

		if (_coordMapperShader) 
		{
			_coordMapperShader.SetVector("d2cMat0", new Vector4(matCamD2C.m00, matCamD2C.m01, matCamD2C.m02, matCamD2C.m03));
			//Debug.Log("SetShader0: " + new Vector4 (matCamD2C.m00, matCamD2C.m01, matCamD2C.m02, matCamD2C.m03));
		}

		Debug.Log(string.Format("vMatAdjustX: {0}, {1}, {2}, {3}", vMatAdjustX.x, vMatAdjustX.y, vMatAdjustX.z, vMatAdjustX.w));
		Debug.Log(string.Format("vMatAdjustY: {0}, {1}, {2}, {3}", vMatAdjustY.x, vMatAdjustY.y, vMatAdjustY.z, vMatAdjustY.w));
	}


	public Vector4 GetMatRowY()
	{
		return vD2CRow1;
	}


	public Vector4 GetMatAdjY()
	{
		return vMatAdjustY;
	}


	public void SetMatAdjY(Vector4 vAdjust)
	{
		//float fValue = (fVal01 - 0.5f) * 40000f;
		//vMatAdjustY = new Vector4(0f, 0f, 0f, fValue);
		vMatAdjustY = vAdjust;
		Vector4 vD2CRow = vD2CRow1 + vMatAdjustY;

		matCamD2C.SetRow(1, vD2CRow);
		Debug.Log(string.Format("d2cMat1: {0}, {1}, {2}, {3}", matCamD2C.m10, matCamD2C.m11, matCamD2C.m12, matCamD2C.m13));

		if (_coordMapperShader) 
		{
			_coordMapperShader.SetVector("d2cMat1", new Vector4(matCamD2C.m10, matCamD2C.m11, matCamD2C.m12, matCamD2C.m13));
			//Debug.Log("SetShader1: " + new Vector4 (matCamD2C.m10, matCamD2C.m11, matCamD2C.m12, matCamD2C.m13));
		}

		Debug.Log(string.Format("vMatAdjustX: {0}, {1}, {2}, {3}", vMatAdjustX.x, vMatAdjustX.y, vMatAdjustX.z, vMatAdjustX.w));
		Debug.Log(string.Format("vMatAdjustY: {0}, {1}, {2}, {3}", vMatAdjustY.x, vMatAdjustY.y, vMatAdjustY.z, vMatAdjustY.w));
	}


	public void SetupSpaceMapping(int depthResX, int depthResY, float depthHfov, float depthVfov)
	{
		this._xzFactor = Mathf.Tan(depthHfov / 2f) * 2f;
		this._yzFactor = Mathf.Tan(depthVfov / 2f) * 2f;
		this._coeffX = (float)depthResX / _xzFactor;
		this._coeffY = (float)depthResY / _yzFactor;

		this._depthResX = (float)depthResX;
		this._depthResY = (float)depthResY;
		this._halfResX = (float)depthResX / 2f;
		this._halfResY = (float)depthResY / 2f;
	}


	// sets up depth2color calibration data, if registration is available
	public void SetupRegCalibrationData(bool isRegCalibData, int depthResX, int depthResY)
	{
		bRegCalibrationData = isRegCalibData;

		d2cTableW = depthResX;
		d2cTableH = depthResY;
		d2cTableLen = depthResX * depthResY;

		depth2ColorTable = new Vector2[d2cTableLen];

		for (int y = 0, i = 0; y < depthResY; y++) 
		{
			for (int x = 0; x < depthResX; x++) 
			{
				depth2ColorTable[i] = new Vector2(x, y);
				i++;
			}
		}
	}


	// sets up depth2color calibration data for Astra & Astra-Pro
	public void SetupCalibrationData(bool bAstraPro)
	{
		instance = this;

		if (!bAstraPro) 
		{
			// Astra
			this.lCamInt1 = new Vector3(288.126f, 0f, 156.578f);
			this.lCamInt2 = new Vector3(0f, 288.780f, 124.968f);
			this.lCamInt3 = new Vector3(0f, 0f, 1f);

			this.rCamInt1 = new Vector3(256.204f, 0f, 163.978f);
			this.rCamInt2 = new Vector3(0f, 256.450f, 118.382f);
			this.rCamInt3 = new Vector3(0f, 0f, 1f);

			this.r2lRot1 = new Vector3(0.999983f, 0.00264383f, -0.0052673f);
			this.r2lRot2 = new Vector3(-0.00264698f, 0.999996f, -0.000589696f);
			this.r2lRot3 = new Vector3(0.00526572f, 0.000603628f, 0.999986f);
			this.r2lTrans = new Vector3(-24.2641f, -0.439535f, -0.577864f);

			this.vMatAdjustX = new Vector4(0f, 0f, 32.53856f, 0f);
			this.vMatAdjustY = new Vector4(0f, 0f, 5.3296f, 0f);
//			this.vMatAdjustX = new Vector4(0.05353134f, 0f, 0f, 2400f);
//			this.vMatAdjustY = new Vector4(0f, 0f, 0f, 9000f);
		}
		else
		{
			// Astra Pro
			this.lCamInt1 = new Vector3(574.679f, 0f, 318.731f);
			this.lCamInt2 = new Vector3(0f, 536.747f, 244.835f);
			this.lCamInt3 = new Vector3(0f, 0f, 1f);

			this.rCamInt1 = new Vector3(590.402f, 0f, 324.859f);
			this.rCamInt2 = new Vector3(0f, 551.584f, 231.235f);
			this.rCamInt3 = new Vector3(0f, 0f, 1f);

			this.r2lRot1 = new Vector3(0.999874f, 0.0158587f, -0.000310587f);
			this.r2lRot2 = new Vector3(-0.015858f, 0.999872f, 0.00199448f);
			this.r2lRot3 = new Vector3(0.000342177f, -0.00198931f, 0.999998f);
			this.r2lTrans = new Vector3(-24.9939f, -0.087019f, -0.091892f);

			this.vMatAdjustX = new Vector4(0f, 0f, 0f, 19370.21f); //3.
			this.vMatAdjustY = new Vector4(0f, 0f, 0f, 0f);
//			this.vMatAdjustX = new Vector4(0f, 0f, 0f, 9880f);
//			this.vMatAdjustY = new Vector4(0f, 0f, 0f, 20000f);
		}

		SetupMatricesRow();
	}


	public void SetupCalibrationData(Vector3 lCamInt1, Vector3 lCamInt2, Vector3 lCamInt3, 
									 Vector3 rCamInt1, Vector3 rCamInt2, Vector3 rCamInt3,
									 Vector3 r2lRot1, Vector3 r2lRot2, Vector3 r2lRot3, Vector3 r2lTrans,
									 Vector4 vAdjustX, Vector4 vAdjustY)
	{
		this.lCamInt1 = lCamInt1;
		this.lCamInt2 = lCamInt2;
		this.lCamInt3 = lCamInt3;

		this.rCamInt1 = rCamInt1;
		this.rCamInt2 = rCamInt2;
		this.rCamInt3 = rCamInt3;

		this.r2lRot1 = r2lRot1;
		this.r2lRot2 = r2lRot2;
		this.r2lRot3 = r2lRot3;
		this.r2lTrans = r2lTrans;

		this.vMatAdjustX = vAdjustX;
		this.vMatAdjustY = vAdjustY;

		SetupMatricesRow();
	}


	public void CleanUp()
	{
		// depth2color
		if (_depthPlaneCoordsBuf != null)
		{
			_depthPlaneCoordsBuf.Release();
			_depthPlaneCoordsBuf = null;
		}

		if (_depthDepthValuesBuf != null)
		{
			_depthDepthValuesBuf.Release();
			_depthDepthValuesBuf = null;
		}

		if (_colorPlaneCoordsBuf != null)
		{
			_colorPlaneCoordsBuf.Release();
			_colorPlaneCoordsBuf = null;
		}

		_coordMapperShader = null;

		// depth2space
		if (_spaceUnitTableBuf != null)
		{
			_spaceUnitTableBuf.Release();
			_spaceUnitTableBuf = null;
		}

		if (_spaceDepthValuesBuf != null)
		{
			_spaceDepthValuesBuf.Release();
			_spaceDepthValuesBuf = null;
		}

		if (_spaceDepthCoordsBuf != null)
		{
			_spaceDepthCoordsBuf.Release();
			_spaceDepthCoordsBuf = null;
		}

		_spaceMapperShader = null;
	}


	// sets up the matrices from calibration data
	private void SetupMatricesRow()
	{
		Vector4 column0001 = new Vector4(0, 0, 0, 1);

		matCamLeft.SetRow(0, (Vector4)lCamInt1);
		matCamLeft.SetRow(1, (Vector4)lCamInt2);
		matCamLeft.SetRow(2, (Vector4)lCamInt3);
		matCamLeft.SetRow(3, column0001);
		//PrintMatrix("matCamLeft", matCamLeft);

		matCamRight.SetRow(0, (Vector4)rCamInt1);
		matCamRight.SetRow(1, (Vector4)rCamInt2);
		matCamRight.SetRow(2, (Vector4)rCamInt3);
		matCamRight.SetRow(3, column0001);
		//PrintMatrix("matCamRight", matCamRight);

		matCamR2L.SetRow(0, (Vector4)r2lRot1);
		matCamR2L.SetRow(1, (Vector4)r2lRot2);
		matCamR2L.SetRow(2, (Vector4)r2lRot3);
		matCamR2L.SetColumn(3, (Vector4)r2lTrans);
		matCamR2L[3, 3] = 1;
		//PrintMatrix("matCamR2L", matCamR2L);

		matCamD2C = matCamRight * matCamR2L * matCamLeft.inverse;
		//PrintMatrix("matCamD2C", matCamD2C);

		vD2CRow0 = matCamD2C.GetRow(0);
		vD2CRow1 = matCamD2C.GetRow(1);

//		vD2CRow0 += vMatAdjustX;
//		vD2CRow1 += vMatAdjustY;

		matCamD2C.SetRow(0, vD2CRow0 + vMatAdjustX);
		matCamD2C.SetRow(1, vD2CRow1 + vMatAdjustY);

//		Debug.Log(string.Format("d2cMatX: {0:F2}, {1:F2}, {2:F2}, {3:F2}", matCamD2C.m00, matCamD2C.m01, matCamD2C.m02, matCamD2C.m03));
//		Debug.Log(string.Format("d2cMatY: {0:F2}, {1:F2}, {2:F2}, {3:F2}", matCamD2C.m10, matCamD2C.m11, matCamD2C.m12, matCamD2C.m13));
	}


//	// prints the given matrix
//	private void PrintMatrix(string matTitle, Matrix4x4 mat)
//	{
//		Debug.Log(matTitle + ":\n" + mat.ToString());
//	}

	public Vector3 MapDepthPointToWorldSpace(Vector3 depthPoint)
	{
		float centerOfsX = depthPoint.x / _depthResX - 0.5f;
		float centerOfsY = 0.5f - depthPoint.y / _depthResY;

		return new Vector3(centerOfsX * depthPoint.z * _xzFactor, centerOfsY * depthPoint.z * _yzFactor, depthPoint.z);
	}

	public Vector3 MapWorldPointToDepthSpace(Vector3 worldPoint)
	{
		return new Vector3(_coeffX * worldPoint.x / worldPoint.z + _halfResX, _coeffY * worldPoint.y / worldPoint.z + _halfResY, worldPoint.z);
	}


	public Vector2 MapSpacePointToDepthCoords(Vector3 spacePos)
	{
//		Vector2 depthPos = Vector3.zero;
//
//		float depthX = 0f, depthY = 0f, depthZ = 0f;
//		int hr = ConvertWorldToDepth(spacePos.x * 1000f, spacePos.y * 1000f, spacePos.z * 1000f, out depthX, out depthY, out depthZ);
//
//		if(hr == 0)
//		{
//			depthPos = new Vector2(depthX, depthY);
//		}

		Vector2 depthPos = new Vector2(_coeffX * spacePos.x / spacePos.z + _halfResX, _depthResY - (_coeffY * spacePos.y / spacePos.z + _halfResY));

		return depthPos;
	}


	public Vector3 MapDepthPointToSpaceCoords (Vector2 depthPos, ushort depthVal)
	{
//		Vector3 spacePos = Vector3.zero;
//
//		float spaceX = 0f, spaceY = 0f, spaceZ = 0f;
//		int hr = ConvertDepthToWorld(depthPos.x, depthPos.y, depthVal, out spaceX, out spaceY, out spaceZ);
//
//		if(hr == 0)
//		{
//			spacePos = new Vector3(spaceX / 1000f, spaceY / 1000f, spaceZ / 1000f);
//		}

		float centerOfsX = depthPos.x / _depthResX - 0.5f;
		float centerOfsY = 0.5f - depthPos.y / _depthResY;
		float depthPosZ = (float)depthVal / 1000f;

		Vector3 spacePos = new Vector3(centerOfsX * _xzFactor * depthPosZ, centerOfsY * _yzFactor * depthPosZ, depthPosZ);

		return spacePos;
	}


	public bool MapDepthFrameToSpaceCoords (KinectInterop.SensorData sensorData, ref Vector3[] vSpaceCoords)
	{
		if (sensorData.depthImage != null  && lastSpaceCoordsTime != sensorData.lastDepth2SpaceCoordsTime)
		{
			lastSpaceCoordsTime = sensorData.lastDepth2SpaceCoordsTime;

			if (_spaceMapperShader == null)
			{
				CreateSpaceMapperShader(sensorData);
			}

			if (_spaceMapperShader)
			{
				//long timeStamp = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

				int depthImageLength = sensorData.depthImageWidth * sensorData.depthImageHeight;
				if (mapperDepthValues == null) 
				{
					mapperDepthValues = new float[depthImageLength];
				}

				for (int di = 0; di < depthImageLength; di++)
				{
					mapperDepthValues[di] = (float)sensorData.depthImage[di];
				}

				_spaceDepthValuesBuf.SetData(mapperDepthValues);
				_spaceMapperShader.Dispatch(_depth2spaceKernel, depthImageLength / 64, 1, 1);

				if (vSpaceCoords == null || vSpaceCoords.Length != depthImageLength)
				{
					vSpaceCoords = new Vector3[depthImageLength];
				}

				_spaceDepthCoordsBuf.GetData(vSpaceCoords);

				//long timeDuration = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond - timeStamp);
				//Debug.Log("depth2spaceTask() took " + timeDuration + " ms");
			}

//			int depthImageLength = sensorData.depthImageWidth * sensorData.depthImageHeight;
//
//			if (depth2SpaceTable == null || depth2SpaceTable.Length != depthImageLength)
//			{
//				depth2SpaceTable = new Vector3[depthImageLength];
//
//				for (int dy = 0, di = 0; dy < sensorData.depthImageHeight; dy++)
//				{
//					for (int dx = 0; dx < sensorData.depthImageWidth; dx++)
//					{
//						Vector2 depthPos = new Vector2(dx, dy);
//						depth2SpaceTable[di] = MapDepthPointToSpaceCoords(depthPos, 1000);
//						di++;
//					}
//				}
//
//				if (lastDepthDataBuf == null)
//				{
//					lastDepthDataBuf = new ushort[sensorData.depthImage.Length];
//				}
//			}
//
//			for (int dy = 0, di = 0; dy < sensorData.depthImageHeight; dy++)
//			{
//				for (int dx = 0; dx < sensorData.depthImageWidth; dx++)
//				{
//					if (di >= 0 && di < lastDepthDataBuf.Length &&
//						sensorData.depthImage[di] != lastDepthDataBuf[di])
//					{
//						lastDepthDataBuf[di] = sensorData.depthImage[di];
//
//						if (sensorData.depthImage[di] != 0)
//						{
//							float depthVal = (float)sensorData.depthImage[di] / 1000f;
//							vSpaceCoords[di] = depth2SpaceTable[di] * depthVal;
//						}
//						else
//						{
//							vSpaceCoords[di] = Vector3.zero;
//						}
//					}
//
//					di++;
//				}
//			}

		}

		return true;
	}


	// maps depth to color coordinates
	public Vector2 MapDepthPointToColorCoords(Vector2 depthPos, ushort depthVal)
	{
		if (bRegCalibrationData) 
		{
			return new Vector2(depthPos.x, depthPos.y);
		}

		if (depthVal == 0) 
		{
			return Vector2.zero;
		}

		float z = (float)depthVal;
		float clrX = matCamD2C.m00 * depthPos.x + matCamD2C.m01 * depthPos.y + matCamD2C.m02 + matCamD2C.m03 / z;
		float clrY = matCamD2C.m10 * depthPos.x + matCamD2C.m11 * depthPos.y + matCamD2C.m12 + matCamD2C.m13 / z;

		return new Vector2(clrX, clrY);
	}


	// maps depth frame to color coordinates
	public bool MapDepthFrameToColorCoords (KinectInterop.SensorData sensorData, bool bAstraPro, ref Vector2[] vColorCoords)
	{
		bool bReadyToMap = sensorData.depthImage != null && sensorData.colorImage != null;

		if (bReadyToMap && bRegCalibrationData) 
		{
			if (vColorCoords == null || vColorCoords.Length != d2cTableLen)
			{
				vColorCoords = new Vector2[d2cTableLen];
			}

			int i0 = 0, i1 = d2cTableLen / 2, i2 = d2cTableLen - 1;

			if (depth2ColorTable[i0] != vColorCoords[i0] || depth2ColorTable[i1] != vColorCoords[i1] || depth2ColorTable[i2] != vColorCoords[i2]) 
			{
				System.Array.Copy(depth2ColorTable, vColorCoords, d2cTableLen);
			}

			return true;
		}

		if (bReadyToMap)
		{
			if (_coordMapperShader == null || _colorPlaneCoordsBuf == null)
			{
				CreateCoordMapperShader(sensorData, bAstraPro, false);
			}

			if (_coordMapperShader)
			{
				//long timeStamp = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

				int depthImageLength = sensorData.depthImageWidth * sensorData.depthImageHeight;
				if (mapperDepthValues == null) 
				{
					mapperDepthValues = new float[depthImageLength];
				}

				for (int di = 0; di < depthImageLength; di++)
				{
					mapperDepthValues[di] = (float)sensorData.depthImage[di];
				}

				_depthDepthValuesBuf.SetData(mapperDepthValues);
				_coordMapperShader.Dispatch(_depth2colorKernel, depthImageLength / 64, 1, 1);

				if (vColorCoords == null || vColorCoords.Length != depthImageLength)
				{
					vColorCoords = new Vector2[depthImageLength];
				}

				_colorPlaneCoordsBuf.GetData(vColorCoords);

				//long timeDuration = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond - timeStamp);
				//Debug.Log("depth2colorTask() took " + timeDuration + " ms");
			}
		}

		return bReadyToMap;
	}


	// maps depth frame to color coordinates
	public bool MapColorFrameToDepthCoords (KinectInterop.SensorData sensorData, bool bAstraPro, ref Vector2[] vDepthCoords)
	{
		bool bReadyToMap = sensorData.depthImage != null && sensorData.colorImage != null;

		if (bReadyToMap && bRegCalibrationData) 
		{
			if (vDepthCoords == null || vDepthCoords.Length != d2cTableLen)
			{
				vDepthCoords = new Vector2[d2cTableLen];
			}

			int i0 = 0, i1 = d2cTableLen / 2, i2 = d2cTableLen - 1;

			if (depth2ColorTable[i0] != vDepthCoords[i0] || depth2ColorTable[i1] != vDepthCoords[i1] || depth2ColorTable[i2] != vDepthCoords[i2]) 
			{
				System.Array.Copy(depth2ColorTable, vDepthCoords, d2cTableLen);
			}

			return true;
		}

		if (bReadyToMap)
		{
			if (_coordMapperShader == null || _colorPlaneCoordsBuf == null)
			{
				CreateCoordMapperShader(sensorData, bAstraPro, true);
			}

			if (_coordMapperShader)
			{
				//long timeStamp = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

				int depthImageLength = sensorData.depthImageWidth * sensorData.depthImageHeight;
				int colorImageLength = sensorData.colorImageWidth * sensorData.colorImageHeight;

				float[] depthDepthValues = new float[depthImageLength];
				for (int di = 0; di < depthImageLength; di++)
				{
					depthDepthValues[di] = (float)sensorData.depthImage[di];
				}

				_depthDepthValuesBuf.SetData(depthDepthValues);
				_coordMapperShader.Dispatch(_depth2colorKernel, depthImageLength / 64, 1, 1);

				if (vDepthCoords == null || vDepthCoords.Length != colorImageLength)
				{
					vDepthCoords = new Vector2[colorImageLength];
				}

				_colorPlaneCoordsBuf.GetData(vDepthCoords);

				//long timeDuration = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond - timeStamp);
				//Debug.Log("depth2colorTask() took " + timeDuration + " ms");
			}
		}

		return bReadyToMap;
	}


	// creates the shader for depth2color coordinate mapping
	private bool CreateCoordMapperShader(KinectInterop.SensorData sensorData, bool bAstraPro, bool bColor2Depth)
	{
		if (_coordMapperShader == null)
		{
			_coordMapperShader = Resources.Load("AstraCoordMapper") as ComputeShader;
		}

		if (_coordMapperShader)
		{
			//bAstraPro = false;  // don't use NN
			_depth2colorKernel = _coordMapperShader.FindKernel("MapDepth2ColorPP");
			//_color2depthKernel = !bAstraPro ? _coordMapperShader.FindKernel("MapColor2DepthPP") : _coordMapperShader.FindKernel("MapColor2DepthNN");

//			float[] space2spaceMat = new float[] {
//				matCamD2C.m00, matCamD2C.m01, matCamD2C.m02, matCamD2C.m03,
//				matCamD2C.m10, matCamD2C.m11, matCamD2C.m12, matCamD2C.m13
//			};

			int depthImageLength = sensorData.depthImageWidth * sensorData.depthImageHeight;
			int colorImageLength = sensorData.colorImageWidth * sensorData.colorImageHeight;

			_coordMapperShader.SetFloat("depthResX", (float)sensorData.depthImageWidth);
			_coordMapperShader.SetFloat("depthResY", (float)sensorData.depthImageHeight);
			_coordMapperShader.SetInt("depthImageLen", depthImageLength);

			_coordMapperShader.SetFloat("colorResX", (float)sensorData.colorImageWidth);
			_coordMapperShader.SetFloat("colorResY", (float)sensorData.colorImageHeight);

			//if (!bColor2Depth)
			{
				_coordMapperShader.SetVector("d2cMat0", new Vector4(matCamD2C.m00, matCamD2C.m01, matCamD2C.m02, matCamD2C.m03));
				_coordMapperShader.SetVector("d2cMat1", new Vector4(matCamD2C.m10, matCamD2C.m11, matCamD2C.m12, matCamD2C.m13));

//				Debug.Log("Shader d2cMat0: " + new Vector4 (matCamD2C.m00, matCamD2C.m01, matCamD2C.m02, matCamD2C.m03));
//				Debug.Log("Shader d2cMat1: " + new Vector4 (matCamD2C.m10, matCamD2C.m11, matCamD2C.m12, matCamD2C.m13));
			}
//			else
//			{
//				_coordMapperShader.SetFloats("color2depthMat", space2spaceMat);
//			}

			// compute buffers
			if(_depthDepthValuesBuf == null)
			{
				_depthDepthValuesBuf = new ComputeBuffer(depthImageLength, sizeof(float));
				_coordMapperShader.SetBuffer(_depth2colorKernel, "depthDepthValues", _depthDepthValuesBuf);
			}

			//if (!bColor2Depth)
			{
				_depthPlaneCoordsBuf = new ComputeBuffer(depthImageLength, 2 * sizeof(float));
				_colorPlaneCoordsBuf = new ComputeBuffer(!bColor2Depth ? depthImageLength : colorImageLength, 2 * sizeof(float));

				// set plane coords
				Vector2[] depthPlaneCoords = new Vector2[depthImageLength];
				for (int dy = 0, di = 0; dy < sensorData.depthImageHeight; dy++)
				{
					for (int dx = 0; dx < sensorData.depthImageWidth; dx++)
					{
						depthPlaneCoords[di] = new Vector2(dx, dy);
						di++;
					}
				}

				_depthPlaneCoordsBuf.SetData(depthPlaneCoords);
				_coordMapperShader.SetBuffer(_depth2colorKernel, "depthPlaneCoords", _depthPlaneCoordsBuf);
				_coordMapperShader.SetBuffer(_depth2colorKernel, "colorPlaneCoords", _colorPlaneCoordsBuf);
			}
//			else
//			{
//				int colorImageLength = sensorData.colorImageWidth * sensorData.colorImageHeight;
//
//				_colorSpaceCoordsBuf = new ComputeBuffer(colorImageLength, 3 * sizeof(float));
//				_colorDepthCoordsBuf = new ComputeBuffer(colorImageLength, 2 * sizeof(float));
//
//				_coordMapperShader.SetBuffer(_color2depthKernel, "colorSpaceCoords", _colorSpaceCoordsBuf);
//				_coordMapperShader.SetBuffer(_color2depthKernel, "colorDepthCoords", _colorDepthCoordsBuf);
//			}
		}

		return (_coordMapperShader != null);
	}


	// creates the shader for depth2space coordinate mapping
	private bool CreateSpaceMapperShader(KinectInterop.SensorData sensorData)
	{
		if (_spaceMapperShader == null)
		{
			_spaceMapperShader = Resources.Load("AstraSpaceMapper") as ComputeShader;
		}

		if (_spaceMapperShader)
		{
			_depth2spaceKernel = _spaceMapperShader.FindKernel("MapDepth2Space");

			int depthImageLength = sensorData.depthImageWidth * sensorData.depthImageHeight;

			_spaceMapperShader.SetFloat("depthResX", (float)sensorData.depthImageWidth);
			_spaceMapperShader.SetFloat("depthResY", (float)sensorData.depthImageHeight);
			_spaceMapperShader.SetInt("depthImageLen", depthImageLength);

			// compute buffers
			if(_spaceDepthValuesBuf == null)
			{
				_spaceDepthValuesBuf = new ComputeBuffer(depthImageLength, sizeof(float));
				_spaceMapperShader.SetBuffer(_depth2spaceKernel, "spaceDepthValues", _spaceDepthValuesBuf);
			}

			_spaceUnitTableBuf = new ComputeBuffer(depthImageLength, 3 * sizeof(float));
			_spaceDepthCoordsBuf = new ComputeBuffer(depthImageLength, 3 * sizeof(float));

			// set space unit coords
			Vector3[] depth2SpaceTable = new Vector3[depthImageLength];
			for (int dy = 0, di = 0; dy < sensorData.depthImageHeight; dy++)
			{
				for (int dx = 0; dx < sensorData.depthImageWidth; dx++)
				{
					Vector2 depthPos = new Vector2(dx, dy);
					depth2SpaceTable[di] = MapDepthPointToSpaceCoords(depthPos, 1000);
					di++;
				}
			}

			_spaceUnitTableBuf.SetData(depth2SpaceTable);
			_spaceMapperShader.SetBuffer(_depth2spaceKernel, "spaceUnitTable", _spaceUnitTableBuf);
			_spaceMapperShader.SetBuffer(_depth2spaceKernel, "spaceDepthCoords", _spaceDepthCoordsBuf);
		}

		return (_spaceMapperShader != null);
	}


}
