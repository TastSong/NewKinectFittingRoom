using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using System.IO;


public class ModelSelector : MonoBehaviour 
{
	public int playerIndex = 0;

	public string modelCategory = "Clothing";

	public int numberOfModels = 3;

	public RectTransform dressingMenu;

	public GameObject dressingItemPrefab;

	public Camera modelRelativeToCamera = null;

	public Camera foregroundCamera;

	public bool keepSelectedModel = true;

	public bool continuousScaling = true;

	[Range(0.0f, 2.0f)]
	public float bodyScaleFactor = 1.0f;

	[Range(0.0f, 2.0f)]
	public float bodyWidthFactor = 1.0f;

	[Range(0.0f, 2.0f)]
	public float armScaleFactor = 1.0f;

	[Range(0.0f, 2.0f)]
	public float legScaleFactor = 1.0f;

	[Range(-0.5f, 0.5f)]
	public float verticalOffset = 0f;

	[Range(-0.5f, 0.5f)]
	public float forwardOffset = 0f;

	private bool applyMuscleLimits = false;

	public UserGender modelGender = UserGender.Unisex;

	public float minimumAge = 0;

	public float maximumAge = 1000;


	[HideInInspector]
	public bool activeSelector = false;

	private Text dressingMenuTitle;

	private RectTransform dressingMenuContent;

	private List<GameObject> dressingPanels = new List<GameObject>();

	private string[] modelNames;
	private Texture2D[] modelThumbs;

	private Vector2 scroll;
	private int selected = -1;
	private int prevSelected = -1;

	private GameObject selModel;

	private float curScaleFactor = 0f;
	private float curModelOffset = 0f;

	public void SetActiveSelector(bool bActive)
	{
		activeSelector = bActive;

		if (dressingMenu) 
		{
			dressingMenu.gameObject.SetActive(activeSelector);

			if (activeSelector) 
			{
				UpdateDressingMenu();
			}
		}

		if (!activeSelector && !keepSelectedModel) 
		{
			DestroySelectedModel();
		}
	}

	public GameObject GetSelectedModel()
	{
		return selModel;
	}

	public void DestroySelectedModel()
	{
		if (selModel) 
		{
			AvatarController ac = selModel.GetComponent<AvatarController>();
			KinectManager km = KinectManager.Instance;

			if (ac != null && km != null) 
			{
				km.avatarControllers.Remove(ac);
			}

			GameObject.Destroy(selModel);
			selModel = null;
			prevSelected = -1;
		}
	}

	public void SelectNextModel()
	{
		selected++;
		if (selected >= numberOfModels) 
			selected = 0;

		OnDressingItemSelected(selected);
	}

	public void SelectPrevModel()
	{
		selected--;
		if (selected < 0) 
			selected = numberOfModels - 1;

		OnDressingItemSelected(selected);
	}

	public void UpdateDressingMenu()
	{
		if (!dressingMenuContent && dressingMenu) 
		{
			Transform dressingHeaderText = dressingMenu.transform.Find("Header/Text");
			if (dressingHeaderText) 
			{
				dressingMenuTitle = dressingHeaderText.gameObject.GetComponent<Text>();
			}

			Transform dressingViewportContent = dressingMenu.transform.Find("Scroll View/Viewport/Content");
			if (dressingViewportContent) 
			{
				dressingMenuContent = dressingViewportContent.gameObject.GetComponent<RectTransform>();
			}
		}

		modelNames = new string[numberOfModels];
		modelThumbs = new Texture2D[numberOfModels];
		dressingPanels.Clear();

		dressingMenuContent.transform.DetachChildren();

		for (int i = 0; i < numberOfModels; i++)
		{
			modelNames[i] = string.Format("{0:0000}", i);

			string previewPath = modelCategory + "/" + modelNames[i] + "/preview.jpg";
			TextAsset resPreview = Resources.Load(previewPath, typeof(TextAsset)) as TextAsset;

			if (resPreview == null) 
			{
				resPreview = Resources.Load("nopreview.jpg", typeof(TextAsset)) as TextAsset;
			}

			{
				modelThumbs[i] = CreatePreviewTexture(resPreview != null ? resPreview.bytes : null);
			}

			InstantiateDressingItem(i);
		}

		if (numberOfModels > 0) 
		{
			selected = 0;
		}

		if (dressingMenuTitle) 
		{
			dressingMenuTitle.text = modelCategory;
		}
	}


	void Start()
	{
		curScaleFactor = bodyScaleFactor + bodyWidthFactor + armScaleFactor + legScaleFactor;
		curModelOffset = verticalOffset + forwardOffset + (applyMuscleLimits ? 1f : 0f);
	}

