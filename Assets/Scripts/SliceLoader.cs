using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class FileNameComparer : IComparer
{
    int IComparer.Compare(object x, object y)
    {
        return int.Parse(((FileInfo)x).Name.Split('.')[0]) - int.Parse(((FileInfo)y).Name.Split('.')[0]);
    }
}

public class SliceLoader : MonoBehaviour {

    public GameObject slice;

    private float _pixelSize;

    private float _sliceDepth = 0.05f;

    public float SliceDepth
    {
        get
        {
            return _sliceDepth;
        }
    }

    private Texture2D _texture;
    private Slicer _slicer;
    private Main _main;
    private int _currentSlice;
    private string _slicesPath;
    private string _slicesHttp;
    private FileInfo[] _filesInfo;
    private bool _refreshAspect;

    private Dictionary<int, int> _realSliceNumbers;

    // memmory stuff
    public float unloadInterval = 1; // in seconds
    private DateTime _lastUnload;

    private int _slicesCount;
    public int SlicesCount
    {
        get
        {
            return _slicesCount;
        }
    }

    // Use this for initialization
    void Start ()
    {
        _slicer = GetComponent<Slicer>();
        _main = GetComponent<Main>();
        _currentSlice = _slicer.Slice;
        _slicesPath = GetComponent<ConfigProperties>().slicesPath;
        _slicesHttp = GetComponent<ConfigProperties>().slicesHttp;
        _sliceDepth = GetComponent<ConfigProperties>().slicesThickness;
        _pixelSize = GetComponent<ConfigProperties>().slicesPixelSize;

        _texture = new Texture2D(1, 1);
        _refreshAspect = true;

        // folder info
        DirectoryInfo info = new DirectoryInfo(_slicesPath);
        _filesInfo = info.GetFiles("*.jpg");
        _slicesCount = _filesInfo.Length;

        // sort files
        IComparer comparer = new FileNameComparer();
        Array.Sort(_filesInfo, comparer);

        _realSliceNumbers = new Dictionary<int, int>();
        for(int i = 1; i <= _slicesCount; i++)
        {
            _realSliceNumbers[i] = int.Parse(_filesInfo[i - 1].Name.Split('.')[0]);
        }

        // load slice
        slice.GetComponent<Renderer>().material.mainTexture = _texture;
        DownloadSlice(_currentSlice);

        // memmory stuff
        _lastUnload = DateTime.Now;
    }
	
	// Update is called once per frame
	void Update ()
    {
        // refresh texture
        if (_currentSlice != _slicer.Slice)
        {
            _currentSlice = _slicer.Slice;

            DownloadSlice(_currentSlice);
        }

        // refresh position
        updateSlicePosition();

        // memmory stuff
        if (DateTime.Now > _lastUnload.AddSeconds(unloadInterval))
        {
            Resources.UnloadUnusedAssets();
            GC.Collect();

            _lastUnload = DateTime.Now;
        }
    }

    private void updateSlicePosition()
    {
        slice.transform.localPosition = new Vector3(slice.transform.localPosition.x, slice.transform.localScale.y / 2.0f, (_currentSlice - 1) * SliceDepth);
    }

    private void DownloadSlice(int sliceNumber)
    {
        // set thumb as slice for immediate feedback
        LoadThumbnail(sliceNumber, _texture);

        // download slice
        StartCoroutine(HTTPGetSlice(sliceNumber));
    }

    private IEnumerator HTTPGetSlice(int sliceNumber)
    {
        string url = "http://" + _slicesHttp + "/" + sliceNumber + ".jpg"; 

        using (WWW www = new WWW(url))
        {
            yield return www;

            if (string.IsNullOrEmpty(www.error))
            {
                if (sliceNumber == _currentSlice) // to ignore multiple requests
                {
                    if (!_slicer.IsSlicing()) // to ignore load if user started slicing again
                    {
                        www.LoadImageIntoTexture(_texture);

                        if (_refreshAspect)
                        {
                            slice.transform.localScale = new Vector3(_texture.width * _pixelSize, _texture.height * _pixelSize, 1.0f);
                            updateSlicePosition();
                            _main.resizeOrtographicCamera();
                            ResizeTabletopSurface();
                            ResizeSliceIndicator();

                            _refreshAspect = false;
                        }
                    }
                }
            }
            else
            {
                Debug.Log("Slice donwload error: " + www.error);
            }
        }
    }

    // estas 2 funcoes deveriam estar no Main...
    private void ResizeTabletopSurface()
    {
        if (_main.deviceType == DeviceType.Meta)
        {
            Transform surface = GameObject.Find("Surface").transform;
            surface.localScale = new Vector3(slice.transform.localScale.x, (GetRealSliceNumber(SlicesCount) - 1) * SliceDepth, 1);
            surface.localPosition = new Vector3(0, 0, surface.localScale.y / 2.0f);

            Transform tabletop = surface.parent;
            tabletop.position = new Vector3(tabletop.position.x - surface.localScale.y / 2.0f, tabletop.position.y, tabletop.position.z);

            Transform translationHandle = GameObject.Find("TranslationHandle").transform;
            MoveOtherObject[] components = translationHandle.gameObject.GetComponents<MoveOtherObject>();
            foreach (MoveOtherObject c in components)
                c.SetPosition(new Vector3(surface.position.x - surface.localScale.y / 2.0f, surface.position.y + 0.5f, surface.position.z - surface.localScale.x / 2.0f));

            Transform rotationHandle = GameObject.Find("RotationHandle").transform;
            rotationHandle.GetComponent<RotateOtherAround>().SetPosition(new Vector3(translationHandle.position.x, translationHandle.position.y, translationHandle.position.z + surface.localScale.x));
        }
    }
    private void ResizeSliceIndicator()
    {
        SliceIndicator.frameWidth = slice.transform.localScale.x;
        SliceIndicator.frameHeight = slice.transform.localScale.y;
    }

    public Texture2D GetThumbnail(int slice)
    {
        Texture2D texture = new Texture2D(1, 1);
        LoadThumbnail(slice, texture);

        return texture;
    }

    private void LoadThumbnail(int slice, Texture2D texture)
    {
        byte[] fileData = File.ReadAllBytes(_slicesPath + System.IO.Path.DirectorySeparatorChar + slice + ".jpg"); 
        texture.LoadImage(fileData);
    }

    public void ForceSliceLoad()
    {
        _currentSlice = -1;
    }

    public int GetRealSliceNumber(int slice)
    {
        return _realSliceNumbers[slice];
    }

    public int GetSliceNumber(int realNumber)
    {
        realNumber = GetNearestSlice(realNumber);

        foreach (KeyValuePair<int, int> p in _realSliceNumbers)
        {
            if (p.Value == realNumber)
                return p.Key;
        }

        return -1;
    }

    public int GetNearestSlice(int slice)
    {
        int lastSlice = -1;

        foreach(int s in _realSliceNumbers.Values)
        {
            if (slice == s)
                return s;

            if(slice < s)
            {
                if (lastSlice < 0)
                    return s;

                if (Math.Abs(lastSlice - slice) < Math.Abs(s - slice))
                    return lastSlice;
                else
                    return s;
            }

            lastSlice = s;
        }

        return lastSlice;
    }
}
