using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveOtherObject : MonoBehaviour {

    public Transform other;

    private Vector3 _lastPos;

	// Use this for initialization
	void Start ()
    {
        _lastPos = transform.position;
    }
	
	// Update is called once per frame
	void Update ()
    {
        other.position += transform.position - _lastPos;

        _lastPos = transform.position;
    }
}
