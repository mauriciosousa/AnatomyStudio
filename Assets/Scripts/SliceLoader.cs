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

    public float sliceDepth = 0.05f;

    private Texture2D _texture;
    private Slicer _slicer;
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
        _currentSlice = _slicer.Slice;
        _slicesPath = GetComponent<ConfigProperties>().slicesPath;
        _slicesHttp = GetComponent<ConfigProperties>().slicesHttp;

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
        DownloadSlice(_currentSlice, true);

        float sliceRatio = Screen.width / (float)Screen.height;
        if (sliceRatio > 1) slice.transform.localScale = new Vector3(sliceRatio, 1.0f, 1.0f);
        else slice.transform.localScale = new Vector3(1.0f, 1.0f / sliceRatio, 1.0f);

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
        slice.transform.localPosition = new Vector3(slice.transform.position.x, slice.transform.position.y, _currentSlice * sliceDepth);

        // memmory stuff
        if(DateTime.Now > _lastUnload.AddSeconds(unloadInterval))
        {
            Resources.UnloadUnusedAssets();
            GC.Collect();

            _lastUnload = DateTime.Now;
        }
    }

    private void DownloadSlice(int sliceNumber, bool refreshAspect = false)
    {
        // set thumb as slice for immediate feedback
        LoadThumbnail(sliceNumber, _texture);

        // download slice
        StartCoroutine(HTTPGetSlice(sliceNumber, refreshAspect));
    }

    private IEnumerator HTTPGetSlice(int sliceNumber, bool refreshAspect)
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

                        if (refreshAspect)
                        {
                            float sliceRatio = _texture.width / (float)_texture.height;
                            if (sliceRatio > 1) slice.transform.localScale = new Vector3(sliceRatio, 1.0f, 1.0f);
                            else slice.transform.localScale = new Vector3(1.0f, 1.0f / sliceRatio, 1.0f);

                            refreshAspect = false;
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
