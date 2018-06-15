using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SliceIndicator : MonoBehaviour {

    public static float frameWidth = 1;
    public static float frameHeight = 1;

    public GameObject frameTop;
    public GameObject frameBottom;
    public GameObject frameFront;
    public GameObject frameBack;

    public GameObject slicePreview;

    public GameObject sliceNr;

    public float frameThickness;
    public float frameDepth;

    private int _slice;
    public int Slice
    {
        get
        {
            return _slice;
        }
        set
        {
            _slice = value;
            AdjustPosition();
        }
    }

    private SliceLoader _loader;

    private bool _inited = false;

    private void init()
    {
        if (!_inited)
        {
            _loader = GameObject.Find("Main").GetComponent<SliceLoader>();

            AdjustComponentsPosition();

            _inited = true;
        }
    }

    void Start ()
    {
        init();
    }
	
	void Update ()
    {
        AdjustComponentsPosition();
    }

    private void AdjustComponentsPosition()
    {
        frameTop.transform.localScale = new Vector3(frameDepth, frameThickness, frameWidth - frameThickness * 2.0f);
        frameBottom.transform.localScale = new Vector3(frameDepth, frameThickness, frameWidth - frameThickness * 2.0f);
        frameFront.transform.localScale = new Vector3(frameDepth, frameHeight, frameThickness);
        frameBack.transform.localScale = new Vector3(frameDepth, frameHeight, frameThickness);

        frameTop.transform.localPosition = new Vector3(0, frameHeight - frameThickness / 2.0f, 0);
        frameBottom.transform.localPosition = new Vector3(0, frameThickness / 2.0f, 0);
        frameFront.transform.localPosition = new Vector3(0, frameHeight / 2.0f, frameWidth / 2.0f - frameThickness / 2.0f);
        frameBack.transform.localPosition = new Vector3(0, frameHeight / 2.0f, -frameWidth / 2.0f + frameThickness / 2.0f);

        slicePreview.transform.localScale = new Vector3(1, frameHeight, frameWidth);
        slicePreview.transform.localPosition = new Vector3(0, frameHeight / 2.0f, 0);

        sliceNr.transform.localPosition = new Vector3(frameDepth + 0.01f, frameHeight / 2.0f, -frameWidth / 2.0f);
    }

    private void AdjustPosition()
    {
        init();

        transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, (_slice - 1) * _loader.SliceDepth);
    }

    public void SetDepth(float depth)
    {
        _slice = _loader.GetNearestSlice(Mathf.RoundToInt(depth / _loader.SliceDepth) + 1);
        AdjustPosition();
    }
}
