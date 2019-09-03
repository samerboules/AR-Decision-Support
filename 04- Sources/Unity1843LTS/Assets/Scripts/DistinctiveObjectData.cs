using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DistinctiveObjectData : MonoBehaviour
{
    //id for each object. Every newly created object will have an id bigger than the last object by 1
    public int id = 0;
    //Initially type is 0.
    //For Sphere objects the type value is going to be set to 1
    //For Arrow objects the type value is going to be set to 2
    public float type = 0;
}
