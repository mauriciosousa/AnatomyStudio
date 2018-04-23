using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Draw : MonoBehaviour {

    private LineRenderer _currentLine;
    private Slicer _slicer;

    private string _currentVolume;
    private Dictionary<string, List<Vector3>> _points;

    private Vector3 _startingPoint;
    private bool _drawing;

    private Main _main;

    // Structures buttons
    private int buttonWidth = 100;
    private int buttonHeight = 50;
    private int buttonBorder = 25;
    private Rect button1, button2, button3;

    public bool Drawing
    {
        get
        {
            return _drawing;
        }
    }

    // Use this for initialization
    void Start ()
    {
        _main = GetComponent<Main>();

        _slicer = GetComponent<Slicer>();
        _currentVolume = "none";
        _points = new Dictionary<string, List<Vector3>>();
        _drawing = false;

        button1 = new Rect(Screen.width - buttonBorder - buttonWidth, buttonBorder, buttonWidth, buttonHeight);
        button2 = new Rect(Screen.width - buttonBorder - buttonWidth, buttonBorder + (buttonBorder + buttonHeight), buttonWidth, buttonHeight);
        button3 = new Rect(Screen.width - buttonBorder - buttonWidth, buttonBorder + (buttonBorder + buttonHeight) * 2.0f, buttonWidth, buttonHeight);
    }
	
	// Update is called once per frame
	void Update ()
    {
        Vector3 mousePosition = mouseToWorld(Input.mousePosition);

        // draw
        if (_main.deviceType == DeviceType.Tablet)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Vector3 mouseToGUI = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);

                if (!(_slicer.SliderArea.Contains(mouseToGUI) ||
                    button1.Contains(mouseToGUI) || button2.Contains(mouseToGUI) || button3.Contains(mouseToGUI)))
                {
                    if (_currentVolume != "none")
                    {
                        startDrawing(mousePosition);
                    }
                }
            }
            if (Input.GetMouseButton(0))
            {
                if (Drawing)
                {
                    updateDrawing(mousePosition);
                }
            }
            if (Input.GetMouseButtonUp(0))
            {
                if (Drawing)
                {
                    endDrawing();
                }
            }
        }
    }

    void OnGUI()
    {
        if (_main.deviceType == DeviceType.Tablet)
        {
            if (GUI.Button(button1, "Structure 1"))
                _currentVolume = "1";
            else if (GUI.Button(button2, "Structure 2"))
                _currentVolume = "2";
            else if (GUI.Button(button3, "Structure 3"))
                _currentVolume = "3";

            GUI.Label(new Rect(Screen.width - 150, Screen.height - buttonBorder - 25, 150, 25), "Current Structure: " + _currentVolume);
        }
    }

    private void endDrawing()
    {
        _updateVolume();

        _drawing = false;
    }

    private void updateDrawing(Vector3 mousePosition)
    {
        if (!float.IsPositiveInfinity(_startingPoint.x))
        {
            if (mousePosition != _startingPoint)
            {
                _currentLine.SetPosition(_currentLine.positionCount++, _startingPoint);
                _points[_currentVolume].Add(_startingPoint);

                _startingPoint = Vector3.one * float.PositiveInfinity;

                _currentLine.SetPosition(_currentLine.positionCount++, mousePosition);
                _points[_currentVolume].Add(mousePosition);
            }
        }
        else if (mousePosition != _currentLine.GetPosition(_currentLine.positionCount - 1))
        {
            _currentLine.SetPosition(_currentLine.positionCount++, mousePosition);
            _points[_currentVolume].Add(mousePosition);
        }
    }

    private void startDrawing(Vector3 point)
    {
        GameObject parent = GameObject.Find(_currentVolume);
        if (parent == null) parent = new GameObject(_currentVolume);

        GameObject go = Instantiate(Resources.Load("Line", typeof(GameObject))) as GameObject;
        go.transform.parent = parent.transform;

        _currentLine = go.GetComponent<LineRenderer>();
        _currentLine.material = Resources.Load("Materials/Line" + _currentVolume, typeof(Material)) as Material;

        if (!_points.ContainsKey(_currentVolume)) _points[_currentVolume] = new List<Vector3>();

        _startingPoint = point;

        _drawing = true;
    }

    private Vector3 mouseToWorld(Vector2 mousePosition)
    {
        Plane plane = new Plane(Camera.main.transform.forward, Camera.main.transform.position + Camera.main.transform.forward * _slicer.slice);
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        float hitDistance;

        if(plane.Raycast(ray, out hitDistance))
            return ray.GetPoint(hitDistance);
        else
            return Vector3.zero;
    }

    private void _updateVolume()
    {
        if (_currentVolume == "none") return;
        if (!_points.ContainsKey(_currentVolume)) return;

        string volumeName = "Volume " + _currentVolume;

        GameObject volume = GameObject.Find(volumeName);
        if(volume == null)
        {
            volume = Instantiate(Resources.Load("Volume", typeof(GameObject))) as GameObject;
            volume.name = volumeName;
            volume.GetComponent<MeshRenderer>().material = Resources.Load("Materials/Volume" + _currentVolume, typeof(Material)) as Material;
        }
        volume.GetComponent<MeshFilter>().mesh = CreateMesh();
    }

    private Mesh CreateMesh()
    {
        Mesh m = new Mesh();
        m.name = "ScriptedMesh";
        List<int> triangles = new List<int>();

        double[][] vertices = new double[_points[_currentVolume].Count][];

        int i = 0;
        foreach (Vector3 v in _points[_currentVolume])
            vertices[i++] = new double[3] { v.x, v.y, v.z };

        try
        {
            var result = MIConvexHull.ConvexHull.Create(vertices);

            List<Vector3> vertices2 = new List<Vector3>();

            i = 0;
            foreach (MIConvexHull.DefaultVertex v in result.Points)
            {
                vertices2.Add(toVec(v));
            }
            m.vertices = vertices2.ToArray();

            foreach (var face in result.Faces)
            {
                triangles.Add(vertices2.IndexOf(toVec(face.Vertices[0])));
                triangles.Add(vertices2.IndexOf(toVec(face.Vertices[1])));
                triangles.Add(vertices2.IndexOf(toVec(face.Vertices[2])));
            }

            m.triangles = triangles.ToArray();
            m.RecalculateNormals();
        }
        catch(Exception e)
        {
            print(e);
        }

        return m;
    }

    private Vector3 toVec(MIConvexHull.DefaultVertex v)
    {
        return new Vector3((float)v.Position[0], (float)v.Position[1], (float)v.Position[2]);
    }
}
