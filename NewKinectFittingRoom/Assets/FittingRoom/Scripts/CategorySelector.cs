using UnityEngine;
using System.Collections;

public class CategorySelector : MonoBehaviour, KinectGestures.GestureListenerInterface, CloudFaceListenerInterface
{	
	public int playerIndex = 0;

	public bool swipeToChangeModel = true;

	public bool raiseHandToChangeCategory = true;

	public bool detectGenderAge = true;

	public UnityEngine.UI.Text infoText;

	private ModelSelector[] allModelSelectors;
	private int iCurSelector = -1;

	private ModelSelector modelSelector;

	private long lastUserId = 0;

	public ModelSelector GetActiveModelSelector()
	{
		return modelSelector;
	}

	public void ActivateNextModelSelector()
	{
		if (allModelSelectors.Length > 0) 
		{
			if (modelSelector)
				modelSelector.SetActiveSelector(false);

			iCurSelector++;
			if (iCurSelector >= allModelSelectors.Length)
				iCurSelector = 0;

			modelSelector = allModelSelectors [iCurSelector];
			modelSelector.SetActiveSelector(true);

			Debug.Log("Category: " + modelSelector.modelCategory);
		}
	}

	public void ActivatePrevModelSelector()
	{
		if (allModelSelectors.Length > 0) 
		{
			if (modelSelector)
				modelSelector.SetActiveSelector(false);

			iCurSelector--;
			if (iCurSelector < 0)
				iCurSelector = allModelSelectors.Length - 1;

			modelSelector = allModelSelectors [iCurSelector];
			modelSelector.SetActiveSelector(true);

			Debug.Log("Category: " + modelSelector.modelCategory);
		}
	}

	public void RefreshModelSelectorsList(UserGender gender, float age, bool bSelectFirst)
	{
		if (allModelSelectors != null && allModelSelectors.Length > 0) 
		{
			if (modelSelector)
				modelSelector.SetActiveSelector(false);
		}

		// find mono scripts containing model selectors
		//MonoBehaviour[] monoScripts = FindObjectsOfType(typeof(MonoBehaviour)) as MonoBehaviour[];
		ModelSelector[] monoScripts = GetComponents<ModelSelector>();

		int countEnabled = 0;
		if (monoScripts != null && monoScripts.Length > 0) 
		{
			//foreach(MonoBehaviour monoScript in monoScripts)
			foreach(ModelSelector monoScript in monoScripts)
			{
				//if((monoScript is ModelSelector) && monoScript.enabled)
				{
					ModelSelector modelSel = (ModelSelector)monoScript;

					bool genderMatch = gender == UserGender.Unisex || modelSel.modelGender == UserGender.Unisex || modelSel.modelGender == gender;
					bool ageMatch = age < 0 || (age >= modelSel.minimumAge && age <= modelSel.maximumAge);

					if (modelSel.playerIndex == playerIndex && genderMatch && ageMatch)
						countEnabled++;
				}
			}
		}

		allModelSelectors = new ModelSelector[countEnabled];

		if (countEnabled > 0) 
		{
			int j = 0;

			foreach(ModelSelector monoScript in monoScripts)
			{
				{
					ModelSelector modelSel = (ModelSelector)monoScript;

					bool genderMatch = gender == UserGender.Unisex || modelSel.modelGender == UserGender.Unisex || modelSel.modelGender == gender;
					bool ageMatch = age < 0 || (age >= modelSel.minimumAge && age <= modelSel.maximumAge);

					if (modelSel.playerIndex == playerIndex && genderMatch && ageMatch)
					{
						allModelSelectors[j] = modelSel;
						modelSel.SetActiveSelector(false);

						j++;
					}
				}
			}
		}

		if (allModelSelectors.Length > 0 && bSelectFirst) 
		{
			iCurSelector = 0;

			modelSelector = allModelSelectors[iCurSelector];
			modelSelector.SetActiveSelector(true);

			Debug.Log("Category: " + modelSelector.modelCategory);
		}

	}


	/////////////////////////////////////////////////////////////////////////////////


	void Start () 
	{
		if (CloudFaceDetector.Instance) 
		{
			CloudFaceDetector.Instance.gameObject.SetActive(detectGenderAge);
		}

		RefreshModelSelectorsList(UserGender.Unisex, -1f, !detectGenderAge);

		KinectManager manager = KinectManager.Instance;
		if (manager && manager.IsInitialized ()) 
		{
			if(infoText != null && manager.playerCalibrationPose == KinectGestures.Gestures.Tpose)
			{
				infoText.text = "Please stand in T-pose for calibration.";
			}
		} 
		else 
		{
			string sMessage = "KinectManager is missing or not initialized";
			Debug.LogError(sMessage);

			if(infoText != null)
			{
				infoText.text = sMessage;
			}
		}
	}


