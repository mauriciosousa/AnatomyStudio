using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Slicer : MonoBehaviour
{
    private Main _main;
    private ASSNetwork _assnetwork;
    private SliceLoader _loader;

    private int _slice = 1;
    private int _sliceNr = 1;
    public int Slice
    {
        get
        {
            return _sliceNr;
        }
        set
        {
            _slice = _fakeSlice = _loader.GetSliceNumber(value);
            _sliceNr = _loader.GetRealSliceNumber(_slice);
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

    public int left = 0;

    // overall slider

    private static int sliderWidth = 50;

    private GUIStyle _backgroundStyle;
    private GUIStyle _cursorStyle;
    float otherSlicesCount;
    float cursorHeight;
    private bool _sliding;
    private float _slideSpeed;

    // detail slider

    private static float sliceWidth = 50;
    private static float sliceHeight = 40;
    private static float currentSliceHeight = sliceHeight;

    private GUIStyle _sliceStyle;
    private GUIStyle _currentSliceStyle;
    private float _offset;
    private float _offsetSpeed;
    private float _slicesChanged;
    private bool _scrolling;
    private int _fakeSlice;

    // thumbnails

    private static float thumbHeight = 200;

    private GUIStyle _nameStyle;

    // Use this for initialization
    void Start ()
    {
        _main = GetComponent<Main>();
        _assnetwork = GameObject.Find("Network").GetComponent<ASSNetwork>();
        _loader = GetComponent<SliceLoader>();

        _sliderArea = new Rect(0, 0, sliderWidth + sliceWidth, Screen.height);

        _sliderAreaStyle = new GUIStyle();
        _sliderAreaStyle.normal.background = Utils.CreateColorTexture(238, 238, 236, 242);

        // overall slider

        _backgroundStyle = new GUIStyle();
        _backgroundStyle.normal.background = Utils.CreateColorTexture(186, 189, 182);

        _cursorStyle = new GUIStyle();
        _cursorStyle.normal.background = Utils.CreateColorTexture(0, 122, 255);

        _sliding = false;

        // detail slider

        _sliceStyle = new GUIStyle();
        _sliceStyle.alignment = TextAnchor.MiddleCenter;
        _sliceStyle.normal.textColor = Utils.ColorFromRGBA(46, 52, 54);
        _sliceStyle.normal.background = Utils.CreateColorTexture(186, 189, 182);

        _currentSliceStyle = new GUIStyle(_sliceStyle);
        _currentSliceStyle.normal.textColor = Color.white;
        _currentSliceStyle.normal.background = Utils.CreateColorTexture(0, 122, 255);

        _offset = 0;
        _slicesChanged = 0;
        _scrolling = false;
        _fakeSlice = _slice;

        // thumbnails

        _nameStyle = new GUIStyle();
        _nameStyle.alignment = TextAnchor.MiddleCenter;
        _nameStyle.normal.textColor = Color.white;
        _nameStyle.normal.background = Utils.CreateColorTexture(Color.black);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            _scrolling = false;
            _sliding = false;

            if (_sliderArea.Contains(Input.mousePosition))
            {
                if (Input.mousePosition.x < sliderWidth)
                {
                    _sliding = true;
                    _slideSpeed = 0;
                }
                else
                {
                    _scrolling = true;
                    _offsetSpeed = 0;
                    _slicesChanged = 0;
                }
            }
        }
        else if (Input.GetMouseButton(0))
        {
            if (_sliding || _scrolling)
            {
                float mouseDelta = -Utils.MouseDelta.y;
                float mouseYSpeed = -Utils.MouseVelocity.y;

                if (_sliding)
                {
                    int slicesDelta = (int)Mathf.Round(mouseDelta / Screen.height * (float)_loader.SlicesCount);
                    _fakeSlice = Mathf.Clamp(_fakeSlice - slicesDelta, 1, _loader.SlicesCount);
                    _slideSpeed = mouseYSpeed;
                }
                else if (_scrolling)
                {
                    _offset += mouseDelta;
                    _offsetSpeed = mouseYSpeed;
                }
            }
        }
        else
        {
            // kinectic scrolling

            float speedLoss = 2000 * Time.deltaTime;

            if (_scrolling)
            {
                if (_offsetSpeed > 0) _offsetSpeed = Mathf.Max(_offsetSpeed - speedLoss, 0);
                else _offsetSpeed = Mathf.Min(_offsetSpeed + speedLoss, 0);

                if (_offsetSpeed == 0)
                {
                    _scrolling = false;
                    _slice = _fakeSlice;
                    _sliceNr = _loader.GetRealSliceNumber(_slice);
                    _loader.ForceSliceLoad();

                    _assnetwork.setSlice("my_id", _sliceNr);
                }
                else _offset += _offsetSpeed * Time.deltaTime;
            }
            else if (_sliding)
            {
                if (_slideSpeed > 0) _slideSpeed = Mathf.Max(_slideSpeed - speedLoss, 0);
                else _slideSpeed = Mathf.Min(_slideSpeed + speedLoss, 0);

                if (_slideSpeed == 0)
                {
                    _sliding = false;
                    _slice = _fakeSlice;
                    _sliceNr = _loader.GetRealSliceNumber(_slice);
                    _loader.ForceSliceLoad();

                    _assnetwork.setSlice("my_id", _sliceNr);
                }
                else
                {
                    int slicesDelta = (int)Mathf.Round(_slideSpeed * Time.deltaTime / Screen.height * (float)_loader.SlicesCount);
                    _fakeSlice = Mathf.Clamp(_fakeSlice - slicesDelta, 1, _loader.SlicesCount);

                    if (_fakeSlice == 1 || _fakeSlice == _loader.SlicesCount)
                        _slideSpeed = 0;
                }
            }
        }

        // change slice

        float halfSliceHeight = sliceHeight / 2.0f;

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
        if ((_fakeSlice == 1 && _offset > 0) || (_fakeSlice == _loader.SlicesCount && _offset < 0))
        {
            _offset = 0;
            _offsetSpeed = 0;
        }
    }

    void OnGUI()
    {
        //if (_main.deviceType == DeviceType.Tablet)
        {
            // background area

            _sliderArea = new Rect(left, 0, sliderWidth + sliceWidth, Screen.height);

            GUI.Box(_sliderArea, "", _sliderAreaStyle);

            // overall slider

            otherSlicesCount = Mathf.Ceil((Screen.height - currentSliceHeight) / sliceHeight) + 1;
            cursorHeight = Mathf.Ceil(Screen.height * ((otherSlicesCount + 1) / _loader.SlicesCount));

            float barHeight = Screen.height;
            float barPosition = (barHeight - cursorHeight) * (1 - (_fakeSlice - 1) / (float)(_loader.SlicesCount - 1)) - Screen.height / 2.0f + cursorHeight / 2.0f;

            GUI.Box(new Rect(left + 1, barPosition, sliderWidth - 2, barHeight), "", _backgroundStyle);
            GUI.Box(new Rect(left + 1, Screen.height / 2.0f - cursorHeight / 2.0f, sliderWidth - 2, cursorHeight), "", _cursorStyle);

            // detail slider

            float currentSlicePosY = Screen.height / 2 - currentSliceHeight / 2 + _offset;
            float halfSliceCount = Mathf.Ceil(otherSlicesCount / 2.0f);

            // draw current slice
            GUI.Box(new Rect(left + sliderWidth + 1, currentSlicePosY + 1, sliceWidth - 2, currentSliceHeight - 2), "" + _loader.GetRealSliceNumber(_fakeSlice), _currentSliceStyle);
            // draw slices up
            for (int i = 1; i <= halfSliceCount; i++)
            {
                if (_fakeSlice - i < 1) break;
                if (GUI.Button(new Rect(left + sliderWidth + 1, currentSlicePosY - sliceHeight * i + 1, sliceWidth - 2, sliceHeight - 2), "" + _loader.GetRealSliceNumber(_fakeSlice - i), _sliceStyle))
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
                if(GUI.Button(new Rect(left + sliderWidth + 1, currentSlicePosY + currentSliceHeight + sliceHeight * (i - 1) + 1, sliceWidth - 2, sliceHeight - 2), "" + _loader.GetRealSliceNumber(_fakeSlice + i), _sliceStyle))
                {
                    if (_slicesChanged == 0)
                    {
                        _fakeSlice = _fakeSlice + i;
                    }
                }
            }

            // draw thumbnail

            if(_sliding || _scrolling)
            {
                Texture2D thumbTex = _loader.GetThumbnail(_loader.GetRealSliceNumber(_fakeSlice));
                float thumbWidth = thumbHeight * thumbTex.width / thumbTex.height;
                GUI.Box(new Rect(left + _sliderArea.width, Screen.height / 2.0f - thumbHeight / 2.0f - 2, thumbWidth + 4, thumbHeight + 4), "", _sliderAreaStyle);
                GUI.DrawTexture(new Rect(left + _sliderArea.width + 2, Screen.height / 2.0f - thumbHeight / 2.0f, thumbWidth, thumbHeight), thumbTex, ScaleMode.ScaleAndCrop);
            }
        }
    }

    public bool IsSlicing()
    {
        return _sliding || _scrolling;
    }
}
