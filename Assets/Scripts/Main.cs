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

    public bool editorCameraControl;
    private Vector3 _perspectiveCameraPosition;
    private Quaternion _perspectiveCameraOrientation;

    private ConfigProperties _config;

	// Use this for initialization
	void Start ()
    {
        _slicer = GetComponent<Slicer>();
        _config = GetComponent<ConfigProperties>();
        deviceType = _config.device;
    }
	
	// Update is called once per frame
	void Update ()
    {
        // Setup camera
        if (deviceType == DeviceType.Tabletop)
        {
            Camera.main.orthographic = false;
            Camera.main.nearClipPlane = 0.0001f;
            Camera.main.farClipPlane = 1000.0f;

            if (!editorCameraControl)
            {
                Camera.main.transform.position = _perspectiveCameraPosition;
                Camera.main.transform.rotation = _perspectiveCameraOrientation;
            }
        }
        else if(deviceType == DeviceType.Tablet)
        {
            Camera.main.orthographic = true;
            Camera.main.nearClipPlane = _slicer.slice - 0.5f;
            Camera.main.farClipPlane = _slicer.slice + 0.5f;
            Camera.main.transform.position = new Vector3(0, 1, -10);
            Camera.main.transform.rotation = Quaternion.identity;
        }
    }
}
