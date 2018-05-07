using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class SliceLoader : MonoBehaviour {

    private Texture2D _texture;
    private Slicer _slicer;
    private int _currentSlice;
    private string _slicesPath;

    // Use this for initialization
    void Start ()
    {
        _slicesPath = GameObject.Find("Main").GetComponent<ConfigProperties>().slicesPath;

        _slicer = GameObject.Find("Main").GetComponent<Slicer>();
        _currentSlice = _slicer.slice;

        _texture = new Texture2D(2, 2, TextureFormat.BGRA32, false);

        byte[] fileData = File.ReadAllBytes(_slicesPath + "\\" + _currentSlice + ".jpg");
        _texture.LoadImage(fileData);

        this.GetComponent<Renderer>().material.mainTexture = _texture;

        float sliceRatio = _texture.width / (float)_texture.height;
        if (sliceRatio > 1) this.transform.localScale = new Vector3(sliceRatio, 1.0f, 1.0f);
        else this.transform.localScale = new Vector3(1.0f, 1.0f / sliceRatio, 1.0f);
    }
	
	// Update is called once per frame
	void Update ()
    {
        // refresh texture
		if(_currentSlice != _slicer.slice)
        {
            _currentSlice = _slicer.slice;

            byte[] fileData = File.ReadAllBytes(_slicesPath + "\\" + _currentSlice + ".jpg");
            _texture.LoadImage(fileData);
        }

        // refresh position
        this.transform.localPosition = new Vector3(this.transform.position.x, this.transform.position.y, _currentSlice * _slicer.SliceDepth);
	}
}
