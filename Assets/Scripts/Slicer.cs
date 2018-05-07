using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Slicer : MonoBehaviour {

    public int slice = 1;
    private float _sliceDepth = 0.05f;

    public float SliceDepth
    {
        get
        {
            return _sliceDepth;
        }
    }

    private int _slideBorder;
    private Rect _sliderArea;

    private Main _main;

    public Rect SliderArea
    {
        get
        {
            return new Rect(0, 0, _slideBorder * 2.0f + _sliderArea.width, Screen.height);
        }
    }

    // Use this for initialization
    void Start ()
    {
        _slideBorder = 25;
        _sliderArea = new Rect(_slideBorder, _slideBorder, _slideBorder * 2.0f, Screen.height - _slideBorder * 2.0f);

        _main = GetComponent<Main>();
    }
	
	// Update is called once per frame
	void Update ()
    {
        if (Input.GetKeyDown(KeyCode.KeypadPlus))
        {
            slice++;
        }
        if (Input.GetKeyDown(KeyCode.KeypadMinus))
        {
            slice = (slice - 1 < 1 ? 1 : slice - 1);
        }
    }

    void OnGUI()
    {
        if (_main.deviceType == DeviceType.Tablet)
        {
            slice = Mathf.RoundToInt(GUI.VerticalSlider(_sliderArea, slice, 1.0F, 11.0F));

            GUI.Label(new Rect(_slideBorder * 2.0f, Screen.height - _slideBorder * 2.0f, 100.0f, _slideBorder), "Slice no.: " + slice);
        }
    }
}
