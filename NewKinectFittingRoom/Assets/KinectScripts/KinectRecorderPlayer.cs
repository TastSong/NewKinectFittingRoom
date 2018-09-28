﻿using UnityEngine;
using System.Collections;
using System.IO;

public class KinectRecorderPlayer : MonoBehaviour 
{
	public string filePath = "BodyRecording.txt";

	public UnityEngine.UI.Text infoText;

	public bool playAtStart = false;


	private static KinectRecorderPlayer instance = null;
	
	private bool isRecording = false;
	private bool isPlaying = false;

	private KinectManager manager = null;

	private long liRelTime = 0;
	private float fStartTime = 0f;
	private float fCurrentTime = 0f;
	private int fCurrentFrame = 0;

	private StreamReader fileReader = null;
	private float fPlayTime = 0f;
	private string sPlayLine = string.Empty;

	public static KinectRecorderPlayer Instance
	{
		get
		{
			return instance;
		}
	}

	
//	public void RecordToggleValueChanged(bool bOn)
//	{
//		if(!isRecording)
//		{
//			StartRecording();
//		}
//		else
//		{
//			StopRecordingOrPlaying();
//		}
//	}
//
//	public void PlayToggleValueChanged(bool bOn)
//	{
//		if(!isPlaying)
//		{
//			StartPlaying();
//		}
//		else
//		{
//			StopRecordingOrPlaying();
//		}
//	}


	// starts recording
	public bool StartRecording()
	{
		if(isRecording)
			return false;

		isRecording = true;

		// avoid recording an playing at the same time
		if(isPlaying && isRecording)
		{
			CloseFile();
			isPlaying = false;
			
			Debug.Log("Playing stopped.");
		}
		
		// stop recording if there is no file name specified
		if(filePath.Length == 0)
		{
			isRecording = false;

			Debug.LogError("No file to save.");
			if(infoText != null)
			{
				infoText.text = "No file to save.";
			}
		}
		
		if(isRecording)
		{
			Debug.Log("Recording started.");
			if(infoText != null)
			{
				infoText.text = "Recording... Say 'Stop' to stop the recorder.";
			}
			
			// delete the old csv file
			if(filePath.Length > 0 && File.Exists(filePath))
			{
				File.Delete(filePath);
			}
			
			// initialize times
			fStartTime = fCurrentTime = Time.time;
			fCurrentFrame = 0;
		}

		return isRecording;
	}


	// starts playing
	public bool StartPlaying()
	{
		if(isPlaying)
			return false;

		isPlaying = true;

		// avoid recording an playing at the same time
		if(isRecording && isPlaying)
		{
			isRecording = false;
			Debug.Log("Recording stopped.");
		}
		
		// stop playing if there is no file name specified
		if(filePath.Length == 0 || !File.Exists(filePath))
		{
			isPlaying = false;
			Debug.LogError("No file to play.");

			if(infoText != null)
			{
				infoText.text = "No file to play.";
			}
		}
		
		if(isPlaying)
		{
			Debug.Log("Playing started.");
			if(infoText != null)
			{
				infoText.text = "Playing... Say 'Stop' to stop the player.";
			}

			// initialize times
			fStartTime = fCurrentTime = Time.time;
			fCurrentFrame = -1;

			// open the file and read a line
#if !UNITY_WSA
			fileReader = new StreamReader(filePath);
#endif
			ReadLineFromFile();
			
			// enable the play mode
			if(manager)
			{
				manager.EnablePlayMode(true);
			}
		}

		return isPlaying;
	}


	// stops recording or playing
	public void StopRecordingOrPlaying()
	{
		if(isRecording)
		{
			isRecording = false;

			Debug.Log("Recording stopped.");
			if(infoText != null)
			{
				infoText.text = "Recording stopped.";
			}
		}

		if(isPlaying)
		{
			// close the file, if it is playing
			CloseFile();
			isPlaying = false;

			Debug.Log("Playing stopped.");
			if(infoText != null)
			{
				infoText.text = "Playing stopped.";
			}
		}

		if(infoText != null)
		{
			infoText.text = "Say: 'Record' to start the recorder, or 'Play' to start the player.";
		}
	}

