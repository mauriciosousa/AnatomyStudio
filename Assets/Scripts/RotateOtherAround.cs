using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateOtherAround : MonoBehaviour {

    public Transform other;
    public Transform pivot;

    private Vector3 _lastVector;

    // Use this for initialization
    void Start ()
    {
        _lastVector = transform.position - pivot.position;
    }
	
	// Update is called once per frame
	void Update ()
    {
        Vector3 newVector = transform.position - pivot.position;

        Vector3 oldVectorProj = Vector3.ProjectOnPlane(_lastVector, Vector3.up);
        Vector3 newVectorProj = Vector3.ProjectOnPlane(newVector, Vector3.up);
        other.RotateAround(pivot.position, Vector3.Cross(oldVectorProj, newVectorProj), Vector3.Angle(oldVectorProj, newVectorProj));

        _lastVector = newVector;
    }

    public void SetPosition(Vector3 position)
    {
        transform.position = position;
        _lastVector = transform.position - pivot.position;
    }
}
