using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateOtherAround : MonoBehaviour {

    public Transform other;
    public Transform pivot;

	// Use this for initialization
	void Start ()
    {
    }
	
	// Update is called once per frame
	void Update ()
    {
        Vector3 newVector = transform.position - pivot.position;

        other.rotation = Quaternion.FromToRotation(Vector3.forward, newVector);
    }
}