	void Update()
	{
		if (activeSelector && selected >= 0 && selected < modelNames.Length && prevSelected != selected)
		{
			KinectManager kinectManager = KinectManager.Instance;

			if (kinectManager && kinectManager.IsInitialized () && kinectManager.IsUserDetected(playerIndex)) 
			{
				OnDressingItemSelected(selected);
			}
		}

		if (selModel != null) 
		{
			float curMuscleLimits = applyMuscleLimits ? 1f : 0f;
			if (Mathf.Abs(curModelOffset - (verticalOffset + forwardOffset + curMuscleLimits)) >= 0.001f) 
			{
				curModelOffset = verticalOffset + forwardOffset + curMuscleLimits;

				AvatarController ac = selModel.GetComponent<AvatarController>();
				if (ac != null) 
				{
					ac.verticalOffset = verticalOffset;
					ac.forwardOffset = forwardOffset;
					ac.applyMuscleLimits = applyMuscleLimits;
				}
			}

			if (Mathf.Abs(curScaleFactor - (bodyScaleFactor + bodyWidthFactor + armScaleFactor + legScaleFactor)) >= 0.001f) 
			{
				curScaleFactor = bodyScaleFactor + bodyWidthFactor + armScaleFactor + legScaleFactor;

				AvatarScaler scaler = selModel.GetComponent<AvatarScaler>();
				if (scaler != null) 
				{
					scaler.continuousScaling = continuousScaling;
					scaler.bodyScaleFactor = bodyScaleFactor;
					scaler.bodyWidthFactor = bodyWidthFactor;
					scaler.armScaleFactor = armScaleFactor;
					scaler.legScaleFactor = legScaleFactor;
				}
			}
		}
	}

	private Texture2D CreatePreviewTexture(byte[] btImage)
	{
		Texture2D tex = new Texture2D(4, 4);
		//Texture2D tex = new Texture2D(100, 143);

		if (btImage != null) 
		{
			tex.LoadImage (btImage);
		}
		
		return tex;
	}

	private void InstantiateDressingItem(int i)
	{
		if (!dressingItemPrefab && i >= 0 && i < numberOfModels)
			return;
		if (!dressingMenuContent)
			return;

		GameObject dressingItemInstance = Instantiate<GameObject>(dressingItemPrefab);

		GameObject dressingImageObj = dressingItemInstance.transform.Find("DressingImagePanel").gameObject;
		dressingImageObj.GetComponentInChildren<RawImage>().texture = modelThumbs[i];

		if(!string.IsNullOrEmpty(modelNames[i])) 
		{
			EventTrigger trigger = dressingItemInstance.GetComponent<EventTrigger>();
			EventTrigger.Entry entry = new EventTrigger.Entry();

			entry.eventID = EventTriggerType.Select;
			entry.callback.AddListener ((eventData) => { OnDressingItemSelected(i); });

			trigger.triggers.Add(entry);
		}

		//if (dressingMenuContent) 
		{
			dressingItemInstance.transform.SetParent(dressingMenuContent, false);
		}

		dressingPanels.Add(dressingItemInstance);
	}

	private void OnDressingItemSelected(int i)
	{
		if (i >= 0 && i < modelNames.Length && prevSelected != i)
		{
			prevSelected = selected = i;
			LoadDressingModel(modelNames[selected]);
		}
	}

	private void LoadDressingModel(string modelDir)
	{
		string modelPath = modelCategory + "/" + modelDir + "/model";
		UnityEngine.Object modelPrefab = Resources.Load(modelPath, typeof(GameObject));
		if(modelPrefab == null)
			return;

		Debug.Log("Model: " + modelPath);

		if(selModel != null) 
		{
			GameObject.Destroy(selModel);
		}

		selModel = (GameObject)GameObject.Instantiate(modelPrefab, Vector3.zero, Quaternion.Euler(0, 180f, 0));
		selModel.name = "Model" + modelDir;

		AvatarController ac = selModel.GetComponent<AvatarController>();
		if (ac == null) 
		{
			ac = selModel.AddComponent<AvatarController>();
			ac.playerIndex = playerIndex;

			ac.mirroredMovement = true;
			ac.verticalMovement = true;
			ac.applyMuscleLimits = applyMuscleLimits;

			ac.verticalOffset = verticalOffset;
			ac.forwardOffset = forwardOffset;
			ac.smoothFactor = 0f;
		}

		ac.posRelativeToCamera = modelRelativeToCamera;
		ac.posRelOverlayColor = (foregroundCamera != null);

		KinectManager km = KinectManager.Instance;
		//ac.Awake();

		if(km && km.IsInitialized()) 
		{
			long userId = km.GetUserIdByIndex(playerIndex);
			if(userId != 0)
			{
				ac.SuccessfulCalibration(userId, false);
			}

			MonoBehaviour[] monoScripts = FindObjectsOfType(typeof(MonoBehaviour)) as MonoBehaviour[];
			km.avatarControllers.Clear();

			foreach(MonoBehaviour monoScript in monoScripts)
			{
				if((monoScript is AvatarController) && monoScript.enabled)
				{
					AvatarController avatar = (AvatarController)monoScript;
					km.avatarControllers.Add(avatar);
				}
			}
		}

		AvatarScaler scaler = selModel.GetComponent<AvatarScaler>();
		if (scaler == null) 
		{
			scaler = selModel.AddComponent<AvatarScaler>();
			scaler.playerIndex = playerIndex;
			scaler.mirroredAvatar = true;

			scaler.continuousScaling = continuousScaling;
			scaler.bodyScaleFactor = bodyScaleFactor;
			scaler.bodyWidthFactor = bodyWidthFactor;
			scaler.armScaleFactor = armScaleFactor;
			scaler.legScaleFactor = legScaleFactor;
		}

		scaler.foregroundCamera = foregroundCamera;
		//scaler.debugText = debugText;

		//scaler.Start();
	}

}
