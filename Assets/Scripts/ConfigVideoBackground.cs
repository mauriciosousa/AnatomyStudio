using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConfigVideoBackground : MonoBehaviour {

    public Transform metaCamera;

    public float ratio;

	// Use this for initialization
	void Start () {
        //this.GetComponent<Camera>().aspect = ratio = metaCamera.aspect;
        //this.transform.localScale = new Vector3(2.2f, 2.2f, 1.0f);
    }
	
	// Update is called once per frame
	void Update () {
        this.transform.position = metaCamera.transform.position;
        this.transform.rotation = metaCamera.transform.rotation;
        this.transform.localScale = new Vector3(2.2f, 2.2f, 1.0f);
        this.GetComponent<Camera>().aspect = ratio;

    }
}
