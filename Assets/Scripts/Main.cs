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

    public List<GameObject> metaObjects;

	// Use this for initialization
	void Start ()
    {
        _slicer = GetComponent<Slicer>();
        _config = GetComponent<ConfigProperties>();
        deviceType = _config.device;

        // hide / show objects
        if (deviceType == DeviceType.Meta || deviceType == DeviceType.Desktop)
        {
            foreach (GameObject go in metaObjects)
                go.SetActive(true);
        }
        else
        {
            foreach (GameObject go in metaObjects)
                go.SetActive(false);
        }

        // Setup camera
        if (deviceType == DeviceType.Tabletop)
        {
            metaCamera.SetActive(false);
            mainCamera.SetActive(true);
            mainCamera.GetComponent<FlyCamera>().enabled = false;
            mainCamera.GetComponent<TouchCamera>().enabled = false;
            initPerspectiveCamera();
        }
        else if (deviceType == DeviceType.Tablet)
        {
            metaCamera.SetActive(false);
            mainCamera.SetActive(true);
            mainCamera.GetComponent<FlyCamera>().enabled = false;
            mainCamera.GetComponent<TouchCamera>().enabled = true;

            Camera camera = mainCamera.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 0.5f;
            mainCamera.transform.localPosition = new Vector3(0, 0.25f, 0);
            mainCamera.transform.localRotation = Quaternion.identity;

            updateOrtographicCamera();
        }
        else if (deviceType == DeviceType.Desktop)
        {
            metaCamera.SetActive(false);
            mainCamera.SetActive(true);
            mainCamera.GetComponent<FlyCamera>().enabled = true;
            mainCamera.GetComponent<TouchCamera>().enabled = false;
            initPerspectiveCamera();
        }
        else if (deviceType == DeviceType.Meta)
        {
            metaCamera.SetActive(true);
            mainCamera.SetActive(false);
        }
    }
	
	// Update is called once per frame
	void Update ()
    {
        // Setup camera
        if(deviceType == DeviceType.Tablet)
        {
            updateOrtographicCamera();
        }
    }

    private void updateOrtographicCamera()
    {
        Camera camera = mainCamera.GetComponent<Camera>();

        camera.nearClipPlane = _slicer.slice * _slicer.SliceDepth - _slicer.SliceDepth * 0.5f;
        camera.farClipPlane = _slicer.slice * _slicer.SliceDepth + _slicer.SliceDepth * 0.5f;
    }

    private void initPerspectiveCamera()
    {
        Camera camera = mainCamera.GetComponent<Camera>();

        camera.orthographic = false;
        camera.nearClipPlane = 0.0001f;
        camera.farClipPlane = 1000.0f;
    }
}
