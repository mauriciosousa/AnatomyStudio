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

    public int fps = 24;

    public DeviceType deviceType;

    private Slicer _slicer;
    private SliceLoader _loader;

    private ConfigProperties _config;

    public GameObject mainCamera;
    public GameObject metaCamera;

    private GameObject _slice;

    public List<GameObject> metaObjects;

    private GameObject _translationHandle;
    private GameObject _rotationHandle;

    // Use this for initialization
    void Start ()
    {
        Application.targetFrameRate = fps;

        _slicer = GetComponent<Slicer>();
        _loader = GetComponent<SliceLoader>();
        _config = GetComponent<ConfigProperties>();
        deviceType = _config.device;

        _slice = GameObject.Find("Slice");

        _translationHandle = GameObject.Find("TranslationHandle");
        _rotationHandle = GameObject.Find("RotationHandle");

        // hide / show objects
        if (deviceType == DeviceType.Meta || deviceType == DeviceType.Desktop)
        {
            foreach (GameObject go in metaObjects)
                go.SetActive(true);

            _slice.SetActive(false);
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
            resizeOrtographicCamera();
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
        if (deviceType == DeviceType.Tablet)
        {
            updateOrtographicCamera();
        }
        else if (deviceType == DeviceType.Meta)
        {
            if(Input.GetKeyDown(KeyCode.C))
            {
                _translationHandle.SetActive(!_translationHandle.activeSelf);
                _rotationHandle.SetActive(!_rotationHandle.activeSelf);
            }
        }
    }

    public void resizeOrtographicCamera()
    {
        if (deviceType == DeviceType.Tablet)
        {
            mainCamera.transform.localPosition = new Vector3(_loader.slice.transform.localPosition.x, _loader.slice.transform.localPosition.y, -_loader.SliceDepth);
            mainCamera.transform.localRotation = Quaternion.identity;

            Camera camera = mainCamera.GetComponent<Camera>();

            camera.orthographic = true;
            camera.orthographicSize = _loader.slice.transform.localScale.y / 2.0f;
        }
    }

    private void updateOrtographicCamera()
    {
        Camera camera = mainCamera.GetComponent<Camera>();

        camera.nearClipPlane = (_slicer.Slice - 1) * _loader.SliceDepth - _loader.SliceDepth * 0.5f - mainCamera.transform.localPosition.z;
        camera.farClipPlane = (_slicer.Slice - 1) * _loader.SliceDepth + _loader.SliceDepth * 0.5f - mainCamera.transform.localPosition.z;
    }

    private void initPerspectiveCamera()
    {
        Camera camera = mainCamera.GetComponent<Camera>();

        camera.orthographic = false;
        camera.nearClipPlane = 0.0001f;
        camera.farClipPlane = 1000.0f;
    }
}
