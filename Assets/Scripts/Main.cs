using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum DeviceType
{
    Tabletop,
    Tablet,
    Desktop,
    Meta
}

public class Main : MonoBehaviour {

    public DeviceType deviceType;

    private Slicer _slicer;

    private ConfigProperties _config;

    public GameObject mainCamera;
    public GameObject metaCamera;

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
            metaCamera.SetActive(false);
            mainCamera.SetActive(true);
            mainCamera.GetComponent<FlyCamera>().enabled = false;
            updatePerspectiveCamera();
        }
        else if(deviceType == DeviceType.Tablet)
        {
            metaCamera.SetActive(false);
            mainCamera.SetActive(true);
            mainCamera.GetComponent<FlyCamera>().enabled = false;
            updateOrtographicCamera();
        }
        else if(deviceType == DeviceType.Desktop)
        {
            metaCamera.SetActive(false);
            mainCamera.SetActive(true);
            mainCamera.GetComponent<FlyCamera>().enabled = true;
            updatePerspectiveCamera();
        }
        else if(deviceType == DeviceType.Meta)
        {
            metaCamera.SetActive(true);
            mainCamera.SetActive(false);
        }
    }

    private void updateOrtographicCamera()
    {
        Camera camera = mainCamera.GetComponent<Camera>();

        camera.orthographic = true;
        camera.orthographicSize = 1.0f;
        camera.nearClipPlane = _slicer.slice * 0.1f - 0.05f;
        camera.farClipPlane = _slicer.slice * 0.1f + 0.05f;
        camera.transform.localPosition = new Vector3(0, 0.25f, 0);
        camera.transform.localRotation = Quaternion.identity;
    }

    private void updatePerspectiveCamera()
    {
        Camera camera = mainCamera.GetComponent<Camera>();

        camera.orthographic = false;
        camera.nearClipPlane = 0.0001f;
        camera.farClipPlane = 1000.0f;
    }
}