	// returns if file recording is in progress at the moment
	public bool IsRecording()
	{
		return isRecording;
	}

	// returns if file-play is in progress at the moment
	public bool IsPlaying()
	{
		return isPlaying;
	}
	

	// ----- end of public functions -----
	
	void Awake()
	{
		instance = this;
	}

	void Start()
	{
		if(infoText != null)
		{
			infoText.text = "Say: 'Record' to start the recorder, or 'Play' to start the player.";
		}

		if(!manager)
		{
			manager = KinectManager.Instance;
		}
		else
		{
			Debug.Log("KinectManager not found, probably not initialized.");

			if(infoText != null)
			{
				infoText.text = "KinectManager not found, probably not initialized.";
			}
		}
		
		if(playAtStart)
		{
			StartPlaying();
		}
	}

	void Update () 
	{
		if(isRecording)
		{
			// save the body frame, if any
			if(manager && manager.IsInitialized())
			{
				const char delimiter = ',';
				string sBodyFrame = manager.GetBodyFrameData(ref liRelTime, ref fCurrentTime, delimiter);

				if(sBodyFrame.Length > 0)
				{
#if !UNITY_WSA
					using(StreamWriter writer = File.AppendText(filePath))
					{
						string sRelTime = string.Format("{0:F3}", (fCurrentTime - fStartTime));
						writer.WriteLine(sRelTime + "|" + sBodyFrame);

						if(infoText != null)
						{
							infoText.text = string.Format("Recording @ {0}s., frame {1}. Say 'Stop' to stop the player.", sRelTime, fCurrentFrame);
						}

						fCurrentFrame++;
					}
#else
					string sRelTime = string.Format("{0:F3}", (fCurrentTime - fStartTime));
					Debug.Log(sRelTime + "|" + sBodyFrame);
#endif
				}
			}
		}

		if(isPlaying)
		{
			// wait for the right time
			fCurrentTime = Time.time;
			float fRelTime = fCurrentTime - fStartTime;

			if(sPlayLine != null && fRelTime >= fPlayTime)
			{
				// then play the line
				if(manager && sPlayLine.Length > 0)
				{
					manager.SetBodyFrameData(sPlayLine);
				}

				// and read the next line
				ReadLineFromFile();
			}

			if(sPlayLine == null)
			{
				// finish playing, if we reached the EOF
				StopRecordingOrPlaying();
			}
		}
	}

	void OnDestroy()
	{
		// don't forget to release the resources
		CloseFile();
		isRecording = isPlaying = false;
	}

	// reads a line from the file
	private bool ReadLineFromFile()
	{
		if(fileReader == null)
			return false;

		// read a line
		sPlayLine = fileReader.ReadLine();
		if(sPlayLine == null)
			return false;

		// extract the unity time and the body frame
		char[] delimiters = { '|' };
		string[] sLineParts = sPlayLine.Split(delimiters);

		if(sLineParts.Length >= 2)
		{
			float.TryParse(sLineParts[0], out fPlayTime);
			sPlayLine = sLineParts[1];
			fCurrentFrame++;

			if(infoText != null)
			{
				infoText.text = string.Format("Playing @ {0:F3}s., frame {1}. Say 'Stop' to stop the player.", fPlayTime, fCurrentFrame);
			}

			return true;
		}

		return false;
	}

	// close the file and disable the play mode
	private void CloseFile()
	{
		// close the file
		if(fileReader != null)
		{
			fileReader.Dispose();
			fileReader = null;
		}

		// disable the play mode
		if(manager)
		{
			manager.EnablePlayMode(false);
		}
	}

}
