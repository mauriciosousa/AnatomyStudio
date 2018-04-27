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
    private ASSNetwork _assnetwork;

    // Structures buttons
    private int buttonWidth = 100;
    private int buttonHeight = 50;
    private int buttonBorder = 25;
    private Rect button1, button2, button3;

    public Transform tabletop;

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
        _assnetwork = GameObject.Find("Network").GetComponent<ASSNetwork>();

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
        Vector3 mousePosition = MouseToWorld(Input.mousePosition);

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
                        StartDrawing(mousePosition);
                    }
                }
            }
            if (Input.GetMouseButton(0))
            {
                if (Drawing)
                {
                    UpdateDrawing(mousePosition);
                }
            }
            if (Input.GetMouseButtonUp(0))
            {
                if (Drawing)
                {
                    EndDrawing();

                    // broadcast line
                    Vector3[] positions = new Vector3[_currentLine.positionCount];
                    _currentLine.GetPositions(positions);
                    _assnetwork.broadcastLine("", _slicer.slice, _currentVolume, positions);
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

    internal void AddLine(string userID, int slice, string structure, Vector3[] line)
    {
        // create line
        LineRenderer lr = CreateLine(structure);

        // add points
        foreach (Vector3 p in line)
        {
            AddPoint(p, lr, structure);
        }

        // update volume (or structure)
        UpdateVolume(structure);
    }

    private void EndDrawing()
    {
        UpdateVolume(_currentVolume);

        _drawing = false;
    }

    private void UpdateDrawing(Vector3 worldPoint)
    {
        Vector3 localPoint = tabletop.transform.worldToLocalMatrix.MultiplyPoint(worldPoint);

        if (!float.IsPositiveInfinity(_startingPoint.x))
        {
            Vector3 localStartingPoint = tabletop.transform.worldToLocalMatrix.MultiplyPoint(_startingPoint);

            if (localPoint != localStartingPoint)
            {
                AddPoint(localStartingPoint, _currentLine, _currentVolume);
                AddPoint(localPoint, _currentLine, _currentVolume);

                _startingPoint = Vector3.one * float.PositiveInfinity;
            }
        }
        else if (localPoint != _currentLine.GetPosition(_currentLine.positionCount - 1))
        {
            AddPoint(localPoint, _currentLine, _currentVolume);
        }
    }

    private void AddPoint(Vector3 localPoint, LineRenderer lineRenderer, string volumeName)
    {
        lineRenderer.SetPosition(lineRenderer.positionCount++, localPoint);
        _points[volumeName].Add(localPoint);
    }

    private void StartDrawing(Vector3 point)
    {
        _currentLine = CreateLine(_currentVolume);

        _startingPoint = point;

        _drawing = true;
    }

    private LineRenderer CreateLine(string volumeName)
    {
        GameObject parent = GameObject.Find(volumeName);
        if (parent == null)
        {
            parent = new GameObject(volumeName);
            parent.transform.parent = tabletop;
            parent.transform.localPosition = Vector3.zero;
            parent.transform.localRotation = Quaternion.identity;
        }

        GameObject go = Instantiate(Resources.Load("Line", typeof(GameObject))) as GameObject;
        go.transform.parent = parent.transform;
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;

        LineRenderer lr = go.GetComponent<LineRenderer>();
        lr.material = Resources.Load("Materials/Line" + volumeName, typeof(Material)) as Material;

        if (!_points.ContainsKey(volumeName)) _points[volumeName] = new List<Vector3>();

        return lr;
    }

    private Vector3 MouseToWorld(Vector2 mousePosition)
    {
        Plane plane = new Plane(Camera.main.transform.forward, Camera.main.transform.position + Camera.main.transform.forward * _slicer.slice * _slicer.sliceDepth);
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        float hitDistance;

        if(plane.Raycast(ray, out hitDistance))
            return ray.GetPoint(hitDistance);
        else
            return Vector3.zero;
    }

    private void UpdateVolume(string volumeName)
    {
        if (volumeName == "none") return;
        if (!_points.ContainsKey(volumeName)) return;

        string fullVolumeName = "Volume " + volumeName;

        GameObject volume = GameObject.Find(fullVolumeName);
        if(volume == null)
        {
            volume = Instantiate(Resources.Load("Volume", typeof(GameObject))) as GameObject;
            volume.name = fullVolumeName;
            volume.GetComponent<MeshRenderer>().material = Resources.Load("Materials/Volume" + volumeName, typeof(Material)) as Material;
            volume.transform.parent = tabletop;
            volume.transform.localPosition = Vector3.zero;
            volume.transform.localRotation = Quaternion.identity;
        }
        volume.GetComponent<MeshFilter>().mesh = CreateMesh(volumeName);
    }

    private Mesh CreateMesh(String volume)
    {
        Mesh m = new Mesh();
        m.name = "ScriptedMesh";
        List<int> triangles = new List<int>();

        double[][] vertices = new double[_points[volume].Count][];

        int i = 0;
        foreach (Vector3 v in _points[volume])
        {
            vertices[i++] = new double[3] { v.x, v.y, v.z };
        }

        try
        {
            var result = MIConvexHull.ConvexHull.Create(vertices);

            List<Vector3> vertices2 = new List<Vector3>();

            i = 0;
            foreach (MIConvexHull.DefaultVertex v in result.Points)
            {
                vertices2.Add(VertexToVector(v));
            }
            m.vertices = vertices2.ToArray();

            foreach (var face in result.Faces)
            {
                triangles.Add(vertices2.IndexOf(VertexToVector(face.Vertices[0])));
                triangles.Add(vertices2.IndexOf(VertexToVector(face.Vertices[1])));
                triangles.Add(vertices2.IndexOf(VertexToVector(face.Vertices[2])));
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

    private Vector3 VertexToVector(MIConvexHull.DefaultVertex v)
    {
        return new Vector3((float)v.Position[0], (float)v.Position[1], (float)v.Position[2]);
    }
}
