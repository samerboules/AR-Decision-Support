using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vuforia;

public class CustomTrackableEventHandler : MainManager, ITrackableEventHandler
{
    public GameObject ImageTargetStatusText;
    #region PROTECTED_MEMBER_VARIABLES

    protected TrackableBehaviour mTrackableBehaviour;
    protected TrackableBehaviour.Status m_PreviousStatus;
    protected TrackableBehaviour.Status m_NewStatus;

    #endregion // PROTECTED_MEMBER_VARIABLES

    #region UNITY_MONOBEHAVIOUR_METHODS

    protected virtual void Start()
    {
        mTrackableBehaviour = GetComponent<TrackableBehaviour>();
        if (mTrackableBehaviour)
            mTrackableBehaviour.RegisterTrackableEventHandler(this);
    }

    protected virtual void OnDestroy()
    {
        if (mTrackableBehaviour)
            mTrackableBehaviour.UnregisterTrackableEventHandler(this);
    }

    #endregion // UNITY_MONOBEHAVIOUR_METHODS

    #region PUBLIC_METHODS

    /// <summary>
    ///     Implementation of the ITrackableEventHandler function called when the
    ///     tracking state changes.
    /// </summary>
    public void OnTrackableStateChanged(
        TrackableBehaviour.Status previousStatus,
        TrackableBehaviour.Status newStatus)
    {
        m_PreviousStatus = previousStatus;
        m_NewStatus = newStatus;

        Debug.Log("Trackable " + mTrackableBehaviour.TrackableName +
                  " " + mTrackableBehaviour.CurrentStatus +
                  " -- " + mTrackableBehaviour.CurrentStatusInfo);

        if (newStatus == TrackableBehaviour.Status.DETECTED ||
            newStatus == TrackableBehaviour.Status.TRACKED ||
            newStatus == TrackableBehaviour.Status.EXTENDED_TRACKED)
        {
            OnTrackingFound();
        }
        else if (previousStatus == TrackableBehaviour.Status.TRACKED &&
                 newStatus == TrackableBehaviour.Status.NO_POSE)
        {
            OnTrackingLost();
        }
        else
        {
            // For combo of previousStatus=UNKNOWN + newStatus=UNKNOWN|NOT_FOUND
            // Vuforia is starting, but tracking has not been lost or found yet
            // Call OnTrackingLost() to hide the augmentations
            OnTrackingLost();
        }
    }

    #endregion // PUBLIC_METHODS

    #region PROTECTED_METHODS

    protected virtual void OnTrackingFound()
    {
        //Display Canvas Children
        GameObject MainCanvas = GameObject.Find("Canvas");
        foreach (Transform  child in MainCanvas.transform)
        {
            if(child.gameObject.name == "MoveObjectButtons")
                child.gameObject.SetActive(true);
            else
                child.gameObject.SetActive(true);
        }

        //Hide the ImageTarget Status text
        ImageTargetStatusText.SetActive(false);

        if (mTrackableBehaviour)
        {
            if (PlayerPrefs.HasKey("NumberOfObjects"))
            {
                //TODO: ReDo
                int NumberOfObjects = PlayerPrefs.GetInt("NumberOfObjects");
                GameObject MyGameObject;
                for (int counter = 1; counter <= NumberOfObjects; counter++)
                {
                    if (GameObject.Find(counter.ToString()) == null)
                    {
                        //Check if the key exists
                        if (PlayerPrefs.HasKey(counter.ToString()))
                        {
                            //Create a dictionary
                            Dictionary<string, float> dict_ = new Dictionary<string, float>();
                            //Hold the string version of the counter
                            string counterString_ = counter.ToString();
                            //Hold the  "key" name of the string, which is equal to the counter in every iteration
                            string stringHolder = PlayerPrefs.GetString(counterString_);
                            //Convert the string content to the dictionary 
                            dict_ = ConvertStringToDict(stringHolder);
                            //instantiate prefab instance 
                            int TypeToCreate = (int)dict_["Type"];
                            MyGameObject = Instantiate(ARObjectsArray[TypeToCreate]);
                            //Assign its name
                            MyGameObject.name = counterString_;

                            //Assign its type
                            DistinctiveObjectData distinctiveObjectData = MyGameObject.GetComponent<DistinctiveObjectData>();
                            distinctiveObjectData.type = TypeToCreate;

                            //set the image target as parent of prefab instance
                            //MyGameObject.transform.parent = this.transform;
                            //Set the local location values of the instance using the information being held in the dictionaries
                            MyGameObject.transform.localPosition = new Vector3(dict_["PosX"], dict_["PosY"], dict_["PosZ"]);

                            //set every prefab instance in layer 9 to make sure that they are the only collidable objects in the scene when raycasting 
                            MyGameObject.layer = 9;
                        }
                    }
                    else//If the gameobject already exists
                    {
                        //Gameobject variable to access the gameobjects
                        GameObject go;
                        go = GameObject.Find(counter.ToString());

                        // Get the Dictionary of this Object
                        Dictionary<string, float> ObjectDictionary = ConvertStringToDict(PlayerPrefs.GetString(counter.ToString()));

                        //Compare current step and the object's step value
                        if (ObjectDictionary["StepValue"] == GetCurrentStep())
                        {
                            //Enable renderer and collider components of the object
                            go.GetComponent<MeshRenderer>().enabled = true;
                            go.GetComponent<Collider>().enabled = true;
                        }
                        else
                        {
                            //Disable renderer and collider components of the object
                            go.GetComponent<MeshRenderer>().enabled = false;
                            go.GetComponent<Collider>().enabled = false;
                        }
                    }
                }
            }
            else
            {
                Debug.Log("There is no object created click create object button to create an object");
            }

            UpdateRenderedObjects();
        }
    }


    protected virtual void OnTrackingLost()
    {
        //Hide all Canvas Children
        GameObject MainCanvas = GameObject.Find("Canvas");
        foreach (Transform child in MainCanvas.transform)
        {
            child.gameObject.SetActive(false);
        }

        //Display the ImageTarget Status text
        ImageTargetStatusText.SetActive(true);

        if (mTrackableBehaviour)
        {
            var rendererComponents = mTrackableBehaviour.GetComponentsInChildren<Renderer>(true);
            var colliderComponents = mTrackableBehaviour.GetComponentsInChildren<Collider>(true);
            var canvasComponents = mTrackableBehaviour.GetComponentsInChildren<Canvas>(true);

            // Disable rendering:
            foreach (var component in rendererComponents)
                component.enabled = false;

            // Disable colliders:
            foreach (var component in colliderComponents)
                component.enabled = false;

            // Disable canvas':
            foreach (var component in canvasComponents)
                component.enabled = false;
        }
    }
#endregion // PROTECTED_METHODS
}
