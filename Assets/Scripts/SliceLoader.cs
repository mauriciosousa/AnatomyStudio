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
    public string SlicesHTTP
    {
        get
        {
            return _slicesHttp;
        }
    }

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
        slice.transform.localPosition = new Vector3(slice.transform.localPosition.x, slice.transform.localPosition.y, (_currentSlice - 1) * SliceDepth);

        // memmory stuff
        if(DateTime.Now > _lastUnload.AddSeconds(unloadInterval))
        {
            Resources.UnloadUnusedAssets();
            GC.Collect();

            _lastUnload = DateTime.Now;
        }
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
                            _main.resizeOrtographicCamera();

                            GetComponent<Storage>().Load();

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
}
