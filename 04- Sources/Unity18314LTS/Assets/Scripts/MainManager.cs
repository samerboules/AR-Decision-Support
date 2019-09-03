using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System;
using System.Text;
using System.IO;
using UnityEngine.UI;

/*
 * *********CREATE NEW AR OBJECT*************
 * 1. Create Object and place it on image target                            CreateNewObjectButton()
 * 2. Select Object when you're close to it                                 Update()
 * 3. Object will be attached to the camera, moving with the phone          SelectObject()
 * 4. Release Object in the desired place (not attached to camera) relative to the image target      ReleaseObject()
 * 
 */


public class MainManager : MonoBehaviour
{
    #region Public Variables
    //Movement Buttons 
    public GameObject MovementButtons;
    //Array of Objects Prefabs
    public GameObject[] ARObjectsArray;
    //Declaration of the generative objects
    public GameObject generativeCube;
    public GameObject generativeSphere;
    public GameObject generativeArrow;

    //UI text to indicate the current Step
    public Text StepUIText;

    //camera to be used for the raycasting
    public Camera cam;
    #endregion

    #region Private Variables

    //holds whichever object is added from the generative objects
    GameObject currentObject;

    //Currently Selected GameObject
    private string SelectedGameObject;

    //Small shift value for each created gameobject to avoid multiple objects created on each other
    private float NewGOPositionShift = 0f;

    //Image target gameobject in the scene
    private GameObject ImageTarget;

    //Current Step (Starts from 1 always)
    public static int CurrentStep;

    public int GetCurrentStep()
    {
        return CurrentStep;
    }
    //Total number of created objects
    private int NumberOfObjects;
    //Position shift for new objects
    private float instantiationPositionShift = 0;

    //Layer that holds the created objects which will collide with the raycast
    int layerMask = 0xffffff;

    //Step for Moving on X, Y and Z axis
    private readonly float MOVING_STEP = 0.1f;
    #endregion

    #region Dictionary functions (for converting dictionary to strings and vice verca)
    public /*Dictionary<string, float>*/void FillDictionary(Dictionary<string, float> dict, string key, float info)//,float posy,float posz,float rotx,float roty,float rotz,float sclx,float scly,float sclz )
    {
        dict.Add(key, info);
    }

    string GetLine(Dictionary<string, float> d)
    {
        // Build up each line one-by-one and then trim the end
        StringBuilder builder = new StringBuilder();
        foreach (KeyValuePair<string, float> pair in d)
        {
            builder.Append(pair.Key).Append(":").Append(pair.Value).Append(',');
        }
        string result = builder.ToString();
        // Remove the final delimiter
        result = result.TrimEnd(',');
        return result;
    }

    /// ConvertDictToString converts a given string, float dictionary to a string using the GetLine method.
    public string ConvertDictToString(Dictionary<string, float> dict)
    {
        //Convert dictionary to string and save
        string objectTypeString = GetLine(dict);
        return objectTypeString;
    }

    //Takes a string returns a dictionary of type <string, float>
    public Dictionary<string, float> ConvertStringToDict(string f)
    {
        //Debug.Log("String to be converted to Dict is: " + f);
        Dictionary<string, float> d = new Dictionary<string, float>();
        string s = f; //= File.ReadAllText(f);
        // Divide all pairs (remove empty strings)
        string[] tokens = s.Split(new char[] { ':', ',' });//, StringSplitOptions.RemoveEmptyEntries);

        // Walk through each item
        for (int i = 0; i < tokens.Length; i += 2)
        {
            string name = tokens[i];
            string freq = tokens[i + 1];

            // Parse the int (this can throw)
            float count = float.Parse(freq);
            // Fill the value in the sorted dictionary
            if (d.ContainsKey(name))
            {
                d[name] += count;
            }
            else
            {
                d.Add(name, count);
            }
        }
        return d;
    }
    #endregion

