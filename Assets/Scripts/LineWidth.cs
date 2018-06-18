using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineWidth : MonoBehaviour {

    private Main _main;

    private LineRenderer _lineRenderer;

    public float width;

	// Use this for initialization
	void Start ()
    {
        _main = GameObject.Find("Main").GetComponent<Main>();

        _lineRenderer = GetComponent<LineRenderer>();

        SetWidth();
    }
	
	// Update is called once per frame
	void Update ()
    {
        SetWidth();
    }

    private void SetWidth()
    {
        if (_main.deviceType == DeviceType.Tablet)
        {
            width = Camera.main.orthographicSize * 0.05f;
            _lineRenderer.startWidth = width;
            _lineRenderer.endWidth = width;
        }
    }
}
