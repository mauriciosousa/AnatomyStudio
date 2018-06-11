using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseSnapshot
{
    public DateTime time;
    public Vector2 position;

    public MouseSnapshot(DateTime time, Vector2 position)
    {
        this.time = time;
        this.position = position;
    }
}

public class Utils : MonoBehaviour
{
    private static Utils _instance;

    private SliceLoader _loader;
    private Slicer _slicer;
    private StructuresList _sList;
    private Eraser _eraser;

    private float _timeInterval = 0.1f; // in seconds

    private List<MouseSnapshot> _mouseHistory;

    private Vector2 _mouseVelocity;
    public static Vector2 MouseVelocity
    {
        get { return _instance._mouseVelocity; }
    }

    private Vector2 _mouseDelta;
    public static Vector2 MouseDelta
    {
        get { return _instance._mouseDelta; }
    }

    // Use this for initialization
    void Start ()
    {
        _instance = this;

        _loader = GetComponent<SliceLoader>();
        _slicer = GetComponent<Slicer>();
        _sList = GetComponent<StructuresList>();
        _eraser = GetComponent<Eraser>();

        _mouseHistory = new List<MouseSnapshot>();
        _mouseVelocity = Vector2.zero;
        _mouseDelta = Vector2.zero;
    }
	
	// Update is called once per frame
	void Update ()
    {
        if (Input.GetMouseButtonDown(0))
        {
            _mouseHistory.Add(new MouseSnapshot(DateTime.Now, Input.mousePosition));
        }
        else if (Input.GetMouseButton(0))
        {
            _mouseHistory.Add(new MouseSnapshot(DateTime.Now, Input.mousePosition));
            _mouseVelocity = CalcMouseVelocity();
            _mouseDelta = _mouseHistory[_mouseHistory.Count - 1].position - _mouseHistory[_mouseHistory.Count - 2].position;
        }
        else if(Input.GetMouseButtonUp(0))
        {
            _mouseHistory.Clear();
            _mouseVelocity = Vector2.zero;
            _mouseDelta = Vector2.zero;
        }
    }

    private Vector2 CalcMouseVelocity()
    {
        DateTime now = DateTime.Now;

        while (_mouseHistory[0].time.AddSeconds(_timeInterval) < now)
        {
            _mouseHistory.RemoveAt(0);
        }

        float time = (float)_mouseHistory[_mouseHistory.Count - 1].time.Subtract(_mouseHistory[0].time).TotalSeconds;
        Vector2 distance = _mouseHistory[_mouseHistory.Count - 1].position - _mouseHistory[0].position;

        return distance / time;
    }

    public static Vector3 MouseToWorld(Vector2 mousePosition)
    {
        // o valor adicional na posição do plano é para garantir que as linhas (e outras interações) estão à frente da slice
        Plane plane = new Plane(Camera.main.transform.forward, _instance._loader.slice.transform.position - Camera.main.transform.forward * (_instance._loader.SliceDepth * 0.1f));
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        float hitDistance;

        if (plane.Raycast(ray, out hitDistance))
            return ray.GetPoint(hitDistance);
        else
            return Vector3.zero;
    }

    public static bool GUIContains(Vector2 mousePosition)
    {
        Vector3 mouseToGUI = new Vector2(mousePosition.x, Screen.height - mousePosition.y);

        return _instance._slicer.SliderArea.Contains(mouseToGUI) || _instance._sList.SliderArea.Contains(mouseToGUI) || _instance._eraser.ButtonArea.Contains(mouseToGUI);
    }

    public static Texture2D CreateColorTexture(int r, int g, int b, int a = 255)
    {
        return CreateColorTexture(ColorFromRGBA(r, g, b, a));
    }

    public static Texture2D CreateColorTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    public static Color ColorFromRGBA(int r, int g, int b, int a = 255)
    {
        return new Color(r / 255.0f, g / 255.0f, b / 255.0f, a / 255.0f);
    }
}