    #region Swipe Right and Left Buttons
    //Function to swipe right through to generative objects
    public void SwipeRight()
    {
        if (generativeCube.activeSelf)
        {
            generativeArrow.SetActive(true);
            generativeCube.SetActive(false);
            generativeSphere.SetActive(false);
        }
        else if (generativeArrow.activeSelf)
        {
            generativeSphere.SetActive(true);
            generativeCube.SetActive(false);
            generativeArrow.SetActive(false);
        }
        else if (generativeSphere.activeSelf)
        {
            generativeCube.SetActive(true);
            generativeArrow.SetActive(false);
            generativeSphere.SetActive(false);
        }
    }

    //function to swipe left through the generative objects
    public void SwipeLeft()
    {
        if (generativeCube.activeSelf)
        {
            generativeSphere.SetActive(true);
            generativeArrow.SetActive(false);
            generativeCube.SetActive(false);
        }
        else if (generativeArrow.activeSelf)
        {
            generativeCube.SetActive(true);
            generativeArrow.SetActive(false);
            generativeSphere.SetActive(false);
        }
        else if (generativeSphere.activeSelf)
        {
            generativeArrow.SetActive(true);
            generativeCube.SetActive(false);
            generativeSphere.SetActive(false);
        }
    }
    #endregion

    #region Create New Object Button
    //Function to create object on click event
    public void CreateNewObjectButton()
    {
        //Create new GameObject
        GameObject NewGameObject = new GameObject();

        //FIND THE SELECTED TYPE TO BE CREATED
        //The index of the object to be created, 
        //0: Sphere
        //1: Arrow
        //2: Cube
        int IndexOfObjectToBeCreated = 0;
        if (generativeSphere.activeSelf)
        {
            IndexOfObjectToBeCreated = 0;
        }
        else if (generativeArrow.activeSelf)
        {
            IndexOfObjectToBeCreated = 1;
        }
        else if (generativeCube.activeSelf)
        {
            IndexOfObjectToBeCreated = 2;
        }

        //INSTANTIATE NEW OBJECT
        NewGameObject = Instantiate(ARObjectsArray[IndexOfObjectToBeCreated]);

        //Set the local position to be above the image target (0,0,0). Also add a shift in case you create multiple objects at the same place
        NewGameObject.transform.localPosition = new Vector3(NewGOPositionShift, 0f, 0f);

        //Increment the PositionShift to avoid multiple objects on the same spot when creating another object
        NewGOPositionShift = NewGOPositionShift + MOVING_STEP;

        //set every NewGameObject in layer 9 to make sure that they are the only collidable objects in the scene when raycasting 
        NewGameObject.layer = 9;

        DistinctiveObjectData distinctiveObjectData = NewGameObject.GetComponent<DistinctiveObjectData>();
        distinctiveObjectData.type = IndexOfObjectToBeCreated;
    }
    #endregion

    #region Select Object
    private void SelectableGameObject(GameObject FunctionInputGameObject)
    {
        //Set the SelectedGameObject string
        SelectedGameObject = FunctionInputGameObject.name;
        Debug.Log("Selected object was : " + SelectedGameObject);

        var Scale = FunctionInputGameObject.transform.localScale;

        //Find the ARCamera in the scene
        GameObject ARCameraGameObject = GameObject.Find("ARCamera");

        //Set the parent to be Camera
        FunctionInputGameObject.transform.parent = ARCameraGameObject.transform;

        //Preserve Scale
        FunctionInputGameObject.transform.localScale = Scale;

        //Center the gameobject in the camera frame. Only put it few cm away
        FunctionInputGameObject.transform.localPosition = new Vector3(0f, 0f, 0.2f);
    }
    #endregion