	void Update()
	{
		KinectManager manager = KinectManager.Instance;

		if(manager && manager.IsInitialized ()) 
		{
			long userId = manager.GetUserIdByIndex(playerIndex);

			if (userId != 0) 
			{
//				MonoBehaviour[] monoScripts = FindObjectsOfType(typeof(MonoBehaviour)) as MonoBehaviour[];
//				foreach(MonoBehaviour monoScript in monoScripts)
//				{
////					if(typeof(AvatarScaler).IsAssignableFrom(monoScript.GetType()) &&
////						monoScript.enabled)
//					if((monoScript is AvatarScaler) && monoScript.enabled)
//					{
//						AvatarScaler scaler = (AvatarScaler)monoScript;
//
//						if(scaler.scalerInited && scaler.playerIndex == playerIndex && 
//							scaler.currentUserId != userId)
//						{
//							scaler.currentUserId = userId;
//
//							if(userId != 0)
//							{
//								scaler.GetUserBodySize(true, true, true);
//
//								if(fixModelHipsAndShoulders)
//									scaler.FixJointsBeforeScale();
//								scaler.ScaleAvatar(0f);
//							}
//						}
//					}
//				}

				if (lastUserId != userId) 
				{
					if(infoText != null)
					{
						string sMessage = swipeToChangeModel && modelSelector ? "Swipe left or right to change clothing." : string.Empty;
						if (raiseHandToChangeCategory && allModelSelectors.Length > 1)
							sMessage += " Raise hand to change category.";
						
						infoText.text = sMessage;
					}

					lastUserId = userId;
				}
			}

			if(userId == 0 && userId != lastUserId)
			{
				lastUserId = userId;

				foreach (ModelSelector modSelector in allModelSelectors) 
				{
					modSelector.DestroySelectedModel();
				}

				if(infoText != null && manager.playerCalibrationPose == KinectGestures.Gestures.Tpose)
				{
					infoText.text = "Please stand in T-pose for calibration.";
				}
			}
		}
	}


	public void UserDetected(long userId, int userIndex)
	{
		KinectManager manager = KinectManager.Instance;
		if(!manager || (userIndex != playerIndex))
			return;

		if (raiseHandToChangeCategory) 
		{
			manager.DetectGesture(userId, KinectGestures.Gestures.RaiseRightHand);
			manager.DetectGesture(userId, KinectGestures.Gestures.RaiseLeftHand);
		}

		if (swipeToChangeModel) 
		{
			manager.DetectGesture(userId, KinectGestures.Gestures.SwipeLeft);
			manager.DetectGesture(userId, KinectGestures.Gestures.SwipeRight);
		}
	}

	public void UserLost(long userId, int userIndex)
	{
		if(userIndex != playerIndex)
			return;
	}

	public void GestureInProgress(long userId, int userIndex, KinectGestures.Gestures gesture, float progress, KinectInterop.JointType joint, Vector3 screenPos)
	{
		// nothing to do here
	}

	public bool GestureCompleted(long userId, int userIndex, KinectGestures.Gestures gesture, KinectInterop.JointType joint, Vector3 screenPos)
	{
		if(userIndex != playerIndex)
			return false;

		switch (gesture)
		{
		case KinectGestures.Gestures.RaiseRightHand:
			ActivateNextModelSelector();
			break;
		case KinectGestures.Gestures.RaiseLeftHand:
			ActivatePrevModelSelector();
			break;
		case KinectGestures.Gestures.SwipeLeft:
			if (modelSelector) 
			{
				modelSelector.SelectNextModel();
			}
			break;
		case KinectGestures.Gestures.SwipeRight:
			if (modelSelector) 
			{
				modelSelector.SelectPrevModel();
			}
			break;
		}

		return true;
	}

	public bool GestureCancelled(long userId, int userIndex, KinectGestures.Gestures gesture, KinectInterop.JointType joint)
	{
		if(userIndex != playerIndex)
			return false;

		return true;
	}

	public void UserFaceDetected(int userIndex, UserGender gender, float age, float smile)
	{
		if(userIndex != playerIndex)
			return;

		RefreshModelSelectorsList(gender, age, true);
	}

}
