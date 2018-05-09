using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Slicer : MonoBehaviour {

    private Main _main;
    private SliceLoader _loader;

    public int slice = 1;

    private float _sliceDepth = 0.05f;
    public float SliceDepth
    {
        get
        {
            return _sliceDepth;
        }
    }

    private Rect _sliderArea;
    public Rect SliderArea
    {
        get
        {
            return _sliderArea;
        }
    }

    private GUIStyle _sliderAreaStyle;

    // overall slider

    private static int sliderWidth = 50;

    private GUIStyle _backgroundStyle;
    private GUIStyle _cursorStyle;
    float otherSlicesCount;
    float cursorHeight;
    float cursorPosY;
    private bool _sliding;

    // detail slider

    private static float sliceWidth = 50;
    private static float sliceHeight = 25;
    private static float currentSliceHeight = sliceHeight;

    private GUIStyle _sliceStyle;
    private GUIStyle _currentSliceStyle;
    private float _offset;
    private float _offsetSpeed;
    private float _slicesChanged;
    private bool _scrolling;
    private float _lastMouseY;
    private int _fakeSlice;

    // Use this for initialization
    void Start ()
    {
        _main = GetComponent<Main>();
        _loader = GameObject.Find("Slice").GetComponent<SliceLoader>();

        _sliderArea = new Rect(0, 0, sliderWidth + sliceWidth, Screen.height);

        _sliderAreaStyle = new GUIStyle();
        _sliderAreaStyle.normal.background = CreateColorTexture(Color.black);

        // overall slider

        _backgroundStyle = new GUIStyle();
        _backgroundStyle.normal.background = CreateColorTexture(Color.gray);

        _cursorStyle = new GUIStyle();
        _cursorStyle.normal.background = CreateColorTexture(Color.blue);

        _sliding = false;

        // detail slider

        _sliceStyle = new GUIStyle();
        _sliceStyle.alignment = TextAnchor.MiddleCenter;
        _sliceStyle.normal.textColor = Color.white;
        _sliceStyle.normal.background = CreateColorTexture(Color.gray);

        _currentSliceStyle = new GUIStyle(_sliceStyle);
        _currentSliceStyle.normal.background = CreateColorTexture(Color.blue);

        _offset = 0;
        _slicesChanged = 0;
        _scrolling = false;
        _fakeSlice = slice;
    }
	
	// Update is called once per frame
	void Update ()
    {
        UpdateCursorSize();

        if (Input.GetMouseButtonDown(0))
        {
            _scrolling = false;

            if (_sliderArea.Contains(Input.mousePosition))
            {
                if (Input.mousePosition.x < sliderWidth)
                {
                    _sliding = true;
                }
                else
                {
                    _scrolling = true;
                    _lastMouseY = Input.mousePosition.y;
                    _slicesChanged = 0;
                }
            }
        }
        else if(Input.GetMouseButton(0))
        {
            if(_sliding)
            {
                float mouseY = Screen.height - Input.mousePosition.y;

                if (mouseY < 2 + cursorHeight / 2.0f)
                    _fakeSlice = 1;
                else if(mouseY > Screen.height - 2 - cursorHeight / 2.0f)
                    _fakeSlice = _loader.SlicesCount;
                else
                    _fakeSlice = (int)Mathf.Round((mouseY - 2 - cursorHeight / 2.0f) / (Screen.height - 4 - cursorHeight) * _loader.SlicesCount);
            }
            else if(_scrolling)
            {
                float offsetDelta = -(Input.mousePosition.y - _lastMouseY);
                _offset += offsetDelta;
                _offsetSpeed = offsetDelta / Time.deltaTime;

                _lastMouseY = Input.mousePosition.y;
            }
        }
        else
        {
            if(_scrolling)
            {
                float speedLoss = 2000 * Time.deltaTime;

                if (_offsetSpeed > 0) _offsetSpeed = Mathf.Max(_offsetSpeed - speedLoss, 0);
                else _offsetSpeed = Mathf.Min(_offsetSpeed + speedLoss, 0);

                if (_offsetSpeed == 0) _scrolling = false;
                else _offset += _offsetSpeed * Time.deltaTime;
            }
            else
            {
                slice = _fakeSlice;
            }

            _sliding = false;
        }

        UpdateCursorPosition();
    }

    void OnGUI()
    {
        if (_main.deviceType == DeviceType.Tablet)
        {
            // old slider
            //slice = Mathf.RoundToInt(GUI.VerticalSlider(_sliderArea, slice, 1.0F, 11.0F));

            GUI.Label(new Rect(SliderArea.width + 1, 1, 100, 100), "Slice: " + _loader.GetSliceName(slice));

            GUI.Box(_sliderArea, "", _sliderAreaStyle);

            // overall slider

            GUI.Box(new Rect(1, 1, sliderWidth - 2, Screen.height - 2), "", _backgroundStyle);

            GUI.Box(new Rect(2, (4 + cursorHeight) / 2.0f + cursorPosY - cursorHeight / 2.0f, sliderWidth - 4, cursorHeight), "", _cursorStyle);

            // detail slider

            float halfSliceHeight = sliceHeight / 2.0f;
            float halfSliceCount = Mathf.Ceil(otherSlicesCount / 2.0f);

            // change current slice
            while (_offset > halfSliceHeight)
            {
                if (_fakeSlice == 1) break;
                _offset -= sliceHeight;
                _fakeSlice--;
                _slicesChanged++;
            }
            while (_offset < -halfSliceHeight)
            {
                if (_fakeSlice == _loader.SlicesCount) break;
                _offset += sliceHeight;
                _fakeSlice++;
                _slicesChanged++;
            }

            // lock min & max slices
            if (_fakeSlice == 1 && _offset > 0)
                _offset = 0;
            if (_fakeSlice == _loader.SlicesCount && _offset < 0)
                _offset = 0;

            float currentSlicePosY = Screen.height / 2 - currentSliceHeight / 2 + _offset;

            // draw current slice
            GUI.Box(new Rect(sliderWidth + 1, currentSlicePosY + 1, sliceWidth - 2, currentSliceHeight - 2), "" + _fakeSlice, _currentSliceStyle);
            // draw slices up
            for (int i = 1; i <= halfSliceCount; i++)
            {
                if (_fakeSlice - i < 1) break;
                if (GUI.Button(new Rect(sliderWidth + 1, currentSlicePosY - sliceHeight * i + 1, sliceWidth - 2, sliceHeight - 2), "" + (_fakeSlice - i), _sliceStyle))
                {
                    if (_slicesChanged == 0)
                    {
                        _fakeSlice = _fakeSlice - i;
                    }
                }
            }
            // draw slices down
            for (int i = 1; i <= halfSliceCount; i++)
            {
                if (_fakeSlice + i > _loader.SlicesCount) break;
                if(GUI.Button(new Rect(sliderWidth + 1, currentSlicePosY + currentSliceHeight + sliceHeight * (i - 1) + 1, sliceWidth - 2, sliceHeight - 2), "" + (_fakeSlice + i), _sliceStyle))
                {
                    if (_slicesChanged == 0)
                    {
                        _fakeSlice = _fakeSlice + i;
                    }
                }
            }
        }
    }

    private void UpdateCursorSize()
    {
        otherSlicesCount = Mathf.Ceil((Screen.height - currentSliceHeight) / sliceHeight) + 1;
        cursorHeight = Mathf.Ceil((Screen.height - 4) * ((otherSlicesCount + 1) / _loader.SlicesCount));
    }

    public void UpdateCursorPosition()
    {
        cursorPosY = (Screen.height - 4 - cursorHeight) * (_fakeSlice / (float)_loader.SlicesCount);
    }

    private Texture2D CreateColorTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }
}