    #region Highlight Object
    //Function to change the color of the selected object, it will also set the color of 'unchosen' objects to default
    public void HighlightSelectedObject(string id)
    {
        //Find the ARCamera in the scene 
        Camera ARCamera = GameObject.Find("ARCamera").GetComponent<Camera>();

        //Find the created and released object number since the beginning
        int numberOfObjects = PlayerPrefs.GetInt("NumberOfObjects");

        //Loop to iterate through the objects
        for (int i = 1; i <= numberOfObjects; i++)
        {
            //Find the gameobject
            GameObject go = GameObject.Find(i.ToString());
            if (go != null)
            {
                //Create a reference to the renderer component of the object
                Renderer renderer = go.GetComponent<Renderer>();

                //Among all the objects, only selected object will have the color cyan
                if (go.name == id)
                {
                    //change color to cyan
                    renderer.material.color = new Color(0, 1, 1, 1);

                    //Change the parent. Set the ARCamera as the new parent. Then set its position to 0,0,1
                    //go.transform.parent = ARCamera.transform;
                    //go.transform.localPosition = new Vector3(0, 0, 1);
                }
                else
                {
                    //set the colour default for all other 'unchosen' objects
                    renderer.material.color = new Color(1, 1, 1, 1);
                }
                //Display the move buttons
                //MovementButtons.SetActive(true);
            }
        }
    }
    #endregion

    #region Release Object Button
    public void ReleaseObject()
    {
        Dictionary<string, float> NewDictionary = new Dictionary<string, float>();
        
        //Find the selected game object
        GameObject ObjectToRelease = GameObject.Find(SelectedGameObject);

        //Get its type value using the DistinctiveObjectData component. Hold the value in a float variable
        DistinctiveObjectData distinctiveObjectData = ObjectToRelease.GetComponent<DistinctiveObjectData>();
        float TypeValue = (float)distinctiveObjectData.type;

        //Fill in the type key in the dictionary with the type value
        FillDictionary(NewDictionary, "Type", TypeValue);

        NumberOfObjects++;
        //Assign the object name to the ID value, then fill the dictionary 
        string NewObjectID = NumberOfObjects.ToString();
        ObjectToRelease.name = NewObjectID;
        FillDictionary(NewDictionary, "ID", float.Parse(NewObjectID));

        //Assıgn the step value ın which the object ıs released
        FillDictionary(NewDictionary, "StepValue", CurrentStep);
      
        //After release, the object should go back to the default color white
        Renderer goRenderer = ObjectToRelease.GetComponent<MeshRenderer>();
        goRenderer.material.color = new Color(1, 1, 1, 1);

        //Assign the parent to be external (No Parent)
        ObjectToRelease.transform.parent = ImageTarget.transform.parent;

        //Fill the position data to the dictionary
        FillDictionary(NewDictionary, "PosX", ObjectToRelease.transform.localPosition.x);
        FillDictionary(NewDictionary, "PosY", ObjectToRelease.transform.localPosition.y);
        FillDictionary(NewDictionary, "PosZ", ObjectToRelease.transform.localPosition.z);

        //Save the number of objects that were released
        PlayerPrefs.SetInt("NumberOfObjects", NumberOfObjects);

        //Convert dictionary to string then assign to a variable
        string StringOfObjectData = ConvertDictToString(NewDictionary);
        Debug.Log(StringOfObjectData);

        //Save the stringified dictionary
        PlayerPrefs.SetString(NewObjectID, StringOfObjectData);
    }
    #endregion

    #region Cancel Creation or Selection Button
    //Cancel the new object creation without releasing it.
    //Cancel the object selection, it will go back to the location where it was selected.
    public void CancelCreationOrSelection()
    {
        //Find the ARCamera in the scene
        Camera ARCamera = GameObject.Find("ARCamera").GetComponent<Camera>();
       
        //Loop to iterate through all the objects created since the beginning starting from 1
        for (int counter = 1; counter <= NumberOfObjects; counter++)
        {
            //Find the game object with the name counter
            GameObject go = GameObject.Find(counter.ToString());
            
            //If the game object exists with the name counter
            if (go != null)
            {
                //If ARCamera is the parent of the of the object
                if (go.transform.parent == ARCamera.transform)
                {
                    //If the object was created and saved earlier, it means the object acquired is selected by the user
                    if (PlayerPrefs.HasKey(counter.ToString()))
                    {
                        //Get the renderer component of the selected object

                        Renderer goRenderer = go.GetComponent<MeshRenderer>();
                        //Create a dictionary to retrieve the values of the object from its string
                        Dictionary<string, float> dict = new Dictionary<string, float>();
                        dict = ConvertStringToDict(PlayerPrefs.GetString(counter.ToString()));
                        
                        //Selection is going to be canceled so the color of the object should go back to the default
                        goRenderer.material.color = new Color(1, 1, 1, 1);
                        
                        //Set the image target parent and place it where it used to be 
                        go.transform.parent = ImageTarget.transform;
                        go.transform.localPosition = new Vector3(dict["PosX"], dict["PosY"], dict["PosZ"]);
                    }
                    //If a string does not exist, this object was never saved
                    else
                    {
                        //Delete the object 
                        Destroy(go);
                    }
                }
                else
                {
                    //No object was created or selected. Thus nothing to reverse
                }
            }
        }
    }
    #endregion

