using UnityEngine;
using System.Collections;

public class LocateAvatarsAndGestureListeners : MonoBehaviour 
{

	void Start () 
	{
		KinectManager manager = KinectManager.Instance;
		
		if(manager)
		{
			manager.avatarControllers.Clear();
			manager.ClearKinectUsers();

			// get the mono scripts. avatar controllers and gesture listeners are among them
			MonoBehaviour[] monoScripts = FindObjectsOfType(typeof(MonoBehaviour)) as MonoBehaviour[];
			
			foreach(MonoBehaviour monoScript in monoScripts)
			{
//				if(typeof(AvatarController).IsAssignableFrom(monoScript.GetType()) &&
//				   monoScript.enabled)
				if((monoScript is AvatarController) && monoScript.enabled)
				{
					AvatarController avatar = (AvatarController)monoScript;
					manager.avatarControllers.Add(avatar);
				}
			}

			manager.gestureManager = null;
			foreach(MonoBehaviour monoScript in monoScripts)
			{
//				if(typeof(KinectGestures).IsAssignableFrom(monoScript.GetType()) && 
//				   monoScript.enabled)
				if((monoScript is KinectGestures) && monoScript.enabled)
				{
					manager.gestureManager = (KinectGestures)monoScript;
					break;
				}
			}

			// locate the available gesture listeners
			manager.gestureListeners.Clear();

			foreach(MonoBehaviour monoScript in monoScripts)
			{
//				if(typeof(KinectGestures.GestureListenerInterface).IsAssignableFrom(monoScript.GetType()) &&
//				   monoScript.enabled)
				if((monoScript is KinectGestures.GestureListenerInterface) && monoScript.enabled)
				{
					//KinectGestures.GestureListenerInterface gl = (KinectGestures.GestureListenerInterface)monoScript;
					manager.gestureListeners.Add(monoScript);
				}
			}

			// check for gesture manager
			if (manager.gestureListeners.Count > 0 && manager.gestureManager == null) 
			{
				Debug.Log("Found " + manager.gestureListeners.Count + " gesture listener(s), but no gesture manager in the scene. Adding KinectGestures-component...");
				manager.gestureManager = manager.gameObject.AddComponent<KinectGestures>();
			}

		}
	}
	
}
