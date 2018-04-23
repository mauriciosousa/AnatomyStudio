using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum DeviceType
{
    Tabletop,
    Tablet,
    Desktop
}

public class Main : MonoBehaviour {

    public DeviceType deviceType;

    private Slicer _slicer;

	// Use this for initialization
	void Start ()
    {
        _slicer = GetComponent<Slicer>();
    }
	
	// Update is called once per frame
	void Update ()
    {
        // Setup camera
        if (deviceType == DeviceType.Tabletop)
        {
            Camera.main.gameObject.GetComponent<FlyCamera>().enabled = false;
            updatePerspectiveCamera();
        }
        else if(deviceType == DeviceType.Tablet)
        {
            Camera.main.gameObject.GetComponent<FlyCamera>().enabled = false;
            updateOrtographicCamera();
        }
        else if(deviceType == DeviceType.Desktop)
        {
            Camera.main.gameObject.GetComponent<FlyCamera>().enabled = true;
            updatePerspectiveCamera();
        }
    }

    private void updateOrtographicCamera()
    {
        Camera.main.orthographic = true;
        Camera.main.nearClipPlane = _slicer.slice - 0.5f;
        Camera.main.farClipPlane = _slicer.slice + 0.5f;
        Camera.main.transform.position = new Vector3(0, 1, -10);
        Camera.main.transform.rotation = Quaternion.identity;
    }

    private void updatePerspectiveCamera()
    {
        Camera.main.orthographic = false;
        Camera.main.nearClipPlane = 0.0001f;
        Camera.main.farClipPlane = 1000.0f;
    }
}