    #region Next Step and Previous Step and Highlight Buttons 
    //This function renders objects based on the step number
    public void UpdateRenderedObjects()
    {
        //Reset the position shift of newly created objects with each step
        NewGOPositionShift = 0;

        //for loop will iterate for all objects starting from the first until it finishes with the last object created.
        for (int counter = 1; counter <= PlayerPrefs.GetInt("NumberOfObjects"); counter++)
        {
            if (PlayerPrefs.HasKey(counter.ToString()))
            {
                //Get the Dictionary of this Object
                Dictionary<string, float> ObjectDictionary = ConvertStringToDict(PlayerPrefs.GetString(counter.ToString()));

                //Gameobject variable to access the gameobjects
                GameObject go;
                go = GameObject.Find(counter.ToString());

                if (go != null)
                {
                    //Compare current step and the object's step value
                    if (ObjectDictionary["StepValue"] == CurrentStep)
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
    }

    //Function to be called when the next button is clicked to change the step
    public void OnClickNext()
    {   
        //Increment the current step 
        CurrentStep++;
        //Update the step text
        StepUIText.text = "Step " + CurrentStep.ToString();

        //Update Rendered based on current step
        UpdateRenderedObjects();
    }

    //Function to be called when the previous button is clicked to change the step
    public void OnClickPrevious()
    {
        //Decrement the current step 
        CurrentStep--;
        if (CurrentStep < 1) CurrentStep = 1;

        //Update the step text
        StepUIText.text = "Step " + CurrentStep.ToString();

        //Update Rendered based on current step
        UpdateRenderedObjects();
    }
    #endregion

    #region Deselect Object
    //Function to disable all move functions for the objects when next or previous buttons are clicked.
    //When one of the next or previous buttons are clicked, value of the SelectedGameObject,
    //should be set to something that a gameobject name can never be.
    public void DeselectObject()
    {
        //Set the name 
        SelectedGameObject = "ImpossibleName";

        //Find the created and released object number since the beginning
        int numberOfObjects = PlayerPrefs.GetInt("NumberOfObjects");

        //Loop to iterate through the objects
        for (int i = 1; i <= numberOfObjects; i++)
        {
            //Check if the key exists or this is the first created object and its not saved yet
            if (PlayerPrefs.HasKey(i.ToString()) || NumberOfObjects == 1)
            {
                //Find the gameobject
                GameObject go = GameObject.Find(i.ToString());
                if(go!=null)
                {
                    //Create a reference to the renderer component of the object
                    Renderer renderer = go.GetComponent<Renderer>();

                    //set the colour default for all other 'unchosen' objects
                    renderer.material.color = new Color(1, 1, 1, 1);
                }                
            }
        }
            //Hide the move buttons
            //MovementButtons.SetActive(false);
    }
    #endregion

    #region Delete Object
    public void DeleteObject()
    {
        //Find the selected game object
        GameObject go = GameObject.Find(SelectedGameObject);

        //If the game object with such name exists
        if (go!= null)
        {
            //Delete the object
            Destroy(go);

            //Delete its saved string
            PlayerPrefs.DeleteKey(SelectedGameObject);
        }
        DeselectObject();
    }
    #endregion

    #region Reset Player Prefs Button
    public void ResetPlayerPrefs()
    {
        PlayerPrefs.DeleteAll();
    }
    #endregion

    #region Moving on X,Y,Z axis buttons
    //move the last selected object +x direction with the click on button Move_Object_on_X_Positive
    public void Move_Object_on_X_Positive()
    {
        //get the last touched object
        currentObject = GameObject.Find(SelectedGameObject);
        Dictionary<string, float> dict = new Dictionary<string, float>();

        //get the latest version of the string value that holds the information on the object
        string a = PlayerPrefs.GetString(SelectedGameObject);

        //Convert the string back to dictionary for manipulation
        dict = ConvertStringToDict(a);

        //change the location on +x
        currentObject.transform.localPosition = new Vector3(currentObject.transform.localPosition.x + MOVING_STEP, currentObject.transform.localPosition.y, currentObject.transform.localPosition.z);

        //Update the dictionary
        dict["PosX"] = currentObject.transform.localPosition.x;
        //FillDictionary(dict,"PosZ", currentObject.transform.localPosition.z);

        //Convert the dictionary back to the string to set the playerprefs
        a = ConvertDictToString(dict);
        Debug.Log(a);
        //save the new string for the object
        PlayerPrefs.SetString(SelectedGameObject, a);
        Debug.Log("New position data stored.");
    }
    //move the last selected object -x direction with the click on button Move_Object_on_X_Negative
    public void Move_Object_on_X_Negative()
    {
        //get the last touched object
        currentObject = GameObject.Find(SelectedGameObject);


        Dictionary<string, float> dict = new Dictionary<string, float>();

        //get the latest version of the string value that holds the information on the object
        string b = PlayerPrefs.GetString(SelectedGameObject);

        //Convert the string back to dictionary for manipulation
        dict = ConvertStringToDict(b);

        //change the location on -x
        currentObject.transform.localPosition = new Vector3(currentObject.transform.localPosition.x - MOVING_STEP, currentObject.transform.localPosition.y, currentObject.transform.localPosition.z);

        //Update the dictionary
        dict["PosX"] = currentObject.transform.localPosition.x;
        //FillDictionary(dict,"PosZ", currentObject.transform.localPosition.z);

        //Convert the dictionary back to the string to set the playerprefs
        b = ConvertDictToString(dict);
        Debug.Log(b);
        //save the new string for the object
        PlayerPrefs.SetString(SelectedGameObject, b);
        Debug.Log("New position data stored.");
    }
    //move the last selected object +y direction with the click on button Move_Object_on_Y_Positive
    public void Move_Object_on_Y_Positive()
    {
        //get the last touched object
        currentObject = GameObject.Find(SelectedGameObject);

        Dictionary<string, float> dict = new Dictionary<string, float>();

        //get the latest version of the string value that holds the information on the object
        string c = PlayerPrefs.GetString(SelectedGameObject);

        //Convert the string back to dictionary for manipulation
        dict = ConvertStringToDict(c);

        //change the location on +y
        currentObject.transform.localPosition = new Vector3(currentObject.transform.localPosition.x, currentObject.transform.localPosition.y + MOVING_STEP, currentObject.transform.localPosition.z);

        //Update the dictionary
        dict["PosY"] = currentObject.transform.localPosition.y;
        //FillDictionary(dict,"PosZ", currentObject.transform.localPosition.z);

        //Convert the dictionary back to the string to set the playerprefs
        c = ConvertDictToString(dict);
        Debug.Log(c);
        //save the new string for the object
        PlayerPrefs.SetString(SelectedGameObject, c);
        Debug.Log("New position data stored.");
    }
    //move the last selected object -y direction with the click on button Move_Object_on_Y_Negative
    public void Move_Object_on_Y_Negative()
    {
        //get the last touched object
        currentObject = GameObject.Find(SelectedGameObject);


        Dictionary<string, float> dict = new Dictionary<string, float>();

        //get the latest version of the string value that holds the information on the object
        string d = PlayerPrefs.GetString(SelectedGameObject);

        //Convert the string back to dictionary for manipulation
        dict = ConvertStringToDict(d);

        //change the location on -y
        currentObject.transform.localPosition = new Vector3(currentObject.transform.localPosition.x, currentObject.transform.localPosition.y - MOVING_STEP, currentObject.transform.localPosition.z);

        //Update the dictionary
        dict["PosY"] = currentObject.transform.localPosition.y;
        //FillDictionary(dict,"PosZ", currentObject.transform.localPosition.z);

        //Convert the dictionary back to the string to set the playerprefs
        d = ConvertDictToString(dict);
        Debug.Log(d);
        //save the new string for the object
        PlayerPrefs.SetString(SelectedGameObject, d);
        Debug.Log("New position data stored.");
    }
    //move the last selected object +z direction with the click on button Move_Object_on_Z_Positive
    public void Move_Object_on_Z_Positive()
    {
        //get the last touched object
        currentObject = GameObject.Find(SelectedGameObject);


        Dictionary<string, float> dict = new Dictionary<string, float>();

        //get the latest version of the string value that holds the information on the object
        string e = PlayerPrefs.GetString(SelectedGameObject);

        //Convert the string back to dictionary for manipulation
        dict = ConvertStringToDict(e);

        //change the location on -z
        currentObject.transform.localPosition = new Vector3(currentObject.transform.localPosition.x, currentObject.transform.localPosition.y, currentObject.transform.localPosition.z + MOVING_STEP);

        //Update the dictionary
        dict["PosZ"] = currentObject.transform.localPosition.z;
        //FillDictionary(dict,"PosZ", currentObject.transform.localPosition.z);

        //Convert the dictionary back to the string to set the playerprefs
        e = ConvertDictToString(dict);
        Debug.Log(e);
        //save the new string for the object
        PlayerPrefs.SetString(SelectedGameObject, e);
        Debug.Log("New position data stored.");
    }
    //move the last selected object -z direction with the click on button Move_Object_on_Z_Negative
    public void Move_Object_on_Z_Negative()
    {
        //get the last touched object
        currentObject = GameObject.Find(SelectedGameObject);


        Dictionary<string, float> dict = new Dictionary<string, float>();

        //get the latest version of the string value that holds the information on the object
        string f = PlayerPrefs.GetString(SelectedGameObject);

        //Convert the string back to dictionary for manipulation
        dict = ConvertStringToDict(f);

        //change the location on -z
        currentObject.transform.localPosition = new Vector3(currentObject.transform.localPosition.x, currentObject.transform.localPosition.y, currentObject.transform.localPosition.z - MOVING_STEP);

        //Update the dictionary
        dict["PosZ"] = currentObject.transform.localPosition.z;
        //FillDictionary(dict,"PosZ", currentObject.transform.localPosition.z);

        //Convert the dictionary back to the string to set the playerprefs
        f = ConvertDictToString(dict);
        Debug.Log(f);
        //save the new string for the object
        PlayerPrefs.SetString(SelectedGameObject, f);
        Debug.Log("New position data stored.");
    }
    #endregion

    #region Unity Functions
    void Start()
    {
        MovementButtons.SetActive(true);

        CurrentStep = 1;
        //Get the Main Camera GameObject
        //cam = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();

        //Find the ImageTarget gameobject
        ImageTarget = GameObject.Find("ImageTarget");
        DefaultTrackableEventHandler DefaultTrackableEventHandler_ = ImageTarget.GetComponent<DefaultTrackableEventHandler>();
        

        //Restore the total number of objects
        NumberOfObjects = PlayerPrefs.GetInt("NumberOfObjects");
        
        if(NumberOfObjects>0)
        {
            //Update rendering based on current step
            UpdateRenderedObjects();
        }
    }

    void Update()
    {
        //HANDLING OF SELECTING OBJECTS
        if (Input.GetMouseButtonDown(0))
        {
            //create ray and RaycastHit objects
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            //check if the ray collides with the objects 
            if (Physics.Raycast(ray, out hit, layerMask))
            {
                if(hit.transform.gameObject.layer == 9 
                    && hit.transform.gameObject.GetComponent<Button>()==null
                    && hit.distance <= 0.20f)
                {
                    SelectableGameObject(hit.transform.gameObject);

                    //Change the color of the selected object while setting the color for all other objects to default
                    HighlightSelectedObject(SelectedGameObject);
                }
                else //Raycast doesn't collide with any objects
                {
                    DeselectObject();
                }
            }
        }
    }
    #endregion
}