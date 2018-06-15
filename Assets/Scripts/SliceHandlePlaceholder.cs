using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SliceHandlePlaceholder : MonoBehaviour {

    private ASSNetwork _assnetwork;
    private Slicer _slicer;
    private SliceLoader _loader;
    private SliceIndicator _indicator;
    private OtherSlices _otherSlices;

    private GameObject _slicePreview;
    private Renderer _slicePreviewFront;
    private Renderer _slicePreviewBack;
    private Transform _sliceHandle;
    private GameObject _sliceNr;

    private int _snapThreshold;

    private bool _movingHandle;

	// Use this for initialization
	void Start ()
    {
        _assnetwork = GameObject.Find("Network").GetComponent<ASSNetwork>();
        _slicer = GameObject.Find("Main").GetComponent<Slicer>();
        _loader = GameObject.Find("Main").GetComponent<SliceLoader>();
        _indicator = transform.parent.GetComponent<SliceIndicator>();
        _otherSlices = GameObject.Find("Main").GetComponent<OtherSlices>();

        _slicePreview = GameObject.Find("SlicePreview");
        _slicePreviewFront = GameObject.Find("SlicePreviewFront").GetComponent<Renderer>();
        _slicePreviewBack = GameObject.Find("SlicePreviewBack").GetComponent<Renderer>();
        _slicePreviewFront.material.mainTexture = _slicePreviewBack.material.mainTexture = _loader.GetThumbnail(_slicer.Slice);
        _slicePreview.SetActive(false);

        _sliceNr = transform.parent.gameObject.GetComponent<SliceIndicator>().sliceNr;

        _sliceHandle = GameObject.Find("SliceHandle").transform;

        _snapThreshold = GameObject.Find("Main").GetComponent<ConfigProperties>().snapThreshold;

        _movingHandle = false;
    }
	
	// Update is called once per frame
	void Update ()
    {
        transform.localPosition = new Vector3(0, SliceIndicator.frameHeight, -SliceIndicator.frameWidth / 2.0f);

        if (_movingHandle)
        {
            int oldSlice = _indicator.Slice;

            float depth = transform.parent.parent.InverseTransformPoint(_sliceHandle.position).z;
            _indicator.SetDepth(depth);

            int newSlice = _indicator.Slice;

            // check other users

            int otherSlice = _otherSlices.GetNearestSliceWithUser(newSlice);

            if (otherSlice != -1 && Mathf.Abs(otherSlice - newSlice) < _snapThreshold)
            {
                newSlice = otherSlice;
                _indicator.Slice = newSlice;
            }

            // load preview

            if (oldSlice != newSlice)
            {
                _slicePreviewFront.GetComponent<Renderer>().material.mainTexture = 
                    _slicePreviewBack.GetComponent<Renderer>().material.mainTexture =
                    _loader.GetThumbnail(newSlice);
            }

            _sliceNr.GetComponent<TextMesh>().text = "" + newSlice;
        }
        else
        {
            if(_slicer.Slice != _indicator.Slice)
            {
                _indicator.Slice = _slicer.Slice;
            }

            _sliceHandle.position = transform.position + new Vector3(0, _sliceHandle.localScale.y / 2.0f, 0);
        }
    }

    public void StartMoving()
    {
        _movingHandle = true;
        _slicePreview.SetActive(true);
        _sliceNr.SetActive(true);
    }

    public void EndMoving()
    {
        _movingHandle = false;
        _slicePreview.SetActive(false);
        _sliceNr.SetActive(false);

        _slicer.Slice = _indicator.Slice;

        _assnetwork.setSlice(_indicator.Slice);
    }
}
